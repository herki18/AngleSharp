using System;

namespace LayoutEngine.Contracts.Platform.Lifecycle.Events;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Represents the type of phase change.
/// </summary>
public enum PhaseChangeType
{
    /// <summary>
    /// Entering a phase.
    /// </summary>
    Enter,

    /// <summary>
    /// Exiting a phase.
    /// </summary>
    Exit
}

/// <summary>
/// Event raised when the document lifecycle phase changes.
/// </summary>
public class PhaseChangedEvent : IPrioritizedEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the priority of this event.
    /// </summary>
    public EventPriority Priority => EventPriority.High;

    /// <summary>
    /// Gets the lifecycle phase.
    /// </summary>
    public DocumentLifecyclePhase Phase { get; }

    /// <summary>
    /// Gets the type of phase change.
    /// </summary>
    public PhaseChangeType ChangeType { get; }

    /// <summary>
    /// Gets a value indicating whether the phase had changes.
    /// Only applicable for Exit change type.
    /// </summary>
    public bool HasChanges { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhaseChangedEvent"/> class.
    /// </summary>
    /// <param name="phase">The lifecycle phase.</param>
    /// <param name="changeType">The type of phase change.</param>
    /// <param name="hasChanges">Whether the phase had changes.</param>
    public PhaseChangedEvent(
        DocumentLifecyclePhase phase,
        PhaseChangeType changeType,
        bool hasChanges = false)
    {
        Phase = phase;
        ChangeType = changeType;
        HasChanges = hasChanges;
    }
}

/// <summary>
/// Event raised when the document lifecycle state changes.
/// </summary>
public class LifecycleStateChangedEvent : IPrioritizedEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the priority of this event.
    /// </summary>
    public EventPriority Priority => EventPriority.High;

    /// <summary>
    /// Gets the old state.
    /// </summary>
    public DocumentLifecycleState OldState { get; }

    /// <summary>
    /// Gets the new state.
    /// </summary>
    public DocumentLifecycleState NewState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LifecycleStateChangedEvent"/> class.
    /// </summary>
    /// <param name="oldState">The old state.</param>
    /// <param name="newState">The new state.</param>
    public LifecycleStateChangedEvent(
        DocumentLifecycleState oldState,
        DocumentLifecycleState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

/// <summary>
/// Event raised when the document is ready.
/// </summary>
public class DocumentReadyEvent : IPrioritizedEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the priority of this event.
    /// </summary>
    public EventPriority Priority => EventPriority.Critical;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentReadyEvent"/> class.
    /// </summary>
    public DocumentReadyEvent()
    {
    }
}

/// <summary>
/// Event raised when the engine is shutting down.
/// </summary>
public class ShutdownEvent : IPrioritizedEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the priority of this event.
    /// </summary>
    public EventPriority Priority => EventPriority.Critical;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShutdownEvent"/> class.
    /// </summary>
    public ShutdownEvent()
    {
    }
}