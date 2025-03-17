namespace LayoutEngine.Contracts.Platform.Updates;

/// <summary>
/// Defines the types of updates that can be scheduled.
/// </summary>
public enum UpdateType
{
    /// <summary>
    /// Style update.
    /// </summary>
    Style,

    /// <summary>
    /// Layout update.
    /// </summary>
    Layout,

    /// <summary>
    /// Paint update.
    /// </summary>
    Paint
}

/// <summary>
/// Schedules and coordinates visual updates.
/// </summary>
public interface IUpdateScheduler
{
    /// <summary>
    /// Determines whether there are pending updates of the specified type.
    /// </summary>
    /// <param name="updateType">The update type to check.</param>
    /// <returns>true if there are pending updates; otherwise, false.</returns>
    bool HasPendingUpdates(UpdateType updateType);

    /// <summary>
    /// Schedules an update of the specified type.
    /// </summary>
    /// <param name="updateType">The update type to schedule.</param>
    void ScheduleUpdate(UpdateType updateType);

    /// <summary>
    /// Processes an update of the specified type.
    /// </summary>
    /// <param name="updateType">The update type to process.</param>
    void ProcessUpdate(UpdateType updateType);

    /// <summary>
    /// Waits for update processing to complete.
    /// </summary>
    void WaitForUpdateProcessing();
}