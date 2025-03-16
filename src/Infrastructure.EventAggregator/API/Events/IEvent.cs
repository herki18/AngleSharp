namespace Infrastructure.EventAggregator.API.Events;

using System;

public interface IEvent
{
    Guid Id { get; }
    DateTime Timestamp { get; }
}