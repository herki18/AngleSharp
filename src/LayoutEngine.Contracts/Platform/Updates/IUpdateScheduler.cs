using System.Threading.Tasks;

namespace LayoutEngine.Contracts.Platform.Updates;

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
    /// Processes pending updates within a specified time budget.
    /// This method is intended to be called repeatedly by the host loop.
    /// </summary>
    /// <param name="timeBudgetMilliseconds">The maximum time in milliseconds allowed for processing in this call.</param>
    /// <returns>A Task representing the asynchronous processing operation (optional for async schedulers).</returns>
    Task ProcessUpdatesAsync(double timeBudgetMilliseconds);

    /// <summary>
    /// Synchronous version of ProcessUpdatesAsync for simpler schedulers or blocking scenarios.
    /// </summary>
    /// <param name="timeBudgetMilliseconds">The maximum time in milliseconds allowed for processing in this call.</param>
    void ProcessUpdates(double timeBudgetMilliseconds);


    /// <summary>
    /// Gets the total number of updates currently pending in the scheduler across all priorities.
    /// </summary>
    /// <returns>The count of pending updates.</returns>
    int GetPendingUpdateCount();
}