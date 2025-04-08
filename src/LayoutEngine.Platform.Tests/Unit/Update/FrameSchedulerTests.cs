namespace LayoutEngine.Platform.Tests.Unit.Update;

using AutoFixture;
using Contracts.Platform.Updates;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Platform.Abstractions;
using LayoutEngine.Platform.Tests.Unit.Helpers;
using LayoutEngine.Platform.Update;
using NSubstitute;

public class FrameSchedulerTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly TestEventAggregator _eventAggregator;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly TestTimeProvider _timeProvider;
    private readonly IFrameTimingStrategy _timingStrategy;
    private readonly FrameScheduler _frameScheduler;

    public FrameSchedulerTests()
    {
        _fixture = new Fixture();
        _eventAggregator = new TestEventAggregator();
        _threadingCoordinator = Substitute.For<IThreadingCoordinator>();
        _timeProvider = new TestTimeProvider(initialTimeMs: 1000);

        // Create a mock timing strategy
        _timingStrategy = Substitute.For<IFrameTimingStrategy>();
        _timingStrategy.IsSynchronousModeEnabled.Returns(false);
        _timingStrategy.GetCurrentTimeMs().Returns(1000.0);

        // When RequestNextFrame is called, schedule the action on the main thread
        _timingStrategy.When(x => x.RequestNextFrame(Arg.Any<Action>()))
            .Do(callInfo => {
                var action = callInfo.Arg<Action>();
                _threadingCoordinator.ScheduleOnMainThread(action);
            });

        _frameScheduler = new FrameScheduler(
            _eventAggregator,
            _threadingCoordinator,
            _timingStrategy,
            _timeProvider);
    }

    [Fact]
    public void CurrentFrameNumber_InitialValue_ShouldBeZero()
    {
        // Assert
        Assert.Equal(0, _frameScheduler.CurrentFrameNumber);
    }

    [Fact]
    public void LastFrameTime_InitialValue_ShouldBeZero()
    {
        // Assert
        Assert.Equal(0, _frameScheduler.LastFrameTime);
    }

    [Fact]
    public void RequestAnimationFrame_ShouldReturnUniqueIds()
    {
        // Arrange
        Action<double> callback = time => { };

        // Act
        int id1 = _frameScheduler.RequestAnimationFrame(callback);
        int id2 = _frameScheduler.RequestAnimationFrame(callback);

        // Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void RequestAnimationFrame_WithNullCallback_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _frameScheduler.RequestAnimationFrame(null!));
    }

    [Fact]
    public void EnableSynchronousMode_ShouldEnableSynchronousMode()
    {
        // Act
        _frameScheduler.EnableSynchronousMode(true);

        // Assert - Check internal state through reflection
        var field = typeof(FrameScheduler).GetField("_synchronousMode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var value = (bool)field!.GetValue(_frameScheduler)!;
        Assert.True(value);
    }

    [Fact]
    public void RunFrameSynchronously_WithoutSynchronousMode_ShouldThrow()
    {
        // Arrange
        _frameScheduler.EnableSynchronousMode(false);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _frameScheduler.RunFrameSynchronously());
    }

    [Fact]
    public void RunFrameSynchronously_ShouldInvokeCallbacks()
    {
        // Arrange
        _frameScheduler.EnableSynchronousMode(true);
        var callbackExecuted = false;
        double callbackTime = 0;

        _frameScheduler.RequestAnimationFrame(time => {
            callbackExecuted = true;
            callbackTime = time;
        });

        // Act
        _frameScheduler.RunFrameSynchronously();

        // Assert
        Assert.True(callbackExecuted);
        Assert.Equal(1000.0, callbackTime);
        Assert.Equal(1, _frameScheduler.CurrentFrameNumber);
        Assert.Equal(1000.0, _frameScheduler.LastFrameTime);
    }

    [Fact]
    public void RunFrameSynchronously_ShouldPublishFrameEvents()
    {
        // Arrange
        _frameScheduler.EnableSynchronousMode(true);
        _frameScheduler.RequestAnimationFrame(_ => { });

        // Act
        _frameScheduler.RunFrameSynchronously();

        // Assert
        var beginEvents = _eventAggregator.GetPublishedEvents<BeginFrameEvent>();
        var endEvents = _eventAggregator.GetPublishedEvents<EndFrameEvent>();

        Assert.Single(beginEvents);
        Assert.Single(endEvents);

        Assert.Equal(1, beginEvents[0].FrameNumber);
        Assert.Equal(1000.0, beginEvents[0].FrameTimestamp);

        Assert.Equal(1, endEvents[0].FrameNumber);
        Assert.Equal(1000.0, endEvents[0].FrameTimestamp);
    }

    [Fact]
    public void CancelAnimationFrame_ShouldRemoveCallback()
    {
        // Arrange
        _frameScheduler.EnableSynchronousMode(true);
        var callbackExecuted = false;

        int id = _frameScheduler.RequestAnimationFrame(_ => {
            callbackExecuted = true;
        });

        // Act
        _frameScheduler.CancelAnimationFrame(id);
        _frameScheduler.RunFrameSynchronously();

        // Assert
        Assert.False(callbackExecuted);
    }

    [Fact]
    public void MultipleFrames_ShouldIncrementFrameNumber()
    {
        // Arrange
        _frameScheduler.EnableSynchronousMode(true);

        // Act - Run 3 frames
        for (int i = 0; i < 3; i++)
        {
            _frameScheduler.RequestAnimationFrame(_ => { });
            _frameScheduler.RunFrameSynchronously();
        }

        // Assert
        Assert.Equal(3, _frameScheduler.CurrentFrameNumber);
    }

    [Fact]
    public void RequestAnimationFrame_InNormalMode_ShouldUseTimingStrategy()
    {
        // Arrange
        _frameScheduler.EnableSynchronousMode(false);
        Action<double> callback = _ => { };

        // Act
        _frameScheduler.RequestAnimationFrame(callback);

        // Assert - Now we check that the timing strategy was used
        _timingStrategy.Received(1).RequestNextFrame(Arg.Any<Action>());
    }

    public void Dispose()
    {
        _frameScheduler.Dispose();
    }
}