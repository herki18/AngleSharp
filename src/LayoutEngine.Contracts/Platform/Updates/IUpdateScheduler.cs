namespace LayoutEngine.Contracts.Platform.Updates;

using LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Schedules and prioritizes visual updates.
/// </summary>
public interface IUpdateScheduler
{
    /// <summary>
    /// Schedules a visual update.
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
    /// Cancels a pending update.
    /// </summary>
    /// <param name="update">The update to cancel.</param>
    /// <returns>True if the update was canceled, otherwise false.</returns>
    bool CancelUpdate(IVisualUpdate update);

    /// <summary>
    /// Pauses all update processing.
    /// </summary>
    void PauseUpdates();

    /// <summary>
    /// Resumes update processing.
    /// </summary>
    void ResumeUpdates();

    /// <summary>
    /// Processes pending updates within a specified time budget.
    /// </summary>
    /// <param name="timeBudgetMilliseconds">The maximum time in milliseconds allowed for processing.</param>
    void ProcessUpdates(double timeBudgetMilliseconds);
}