namespace AngleSharp.StyleSystem.Tasks;

using System;

/// <summary>
/// Schedules and prioritizes style tasks.
/// </summary>
public interface IStyleTaskScheduler : IDisposable
{
    /// <summary>
    /// Enqueues a task for execution.
    /// </summary>
    /// <param name="task">The task to enqueue.</param>
    void EnqueueTask(IStyleTask task);

    /// <summary>
    /// Processes pending tasks.
    /// </summary>
    void ProcessTasks();

    /// <summary>
    /// Processes pending tasks asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    System.Threading.Tasks.Task ProcessTasksAsync(System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether there are pending tasks to process.
    /// </summary>
    bool HasPendingTasks { get; }

    /// <summary>
    /// Cancels all pending tasks.
    /// </summary>
    void CancelPendingTasks();
}