using System;
using Infrastructure.EventAggregator.API.Events;
using LayoutEngine.Contracts.Platform.Updates;

namespace LayoutEngine.Contracts.Platform.Events;

/// <summary>
/// Event fired when a visual update has been processed.
/// </summary>
public class UpdateProcessedEvent : EventBase
{
    /// <summary>
    /// Gets the visual update that was processed.
    /// </summary>
    public IVisualUpdate Update { get; }

    /// <summary>
    /// Gets the time it took to process the update in milliseconds.
    /// </summary>
    public double ProcessingTimeMs { get; }

    /// <summary>
    /// Gets whether the update was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets any error that occurred during processing.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProcessedEvent"/> class.
    /// </summary>
    /// <param name="update">The visual update that was processed.</param>
    /// <param name="processingTimeMs">The time it took to process the update in milliseconds.</param>
    /// <param name="success">Whether the update was successful.</param>
    /// <param name="error">Any error that occurred during processing.</param>
    public UpdateProcessedEvent(
        IVisualUpdate update,
        double processingTimeMs = 0,
        bool success = true,
        Exception? error = null)
    {
        Update = update ?? throw new ArgumentNullException(nameof(update));
        ProcessingTimeMs = processingTimeMs;
        Success = success;
        Error = error;
    }
}