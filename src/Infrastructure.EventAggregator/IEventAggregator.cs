namespace Infrastructure.EventAggregator;

using System;

public interface IEventAggregator
{
    void Publish<TEvent>(TEvent eventData) where TEvent : class, IEvent;
    IObservable<TEvent> GetObservable<TEvent>() where TEvent : class, IEvent;
    ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent;
    ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler) where TEvent : class, IEvent;
    void Unsubscribe(ISubscriptionToken token);
    void ClearSubscriptions<TEvent>() where TEvent : class, IEvent;
}