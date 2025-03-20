namespace Infrastructure.EventAggregator.API.Events;

using System;

public class EventBase : IEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

}