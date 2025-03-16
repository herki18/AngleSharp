namespace Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Interface for events that have an assigned priority level.
/// Prioritized events are processed according to their priority level,
/// with higher priority events being processed before lower priority events.
/// </summary>
public interface IPrioritizedEvent : IEvent
{
    /// <summary>
    /// Gets the priority level of this event.
    /// </summary>
    EventPriority Priority { get; }
}