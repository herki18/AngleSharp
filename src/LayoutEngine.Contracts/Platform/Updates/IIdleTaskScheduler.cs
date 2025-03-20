using System;
using System.Threading;
using System.Threading.Tasks;

namespace LayoutEngine.Contracts.Platform.Updates;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Schedules and executes tasks during idle periods in the application.
/// </summary>
public interface IIdleTaskScheduler
{
    /// <summary>
    /// Schedules a task to be executed during an idle period.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>An idle task that can be used to cancel the operation.</returns>
    IIdleTask ScheduleIdleTask(Action action, IdleTaskPriority priority = IdleTaskPriority.Normal);

    /// <summary>
    /// Schedules a task to be executed during an idle period with cancellation support.
    /// </summary>
    /// <param name="action">The action to execute with cancellation token.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>An idle task that can be used to cancel the operation.</returns>
    IIdleTask ScheduleIdleTask(Action<CancellationToken> action, IdleTaskPriority priority = IdleTaskPriority.Normal);

    /// <summary>
    /// Schedules a task to be executed during an idle period and returns its result asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<TResult> ScheduleIdleTaskAsync<TResult>(Func<TResult> function, IdleTaskPriority priority = IdleTaskPriority.Normal);

    /// <summary>
    /// Schedules a task to be executed during an idle period with cancellation support and returns its result asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="function">The function to execute with cancellation token.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<TResult> ScheduleIdleTaskAsync<TResult>(Func<CancellationToken, TResult> function, IdleTaskPriority priority = IdleTaskPriority.Normal);

    /// <summary>
    /// Cancels a scheduled task.
    /// </summary>
    /// <param name="taskId">The ID of the task to cancel.</param>
    void CancelTask(Guid taskId);

    /// <summary>
    /// Pauses the execution of idle tasks.
    /// </summary>
    void PauseTasks();

    /// <summary>
    /// Resumes the execution of idle tasks.
    /// </summary>
    void ResumeTasks();

    /// <summary>
    /// Gets the number of pending tasks.
    /// </summary>
    int PendingTaskCount { get; }
}

/// <summary>
/// Represents a task scheduled to run during idle periods.
/// </summary>
public interface IIdleTask : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this task.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the priority of this task.
    /// </summary>
    IdleTaskPriority Priority { get; }

    /// <summary>
    /// Gets whether this task has been canceled.
    /// </summary>
    bool IsCanceled { get; }

    /// <summary>
    /// Cancels this task.
    /// </summary>
    void Cancel();
}

/// <summary>
/// Defines the priority levels for idle tasks.
/// </summary>
public enum IdleTaskPriority
{
    /// <summary>
    /// Low priority tasks that are executed only when there is significant idle time.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority tasks that are executed during regular idle periods.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority tasks that are executed as soon as any idle time is available.
    /// </summary>
    High = 20
}

/// <summary>
/// Event raised when an idle task is completed.
/// </summary>
public class IdleTaskCompletedEvent : EventBase
{
    /// <summary>
    /// Gets the ID of the completed task.
    /// </summary>
    public Guid TaskId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdleTaskCompletedEvent"/> class.
    /// </summary>
    /// <param name="taskId">The ID of the completed task.</param>
    public IdleTaskCompletedEvent(Guid taskId)
    {
        TaskId = taskId;
    }
}