namespace AngleSharp.StyleSystem.Events;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Represents a subscription token for an event
/// </summary>
public class SubscriptionToken : ISubscriptionToken
{
    private readonly Action _unsubscribeAction;
    private bool _isDisposed;

    /// <summary>
    /// Gets the type of event this subscription is for
    /// </summary>
    public Type EventType { get; }

    /// <summary>
    /// Gets whether this subscription token has been disposed
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// Creates a new subscription token
    /// </summary>
    /// <param name="eventType">The type of event</param>
    /// <param name="unsubscribeAction">Action to execute when unsubscribing</param>
    public SubscriptionToken(Type eventType, Action unsubscribeAction)
    {
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
        _isDisposed = false;
    }

    /// <summary>
    /// Disposes the subscription token and unsubscribes from the event
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _unsubscribeAction();
            _isDisposed = true;
        }
    }
}

/// <summary>
/// Represents a token for a subscription to an event
/// </summary>
public interface ISubscriptionToken : IDisposable
{
    /// <summary>
    /// Gets the type of event this subscription is for
    /// </summary>
    Type EventType { get; }

    /// <summary>
    /// Gets whether this subscription token has been disposed
    /// </summary>
    bool IsDisposed { get; }
}

/// <summary>
/// Represents the fundamental event bus for publishing messages and managing subscriptions
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers of the specified event type
    /// </summary>
    void Publish<TEvent>(TEvent eventData) where TEvent : class;

    /// <summary>
    /// Subscribes to events of the specified type
    /// </summary>
    /// <returns>A subscription token that can be used to unsubscribe</returns>
    ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

    /// <summary>
    /// Subscribes to events of the specified type with a filter predicate
    /// </summary>
    /// <returns>A subscription token that can be used to unsubscribe</returns>
    ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler) where TEvent : class;

    /// <summary>
    /// Unsubscribes using a previously obtained subscription token
    /// </summary>
    void Unsubscribe(ISubscriptionToken token);

    /// <summary>
    /// Removes all subscriptions for the specified event type
    /// </summary>
    void ClearSubscriptions<TEvent>() where TEvent : class;

    /// <summary>
    /// Removes all subscriptions across all event types
    /// </summary>
    void ClearAllSubscriptions();
}

/// <summary>
/// Interface for subscription collections to avoid dynamic casting
/// </summary>
internal interface ISubscriptionCollection
{
    /// <summary>
    /// Removes a subscription by ID
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to remove</param>
    /// <returns>True if the subscription was removed, false otherwise</returns>
    bool RemoveSubscription(Guid subscriptionId);

    /// <summary>
    /// Gets whether this collection has any subscriptions
    /// </summary>
    bool HasSubscriptions { get; }
}

/// <summary>
/// A generic implementation of the event aggregator pattern
/// </summary>
public class EventAggregator : IEventAggregator
{
    private class Subscription<TEvent> where TEvent : class
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Predicate<TEvent>? Filter { get; }
        public Action<TEvent> Handler { get; }

        public Subscription(Action<TEvent> handler, Predicate<TEvent>? filter = null)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Filter = filter;
        }

        public bool ShouldHandle(TEvent eventData)
        {
            return Filter == null || Filter(eventData);
        }
    }

    private class SubscriptionCollection<TEvent> : ISubscriptionCollection where TEvent : class
    {
        private readonly ConcurrentDictionary<Guid, Subscription<TEvent>> _subscriptions = new();

        public bool HasSubscriptions => _subscriptions.Count > 0;

        public bool RemoveSubscription(Guid subscriptionId)
        {
            return _subscriptions.TryRemove(subscriptionId, out _);
        }

        public void Add(Subscription<TEvent> subscription)
        {
            _subscriptions.TryAdd(subscription.Id, subscription);
        }

        public IEnumerable<Subscription<TEvent>> GetSubscriptions()
        {
            return _subscriptions.Values;
        }
    }

    private readonly ConcurrentDictionary<Type, ISubscriptionCollection> _subscriptions = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Publishes an event to all subscribers of the specified event type
    /// </summary>
    public void Publish<TEvent>(TEvent eventData) where TEvent : class
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        var eventType = typeof(TEvent);

        // Use a read lock as publishing is more frequent than subscription changes
        _lock.EnterReadLock();
        try
        {
            if (_subscriptions.TryGetValue(eventType, out var subscriptionCollection))
            {
                var typedCollection = (SubscriptionCollection<TEvent>)subscriptionCollection;
                foreach (var subscription in typedCollection.GetSubscriptions())
                {
                    if (subscription.ShouldHandle(eventData))
                    {
                        try
                        {
                            subscription.Handler(eventData);
                        }
                        catch (Exception ex)
                        {
                            // Log exception but don't let it affect other handlers
                            System.Diagnostics.Debug.WriteLine($"Exception in event handler: {ex.Message}");
                        }
                    }
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Subscribes to events of the specified type
    /// </summary>
    /// <returns>A subscription token that can be used to unsubscribe</returns>
    public ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        return Subscribe<TEvent>(null, handler);
    }

    /// <summary>
    /// Subscribes to events of the specified type with a filter predicate
    /// </summary>
    /// <returns>A subscription token that can be used to unsubscribe</returns>
    public ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler) where TEvent : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        var subscription = new Subscription<TEvent>(handler, filter);

        _lock.EnterWriteLock();
        try
        {
            var subscriptionCollection = (SubscriptionCollection<TEvent>)_subscriptions.GetOrAdd(
                eventType,
                _ => new SubscriptionCollection<TEvent>());

            subscriptionCollection.Add(subscription);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return new SubscriptionToken(eventType, () => UnsubscribeInternal(eventType, subscription.Id));
    }

    /// <summary>
    /// Unsubscribes using a previously obtained subscription token
    /// </summary>
    public void Unsubscribe(ISubscriptionToken token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        if (token.IsDisposed)
            return;

        if (token is SubscriptionToken subscriptionToken)
        {
            subscriptionToken.Dispose();
        }
    }

    /// <summary>
    /// Removes all subscriptions for the specified event type
    /// </summary>
    public void ClearSubscriptions<TEvent>() where TEvent : class
    {
        var eventType = typeof(TEvent);

        _lock.EnterWriteLock();
        try
        {
            _subscriptions.TryRemove(eventType, out _);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes all subscriptions across all event types
    /// </summary>
    public void ClearAllSubscriptions()
    {
        _lock.EnterWriteLock();
        try
        {
            _subscriptions.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void UnsubscribeInternal(Type eventType, Guid subscriptionId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_subscriptions.TryGetValue(eventType, out var subscriptionCollection))
            {
                // Remove the subscription
                subscriptionCollection.RemoveSubscription(subscriptionId);

                // Remove the collection if it's empty
                if (!subscriptionCollection.HasSubscriptions)
                {
                    _subscriptions.TryRemove(eventType, out _);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}