namespace Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Defines priority levels for events in the system.
/// Higher priority events are processed before lower priority events.
/// </summary>
public enum EventPriority
{
    /// <summary>
    /// Low priority events, processed after normal events.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority events, the default level.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority events, processed before normal events.
    /// </summary>
    High = 20,

    /// <summary>
    /// Critical priority events, processed before all other events.
    /// </summary>
    Critical = 30
}