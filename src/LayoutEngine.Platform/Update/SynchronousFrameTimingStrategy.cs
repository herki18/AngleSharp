namespace LayoutEngine.Platform.Update;

using System;
using LayoutEngine.Contracts.Platform.Abstractions;
using LayoutEngine.Contracts.Platform.Updates;

/// <summary>
/// A simplified frame timing strategy that executes frames synchronously.
/// </summary>
public class SynchronousFrameTimingStrategy : IFrameTimingStrategy
{
    private readonly ITimeProvider _timeProvider;
    private readonly double _frameIntervalMs;

    /// <summary>
    /// Creates a new instance of SynchronousFrameTimingStrategy.
    /// </summary>
    /// <param name="timeProvider">The time provider to use for timestamps.</param>
    /// <param name="targetFps">Target frames per second (default: 60fps)</param>
    public SynchronousFrameTimingStrategy(ITimeProvider timeProvider, int targetFps = 60)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _frameIntervalMs = 1000.0 / targetFps;
    }

    /// <summary>
    /// Gets whether the frame execution should happen synchronously.
    /// Always returns true for this implementation.
    /// </summary>
    public bool IsSynchronousModeEnabled => true;

    /// <summary>
    /// Gets the current time in milliseconds using the time provider.
    /// </summary>
    public double GetCurrentTimeMs()
    {
        return _timeProvider.GetCurrentTimeMilliseconds();
    }

    /// <summary>
    /// Requests the next animation frame to be scheduled.
    /// In this synchronous implementation, it executes the frame action immediately.
    /// </summary>
    /// <param name="frameAction">The action to execute on the next frame.</param>
    public void RequestNextFrame(Action frameAction)
    {
        if (frameAction == null)
            throw new ArgumentNullException(nameof(frameAction));

        // Execute the frame action immediately
        frameAction();
    }
}