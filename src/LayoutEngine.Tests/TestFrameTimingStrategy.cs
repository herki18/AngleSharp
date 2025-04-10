using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;

namespace LayoutEngine.Tests;

/// <summary>
/// Test implementation of the frame timing strategy that allows synchronous frame execution for tests.
/// </summary>
public class TestFrameTimingStrategy : IFrameTimingStrategy
{
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly bool _synchronousMode;
    private double _currentTimeMs;

    public TestFrameTimingStrategy(
        IThreadingCoordinator threadingCoordinator,
        bool synchronousMode = true,
        double initialTimeMs = 1000.0)
    {
        _threadingCoordinator = threadingCoordinator ??
                                throw new ArgumentNullException(nameof(threadingCoordinator));
        _synchronousMode = synchronousMode;
        _currentTimeMs = initialTimeMs;
    }

    /// <summary>
    /// Gets whether the frame execution should happen synchronously.
    /// </summary>
    public bool IsSynchronousModeEnabled => _synchronousMode;

    /// <summary>
    /// Gets the current time in milliseconds using the platform's timing system.
    /// </summary>
    public double GetCurrentTimeMs() => _currentTimeMs;

    /// <summary>
    /// Requests the next animation frame to be scheduled.
    /// In test mode, executes the frame action immediately when in synchronous mode.
    /// </summary>
    /// <param name="frameAction">The action to execute on the next frame.</param>
    public void RequestNextFrame(Action frameAction)
    {
        if (_synchronousMode)
        {
            // Execute immediately in synchronous mode for tests
            frameAction();
            _currentTimeMs += 16.67; // Advance time (typical frame at 60fps)
        }
        else
        {
            // In non-synchronous mode, schedule on main thread
            _threadingCoordinator.ScheduleOnMainThread(() =>
            {
                frameAction();
                _currentTimeMs += 16.67;
            });
        }
    }
}