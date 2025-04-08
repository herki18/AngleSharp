namespace LayoutEngine.Platform.Tests.Unit.Update;

using System;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;

/// <summary>
/// Test implementation of the frame timing strategy that allows manual control over frame execution.
/// </summary>
public class TestFrameTimingStrategy : IFrameTimingStrategy
{
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly bool _synchronousMode;
    private readonly double _currentTimeMs;

    public TestFrameTimingStrategy(
        IThreadingCoordinator threadingCoordinator,
        bool synchronousMode = true,
        double currentTimeMs = 1000.0)
    {
        _threadingCoordinator = threadingCoordinator ??
                                throw new ArgumentNullException(nameof(threadingCoordinator));
        _synchronousMode = synchronousMode;
        _currentTimeMs = currentTimeMs;
    }

    public bool IsSynchronousModeEnabled => _synchronousMode;

    public double GetCurrentTimeMs() => _currentTimeMs;

    public void RequestNextFrame(Action frameAction)
    {
        // In test mode with synchronous mode enabled, do nothing
        // The RunFrameSynchronously() method will be called directly

        // In non-synchronous mode, schedule on main thread
        if (!_synchronousMode)
        {
            _threadingCoordinator.ScheduleOnMainThread(frameAction);
        }
    }
}