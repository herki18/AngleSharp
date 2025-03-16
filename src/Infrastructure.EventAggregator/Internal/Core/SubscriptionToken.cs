namespace Infrastructure.EventAggregator.Internal.Core;

using System;
using Infrastructure.EventAggregator.API.Aggregation;

public class SubscriptionToken : ISubscriptionToken
{
    private readonly Type _eventType;
    private readonly Action _unsubscribeAction;
    private bool _isDisposed;

    public Type EventType => _eventType;

    public SubscriptionToken(Type eventType, Action unsubscribeAction)
    {
        _eventType = eventType;
        _unsubscribeAction = unsubscribeAction;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _unsubscribeAction?.Invoke();
    }
}