using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.Resource;
using Infrastructure.EventAggregator.API.Aggregation;

namespace LayoutEngine.Platform.Update
{
    using System.Linq;

    /// <summary>
    /// Implements an idle task scheduler that aligns with browser engine behavior,
    /// particularly Blink's (Chrome) implementation of RequestIdleCallback.
    /// </summary>
    public sealed class IdleTaskScheduler : IIdleTaskScheduler, IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IFrameScheduler _frameScheduler;
        private readonly IThreadingCoordinator _threadingCoordinator;
        private readonly ILogger<IdleTaskScheduler>? _logger;
        private readonly ConcurrentDictionary<Guid, PrioritizedIdleTask> _pendingTasks = new();
        private readonly TaskPriorityQueue _taskQueue = new();
        private readonly List<ISubscriptionToken> _subscriptions = new();
        private readonly CancellationTokenSource _cts = new();

        // Aligned with browser constants
        private bool _isProcessing;
        private bool _isPaused;
        private bool _isDisposed;
        private double _lastFrameDuration;
        private double _targetFrameDuration = 16.6; // Default for 60fps
        private double _idleThreshold = 5.0; // Conservative idle threshold
        private const double MIN_IDLE_PERIOD_MS = 1.0; // Smallest period considered worthwhile
        private const double DEADLINE_BUFFER_MS = 5.0; // Safety buffer before next frame

        // Frame timing tracking
        private readonly Queue<double> _recentFrameTimes = new(10); // Remember last 10 frames
        private double _averageFrameTime = 16.6;
        private double _jankThreshold = 50.0; // Consider frames over 50ms as jank
        private int _jankFrameCount = 0;
        private int _totalFrameCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdleTaskScheduler"/> class.
        /// </summary>
        public IdleTaskScheduler(
            IEventAggregator eventAggregator,
            IFrameScheduler frameScheduler,
            IThreadingCoordinator threadingCoordinator,
            ILogger<IdleTaskScheduler>? logger = null)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _frameScheduler = frameScheduler ?? throw new ArgumentNullException(nameof(frameScheduler));
            _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
            _logger = logger;

            _subscriptions.Add(_eventAggregator.Subscribe<BeginFrameEvent>(OnBeginFrame));
            _subscriptions.Add(_eventAggregator.Subscribe<EndFrameEvent>(OnEndFrame));
            _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));

            _logger?.LogInformation("IdleTaskScheduler initialized with target frame time: {TargetFrameTime}ms, idle threshold: {IdleThreshold}ms",
                _targetFrameDuration, _idleThreshold);
        }

        /// <inheritdoc />
        public int PendingTaskCount => _pendingTasks.Count;

        /// <inheritdoc />
        public IIdleTask ScheduleIdleTask(Action action, IdleTaskPriority priority = IdleTaskPriority.Normal)
        {
            ThrowIfDisposed();
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var task = new PrioritizedIdleTask(action, priority);
            _pendingTasks[task.Id] = task;
            _taskQueue.Enqueue(task);

            _logger?.LogDebug("Scheduled idle task {TaskId} with priority {Priority}",
                task.Id, priority);

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

            _logger?.LogDebug("Scheduled idle task with token {TaskId} with priority {Priority}",
                task.Id, priority);

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
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, priority);

            _pendingTasks[task.Id] = task;
            _taskQueue.Enqueue(task);

            _logger?.LogDebug("Scheduled async idle task {TaskId} with priority {Priority}",
                task.Id, priority);

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
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, priority);

            _pendingTasks[task.Id] = task;
            _taskQueue.Enqueue(task);

            _logger?.LogDebug("Scheduled async idle task with token {TaskId} with priority {Priority}",
                task.Id, priority);

            return tcs.Task;
        }

        /// <inheritdoc />
        public void CancelTask(Guid taskId)
        {
            ThrowIfDisposed();
            if (_pendingTasks.TryRemove(taskId, out var task))
            {
                task.Cancel();
                _logger?.LogDebug("Canceled idle task {TaskId}", taskId);
            }
        }

        /// <inheritdoc />
        public void PauseTasks()
        {
            ThrowIfDisposed();
            _isPaused = true;
            _logger?.LogInformation("Idle task processing paused");
        }

        /// <inheritdoc />
        public void ResumeTasks()
        {
            ThrowIfDisposed();
            _isPaused = false;
            _logger?.LogInformation("Idle task processing resumed");
        }

        /// <summary>
        /// Handle the beginning of a new frame. Resets frame duration tracking.
        /// </summary>
        private void OnBeginFrame(BeginFrameEvent e)
        {
            if (_isDisposed)
                return;

            _lastFrameDuration = 0;
            _logger?.LogTrace("Begin frame {FrameNumber} at {FrameTimestamp}",
                e.FrameNumber, e.FrameTimestamp);
        }

        /// <summary>
        /// Handle the end of a frame. Calculates idle time and schedules task processing if appropriate.
        /// </summary>
        private void OnEndFrame(EndFrameEvent e)
        {
            if (_isDisposed || _isPaused || _isProcessing || _taskQueue.IsEmpty)
                return;

            // Calculate frame duration and idle time
            var frameDuration = Math.Max(0, e.FrameTimestamp - _frameScheduler.LastFrameTime);
            _lastFrameDuration = frameDuration;

            // Track frame timing history
            TrackFrameTiming(frameDuration);

            // Calculate available idle time
            double idleTime = Math.Max(0, _targetFrameDuration - frameDuration);

            // Dynamically adjust idle threshold based on frame history
            AdjustIdleThreshold();

            _logger?.LogTrace("Frame {FrameNumber} took {FrameDuration:F2}ms, idle time: {IdleTime:F2}ms, threshold: {IdleThreshold:F2}ms",
                e.FrameNumber, frameDuration, idleTime, _idleThreshold);

            // Process tasks if we have enough idle time or a forced processing is needed
            if (idleTime > _idleThreshold || ShouldForceTaskProcessing())
            {
                ProcessIdleTasks(idleTime);
            }
        }

        /// <summary>
        /// Handle memory pressure events by potentially cancelling low priority tasks.
        /// </summary>
        private void OnMemoryPressure(MemoryPressureEvent e)
        {
            if (e.Severity >= MemoryPressureSeverity.High)
            {
                CancelLowPriorityTasks();
            }
        }

        /// <summary>
        /// Process idle tasks during available idle time.
        /// </summary>
        /// <param name="availableTimeMs">Available idle time in milliseconds</param>
        private void ProcessIdleTasks(double availableTimeMs)
        {
            if (_isProcessing || _isPaused || _taskQueue.IsEmpty)
                return;

            _isProcessing = true;

            // Align with browser behavior - respect vsync by reserving a buffer time
            var effectiveIdleTimeMs = Math.Max(MIN_IDLE_PERIOD_MS, availableTimeMs - DEADLINE_BUFFER_MS);
            var deadline = DateTime.UtcNow.AddMilliseconds(effectiveIdleTimeMs);

            _logger?.LogDebug("Starting idle task processing with {AvailableTime:F2}ms (effective: {EffectiveTime:F2}ms), {PendingCount} pending tasks",
                availableTimeMs, effectiveIdleTimeMs, _pendingTasks.Count);

            _threadingCoordinator.ScheduleOnMainThread(() =>
            {
                try
                {
                    // Browser-aligned approach: ensure at least one task executes,
                    // then continue until deadline approaches
                    bool processedAtLeastOne = false;
                    int taskCount = 0;
                    var startTime = DateTime.UtcNow;

                    while ((DateTime.UtcNow < deadline || !processedAtLeastOne) &&
                           !_isPaused && !_isDisposed && _taskQueue.TryDequeue(out var task))
                    {
                        // Skip canceled or already removed tasks
                        if (task.IsCanceled || !_pendingTasks.ContainsKey(task.Id))
                        {
                            _pendingTasks.TryRemove(task.Id, out _);
                            continue;
                        }

                        // Only re-enqueue if we've processed at least one task and we're close to the deadline
                        if (processedAtLeastOne && DateTime.UtcNow.AddMilliseconds(1) >= deadline)
                        {
                            _taskQueue.Enqueue(task);
                            _logger?.LogTrace("Re-enqueued task {TaskId} as deadline approaching", task.Id);
                            break;
                        }

                        try
                        {
                            // Track execution start time for performance monitoring
                            var taskStartTime = DateTime.UtcNow;
                            task.Execute(_cts.Token);
                            var taskDuration = (DateTime.UtcNow - taskStartTime).TotalMilliseconds;

                            processedAtLeastOne = true;
                            taskCount++;

                            _pendingTasks.TryRemove(task.Id, out _);
                            _eventAggregator.Publish(new IdleTaskCompletedEvent(task.Id));

                            _logger?.LogTrace("Executed idle task {TaskId} in {TaskDuration:F2}ms",
                                task.Id, taskDuration);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger?.LogDebug("Task {TaskId} was canceled during execution", task.Id);
                            _pendingTasks.TryRemove(task.Id, out _);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error executing idle task {TaskId}", task.Id);
                            _pendingTasks.TryRemove(task.Id, out _);
                        }
                    }

                    var totalDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger?.LogDebug("Completed {TaskCount} idle tasks in {TotalDuration:F2}ms",
                        taskCount, totalDuration);
                }
                finally
                {
                    _isProcessing = false;

                    // If we still have time and tasks, schedule another round
                    if (!_taskQueue.IsEmpty && !_isPaused && !_isDisposed)
                    {
                        var remainingTime = (deadline - DateTime.UtcNow).TotalMilliseconds;
                        if (remainingTime > MIN_IDLE_PERIOD_MS)
                        {
                            _logger?.LogTrace("Scheduling additional idle processing with {RemainingTime:F2}ms remaining",
                                remainingTime);
                            ProcessIdleTasks(remainingTime);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Cancel low priority tasks to free up resources during memory pressure.
        /// </summary>
        private void CancelLowPriorityTasks()
        {
            _logger?.LogInformation("Canceling low priority tasks due to memory pressure");

            var tasks = new List<PrioritizedIdleTask>(_pendingTasks.Values);
            foreach (var task in tasks)
            {
                if (task.Priority == IdleTaskPriority.Low)
                {
                    CancelTask(task.Id);
                }
            }
        }

        /// <summary>
        /// Track frame timing history to inform scheduling decisions.
        /// </summary>
        private void TrackFrameTiming(double frameDuration)
        {
            _recentFrameTimes.Enqueue(frameDuration);
            _totalFrameCount++;

            if (_recentFrameTimes.Count > 10)
                _recentFrameTimes.Dequeue();

            if (frameDuration > _jankThreshold)
                _jankFrameCount++;

            // Recalculate average frame time
            double sum = 0;
            foreach (var time in _recentFrameTimes)
                sum += time;

            _averageFrameTime = _recentFrameTimes.Count > 0 ? sum / _recentFrameTimes.Count : 16.6;
        }

        /// <summary>
        /// Dynamically adjust the idle threshold based on frame timing history.
        /// </summary>
        private void AdjustIdleThreshold()
        {
            if (_totalFrameCount < 10)
                return; // Not enough data yet

            // Calculate jank percentage
            var jankPercentage = (double)_jankFrameCount / _totalFrameCount;

            // Adjust idle threshold based on jank and average frame time
            if (jankPercentage > 0.05) // More than 5% janky frames
            {
                // Be more conservative
                _idleThreshold = Math.Min(8.0, _idleThreshold + 0.5);
            }
            else if (_averageFrameTime < _targetFrameDuration * 0.8) // Plenty of frame budget
            {
                // Be more aggressive
                _idleThreshold = Math.Max(2.0, _idleThreshold - 0.2);
            }

            // Reset counters periodically to adapt to changing conditions
            if (_totalFrameCount > 100)
            {
                _jankFrameCount = _jankFrameCount / 2;
                _totalFrameCount = _totalFrameCount / 2;
            }
        }

        /// <summary>
        /// Determine if task processing should be forced even with limited idle time.
        /// </summary>
        private bool ShouldForceTaskProcessing()
        {
            // Process tasks regardless of idle time if:
            // 1. We have high priority tasks
            // 2. We have a small number of tasks that need processing
            // 3. Tasks have been waiting too long

            if (_taskQueue.HasHighPriorityTasks)
                return true;

            if (_taskQueue.Count <= 3) // Small queue can be processed quickly
                return true;

            // Could add logic for tasks waiting too long

            return false;
        }

        /// <summary>
        /// Execute a specific task - primarily for testing
        /// </summary>
        internal void ExecuteTask(Guid taskId)
        {
            if (_pendingTasks.TryGetValue(taskId, out var task) && !task.IsCanceled)
            {
                try
                {
                    task.Execute(_cts.Token);
                    _pendingTasks.TryRemove(taskId, out _);
                    _eventAggregator.Publish(new IdleTaskCompletedEvent(taskId));
                }
                catch
                {
                    _pendingTasks.TryRemove(taskId, out _);
                }
            }
        }

        /// <summary>
        /// Execute next pending task - primarily for testing
        /// </summary>
        internal void ExecuteNextTask()
        {
            if (_taskQueue.TryDequeue(out var task) && !task.IsCanceled && _pendingTasks.ContainsKey(task.Id))
            {
                try
                {
                    task.Execute(_cts.Token);
                    _pendingTasks.TryRemove(task.Id, out _);
                    _eventAggregator.Publish(new IdleTaskCompletedEvent(task.Id));
                }
                catch
                {
                    _pendingTasks.TryRemove(task.Id, out _);
                }
            }
        }

        /// <summary>
        /// Get metrics about the scheduler state - primarily for monitoring and testing
        /// </summary>
        internal IdleTaskSchedulerMetrics GetMetrics()
        {
            return new IdleTaskSchedulerMetrics
            {
                PendingTaskCount = _pendingTasks.Count,
                AverageFrameTime = _averageFrameTime,
                JankPercentage = _totalFrameCount > 0 ? (double)_jankFrameCount / _totalFrameCount * 100 : 0,
                IdleThreshold = _idleThreshold,
                IsPaused = _isPaused,
                IsProcessing = _isProcessing
            };
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

            _logger?.LogInformation("IdleTaskScheduler disposing, canceling {PendingTaskCount} pending tasks",
                _pendingTasks.Count);

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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during dispose of IdleTaskScheduler");
            }

            foreach (var task in _pendingTasks.Values)
            {
                task.Cancel();
            }
            _pendingTasks.Clear();
        }

        #region Helper Classes

        /// <summary>
        /// Represents a prioritized idle task with execution logic.
        /// </summary>
        private class PrioritizedIdleTask : IIdleTask
        {
            private readonly Action? _action;
            private readonly Action<CancellationToken>? _actionWithToken;
            private readonly CancellationTokenSource? _localCts;
            private volatile bool _isCanceled;

            public PrioritizedIdleTask(Action action, IdleTaskPriority priority)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
                Priority = priority;
                Id = Guid.NewGuid();
                CreationTime = DateTime.UtcNow;
            }

            public PrioritizedIdleTask(Action<CancellationToken> action, IdleTaskPriority priority)
            {
                _actionWithToken = action ?? throw new ArgumentNullException(nameof(action));
                _localCts = new CancellationTokenSource();
                Priority = priority;
                Id = Guid.NewGuid();
                CreationTime = DateTime.UtcNow;
            }

            public Guid Id { get; }
            public IdleTaskPriority Priority { get; }
            public bool IsCanceled => _isCanceled || (_localCts?.IsCancellationRequested ?? false);
            public DateTime CreationTime { get; }
            public TimeSpan Age => DateTime.UtcNow - CreationTime;

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

        /// <summary>
        /// Provides a queue that dequeues items based on priority.
        /// </summary>
        private class TaskPriorityQueue
        {
            private readonly ConcurrentDictionary<IdleTaskPriority, ConcurrentQueue<PrioritizedIdleTask>> _priorityQueues = new();
            private readonly IdleTaskPriority[] _priorityLevels;

            public TaskPriorityQueue()
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

            public int Count
            {
                get
                {
                    int count = 0;
                    foreach (var queue in _priorityQueues.Values)
                    {
                        count += queue.Count;
                    }
                    return count;
                }
            }

            public bool HasHighPriorityTasks => !_priorityQueues[IdleTaskPriority.High].IsEmpty;

            public void Enqueue(PrioritizedIdleTask item)
            {
                var queue = _priorityQueues[item.Priority];
                queue.Enqueue(item);
            }

            public bool TryDequeue(out PrioritizedIdleTask task)
            {
                task = default!;

                foreach (var priority in _priorityLevels)
                {
                    var queue = _priorityQueues[priority];
                    if (queue.TryDequeue(out var dequeuedTask))
                    {
                        task = dequeuedTask;
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Contains metrics about the scheduler state.
        /// </summary>
        public class IdleTaskSchedulerMetrics
        {
            public int PendingTaskCount { get; set; }
            public double AverageFrameTime { get; set; }
            public double JankPercentage { get; set; }
            public double IdleThreshold { get; set; }
            public bool IsPaused { get; set; }
            public bool IsProcessing { get; set; }
        }

        #endregion
    }
}