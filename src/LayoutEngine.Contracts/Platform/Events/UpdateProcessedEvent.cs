namespace LayoutEngine.Contracts.Platform.Events;

using System;
using Infrastructure.EventAggregator.API.Events;
using Updates;

/// <summary>
/// Event raised when an update is processed.
/// </summary>
public class UpdateProcessedEvent : EventBase
{
    /// <summary>
    /// Gets the update that was processed.
    /// </summary>
    public IVisualUpdate Update { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProcessedEvent"/> class.
    /// </summary>
    /// <param name="update">The update that was processed.</param>
    public UpdateProcessedEvent(IVisualUpdate update)
    {
        Update = update ?? throw new ArgumentNullException(nameof(update));
    }
}