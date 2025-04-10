using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Platform.Threading;

namespace LayoutEngine.Platform.Threading;

/// <summary>
/// Simplified threading coordinator that executes everything on the main thread.
/// </summary>
public class SimplifiedThreadingCoordinator : IThreadingCoordinator, IDisposable
{
    private readonly SynchronizationContext _mainThreadContext;
    private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
    private readonly Queue<Action> _renderThreadQueue = new Queue<Action>();
    private readonly Queue<Action> _workerThreadQueue = new Queue<Action>();
    private readonly Dictionary<int, SimplifiedWorker> _workers = new Dictionary<int, SimplifiedWorker>();

    private bool _isDisposed;
    private int _nextWorkerId = 1;

    /// <summary>
    /// Initializes a new instance of the SimplifiedThreadingCoordinator.
    /// </summary>
    public SimplifiedThreadingCoordinator()
    {
        // Capture the current synchronization context (main thread)
        _mainThreadContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    /// <summary>
    /// Gets the main thread synchronization context.
    /// </summary>
    public SynchronizationContext MainThreadContext => _mainThreadContext;

    /// <summary>
    /// Gets the render thread synchronization context.
    /// In the simplified model, this is the same as the main thread context.
    /// </summary>
    public SynchronizationContext RenderThreadContext => _mainThreadContext;

    /// <summary>
    /// Schedules an action to be executed on the main thread.
    /// </summary>
    public void ScheduleOnMainThread(Action action)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedThreadingCoordinator));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        // If we're already on the main thread, execute immediately
        if (Thread.CurrentThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action();
        }
        else
        {
            // Queue the action for later processing
            _mainThreadQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Schedules an action to be executed on the render thread.
    /// In the simplified model, this is the same as the main thread.
    /// </summary>
    public void ScheduleOnRenderThread(Action action)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedThreadingCoordinator));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        // Queue the action for later processing
        _renderThreadQueue.Enqueue(action);
    }

    /// <summary>
    /// Schedules an action to be executed on a worker thread.
    /// In the simplified model, this executes on the main thread.
    /// </summary>
    public void ScheduleOnWorkerThread(Action action)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedThreadingCoordinator));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        // Queue the action for later processing
        _workerThreadQueue.Enqueue(action);
    }

    /// <summary>
    /// Creates a worker with the specified worker type.
    /// </summary>
    public IWorker CreateWorker(WorkerType workerType)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedThreadingCoordinator));

        var workerId = _nextWorkerId++;
        var worker = new SimplifiedWorker(workerId, workerType, this);
        _workers[workerId] = worker;

        return worker;
    }

    /// <summary>
    /// Processes all queued actions.
    /// Call this method from your game engine's update loop.
    /// </summary>
    /// <param name="maxActionsPerQueue">Maximum number of actions to process per queue.</param>
    public void ProcessQueuedActions(int maxActionsPerQueue = 10)
    {
        if (_isDisposed)
            return;

        // Process main thread queue
        ProcessQueue(_mainThreadQueue, maxActionsPerQueue);

        // Process render thread queue
        ProcessQueue(_renderThreadQueue, maxActionsPerQueue);

        // Process worker thread queue
        ProcessQueue(_workerThreadQueue, maxActionsPerQueue);

        // Process each worker's queue
        foreach (var worker in _workers.Values)
        {
            worker.ProcessPendingWork(maxActionsPerQueue);
        }
    }

    /// <summary>
    /// Processes a queue of actions.
    /// </summary>
    private void ProcessQueue(Queue<Action> queue, int maxActions)
    {
        int count = Math.Min(queue.Count, maxActions);

        for (int i = 0; i < count; i++)
        {
            if (queue.Count == 0)
                break;

            var action = queue.Dequeue();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.Error.WriteLine($"Error processing queued action: {ex}");
            }
        }
    }

    /// <summary>
    /// Disposes the threading coordinator.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Clear all queues
        _mainThreadQueue.Clear();
        _renderThreadQueue.Clear();
        _workerThreadQueue.Clear();

        // Dispose all workers
        foreach (var worker in _workers.Values)
        {
            worker.Dispose();
        }

        _workers.Clear();
    }

    /// <summary>
    /// Simplified worker implementation that runs on the main thread.
    /// </summary>
    private class SimplifiedWorker : IWorker
    {
        private readonly int _id;
        private readonly WorkerType _workerType;
        private readonly SimplifiedThreadingCoordinator _coordinator;
        private readonly Queue<WorkItem> _workItems = new Queue<WorkItem>();

        private bool _isDisposed;

        public SimplifiedWorker(int id, WorkerType workerType, SimplifiedThreadingCoordinator coordinator)
        {
            _id = id;
            _workerType = workerType;
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        /// <summary>
        /// Gets the worker ID.
        /// </summary>
        public int Id => _id;

        /// <summary>
        /// Gets the worker type.
        /// </summary>
        public WorkerType WorkerType => _workerType;

        /// <summary>
        /// Gets whether the worker has pending work items.
        /// </summary>
        public bool IsBusy => _workItems.Count > 0;

        /// <summary>
        /// Posts work to be executed by the worker.
        /// </summary>
        public void PostWork(Action workAction)
        {
            PostWork(workAction, null);
        }

        /// <summary>
        /// Posts work to be executed by the worker with completion callback.
        /// </summary>
        public void PostWork(Action workAction, Action<bool>? completionCallback)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SimplifiedWorker));

            if (workAction == null)
                throw new ArgumentNullException(nameof(workAction));

            // Queue the work item
            _workItems.Enqueue(new WorkItem(workAction, completionCallback));
        }

        /// <summary>
        /// Posts work to be executed by the worker and returns a task.
        /// </summary>
        public Task PostWorkAsync(Action workAction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SimplifiedWorker));

            if (workAction == null)
                throw new ArgumentNullException(nameof(workAction));

            var tcs = new TaskCompletionSource<bool>();

            PostWork(workAction, success =>
            {
                if (success)
                    tcs.TrySetResult(true);
                else
                    tcs.TrySetCanceled();
            });

            return tcs.Task;
        }

        /// <summary>
        /// Cancels all pending work.
        /// </summary>
        public void CancelPendingWork()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SimplifiedWorker));

            // Notify completion callbacks that work was cancelled
            while (_workItems.Count > 0)
            {
                var workItem = _workItems.Dequeue();
                workItem.CompletionCallback?.Invoke(false);
            }
        }

        /// <summary>
        /// Processes pending work items.
        /// </summary>
        internal void ProcessPendingWork(int maxItems)
        {
            if (_isDisposed)
                return;

            int count = Math.Min(_workItems.Count, maxItems);

            for (int i = 0; i < count; i++)
            {
                if (_workItems.Count == 0)
                    break;

                var workItem = _workItems.Dequeue();
                try
                {
                    workItem.Action();
                    workItem.CompletionCallback?.Invoke(true);
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.Error.WriteLine($"Error processing work item: {ex}");
                    workItem.CompletionCallback?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// Disposes the worker.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            CancelPendingWork();
        }

        /// <summary>
        /// Represents a work item to be executed by the worker.
        /// </summary>
        private class WorkItem
        {
            public WorkItem(Action action, Action<bool>? completionCallback)
            {
                Action = action;
                CompletionCallback = completionCallback;
            }

            public Action Action { get; }
            public Action<bool>? CompletionCallback { get; }
        }
    }
}