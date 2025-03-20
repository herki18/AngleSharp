using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.Threading;
using Infrastructure.EventAggregator.API.Aggregation;

namespace LayoutEngine.Platform.Update;

/// <summary>
/// Schedules and executes tasks during idle periods in the application.
/// </summary>
public sealed class IdleTaskScheduler : IIdleTaskScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IFrameScheduler _frameScheduler;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly ConcurrentDictionary<Guid, PrioritizedIdleTask> _pendingTasks = new();
    private readonly ConcurrentPriorityQueue<PrioritizedIdleTask> _taskQueue = new();
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private readonly CancellationTokenSource _cts = new();

    private bool _isProcessing;
    private bool _isPaused;
    private bool _isDisposed;
    private double _lastFrameDuration;
    private double _targetFrameDuration = 16.6; // Target 60fps (~16.6ms per frame)
    private double _idleThreshold = 5.0; // Consider time idle if we have at least 5ms

    /// <summary>
    /// Initializes a new instance of the <see cref="IdleTaskScheduler"/> class.
    /// </summary>
    public IdleTaskScheduler(
        IEventAggregator eventAggregator,
        IFrameScheduler frameScheduler,
        IThreadingCoordinator threadingCoordinator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _frameScheduler = frameScheduler ?? throw new ArgumentNullException(nameof(frameScheduler));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));

        // Subscribe to frame events to detect idle periods
        _subscriptions.Add(_eventAggregator.Subscribe<BeginFrameEvent>(OnBeginFrame));
        _subscriptions.Add(_eventAggregator.Subscribe<EndFrameEvent>(OnEndFrame));
        _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));
    }

    /// <inheritdoc />
    public IIdleTask ScheduleIdleTask(Action action, IdleTaskPriority priority = IdleTaskPriority.Normal)
    {
        ThrowIfDisposed();

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var task = new PrioritizedIdleTask(action, priority);
        _pendingTasks[task.Id] = task;
        _taskQueue.Enqueue(task);

        return task;
    }

    /// <inheritdoc />
    public IIdleTask ScheduleIdleTask(Action<CancellationToken> action, IdleTaskPriority priority = IdleTaskPriority.Normal)
    {
        ThrowIfDisposed();

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var task = new PrioritizedIdleTask(action, priority);
        _pendingTasks[task.Id] = task;
        _taskQueue.Enqueue(task);

        return task;
    }

    /// <inheritdoc />
    public Task<TResult> ScheduleIdleTaskAsync<TResult>(Func<TResult> function, IdleTaskPriority priority = IdleTaskPriority.Normal)
    {
        ThrowIfDisposed();

        if (function == null)
            throw new ArgumentNullException(nameof(function));

        var tcs = new TaskCompletionSource<TResult>();
        var task = new PrioritizedIdleTask(ct =>
        {
            try
            {
                var result = function();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, priority);

        _pendingTasks[task.Id] = task;
        _taskQueue.Enqueue(task);

        return tcs.Task;
    }

    /// <inheritdoc />
    public Task<TResult> ScheduleIdleTaskAsync<TResult>(Func<CancellationToken, TResult> function, IdleTaskPriority priority = IdleTaskPriority.Normal)
    {
        ThrowIfDisposed();

        if (function == null)
            throw new ArgumentNullException(nameof(function));

        var tcs = new TaskCompletionSource<TResult>();
        var task = new PrioritizedIdleTask(ct =>
        {
            try
            {
                var result = function(ct);
                tcs.SetResult(result);
            }
            catch (OperationCanceledException)
            {
                tcs.SetCanceled();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, priority);

        _pendingTasks[task.Id] = task;
        _taskQueue.Enqueue(task);

        return tcs.Task;
    }

    /// <inheritdoc />
    public void CancelTask(Guid taskId)
    {
        ThrowIfDisposed();

        if (_pendingTasks.TryRemove(taskId, out var task))
        {
            task.Cancel();
        }
    }

    /// <inheritdoc />
    public void PauseTasks()
    {
        ThrowIfDisposed();
        _isPaused = true;
    }

    /// <inheritdoc />
    public void ResumeTasks()
    {
        ThrowIfDisposed();
        _isPaused = false;
    }

    /// <inheritdoc />
    public int PendingTaskCount => _pendingTasks.Count;

    private void OnBeginFrame(BeginFrameEvent e)
    {
        if (_isDisposed)
            return;

        // Start timing the frame
        _lastFrameDuration = 0;
    }

    private void OnEndFrame(EndFrameEvent e)
    {
        if (_isDisposed || _isPaused || _isProcessing || _taskQueue.IsEmpty)
            return;

        // Calculate time spent on the frame
        _lastFrameDuration = e.FrameTime - e.FrameNumber > 0 ?
            e.FrameTime - _frameScheduler.LastFrameTime :
            0;

        // Calculate how much idle time we have available
        var idleTime = _targetFrameDuration - _lastFrameDuration;

        // If we have enough idle time, process some tasks
        if (idleTime > _idleThreshold)
        {
            ProcessIdleTasks(idleTime);
        }
    }

    private void OnMemoryPressure(MemoryPressureEvent e)
    {
        if (e.Severity >= MemoryPressureSeverity.High)
        {
            // Cancel low-priority tasks when memory pressure is high
            CancelLowPriorityTasks();
        }
    }

    private void ProcessIdleTasks(double availableTimeMs)
    {
        if (_isProcessing || _isPaused || _taskQueue.IsEmpty)
            return;

        _isProcessing = true;

        // Convert to a deadline
        var deadline = DateTime.UtcNow.AddMilliseconds(availableTimeMs);

        _threadingCoordinator.ScheduleOnMainThread(() =>
        {
            try
            {
                // Process tasks until we run out of time or tasks
                while (DateTime.UtcNow < deadline && !_isPaused && !_isDisposed && _taskQueue.TryDequeue(out var task))
                {
                    // Skip canceled tasks
                    if (task.IsCanceled || !_pendingTasks.ContainsKey(task.Id))
                    {
                        _pendingTasks.TryRemove(task.Id, out _);
                        continue;
                    }

                    try
                    {
                        // Check if we still have time
                        if (DateTime.UtcNow >= deadline)
                        {
                            // If we're out of time, put the task back in the queue and exit
                            _taskQueue.Enqueue(task);
                            break;
                        }

                        // Execute the task
                        task.Execute(_cts.Token);

                        // Remove completed task
                        _pendingTasks.TryRemove(task.Id, out _);

                        // Publish task completed event
                        _eventAggregator.Publish(new IdleTaskCompletedEvent(task.Id));
                    }
                    catch (Exception)
                    {
                        // Remove failed task
                        _pendingTasks.TryRemove(task.Id, out _);
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        });
    }

    private void CancelLowPriorityTasks()
    {
        var tasks = new List<PrioritizedIdleTask>(_pendingTasks.Values);
        foreach (var task in tasks)
        {
            if (task.Priority == IdleTaskPriority.Low)
            {
                CancelTask(task.Id);
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(IdleTaskScheduler));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }

        _subscriptions.Clear();

        try
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        catch
        {
            // Ignore any exceptions during disposal
        }

        // Cancel all pending tasks
        foreach (var task in _pendingTasks.Values)
        {
            task.Cancel();
        }

        _pendingTasks.Clear();
    }

    /// <summary>
    /// A prioritized idle task implementation.
    /// </summary>
    private class PrioritizedIdleTask : IIdleTask
    {
        private readonly Action? _action;
        private readonly Action<CancellationToken>? _actionWithToken;
        private readonly CancellationTokenSource? _localCts;
        private bool _isCanceled;

        public PrioritizedIdleTask(Action action, IdleTaskPriority priority)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            Priority = priority;
            Id = Guid.NewGuid();
        }

        public PrioritizedIdleTask(Action<CancellationToken> action, IdleTaskPriority priority)
        {
            _actionWithToken = action ?? throw new ArgumentNullException(nameof(action));
            _localCts = new CancellationTokenSource();
            Priority = priority;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }
        public IdleTaskPriority Priority { get; }
        public bool IsCanceled => _isCanceled || (_localCts?.IsCancellationRequested ?? false);

        public void Execute(CancellationToken token)
        {
            if (IsCanceled)
                return;

            if (_action != null)
            {
                _action();
            }
            else if (_actionWithToken != null)
            {
                // Create a linked token that respects both our local cancellation and the global one
                using var linkedCts = _localCts != null
                    ? CancellationTokenSource.CreateLinkedTokenSource(_localCts.Token, token)
                    : new CancellationTokenSource();

                _actionWithToken(linkedCts.Token);
            }
        }

        public void Cancel()
        {
            _isCanceled = true;
            _localCts?.Cancel();
        }

        public void Dispose()
        {
            Cancel();
            _localCts?.Dispose();
        }
    }
}

/// <summary>
/// A concurrent priority queue implementation.
/// </summary>
internal class ConcurrentPriorityQueue<T> where T : PrioritizedIdleTask
{
    private readonly ConcurrentDictionary<IdleTaskPriority, ConcurrentQueue<T>> _priorityQueues = new();
    private readonly IdleTaskPriority[] _priorityLevels;

    public ConcurrentPriorityQueue()
    {
        _priorityLevels = new[]
        {
            IdleTaskPriority.High,
            IdleTaskPriority.Normal,
            IdleTaskPriority.Low
        };

        foreach (var priority in _priorityLevels)
        {
            _priorityQueues[priority] = new ConcurrentQueue<T>();
        }
    }

    public bool IsEmpty => _priorityQueues.Values.All(q => q.IsEmpty);

    public void Enqueue(T item)
    {
        var queue = _priorityQueues[item.Priority];
        queue.Enqueue(item);
    }

    public bool TryDequeue(out T item)
    {
        item = default!;

        foreach (var priority in _priorityLevels)
        {
            var queue = _priorityQueues[priority];
            if (queue.TryDequeue(out var dequeuedItem))
            {
                item = dequeuedItem;
                return true;
            }
        }

        return false;
    }
}