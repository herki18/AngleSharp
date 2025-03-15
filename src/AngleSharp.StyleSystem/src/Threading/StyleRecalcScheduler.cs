using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Events;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Tasks;

namespace AngleSharp.StyleSystem.Threading;

/// <summary>
/// Schedules style recalculation work using System.Threading.Channels for improved performance.
/// </summary>
public class StyleRecalcScheduler : IStyleRecalcScheduler, IDisposable
{
    private readonly Channel<StyleRecalcWork> _workChannel;
    private readonly PriorityMap<StyleRecalcWork> _priorityMap;
    private readonly IBrowsingContext _context;
    private readonly IStyleEngine _styleEngine;
    private readonly IStyleTaskScheduler _taskScheduler;
    private readonly IMainThreadStyleWork? _mainThreadWork;
    private readonly IWorkerThreadStylePool? _workerThreadPool;
    private readonly ViewportDetector _viewportDetector;
    private readonly ISubscriptionToken[] _subscriptionTokens;

    private readonly CancellationTokenSource _processingCts = new();
    private readonly Task _processingTask;
    private int _throttleIntervalMs = 16;
    private int _maxBatchSize = 100;
    private bool _useWorkerThreads = true;
    private Action? _completionCallback;
    private bool _isDisposed;

    /// <summary>
    /// Creates a new StyleRecalcScheduler instance.
    /// </summary>
    public StyleRecalcScheduler(
        IStyleEngine styleEngine,
        IBrowsingContext context,
        IStyleTaskScheduler taskScheduler,
        IEventAggregator eventAggregator,
        IMainThreadStyleWork? mainThreadWork = null,
        IWorkerThreadStylePool? workerThreadPool = null,
        int throttleIntervalMs = 16,
        int maxBatchSize = 100)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        _mainThreadWork = mainThreadWork;
        _workerThreadPool = workerThreadPool;
        _throttleIntervalMs = throttleIntervalMs;
        _maxBatchSize = maxBatchSize;
        _viewportDetector = new ViewportDetector(_styleEngine.RenderDevice);
        _priorityMap = new PriorityMap<StyleRecalcWork>();

        // Create a bounded channel for work items
        var options = new BoundedChannelOptions(maxBatchSize * 10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _workChannel = Channel.CreateBounded<StyleRecalcWork>(options);

        // Start the background processing task
        _processingTask = ProcessWorkItemsAsync(_processingCts.Token);

        // Subscribe to events
        _subscriptionTokens = new ISubscriptionToken[]
        {
            eventAggregator.Subscribe<ElementInvalidatedEvent>(OnElementInvalidated),
            eventAggregator.Subscribe<PropertiesInvalidatedEvent>(OnPropertiesInvalidated),
            eventAggregator.Subscribe<SubtreeInvalidatedEvent>(OnSubtreeInvalidated),
            eventAggregator.Subscribe<DeviceDependentElementsInvalidatedEvent>(OnDeviceDependentElementsInvalidated)
        };
    }

    #region IStyleRecalcScheduler Implementation

    /// <inheritdoc />
    public int ThrottleIntervalMs
    {
        get => _throttleIntervalMs;
        set => _throttleIntervalMs = Math.Max(1, value);
    }

    /// <inheritdoc />
    public int MaxBatchSize
    {
        get => _maxBatchSize;
        set => _maxBatchSize = Math.Max(1, value);
    }

    /// <inheritdoc />
    public bool UseWorkerThreads
    {
        get => _useWorkerThreads;
        set => _useWorkerThreads = value;
    }

    /// <inheritdoc />
    public void ScheduleElementRecalc(IElement element, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var work = new StyleRecalcWork(element, priority, StyleWorkType.Element);

        _priorityMap.AddOrUpdate(element, work, (existing) => {
            // Keep the highest priority and prefer subtree work over element work
            return new StyleRecalcWork(
                element,
                (RecalcPriority)Math.Max((byte)existing.Priority, (byte)priority),
                existing.WorkType == StyleWorkType.Subtree ? StyleWorkType.Subtree : StyleWorkType.Element
            );
        });

        // Try to write to the channel without blocking
        if (!_workChannel.Writer.TryWrite(work))
        {
            // Only log if we couldn't write - the work will still be in the priorityMap
            // and will be picked up by the periodic processing
            System.Diagnostics.Debug.WriteLine("Channel is full - work queued for later processing");
        }
    }

    /// <inheritdoc />
    public void ScheduleSubtreeRecalc(IElement rootElement, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (rootElement == null)
            throw new ArgumentNullException(nameof(rootElement));

        var work = new StyleRecalcWork(rootElement, priority, StyleWorkType.Subtree);

        _priorityMap.AddOrUpdate(rootElement, work, (existing) => {
            // Always upgrade to subtree and use highest priority
            return new StyleRecalcWork(
                rootElement,
                (RecalcPriority)Math.Max((byte)existing.Priority, (byte)priority),
                StyleWorkType.Subtree
            );
        });

        // Try to write to the channel without blocking
        if (!_workChannel.Writer.TryWrite(work))
        {
            // Work will be processed on next cycle through priorityMap
            System.Diagnostics.Debug.WriteLine("Channel is full - work queued for later processing");
        }
    }

    /// <inheritdoc />
    public void ProcessImmediately()
    {
        // Process any pending work in the priority map
        ProcessPendingWorkSync();

        // Drain the channel
        while (_workChannel.Reader.TryRead(out var work))
        {
            ProcessWorkItem(work);
        }

        // Process any worker thread results
        if (_workerThreadPool != null && _workerThreadPool.HasPendingWork)
        {
            _workerThreadPool.SynchronizeResultsAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        _completionCallback?.Invoke();
    }

    /// <inheritdoc />
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // First process items from the priority map
        await Task.Run(() => ProcessPendingWorkSync(), linkedTokenSource.Token);

        // Process as many items from the channel as possible
        var count = 0;
        while (count < _maxBatchSize &&
               !linkedTokenSource.Token.IsCancellationRequested &&
               _workChannel.Reader.TryRead(out var work))
        {
            await Task.Run(() => ProcessWorkItem(work), linkedTokenSource.Token);
            count++;
        }

        // Process any worker thread results
        if (_workerThreadPool != null && _workerThreadPool.HasPendingWork)
        {
            await _workerThreadPool.SynchronizeResultsAsync(linkedTokenSource.Token);
        }

        _completionCallback?.Invoke();
    }

    /// <inheritdoc />
    public void CancelPendingWork()
    {
        _processingCts.Cancel();
    }

    /// <inheritdoc />
    public bool HasPendingWork =>
        _priorityMap.Count > 0 ||
        !_workChannel.Reader.Completion.IsCompleted ||
        (_mainThreadWork?.HasPendingWork ?? false) ||
        (_workerThreadPool?.HasPendingWork ?? false) ||
        _taskScheduler.HasPendingTasks;

    /// <inheritdoc />
    public void FlushPendingStylesAtEndOfFrame()
    {
        Task.Delay(_throttleIntervalMs).ContinueWith(_ => ProcessImmediately());
    }

    /// <inheritdoc />
    public void SetCompletionCallback(Action callback)
    {
        _completionCallback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    #endregion

    #region Event Handlers

    private void OnElementInvalidated(ElementInvalidatedEvent eventData)
    {
        RecalcPriority priority = DeterminePriority(eventData.Element);
        ScheduleElementRecalc(eventData.Element, priority);
    }

    private void OnPropertiesInvalidated(PropertiesInvalidatedEvent eventData)
    {
        RecalcPriority priority = DeterminePriority(eventData.Element);
        bool hasLayoutProperties = eventData.Properties.Any(IsLayoutProperty);
        bool hasVisualProperties = eventData.Properties.Any(IsVisualProperty);

        if (hasLayoutProperties)
        {
            priority = (RecalcPriority)Math.Max((byte)priority, (byte)RecalcPriority.High);
        }

        ScheduleElementRecalc(eventData.Element, priority);

        if (hasLayoutProperties && CouldAffectChildren(eventData.Element, eventData.Properties))
        {
            ScheduleSubtreeRecalc(eventData.Element, priority);
        }
    }

    private void OnSubtreeInvalidated(SubtreeInvalidatedEvent eventData)
    {
        RecalcPriority priority = DeterminePriority(eventData.RootElement);
        ScheduleSubtreeRecalc(eventData.RootElement, priority);
    }

    private void OnDeviceDependentElementsInvalidated(DeviceDependentElementsInvalidatedEvent eventData)
    {
        if (_context.Active?.DocumentElement != null)
        {
            ScheduleSubtreeRecalc(_context.Active.DocumentElement, RecalcPriority.High);
        }
    }

    #endregion

    #region Processing Logic

    private async Task ProcessWorkItemsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Periodically process the priority map (for items that couldn't be written to channel)
            _ = Task.Run(async () => {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_throttleIntervalMs, cancellationToken);
                    ProcessPendingWorkSync();
                }
            }, cancellationToken);

            // Process items from the channel as they arrive
            await foreach (var work in _workChannel.Reader.ReadAllAsync(cancellationToken))
            {
                ProcessWorkItem(work);

                // Allow other tasks to run if not high priority
                if (work.Priority < RecalcPriority.High)
                {
                    await Task.Yield();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal cancellation, do nothing
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in style processing: {ex.Message}");
        }
    }

    private void ProcessPendingWorkSync()
    {
        // Process items in priority order
        foreach (var priority in new[] { RecalcPriority.Critical, RecalcPriority.High, RecalcPriority.Normal, RecalcPriority.Low })
        {
            var items = _priorityMap.GetItemsByPriority(priority);
            foreach (var work in items.Take(_maxBatchSize))
            {
                ProcessWorkItem(work);
                _priorityMap.Remove(work.Element);
            }
        }
    }

    private void ProcessWorkItem(StyleRecalcWork work)
    {
        try
        {
            if (work.Priority == RecalcPriority.Critical && _mainThreadWork != null)
            {
                // Critical work goes to main thread
                _mainThreadWork.EnqueueElement(work.Element, work.Priority);
                _mainThreadWork.ProcessSync();
            }
            else if (_useWorkerThreads && _workerThreadPool != null && work.Priority != RecalcPriority.Critical)
            {
                // Non-critical work can go to worker threads if available
                if (work.WorkType == StyleWorkType.Subtree)
                {
                    // Process children through worker pool for subtree work
                    var childElements = work.Element.Children.ToList();
                    _workerThreadPool.EnqueueElements(childElements, work.Priority);
                }
                else
                {
                    _workerThreadPool.EnqueueElement(work.Element, work.Priority);
                }
            }
            else
            {
                // Otherwise use the task scheduler
                var task = work.WorkType == StyleWorkType.Subtree
                    ? StyleTasks.UpdateSubtreeStyles(work.Element, work.Priority)
                    : StyleTasks.ComputeElementStyle(work.Element, work.Priority);

                _taskScheduler.EnqueueTask(task);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing style work: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private RecalcPriority DeterminePriority(IElement element)
    {
        if (_viewportDetector.IsInViewport(element))
        {
            return RecalcPriority.Critical;
        }

        if (_viewportDetector.IsNearViewport(element))
        {
            return RecalcPriority.High;
        }

        if (ElementIsLikelyToBeVisibleSoon(element))
        {
            return RecalcPriority.Normal;
        }

        return RecalcPriority.Low;
    }

    private bool ElementIsLikelyToBeVisibleSoon(IElement element)
    {
        var parent = element.ParentElement;
        while (parent != null)
        {
            if (_viewportDetector.IsInViewport(parent) || _viewportDetector.IsNearViewport(parent))
                return true;

            parent = parent.ParentElement;
        }

        return false;
    }

    private bool IsLayoutProperty(string propertyName)
    {
        return propertyName.StartsWith("width") ||
               propertyName.StartsWith("height") ||
               propertyName.StartsWith("margin") ||
               propertyName.StartsWith("padding") ||
               propertyName.StartsWith("border") ||
               propertyName.StartsWith("position") ||
               propertyName.StartsWith("display") ||
               propertyName.StartsWith("flex") ||
               propertyName.StartsWith("grid") ||
               propertyName.StartsWith("top") ||
               propertyName.StartsWith("right") ||
               propertyName.StartsWith("bottom") ||
               propertyName.StartsWith("left");
    }

    private bool IsVisualProperty(string propertyName)
    {
        return propertyName.StartsWith("color") ||
               propertyName.StartsWith("background") ||
               propertyName.StartsWith("font") ||
               propertyName.StartsWith("text") ||
               propertyName.StartsWith("box-shadow") ||
               propertyName.StartsWith("opacity") ||
               propertyName.StartsWith("transform") ||
               propertyName.StartsWith("animation") ||
               propertyName.StartsWith("transition");
    }

    private bool CouldAffectChildren(IElement element, IEnumerable<string> properties)
    {
        return properties.Any(p =>
            p == "display" ||
            p == "position" ||
            p == "font-size" ||
            p == "line-height" ||
            p.StartsWith("flex") ||
            p.StartsWith("grid"));
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _processingCts.Cancel();
        _workChannel.Writer.Complete();

        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch { /* Ignore task cancellation exceptions */ }

        foreach (var token in _subscriptionTokens)
        {
            token.Dispose();
        }

        _processingCts.Dispose();

        _isDisposed = true;
    }

    #endregion
}

/// <summary>
/// Helper class to maintain a priority-based map of work items.
/// </summary>
internal class PriorityMap<T> where T : class
{
    private readonly Dictionary<object, T> _items = new Dictionary<object, T>();
    private readonly Dictionary<RecalcPriority, HashSet<object>> _priorityToKeys = new Dictionary<RecalcPriority, HashSet<object>>();
    private readonly object _lock = new object();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _items.Count;
            }
        }
    }

    public void AddOrUpdate(object key, T item, Func<T, T> updateFactory)
    {
        lock (_lock)
        {
            RecalcPriority oldPriority = RecalcPriority.Normal;

            if (_items.TryGetValue(key, out var existingItem))
            {
                oldPriority = GetPriority(existingItem);

                // Remove from old priority group
                if (_priorityToKeys.TryGetValue(oldPriority, out var keys))
                {
                    keys.Remove(key);
                }

                // Update the item
                _items[key] = updateFactory(existingItem);
            }
            else
            {
                _items[key] = item;
            }

            // Add to new priority group
            var priority = GetPriority(_items[key]);
            if (!_priorityToKeys.TryGetValue(priority, out var priorityKeys))
            {
                priorityKeys = new HashSet<object>();
                _priorityToKeys[priority] = priorityKeys;
            }

            priorityKeys.Add(key);
        }
    }

    public bool Remove(object key)
    {
        lock (_lock)
        {
            if (_items.TryGetValue(key, out var item))
            {
                var priority = GetPriority(item);
                if (_priorityToKeys.TryGetValue(priority, out var keys))
                {
                    keys.Remove(key);
                }

                return _items.Remove(key);
            }

            return false;
        }
    }

    public IEnumerable<T> GetItemsByPriority(RecalcPriority priority)
    {
        lock (_lock)
        {
            if (_priorityToKeys.TryGetValue(priority, out var keys))
            {
                foreach (var key in keys)
                {
                    if (_items.TryGetValue(key, out var item))
                    {
                        yield return item;
                    }
                }
            }
        }
    }

    private RecalcPriority GetPriority(T item)
    {
        // Assumes the item is a StyleRecalcWork
        if (item is StyleRecalcWork work)
        {
            return work.Priority;
        }

        return RecalcPriority.Normal;
    }
}