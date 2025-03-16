using System;
using Infrastructure.EventAggregator.API.Events;

namespace Infrastructure.EventAggregator.Internal.Core;

/// <summary>
/// Wrapper class that associates an event with its priority and type information.
/// Used by the PriorityEventQueue to handle different event types uniformly.
/// </summary>
internal class PrioritizedEventWrapper
{
    /// <summary>
    /// Gets the event data.
    /// </summary>
    public object EventData { get; }

    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    public Type EventType { get; }

    /// <summary>
    /// Gets the priority of the event.
    /// </summary>
    public EventPriority Priority { get; }

    /// <summary>
    /// Gets the timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the PrioritizedEventWrapper class.
    /// </summary>
    /// <param name="eventData">The event data.</param>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="priority">The priority of the event.</param>
    public PrioritizedEventWrapper(object eventData, Type eventType, EventPriority priority)
    {
        EventData = eventData ?? throw new ArgumentNullException(nameof(eventData));
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Priority = priority;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new instance of PrioritizedEventWrapper from an event implementing IPrioritizedEvent.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventData">The event data.</param>
    /// <returns>A new PrioritizedEventWrapper instance.</returns>
    public static PrioritizedEventWrapper FromEvent<TEvent>(TEvent eventData) where TEvent : class, IEvent
    {
        EventPriority priority = EventPriority.Normal;

        if (eventData is IPrioritizedEvent prioritizedEvent)
        {
            priority = prioritizedEvent.Priority;
        }

        return new PrioritizedEventWrapper(eventData, typeof(TEvent), priority);
    }
}