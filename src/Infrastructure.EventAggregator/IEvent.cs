namespace Infrastructure.EventAggregator;

using System;

public interface IEvent
{
    Guid Id { get; }
    DateTime Timestamp { get; }
}