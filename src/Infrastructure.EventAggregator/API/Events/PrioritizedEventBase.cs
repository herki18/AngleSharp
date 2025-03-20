using System;

namespace Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Base class for prioritized events.
/// Provides default implementation of IEvent and IPrioritizedEvent interfaces.
/// </summary>
public abstract class PrioritizedEventBase : EventBase, IPrioritizedEvent
{
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