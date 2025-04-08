namespace LayoutEngine.Contracts.Platform.Updates;

using System;

/// <summary>
/// Platform abstraction for frame timing and scheduling.
/// </summary>
public interface IFrameTimingStrategy
{
    /// <summary>
    /// Gets whether the frame execution should happen synchronously.
    /// </summary>
    bool IsSynchronousModeEnabled { get; }

    /// <summary>
    /// Gets the current time in milliseconds using the platform's timing system.
    /// </summary>
    double GetCurrentTimeMs();

    /// <summary>
    /// Requests the next animation frame to be scheduled.
    /// </summary>
    /// <param name="frameAction">The action to execute on the next frame.</param>
    void RequestNextFrame(Action frameAction);
}