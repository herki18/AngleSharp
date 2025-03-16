namespace Infrastructure.EventAggregator.API.Aggregation;

using System;
using Events;

/// <summary>
/// Interface for the event aggregator, providing methods for publishing and subscribing to events.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventData">The event data.</param>
    void Publish<TEvent>(TEvent eventData) where TEvent : class, IEvent;

    /// <summary>
    /// Publishes an event with the specified priority.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventData">The event data.</param>
    /// <param name="priority">The priority to use when publishing the event.</param>
    void Publish<TEvent>(TEvent eventData, EventPriority priority) where TEvent : class, IEvent;

    /// <summary>
    /// Gets an observable for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <returns>An observable for the event type.</returns>
    IObservable<TEvent> GetObservable<TEvent>() where TEvent : class, IEvent;

    /// <summary>
    /// Subscribes to an event.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The event handler.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent;

    /// <summary>
    /// Subscribes to an event with a filter.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="filter">The filter to apply to events.</param>
    /// <param name="handler">The event handler.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler) where TEvent : class, IEvent;

    /// <summary>
    /// Subscribes to events with the minimum priority specified.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The event handler.</param>
    /// <param name="minimumPriority">The minimum priority of events to receive.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    ISubscriptionToken SubscribeWithPriority<TEvent>(Action<TEvent> handler, EventPriority minimumPriority)
        where TEvent : class, IEvent;

    /// <summary>
    /// Unsubscribes from an event.
    /// </summary>
    /// <param name="token">The subscription token.</param>
    void Unsubscribe(ISubscriptionToken token);

    /// <summary>
    /// Clears all subscriptions for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    void ClearSubscriptions<TEvent>() where TEvent : class, IEvent;
}