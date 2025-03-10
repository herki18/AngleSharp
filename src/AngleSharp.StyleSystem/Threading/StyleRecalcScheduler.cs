namespace AngleSharp.StyleSystem.Threading;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Models;
using Interfaces;

/// <summary>
/// Implements the scheduling and coordination of style recalculation work.
/// </summary>
/// <remarks>
/// Note: Currently all work is performed on the main thread, but the architecture
/// is designed to support multi-threading in the future through WorkerThreadStylePool integration.
/// </remarks>
public class StyleRecalcScheduler : IStyleRecalcScheduler, IDisposable
{
    private readonly StyleEngine _styleEngine;
    private readonly IBrowsingContext _context;
    private readonly ConcurrentDictionary<IElement, StyleRecalcWork> _pendingWork;
    private readonly SemaphoreSlim _workSemaphore;
    private readonly ViewportDetector _viewportDetector;
    private readonly IMainThreadStyleWork _mainThreadWork;
    private readonly IWorkerThreadStylePool? _workerThreadPool;
    private readonly object _processingLock = new object();
    private bool _isProcessing;
    private CancellationTokenSource? _processingCts;

    // Configuration values
    private readonly int _batchSize;
    private readonly int _throttleIntervalMs;
    private readonly int _maxBatches;
    private Timer? _throttleTimer;

    /// <summary>
    /// Creates a new instance of the StyleRecalcScheduler.
    /// </summary>
    /// <param name="styleEngine">The style engine to use for recalculation.</param>
    /// <param name="context">The browsing context.</param>
    /// <param name="mainThreadWork">The main thread work handler.</param>
    /// <param name="workerThreadPool">The worker thread pool handler (optional).</param>
    /// <param name="batchSize">The maximum number of elements to process in a single batch.</param>
    /// <param name="throttleIntervalMs">The minimum time between processing batches in milliseconds.</param>
    /// <param name="maxBatches">The maximum number of batches to process in a single run.</param>
    public StyleRecalcScheduler(
        StyleEngine styleEngine,
        IBrowsingContext context,
        IMainThreadStyleWork mainThreadWork,
        IWorkerThreadStylePool? workerThreadPool = null,
        int batchSize = 100,
        int throttleIntervalMs = 16, // ~60fps
        int maxBatches = 10)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mainThreadWork = mainThreadWork ?? throw new ArgumentNullException(nameof(mainThreadWork));
        _workerThreadPool = workerThreadPool;
        _pendingWork = new ConcurrentDictionary<IElement, StyleRecalcWork>();
        _workSemaphore = new SemaphoreSlim(1, 1);
        _viewportDetector = new ViewportDetector(_styleEngine.RenderDevice);
        _batchSize = batchSize;
        _throttleIntervalMs = throttleIntervalMs;
        _maxBatches = maxBatches;
    }

    /// <inheritdoc/>
    public void ScheduleElementRecalc(IElement element, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var work = new StyleRecalcWork(element, priority, StyleWorkType.Element);
        _pendingWork.AddOrUpdate(element, work, (_, existing) =>
            new StyleRecalcWork(element, (RecalcPriority)Math.Max((byte)existing.Priority, (byte)priority), existing.WorkType));

        ScheduleProcessing();
    }

    /// <inheritdoc/>
    public void ScheduleSubtreeRecalc(IElement rootElement, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (rootElement == null)
            throw new ArgumentNullException(nameof(rootElement));

        var work = new StyleRecalcWork(rootElement, priority, StyleWorkType.Subtree);
        _pendingWork.AddOrUpdate(rootElement, work, (_, existing) =>
            new StyleRecalcWork(rootElement, (RecalcPriority)Math.Max((byte)existing.Priority, (byte)priority), StyleWorkType.Subtree));

        ScheduleProcessing();
    }

    /// <inheritdoc/>
    public void ProcessImmediately()
    {
        CancelPendingWork();

        lock (_processingLock)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            _processingCts = new CancellationTokenSource();
        }

        try
        {
            ProcessBatches(_processingCts.Token);
        }
        finally
        {
            lock (_processingLock)
            {
                _isProcessing = false;
                _processingCts?.Dispose();
                _processingCts = null;
            }
        }
    }

    /// <inheritdoc/>
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await _workSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Create a linked token to handle both external cancellation and internal cancellation
            CancellationTokenSource? linkedCts = null;

            lock (_processingLock)
            {
                if (_isProcessing)
                    return;

                _isProcessing = true;
                _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCts = _processingCts;
            }

            try
            {
                await Task.Run(() => ProcessBatches(linkedCts.Token), linkedCts.Token).ConfigureAwait(false);
            }
            finally
            {
                lock (_processingLock)
                {
                    _isProcessing = false;
                    linkedCts?.Dispose();
                    _processingCts = null;
                }
            }
        }
        finally
        {
            _workSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public void CancelPendingWork()
    {
        lock (_processingLock)
        {
            _processingCts?.Cancel();
        }

        _throttleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc/>
    public bool HasPendingWork => _pendingWork.Count > 0 || _mainThreadWork.HasPendingWork ||
        (_workerThreadPool?.HasPendingWork ?? false);

    /// <summary>
    /// Processes work items in batches.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop processing.</param>
    private void ProcessBatches(CancellationToken cancellationToken)
    {
        if (!HasPendingWork)
            return;

        // Get work items sorted by priority
        var workItems = GetSortedWorkItems();

        // Process in batches
        int batchCount = 0;
        int processedCount = 0;

        while (workItems.Count > 0 && batchCount < _maxBatches && !cancellationToken.IsCancellationRequested)
        {
            var batch = ExtractBatch(workItems, _batchSize);
            ProcessBatch(batch, cancellationToken);

            batchCount++;
            processedCount += batch.Count;

            // If we have more items but reached the max batch count, schedule another processing run
            if (workItems.Count > 0 && batchCount >= _maxBatches)
            {
                ScheduleProcessing();
                break;
            }
        }

        // Process any remaining work
        if (!cancellationToken.IsCancellationRequested)
        {
            _mainThreadWork.ProcessSync();

            if (_workerThreadPool != null && _workerThreadPool.HasPendingWork)
            {
                // This would be awaited in a true async implementation
                _workerThreadPool.StartProcessingAsync(cancellationToken).Wait(cancellationToken);
                _workerThreadPool.SynchronizeResultsAsync(cancellationToken).Wait(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Gets work items sorted by priority.
    /// </summary>
    /// <returns>A list of work items sorted by priority.</returns>
    private List<StyleRecalcWork> GetSortedWorkItems()
    {
        // Update priorities based on visibility
        UpdateWorkPriorities();

        // Sort work items by priority (highest first)
        var workItems = _pendingWork.Values
            .OrderByDescending(w => w.Priority)
            .ToList();

        return workItems;
    }

    /// <summary>
    /// Extracts a batch of items from the work list.
    /// </summary>
    /// <param name="workItems">The list of work items.</param>
    /// <param name="batchSize">The size of the batch to extract.</param>
    /// <returns>A batch of work items.</returns>
    private List<StyleRecalcWork> ExtractBatch(List<StyleRecalcWork> workItems, int batchSize)
    {
        int count = Math.Min(batchSize, workItems.Count);
        var batch = workItems.Take(count).ToList();
        workItems.RemoveRange(0, count);
        return batch;
    }

    /// <summary>
    /// Processes a batch of work items.
    /// </summary>
    /// <param name="batch">The batch of work items to process.</param>
    /// <param name="cancellationToken">A token that can be used to cancel processing.</param>
    private void ProcessBatch(List<StyleRecalcWork> batch, CancellationToken cancellationToken)
    {
        // Process critical items first
        var criticalItems = batch.Where(w => w.Priority == RecalcPriority.Critical).ToList();
        var normalItems = batch.Where(w => w.Priority != RecalcPriority.Critical).ToList();

        // Process critical items on the main thread
        foreach (var work in criticalItems)
        {
            _pendingWork.TryRemove(work.Element, out _);
            _mainThreadWork.EnqueueElement(work.Element, work.Priority);
        }

        // Process normal items
        if (_workerThreadPool != null)
        {
            // Process non-critical items on worker threads if available
            var elements = normalItems.Select(w => w.Element).ToList();
            foreach (var work in normalItems)
            {
                _pendingWork.TryRemove(work.Element, out _);
            }

            _workerThreadPool.EnqueueElements(elements, RecalcPriority.Normal);
        }
        else
        {
            // No worker threads, so process on main thread
            foreach (var work in normalItems)
            {
                _pendingWork.TryRemove(work.Element, out _);
                _mainThreadWork.EnqueueElement(work.Element, work.Priority);
            }
        }
    }

    /// <summary>
    /// Updates priorities of work items based on their visibility.
    /// </summary>
    private void UpdateWorkPriorities()
    {
        foreach (var kvp in _pendingWork)
        {
            var element = kvp.Key;
            var work = kvp.Value;

            // Skip if already critical
            if (work.Priority == RecalcPriority.Critical)
                continue;

            // Check if element is in viewport
            RecalcPriority newPriority = work.Priority;

            if (_viewportDetector.IsInViewport(element))
            {
                newPriority = RecalcPriority.Critical;
            }
            else if (_viewportDetector.IsNearViewport(element))
            {
                newPriority = (RecalcPriority)Math.Max((byte)newPriority, (byte)RecalcPriority.High);
            }

            // Update if priority changed
            if (newPriority != work.Priority)
            {
                _pendingWork.TryUpdate(element,
                    new StyleRecalcWork(element, newPriority, work.WorkType),
                    work);
            }
        }
    }

    /// <summary>
    /// Schedules processing with throttling.
    /// </summary>
    private void ScheduleProcessing()
    {
        if (_throttleTimer == null)
        {
            _throttleTimer = new Timer(
                _ => Task.Run(ProcessImmediately),
                null,
                _throttleIntervalMs,
                Timeout.Infinite);
        }
        else
        {
            _throttleTimer.Change(_throttleIntervalMs, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        CancelPendingWork();
        _throttleTimer?.Dispose();
        _workSemaphore.Dispose();
        _processingCts?.Dispose();
        (_workerThreadPool as IDisposable)?.Dispose();
    }
}