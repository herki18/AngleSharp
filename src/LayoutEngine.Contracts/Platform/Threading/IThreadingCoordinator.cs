namespace LayoutEngine.Contracts.Platform.Threading;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages thread assignments and synchronization.
/// </summary>
public interface IThreadingCoordinator
{
    /// <summary>
    /// Gets the main thread SynchronizationContext.
    /// </summary>
    SynchronizationContext MainThreadContext { get; }

    /// <summary>
    /// Gets the render thread SynchronizationContext.
    /// </summary>
    SynchronizationContext RenderThreadContext { get; }

    /// <summary>
    /// Schedules an action to run on the main thread.
    /// </summary>
    /// <param name="action">The action to schedule.</param>
    void ScheduleOnMainThread(Action action);

    /// <summary>
    /// Schedules an action to run on the render thread.
    /// </summary>
    /// <param name="action">The action to schedule.</param>
    void ScheduleOnRenderThread(Action action);

    /// <summary>
    /// Schedules an action to run on a worker thread.
    /// </summary>
    /// <param name="action">The action to schedule.</param>
    void ScheduleOnWorkerThread(Action action);

    /// <summary>
    /// Creates a worker that runs on an appropriate thread.
    /// </summary>
    /// <param name="workerType">The worker type.</param>
    /// <returns>The created worker.</returns>
    IWorker CreateWorker(WorkerType workerType);
}

/// <summary>
/// Manages a pool of worker threads.
/// </summary>
public interface IThreadPool
{
    /// <summary>
    /// Gets the number of active threads in the pool.
    /// </summary>
    int ActiveThreadCount { get; }

    /// <summary>
    /// Queues a work item for execution on a worker thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the work item.</param>
    void QueueWorkItem(Action action, WorkItemPriority priority = WorkItemPriority.Normal);

    /// <summary>
    /// Queues a work item for execution and returns a task that completes when the work is done.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the work item.</param>
    /// <returns>A task that completes when the work item completes.</returns>
    Task QueueWorkItemAsync(Action action, WorkItemPriority priority = WorkItemPriority.Normal);
}

/// <summary>
/// Represents a worker that performs specific tasks.
/// </summary>
public interface IWorker : IDisposable
{
    /// <summary>
    /// Gets the worker type.
    /// </summary>
    WorkerType WorkerType { get; }

    /// <summary>
    /// Gets whether the worker is busy.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Posts work to the worker.
    /// </summary>
    /// <param name="workAction">The work action.</param>
    void PostWork(Action workAction);

    /// <summary>
    /// Posts work to the worker with a callback.
    /// </summary>
    /// <param name="workAction">The work action.</param>
    /// <param name="completionCallback">The completion callback.</param>
    void PostWork(Action workAction, Action<bool>? completionCallback);

    /// <summary>
    /// Posts work to the worker and returns a task.
    /// </summary>
    /// <param name="workAction">The work action.</param>
    /// <returns>A task that completes when the work completes.</returns>
    Task PostWorkAsync(Action workAction);

    /// <summary>
    /// Cancels all pending work.
    /// </summary>
    void CancelPendingWork();
}

/// <summary>
/// Defines the type of worker thread.
/// </summary>
public enum WorkerType
{
    /// <summary>
    /// General purpose worker.
    /// </summary>
    General,

    /// <summary>
    /// Style calculation worker.
    /// </summary>
    Style,

    /// <summary>
    /// Layout calculation worker.
    /// </summary>
    Layout,

    /// <summary>
    /// Resource loading worker.
    /// </summary>
    Resource,

    /// <summary>
    /// Rendering worker.
    /// </summary>
    Render
}

/// <summary>
/// Defines the priority of a work item.
/// </summary>
public enum WorkItemPriority
{
    /// <summary>
    /// Low priority work items.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority work items.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority work items.
    /// </summary>
    High = 20,

    /// <summary>
    /// Critical priority work items.
    /// </summary>
    Critical = 30
}