namespace Infrastructure.EventAggregator.API.Aggregation;

using System;

public interface ISubscriptionToken : IDisposable
{
    Type EventType { get; }
}