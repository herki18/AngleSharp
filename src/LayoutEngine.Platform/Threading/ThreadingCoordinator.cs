using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LayoutEngine.Platform.Threading;

using Contracts.Platform.Threading;

public class ThreadingCoordinator : IThreadingCoordinator, IDisposable
{
    private readonly SynchronizationContext _mainThreadContext;
    private readonly SynchronizationContext? _renderThreadContext;
    private readonly ConcurrentDictionary<int, WorkerInfo> _workers = new();
    private readonly IThreadPool _threadPool;
    private readonly int _mainThreadId;
    private bool _isDisposed;
    private bool _synchronousMode;
    private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
    private readonly Queue<Action> _renderThreadQueue = new Queue<Action>();
    private readonly Queue<Action> _workerThreadQueue = new Queue<Action>();

    public ThreadingCoordinator(IThreadPool threadPool)
    {
        _threadPool = threadPool ?? throw new ArgumentNullException(nameof(threadPool));
        _mainThreadContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        _renderThreadContext = CreateRenderThreadContext();
    }

    public SynchronizationContext MainThreadContext => _mainThreadContext;
    public SynchronizationContext RenderThreadContext => _renderThreadContext ?? _mainThreadContext;

    // Added for testing
    public void EnableSynchronousMode(bool enabled)
    {
        _synchronousMode = enabled;
    }

    public void ScheduleOnMainThread(Action action)
    {
        ThrowIfDisposed();
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (_synchronousMode)
        {
            _mainThreadQueue.Enqueue(action);
            return;
        }

        if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            action();
        }
        else
        {
            _mainThreadContext.Post(_ => action(), null);
        }
    }

    public void ScheduleOnRenderThread(Action action)
    {
        ThrowIfDisposed();
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (_synchronousMode)
        {
            _renderThreadQueue.Enqueue(action);
            return;
        }

        if (_renderThreadContext == _mainThreadContext && Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            action();
        }
        else
        {
            _renderThreadContext?.Post(_ => action(), null);
        }
    }

    public void ScheduleOnWorkerThread(Action action)
    {
        ThrowIfDisposed();
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (_synchronousMode)
        {
            _workerThreadQueue.Enqueue(action);
            return;
        }

        _threadPool.QueueWorkItem(action);
    }

    // Added for testing - executes all queued actions synchronously
    public void ExecuteQueuedActionsSync()
    {
        if (!_synchronousMode)
            throw new InvalidOperationException("Synchronous mode must be enabled to execute queued actions synchronously");

        // Process main thread queue
        while (_mainThreadQueue.Count > 0)
        {
            var action = _mainThreadQueue.Dequeue();
            action();
        }

        // Process render thread queue
        while (_renderThreadQueue.Count > 0)
        {
            var action = _renderThreadQueue.Dequeue();
            action();
        }

        // Process worker thread queue
        while (_workerThreadQueue.Count > 0)
        {
            var action = _workerThreadQueue.Dequeue();
            action();
        }
    }

    public IWorker CreateWorker(WorkerType workerType)
    {
        ThrowIfDisposed();
        var worker = new Worker(workerType, this, _synchronousMode);
        _workers[worker.Id] = new WorkerInfo(worker, workerType);
        return worker;
    }

    // For testability
    internal int MainThreadQueueCount => _mainThreadQueue.Count;
    internal int RenderThreadQueueCount => _renderThreadQueue.Count;
    internal int WorkerThreadQueueCount => _workerThreadQueue.Count;
    internal bool IsSynchronousModeEnabled => _synchronousMode;

    // Changed from private to protected virtual for testability
    protected virtual SynchronizationContext? CreateRenderThreadContext()
    {
        return null;
    }

    protected virtual void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ThreadingCoordinator));
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        foreach (var workerInfo in _workers.Values)
        {
            workerInfo.Worker.Dispose();
        }
        _workers.Clear();
    }

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

    // Updated Worker class to support synchronous mode
    internal class Worker : IWorker
    {
        private readonly WorkerType _workerType;
        private readonly ThreadingCoordinator _coordinator;
        private readonly ConcurrentQueue<WorkItem> _workItems = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly int _id;
        private readonly bool _synchronousMode;
        private volatile bool _isBusy;
        private volatile bool _isDisposed;

        public Worker(WorkerType workerType, ThreadingCoordinator coordinator, bool synchronousMode)
        {
            _workerType = workerType;
            _coordinator = coordinator;
            _synchronousMode = synchronousMode;
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

            if (_synchronousMode)
                return; // In synchronous mode, don't automatically schedule processing

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

        // Added for testing - processes all work synchronously
        public void ProcessWorkSynchronously()
        {
            if (!_synchronousMode)
                throw new InvalidOperationException("Synchronous mode must be enabled to process work synchronously");

            ProcessWorkItems();
        }

        public void CancelPendingWork()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(Worker));

            while (_workItems.TryDequeue(out var workItem))
            {
                workItem.CompletionCallback?.Invoke(false);
            }
        }

        // For testability
        internal int PendingWorkItemCount => _workItems.Count;

        private void ScheduleWorkProcessing()
        {
            if (_isBusy || _workItems.IsEmpty)
                return;

            _isBusy = true;
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
                if (!_workItems.IsEmpty && !_isDisposed && !_cts.IsCancellationRequested && !_synchronousMode)
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
            CancelPendingWork();
            try
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            catch
            {
                // Ignore exceptions during dispose
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