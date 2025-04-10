using System;
using LayoutEngine.Contracts.Platform.Updates;

namespace LayoutEngine.Tests;

/// <summary>
/// Minimal test implementation of IFrameTimingStrategy with no dependencies.
/// Useful for unit tests and scenarios where IThreadingCoordinator might not be available.
/// </summary>
public class MinimalTestFrameTimingStrategy : IFrameTimingStrategy
{
    private double _currentTimeMs;

    public MinimalTestFrameTimingStrategy(double initialTimeMs = 1000.0)
    {
        _currentTimeMs = initialTimeMs;
    }

    /// <summary>
    /// Gets whether the frame execution should happen synchronously.
    /// Always returns true for this test implementation.
    /// </summary>
    public bool IsSynchronousModeEnabled => true;

    /// <summary>
    /// Gets the current time in milliseconds.
    /// </summary>
    public double GetCurrentTimeMs() => _currentTimeMs;

    /// <summary>
    /// Requests the next animation frame to be scheduled.
    /// In this minimal implementation, executes the frame action immediately.
    /// </summary>
    /// <param name="frameAction">The action to execute on the next frame.</param>
    public void RequestNextFrame(Action frameAction)
    {
        // Execute the frame action immediately
        frameAction();
        _currentTimeMs += 16.67; // Advance time (typical frame at 60fps)
    }
}