namespace LayoutEngine.Contracts.Platform.Updates;

using System;

/// <summary>
/// Schedules animation frames.
/// </summary>
public interface IFrameScheduler
{
    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    long CurrentFrameNumber { get; }

    /// <summary>
    /// Gets the time of the last frame.
    /// </summary>
    double LastFrameTime { get; }

    /// <summary>
    /// Schedules a callback for the next animation frame.
    /// </summary>
    /// <param name="callback">The callback to invoke when the frame starts.</param>
    /// <returns>A request ID that can be used to cancel the callback.</returns>
    int RequestAnimationFrame(Action<double> callback);

    /// <summary>
    /// Cancels a scheduled animation frame callback.
    /// </summary>
    /// <param name="requestId">The request ID of the callback to cancel.</param>
    void CancelAnimationFrame(int requestId);
}