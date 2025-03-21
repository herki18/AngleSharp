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

using System.Linq;
using Contracts.Resource;

public sealed class IdleTaskScheduler : IIdleTaskScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IFrameScheduler _frameScheduler;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly ConcurrentDictionary<Guid, PrioritizedIdleTask> _pendingTasks = new();
    private readonly ConcurrentPriorityQueue _taskQueue = new();
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _isProcessing;
    private bool _isPaused;
    private bool _isDisposed;
    private double _lastFrameDuration;
    private double _targetFrameDuration = 16.6;
    private double _idleThreshold = 5.0;
    public IdleTaskScheduler(
        IEventAggregator eventAggregator,
        IFrameScheduler frameScheduler,
        IThreadingCoordinator threadingCoordinator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _frameScheduler = frameScheduler ?? throw new ArgumentNullException(nameof(frameScheduler));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
        _subscriptions.Add(_eventAggregator.Subscribe<BeginFrameEvent>(OnBeginFrame));
        _subscriptions.Add(_eventAggregator.Subscribe<EndFrameEvent>(OnEndFrame));
        _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));
    }
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
    public void CancelTask(Guid taskId)
    {
        ThrowIfDisposed();
        if (_pendingTasks.TryRemove(taskId, out var task))
        {
            task.Cancel();
        }
    }
    public void PauseTasks()
    {
        ThrowIfDisposed();
        _isPaused = true;
    }
    public void ResumeTasks()
    {
        ThrowIfDisposed();
        _isPaused = false;
    }
    public int PendingTaskCount => _pendingTasks.Count;
    private void OnBeginFrame(BeginFrameEvent e)
    {
        if (_isDisposed)
            return;
        _lastFrameDuration = 0;
    }
    private void OnEndFrame(EndFrameEvent e)
    {
        if (_isDisposed || _isPaused || _isProcessing || _taskQueue.IsEmpty)
            return;
        _lastFrameDuration = e.FrameTimestamp - e.FrameNumber > 0 ?
            e.FrameTimestamp - _frameScheduler.LastFrameTime :
            0;
        var idleTime = _targetFrameDuration - _lastFrameDuration;
        if (idleTime > _idleThreshold)
        {
            ProcessIdleTasks(idleTime);
        }
    }
    private void OnMemoryPressure(MemoryPressureEvent e)
    {
        if (e.Severity >= MemoryPressureSeverity.High)
        {
            CancelLowPriorityTasks();
        }
    }
    private void ProcessIdleTasks(double availableTimeMs)
    {
        if (_isProcessing || _isPaused || _taskQueue.IsEmpty)
            return;
        _isProcessing = true;
        var deadline = DateTime.UtcNow.AddMilliseconds(availableTimeMs);
        _threadingCoordinator.ScheduleOnMainThread(() =>
        {
            try
            {
                while (DateTime.UtcNow < deadline && !_isPaused && !_isDisposed && _taskQueue.TryDequeue(out var task))
                {
                    if (task.IsCanceled || !_pendingTasks.ContainsKey(task.Id))
                    {
                        _pendingTasks.TryRemove(task.Id, out _);
                        continue;
                    }
                    try
                    {
                        if (DateTime.UtcNow >= deadline)
                        {
                            _taskQueue.Enqueue(task);
                            break;
                        }
                        task.Execute(_cts.Token);
                        _pendingTasks.TryRemove(task.Id, out _);
                        _eventAggregator.Publish(new IdleTaskCompletedEvent(task.Id));
                    }
                    catch (Exception)
                    {
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
        }
        foreach (var task in _pendingTasks.Values)
        {
            task.Cancel();
        }
        _pendingTasks.Clear();
    }

    // Made private nested class for better encapsulation
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

    // Made ConcurrentPriorityQueue a nested class within IdleTaskScheduler
    private class ConcurrentPriorityQueue
    {
        private readonly ConcurrentDictionary<IdleTaskPriority, ConcurrentQueue<PrioritizedIdleTask>> _priorityQueues = new();
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
                _priorityQueues[priority] = new ConcurrentQueue<PrioritizedIdleTask>();
            }
        }

        public bool IsEmpty => _priorityQueues.Values.All(q => q.IsEmpty);

        public void Enqueue(PrioritizedIdleTask item)
        {
            var queue = _priorityQueues[item.Priority];
            queue.Enqueue(item);
        }

        public bool TryDequeue(out PrioritizedIdleTask item)
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
}