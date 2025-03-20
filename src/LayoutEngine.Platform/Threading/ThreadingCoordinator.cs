using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Threading;

namespace LayoutEngine.Platform.Threading;

/// <summary>
/// Manages thread assignments and synchronization.
/// Provides a unified interface for thread scheduling.
/// </summary>
public sealed class ThreadingCoordinator : IThreadingCoordinator, IDisposable
{
    private readonly SynchronizationContext _mainThreadContext;
    private readonly SynchronizationContext? _renderThreadContext;
    private readonly ConcurrentDictionary<int, WorkerInfo> _workers = new();
    private readonly IThreadPool _threadPool;
    private readonly int _mainThreadId;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadingCoordinator"/> class.
    /// </summary>
    /// <param name="threadPool">The thread pool for worker threads.</param>
    public ThreadingCoordinator(IThreadPool threadPool)
    {
        _threadPool = threadPool ?? throw new ArgumentNullException(nameof(threadPool));

        // Store the main thread context
        _mainThreadContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;

        // Create a dedicated render thread if supported
        _renderThreadContext = CreateRenderThreadContext();
    }

    /// <summary>
    /// Gets the main thread SynchronizationContext.
    /// </summary>
    public SynchronizationContext MainThreadContext => _mainThreadContext;

    /// <summary>
    /// Gets the render thread SynchronizationContext.
    /// </summary>
    public SynchronizationContext RenderThreadContext => _renderThreadContext ?? _mainThreadContext;

    /// <summary>
    /// Schedules an action to run on the main thread.
    /// </summary>
    /// <param name="action">The action to schedule.</param>
    public void ScheduleOnMainThread(Action action)
    {
        ThrowIfDisposed();

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            // Already on main thread, execute directly
            action();
        }
        else
        {
            // Post to main thread
            _mainThreadContext.Post(_ => action(), null);
        }
    }

    /// <summary>
    /// Schedules an action to run on the render thread.
    /// </summary>
    /// <param name="action">The action to schedule.</param>
    public void ScheduleOnRenderThread(Action action)
    {
        ThrowIfDisposed();

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        // If render thread is the same as main thread, execute directly if on main thread
        if (_renderThreadContext == _mainThreadContext && Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            action();
        }
        else
        {
            // Post to render thread
            _renderThreadContext?.Post(_ => action(), null);
        }
    }

    /// <summary>
    /// Schedules an action to run on a worker thread.
    /// </summary>
    /// <param name="action">The action to schedule.</param>
    public void ScheduleOnWorkerThread(Action action)
    {
        ThrowIfDisposed();

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _threadPool.QueueWorkItem(action);
    }

    /// <summary>
    /// Creates a worker that runs on an appropriate thread.
    /// </summary>
    /// <param name="workerType">The worker type.</param>
    /// <returns>The created worker.</returns>
    public IWorker CreateWorker(WorkerType workerType)
    {
        ThrowIfDisposed();

        var worker = new Worker(workerType, this);
        _workers[worker.Id] = new WorkerInfo(worker, workerType);
        return worker;
    }

    /// <summary>
    /// Creates the render thread context.
    /// </summary>
    /// <returns>The render thread context, or null if not supported.</returns>
    private SynchronizationContext? CreateRenderThreadContext()
    {
        // In a real implementation, this would create a dedicated render thread
        // For now, just return null to indicate no dedicated render thread
        return null;
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ThreadingCoordinator));
        }
    }

    /// <summary>
    /// Disposes the ThreadingCoordinator and all workers.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Dispose all workers
        foreach (var workerInfo in _workers.Values)
        {
            workerInfo.Worker.Dispose();
        }

        _workers.Clear();
    }

    /// <summary>
    /// Stores information about a worker.
    /// </summary>
    private class WorkerInfo
    {
        public WorkerInfo(Worker worker, WorkerType workerType)
        {
            Worker = worker;
            WorkerType = workerType;
        }

        public Worker Worker { get; }
        public WorkerType WorkerType { get; }
    }

    /// <summary>
    /// Implements a worker that runs on a specific thread.
    /// </summary>
    private class Worker : IWorker
    {
        private readonly WorkerType _workerType;
        private readonly ThreadingCoordinator _coordinator;
        private readonly ConcurrentQueue<WorkItem> _workItems = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly int _id;
        private volatile bool _isBusy;
        private volatile bool _isDisposed;

        public Worker(WorkerType workerType, ThreadingCoordinator coordinator)
        {
            _workerType = workerType;
            _coordinator = coordinator;
            _id = Interlocked.Increment(ref s_workerIdCounter);
        }

        public int Id => _id;

        public WorkerType WorkerType => _workerType;

        public bool IsBusy => _isBusy;

        public void PostWork(Action workAction)
        {
            PostWork(workAction, null);
        }

        public void PostWork(Action workAction, Action<bool>? completionCallback)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(Worker));

            if (workAction == null)
                throw new ArgumentNullException(nameof(workAction));

            var workItem = new WorkItem(workAction, completionCallback);
            _workItems.Enqueue(workItem);

            // Schedule work processing on appropriate thread
            ScheduleWorkProcessing();
        }

        public Task PostWorkAsync(Action workAction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(Worker));

            if (workAction == null)
                throw new ArgumentNullException(nameof(workAction));

            var tcs = new TaskCompletionSource<bool>();

            PostWork(workAction, success =>
            {
                if (success)
                    tcs.SetResult(true);
                else
                    tcs.SetCanceled();
            });

            return tcs.Task;
        }

        public void CancelPendingWork()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(Worker));

            // Clear all pending work items
            while (_workItems.TryDequeue(out var workItem))
            {
                workItem.CompletionCallback?.Invoke(false);
            }
        }

        private void ScheduleWorkProcessing()
        {
            if (_isBusy || _workItems.IsEmpty)
                return;

            _isBusy = true;

            // Schedule work processing on appropriate thread
            switch (_workerType)
            {
                case WorkerType.General:
                case WorkerType.Style:
                case WorkerType.Layout:
                case WorkerType.Resource:
                    _coordinator.ScheduleOnWorkerThread(ProcessWorkItems);
                    break;

                case WorkerType.Render:
                    _coordinator.ScheduleOnRenderThread(ProcessWorkItems);
                    break;
            }
        }

        private void ProcessWorkItems()
        {
            try
            {
                // Process all work items in the queue
                while (_workItems.TryDequeue(out var workItem) && !_isDisposed && !_cts.IsCancellationRequested)
                {
                    try
                    {
                        workItem.Action();
                        workItem.CompletionCallback?.Invoke(true);
                    }
                    catch (Exception)
                    {
                        workItem.CompletionCallback?.Invoke(false);
                        throw;
                    }
                }
            }
            finally
            {
                _isBusy = false;

                // If we still have work items, schedule again
                if (!_workItems.IsEmpty && !_isDisposed && !_cts.IsCancellationRequested)
                {
                    ScheduleWorkProcessing();
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Cancel all pending work
            CancelPendingWork();

            // Cancel and dispose cancellation token source
            try
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            catch
            {
                // Ignore
            }
        }

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

        private static int s_workerIdCounter;
    }
}