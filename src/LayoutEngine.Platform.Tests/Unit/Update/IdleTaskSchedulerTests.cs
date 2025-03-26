namespace LayoutEngine.Platform.Tests.Unit.Update;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Helpers;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform.Update;
using NSubstitute;
using Xunit;

public class IdleTaskSchedulerTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly TestEventAggregator _eventAggregator;
    private readonly IFrameScheduler _frameScheduler;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly IdleTaskScheduler _idleTaskScheduler;

    public IdleTaskSchedulerTests()
    {
        _fixture = new Fixture();
        _eventAggregator = new TestEventAggregator();
        _frameScheduler = Substitute.For<IFrameScheduler>();
        _threadingCoordinator = Substitute.For<IThreadingCoordinator>();

        // Configure threading coordinator to execute main thread actions immediately
        _threadingCoordinator.When(x => x.ScheduleOnMainThread(Arg.Any<Action>()))
            .Do(callback => callback.Arg<Action>()());

        _idleTaskScheduler = new IdleTaskScheduler(_eventAggregator, _frameScheduler, _threadingCoordinator);
    }

    [Fact]
    public void PendingTaskCount_InitialValue_ShouldBeZero()
    {
        // Assert
        Assert.Equal(0, _idleTaskScheduler.PendingTaskCount);
    }

    [Fact]
    public void ScheduleIdleTask_ShouldReturnValidTask()
    {
        // Arrange
        Action action = () => { _ = true; };

        // Act
        IIdleTask task = _idleTaskScheduler.ScheduleIdleTask(action);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(IdleTaskPriority.Normal, task.Priority);
        Assert.False(task.IsCanceled);
        Assert.Equal(1, _idleTaskScheduler.PendingTaskCount);
    }

    [Fact]
    public void ScheduleIdleTask_WithCancellationToken_ShouldReturnValidTask()
    {
        // Arrange
        Action<CancellationToken> action = (token) => { _ = true; };

        // Act
        IIdleTask task = _idleTaskScheduler.ScheduleIdleTask(action);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(IdleTaskPriority.Normal, task.Priority);
        Assert.False(task.IsCanceled);
        Assert.Equal(1, _idleTaskScheduler.PendingTaskCount);
    }

    [Fact]
    public async Task ScheduleIdleTaskAsync_ShouldReturnCompletedTask()
    {
        // Arrange
        int result = 0;
        Func<int> function = () => { result = 42; return result; };

        // Act - schedule the task and add a safety timeout
        var task = _idleTaskScheduler.ScheduleIdleTaskAsync(function);

        // Use direct frame values that are known to work
        _frameScheduler.LastFrameTime.Returns(950.0);
        var endFrameEvent = new EndFrameEvent(1, 1000.0);
        _eventAggregator.Publish(endFrameEvent);

        // Add a timeout to prevent hanging
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(1), cts.Token));

        if (completedTask != task)
        {
            Assert.Fail($"Task did not complete within timeout. PendingTaskCount: {_idleTaskScheduler.PendingTaskCount}");
        }

        // Get the result after confirming the task completed
        int returnedResult = await task;

        // Assert
        Assert.Equal(42, returnedResult);
        Assert.Equal(0, _idleTaskScheduler.PendingTaskCount);
    }

    [Fact]
    public async Task ScheduleIdleTaskAsync_WithCancellationToken_ShouldReturnCompletedTask()
    {
        // Arrange
        int result = 0;
        Func<CancellationToken, int> function = (token) => { result = 42; return result; };

        // Act
        var task = _idleTaskScheduler.ScheduleIdleTaskAsync(function);
        SimulateIdleTime(20);
        int returnedResult = await task;

        // Assert
        Assert.Equal(42, returnedResult);
        Assert.Equal(0, _idleTaskScheduler.PendingTaskCount);
    }

    [Fact]
    public void CancelTask_ShouldCancelTask()
    {
        // Arrange
        Action action = () => { _ = true; };
        IIdleTask task = _idleTaskScheduler.ScheduleIdleTask(action);

        // Act
        _idleTaskScheduler.CancelTask(task.Id);

        // Assert
        Assert.True(task.IsCanceled);
    }

    [Fact]
    public void PauseTasks_ShouldPreventExecution()
    {
        // Arrange
        bool taskExecuted = false;
        Action action = () => { taskExecuted = true; };
        _idleTaskScheduler.ScheduleIdleTask(action);

        // Act
        _idleTaskScheduler.PauseTasks();
        SimulateIdleTime(20);

        // Assert
        Assert.False(taskExecuted);
    }

    [Fact]
    public void ResumeTasks_ShouldAllowExecution()
    {
        // Arrange
        bool taskExecuted = false;
        Action action = () => { taskExecuted = true; };
        _idleTaskScheduler.ScheduleIdleTask(action);
        _idleTaskScheduler.PauseTasks();

        // Act
        _idleTaskScheduler.ResumeTasks();
        SimulateIdleTime(20);

        // Assert
        Assert.True(taskExecuted);
    }

    [Fact]
    public void OnBeginFrame_ShouldResetFrameDuration()
    {
        // Arrange
        var beginFrameEvent = new BeginFrameEvent(1, 1000.0);

        // Act
        _eventAggregator.Publish(beginFrameEvent);

        // Assert - This is mostly checking that it doesn't throw
        Assert.Equal(0, _idleTaskScheduler.PendingTaskCount);
    }

    [Fact]
    public void OnEndFrame_WithIdleTime_ShouldProcessTasks()
    {
        // Arrange
        bool taskExecuted = false;
        Action action = () => { taskExecuted = true; };
        _idleTaskScheduler.ScheduleIdleTask(action);

        _frameScheduler.LastFrameTime.Returns(980.0);
        var endFrameEvent = new EndFrameEvent(1, 1000.0);

        // Act
        _eventAggregator.Publish(endFrameEvent);

        // Assert
        Assert.True(taskExecuted);
        Assert.Equal(0, _idleTaskScheduler.PendingTaskCount);
    }

    [Fact]
    public void IdleTasks_ShouldExecuteInPriorityOrder()
    {
        // Arrange
        var executionOrder = new List<string>();

        Action highPriorityAction = () => { executionOrder.Add("High"); };
        Action normalPriorityAction = () => { executionOrder.Add("Normal"); };
        Action lowPriorityAction = () => { executionOrder.Add("Low"); };

        // Schedule tasks in reverse priority order
        _idleTaskScheduler.ScheduleIdleTask(lowPriorityAction, IdleTaskPriority.Low);
        _idleTaskScheduler.ScheduleIdleTask(normalPriorityAction, IdleTaskPriority.Normal);
        _idleTaskScheduler.ScheduleIdleTask(highPriorityAction, IdleTaskPriority.High);

        // Act
        SimulateIdleTime(50); // Provide enough idle time to execute all tasks

        // Assert
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("High", executionOrder[0]);
        Assert.Equal("Normal", executionOrder[1]);
        Assert.Equal("Low", executionOrder[2]);
    }

    [Fact]
    public void CompletedTasks_ShouldFireCompletionEvents()
    {
        // Arrange
        bool taskExecuted = false;
        Action action = () => { taskExecuted = true; };
        var task = _idleTaskScheduler.ScheduleIdleTask(action);

        // Act - Use the same pattern as other successful tests with more idle time
        _frameScheduler.LastFrameTime.Returns(950.0);
        var endFrameEvent = new EndFrameEvent(1, 1000.0);
        _eventAggregator.Publish(endFrameEvent);

        // Assert
        Assert.True(taskExecuted, "Task should have been executed");

        var completionEvents = _eventAggregator.GetPublishedEvents<IdleTaskCompletedEvent>();
        Assert.Single(completionEvents, "Should have published exactly one completion event");
        Assert.Equal(task.Id, completionEvents[0].TaskId);
    }

    private void SimulateIdleTime(double milliseconds)
    {
        // Use the same pattern as in the OnEndFrame_WithIdleTime_ShouldProcessTasks test
        _frameScheduler.LastFrameTime.Returns(1000.0 - milliseconds);
        var endFrameEvent = new EndFrameEvent(1, 1000.0);
        _eventAggregator.Publish(endFrameEvent);
    }

    public void Dispose()
    {
        _idleTaskScheduler.Dispose();
    }
}