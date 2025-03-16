using System;

namespace Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Base class for prioritized events.
/// Provides default implementation of IEvent and IPrioritizedEvent interfaces.
/// </summary>
public abstract class PrioritizedEventBase : IPrioritizedEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the priority level of this event.
    /// </summary>
    public EventPriority Priority { get; }

    /// <summary>
    /// Initializes a new instance of the PrioritizedEventBase class.
    /// </summary>
    /// <param name="priority">The priority level for this event. Defaults to Normal.</param>
    protected PrioritizedEventBase(EventPriority priority = EventPriority.Normal)
    {
        Priority = priority;
    }
}