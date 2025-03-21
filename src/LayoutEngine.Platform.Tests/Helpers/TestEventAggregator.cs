namespace LayoutEngine.Platform.Tests.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Complete implementation of IEventAggregator for testing
/// </summary>
public class TestEventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<HandlerInfo>> _handlers = new Dictionary<Type, List<HandlerInfo>>();
    private int _tokenCounter;

    /// <summary>
    /// Gets all events that have been published
    /// </summary>
    public List<IEvent> PublishedEvents { get; } = new List<IEvent>();

    /// <summary>
    /// Publishes an event with normal priority
    /// </summary>
    public void Publish<TEvent>(TEvent eventData) where TEvent : class, IEvent
    {
        Publish(eventData, EventPriority.Normal);
    }

    /// <summary>
    /// Publishes an event with the specified priority
    /// </summary>
    public void Publish<TEvent>(TEvent eventData, EventPriority priority) where TEvent : class, IEvent
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        PublishedEvents.Add(eventData);

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
            return;

        // If it's a prioritized event, use its own priority
        var effectivePriority = priority;
        if (eventData is IPrioritizedEvent prioritizedEvent)
        {
            effectivePriority = prioritizedEvent.Priority;
        }

        // Get handlers that match the priority
        var matchingHandlers = handlers
            .Where(h => h.MinimumPriority <= effectivePriority)
            .ToList();

        foreach (var handler in matchingHandlers)
        {
            if (handler.Filter == null || ((Predicate<TEvent>)handler.Filter)(eventData))
            {
                ((Action<TEvent>)handler.Handler)(eventData);
            }
        }
    }

    /// <summary>
    /// Gets an observable for events of the specified type
    /// </summary>
    public IObservable<TEvent> GetObservable<TEvent>() where TEvent : class, IEvent
    {
        return new EventObservable<TEvent>(this);
    }

    /// <summary>
    /// Subscribes to events of the specified type
    /// </summary>
    public ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent
    {
        return Subscribe<TEvent>(null, handler);
    }

    /// <summary>
    /// Subscribes to events of the specified type with a filter
    /// </summary>
    public ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler) where TEvent : class, IEvent
    {
        return SubscribeInternal(filter, handler, EventPriority.Low);
    }

    /// <summary>
    /// Subscribes to events of the specified type with a minimum priority
    /// </summary>
    public ISubscriptionToken SubscribeWithPriority<TEvent>(Action<TEvent> handler, EventPriority minimumPriority) where TEvent : class, IEvent
    {
        return SubscribeInternal(null, handler, minimumPriority);
    }

    /// <summary>
    /// Unsubscribes using the provided token
    /// </summary>
    public void Unsubscribe(ISubscriptionToken token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        if (token is not TestSubscriptionToken testToken)
            return;

        if (_handlers.TryGetValue(testToken.EventType, out var handlers))
        {
            var handler = handlers.FirstOrDefault(h => h.Token.Id == testToken.Id);
            if (handler != null)
            {
                handlers.Remove(handler);
            }
        }
    }

    /// <summary>
    /// Clears all subscriptions for the specified event type
    /// </summary>
    public void ClearSubscriptions<TEvent>() where TEvent : class, IEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers.Clear();
        }
    }

    /// <summary>
    /// Gets all published events of a specific type
    /// </summary>
    public List<TEvent> GetPublishedEvents<TEvent>() where TEvent : class, IEvent
    {
        return PublishedEvents.OfType<TEvent>().ToList();
    }

    /// <summary>
    /// Clears the list of published events
    /// </summary>
    public void ClearPublishedEvents()
    {
        PublishedEvents.Clear();
    }

    /// <summary>
    /// Manually triggers an event for testing
    /// </summary>
    public void TriggerEvent<TEvent>(TEvent eventData) where TEvent : class, IEvent
    {
        Publish(eventData);
    }

    /// <summary>
    /// Internal helper to subscribe to events
    /// </summary>
    private ISubscriptionToken SubscribeInternal<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler, EventPriority minimumPriority)
        where TEvent : class, IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers = new List<HandlerInfo>();
            _handlers[typeof(TEvent)] = handlers;
        }

        var token = new TestSubscriptionToken(_tokenCounter++, typeof(TEvent));
        handlers.Add(new HandlerInfo
        {
            Handler = handler,
            Filter = filter,
            Token = token,
            MinimumPriority = minimumPriority
        });

        return token;
    }

    /// <summary>
    /// Class to store handler information
    /// </summary>
    private class HandlerInfo
    {
        public object Handler { get; init; } = null!;
        public object? Filter { get; init; }
        public TestSubscriptionToken Token { get; init; } = null!;
        public EventPriority MinimumPriority { get; init; }
    }

    /// <summary>
    /// Implementation of subscription token for testing
    /// </summary>
    private class TestSubscriptionToken : ISubscriptionToken
    {
        public int Id { get; }
        public Type EventType { get; }

        public TestSubscriptionToken(int id, Type eventType)
        {
            Id = id;
            EventType = eventType;
        }

        public void Dispose()
        {
            // Normally would unsubscribe, but we'll let the test control this
        }
    }

    /// <summary>
    /// Simple observable implementation for testing
    /// </summary>
    private class EventObservable<TEvent> : IObservable<TEvent> where TEvent : class, IEvent
    {
        private readonly TestEventAggregator _eventAggregator;

        public EventObservable(TestEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            var token = _eventAggregator.Subscribe<TEvent>(e => observer.OnNext(e));
            return new SubscriptionDisposable(_eventAggregator, token);
        }

        private class SubscriptionDisposable : IDisposable
        {
            private readonly TestEventAggregator _eventAggregator;
            private readonly ISubscriptionToken _token;
            private bool _disposed;

            public SubscriptionDisposable(TestEventAggregator eventAggregator, ISubscriptionToken token)
            {
                _eventAggregator = eventAggregator;
                _token = token;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _eventAggregator.Unsubscribe(_token);
                    _disposed = true;
                }
            }
        }
    }
}