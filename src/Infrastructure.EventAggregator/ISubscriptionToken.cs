namespace Infrastructure.EventAggregator;

using System;

public interface ISubscriptionToken : IDisposable
{
    Type EventType { get; }
}