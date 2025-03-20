namespace LayoutEngine.Contracts.Platform.Updates;

using System;
using System.Collections.Generic;
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
}

/// <summary>
/// Schedules animation frames.
/// </summary>
public interface IFrameScheduler
{
    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    long CurrentFrameNumber { get; }

    /// <summary>
    /// Gets the time of the last frame.
    /// </summary>
    double LastFrameTime { get; }

    /// <summary>
    /// Schedules a callback for the next animation frame.
    /// </summary>
    /// <param name="callback">The callback to invoke when the frame starts.</param>
    /// <returns>A request ID that can be used to cancel the callback.</returns>
    int RequestAnimationFrame(Action<double> callback);

    /// <summary>
    /// Cancels a scheduled animation frame callback.
    /// </summary>
    /// <param name="requestId">The request ID of the callback to cancel.</param>
    void CancelAnimationFrame(int requestId);
}

/// <summary>
/// Represents a visual update to be scheduled.
/// </summary>
public interface IVisualUpdate
{
    /// <summary>
    /// Gets the update ID.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the update type.
    /// </summary>
    UpdateType Type { get; }

    /// <summary>
    /// Gets the element to update.
    /// </summary>
    IElement Element { get; }

    /// <summary>
    /// Gets the properties that were changed.
    /// </summary>
    IReadOnlyList<string> ChangedProperties { get; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// Represents an idle deadline.
/// </summary>
public interface IdleDeadline
{
    /// <summary>
    /// Gets the time remaining in the idle period.
    /// </summary>
    TimeSpan TimeRemaining { get; }

    /// <summary>
    /// Gets whether the callback is being called because the timeout fired.
    /// </summary>
    bool DidTimeout { get; }
}

/// <summary>
/// Defines the type of visual update.
/// </summary>
public enum UpdateType
{
    /// <summary>
    /// Style-only update.
    /// </summary>
    Style,

    /// <summary>
    /// Layout update (may include style changes).
    /// </summary>
    Layout,

    /// <summary>
    /// Render update (may include style and layout changes).
    /// </summary>
    Render,

    /// <summary>
    /// Resource-related update.
    /// </summary>
    Resource,

    /// <summary>
    /// Full update of all aspects.
    /// </summary>
    Full
}

/// <summary>
/// Defines the priority of a visual update.
/// </summary>
public enum UpdatePriority
{
    /// <summary>
    /// Low priority updates.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority updates.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority updates.
    /// </summary>
    High = 20,

    /// <summary>
    /// Critical priority updates.
    /// </summary>
    Critical = 30
}