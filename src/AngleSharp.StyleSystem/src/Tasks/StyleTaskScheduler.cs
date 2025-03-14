namespace AngleSharp.StyleSystem.Tasks;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.StyleSystem.Interfaces;
using Dom;

/// <summary>
/// Default implementation of IStyleTaskScheduler.
/// </summary>
public class StyleTaskScheduler : IStyleTaskScheduler
{
    private readonly ConcurrentDictionary<IElement, IStyleTask> _pendingTasks = new();
    private readonly object _processingLock = new();
    private readonly SemaphoreSlim _workSemaphore = new(1, 1);
    private readonly IStyleEngine _styleEngine;
    private readonly int _throttleIntervalMs;
    private readonly int _batchSize;
    private Timer? _throttleTimer;
    private CancellationTokenSource? _processingCts;
    private bool _isProcessing;

    /// <summary>
    /// Creates a new StyleTaskScheduler.
    /// </summary>
    /// <param name="styleEngine">The style engine to use for task execution.</param>
    /// <param name="throttleIntervalMs">The throttle interval in milliseconds.</param>
    /// <param name="batchSize">The maximum number of tasks to process in a batch.</param>
    public StyleTaskScheduler(
        IStyleEngine styleEngine,
        int throttleIntervalMs = 16,
        int batchSize = 100)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _throttleIntervalMs = throttleIntervalMs;
        _batchSize = batchSize;
    }

    /// <inheritdoc />
    public void EnqueueTask(IStyleTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        _pendingTasks.AddOrUpdate(task.Element, task, (_, existing) =>
            (byte)task.Priority > (byte)existing.Priority ? task : existing);

        ScheduleProcessing();
    }

    /// <inheritdoc />
    public void ProcessTasks()
    {
        lock (_processingLock)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            _processingCts = new CancellationTokenSource();
        }

        try
        {
            ProcessBatchedTasks(_processingCts.Token);
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

    /// <inheritdoc />
    public async Task ProcessTasksAsync(CancellationToken cancellationToken = default)
    {
        await _workSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
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
                await Task.Run(() => ProcessBatchedTasks(linkedCts.Token), linkedCts.Token)
                    .ConfigureAwait(false);
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

    /// <inheritdoc />
    public bool HasPendingTasks => _pendingTasks.Count > 0;

    /// <inheritdoc />
    public void CancelPendingTasks()
    {
        lock (_processingLock)
        {
            _processingCts?.Cancel();
        }

        _throttleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CancelPendingTasks();
        _throttleTimer?.Dispose();
        _workSemaphore.Dispose();
        _processingCts?.Dispose();
    }

    private void ProcessBatchedTasks(CancellationToken cancellationToken)
    {
        if (!HasPendingTasks)
            return;

        var tasks = GetSortedTasks();
        int batchCount = 0;

        while (tasks.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var batch = tasks.Take(_batchSize).ToList();
            tasks.RemoveRange(0, Math.Min(batch.Count, tasks.Count));

            ExecuteBatch(batch);

            batchCount++;

            // If there are still tasks and we've processed many batches, schedule another run
            if (tasks.Count > 0 && batchCount >= 10)
            {
                foreach (var remainingTask in tasks)
                {
                    EnqueueTask(remainingTask);
                }
                break;
            }
        }
    }

    private List<IStyleTask> GetSortedTasks()
    {
        return _pendingTasks.Values
            .OrderByDescending(t => t.Priority)
            .ToList();
    }

    private void ExecuteBatch(List<IStyleTask> batch)
    {
        foreach (var task in batch)
        {
            try
            {
                _pendingTasks.TryRemove(task.Element, out _);
                task.Execute(_styleEngine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing style task: {ex.Message}");
            }
        }
    }

    private void ScheduleProcessing()
    {
        if (_throttleTimer == null)
        {
            _throttleTimer = new Timer(
                _ => ProcessTasks(),
                null,
                _throttleIntervalMs,
                Timeout.Infinite);
        }
        else
        {
            _throttleTimer.Change(_throttleIntervalMs, Timeout.Infinite);
        }
    }
}