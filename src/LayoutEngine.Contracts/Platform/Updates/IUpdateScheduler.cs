using System.Threading.Tasks;

namespace LayoutEngine.Contracts.Platform.Updates;

using System.Threading;

/// <summary>
/// Schedules and prioritizes visual updates for processing.
/// </summary>
public interface IUpdateScheduler
{
    /// <summary>
    /// Schedules a visual update with normal priority.
    /// </summary>
    /// <param name="update">The update to schedule.</param>
    void ScheduleUpdate(IVisualUpdate update);

    /// <summary>
    /// Schedules a visual update with a specific priority.
    /// </summary>
    /// <param name="update">The update to schedule.</param>
    /// <param name="priority">The priority level for the update.</param>
    void ScheduleUpdate(IVisualUpdate update, UpdatePriority priority);

    /// <summary>
    /// Attempts to cancel a pending update before it's processed.
    /// </summary>
    /// <param name="update">The update to cancel.</param>
    /// <returns>True if the update was found and canceled, otherwise false.</returns>
    bool CancelUpdate(IVisualUpdate update);

    /// <summary>
    /// Temporarily pauses the processing of scheduled updates. Updates can still be queued.
    /// </summary>
    void PauseUpdates();

    /// <summary>
    /// Resumes the processing of scheduled updates.
    /// </summary>
    void ResumeUpdates();

    /// <summary>
    /// Asynchronously processes pending updates within a specified time budget.
    /// This is the preferred method for integration with async workflows.
    /// </summary>
    /// <param name="timeBudgetMilliseconds">The maximum time in milliseconds allowed for processing.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A Task representing the asynchronous processing operation.</returns>
    Task ProcessUpdatesAsync(double timeBudgetMilliseconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously processes pending updates within a specified time budget.
    /// This wraps the async version and blocks until completion. Use with caution.
    /// </summary>
    /// <param name="timeBudgetMilliseconds">The maximum time in milliseconds allowed for processing.</param>
    void ProcessUpdates(double timeBudgetMilliseconds);

    /// <summary>
    /// Gets the total number of updates currently pending in the scheduler across all priorities.
    /// </summary>
    /// <returns>The count of pending updates.</returns>
    int GetPendingUpdateCount();
}