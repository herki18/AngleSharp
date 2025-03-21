namespace LayoutEngine.Platform.Tests.Unit.Threading;

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Platform.Threading;
using Microsoft.Extensions.Options;
using Xunit;
using ThreadPool = Platform.Threading.ThreadPool;

public class ThreadPoolTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly ThreadPoolOptions _options;
    private readonly IOptions<ThreadPoolOptions> _optionsWrapper;
    private readonly ThreadPool _threadPool;

    public ThreadPoolTests()
    {
        _fixture = new Fixture();
        _options = new ThreadPoolOptions
        {
            MaxThreads = 2,
            MonitorThreadHealth = true,
            ThreadIdleTimeoutMs = 1000
        };
        _optionsWrapper = Options.Create(_options);
        _threadPool = new ThreadPool(_optionsWrapper);
    }

    [Fact]
    public void ActiveThreadCount_InitialValue_ShouldBeZero()
    {
        // Assert
        Assert.Equal(0, _threadPool.ActiveThreadCount);
    }

    [Fact]
    public Task QueueWorkItem_ShouldExecuteAction()
    {
        // Arrange
        var signal = new ManualResetEventSlim(false);
        var actionExecuted = false;

        // Act
        _threadPool.QueueWorkItem(() =>
        {
            actionExecuted = true;
            signal.Set();
        });

        // Wait for action to complete
        var signaled = signal.Wait(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(signaled, "Action should have executed within timeout");
        Assert.True(actionExecuted);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task QueueWorkItemAsync_ShouldReturnCompletedTask()
    {
        // Arrange
        var value = 42;

        // Act
        await _threadPool.QueueWorkItemAsync(() => { value *= 2; });

        // Assert
        Assert.Equal(84, value);
    }

    [Fact]
    public async Task QueueWorkItemAsync_WithException_ShouldFaultTask()
    {
        // Arrange
        var expectedError = new InvalidOperationException("Test error");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _threadPool.QueueWorkItemAsync(() => { throw expectedError; });
        });

        Assert.Same(expectedError, exception);
    }

    [Fact]
    public Task QueueMultipleWorkItems_ShouldExecuteAll()
    {
        // Arrange
        const int itemCount = 10;
        var completedCount = 0;
        var signal = new CountdownEvent(itemCount);

        // Act
        for (int i = 0; i < itemCount; i++)
        {
            _threadPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref completedCount);
                signal.Signal();
            });
        }

        // Wait for all actions to complete
        var allCompleted = signal.Wait(TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(allCompleted, "All actions should have completed within timeout");
        Assert.Equal(itemCount, completedCount);
        return Task.CompletedTask;
    }

    [Fact]
    public Task QueueWorkItem_WithDifferentPriorities_ShouldExecuteAll()
    {
        // Arrange
        const int itemCount = 6;
        var completedCount = 0;
        var signal = new CountdownEvent(itemCount);
        var priorities = new[]
        {
            WorkItemPriority.Low,
            WorkItemPriority.Normal,
            WorkItemPriority.High,
            WorkItemPriority.Critical,
            WorkItemPriority.Normal,
            WorkItemPriority.Low
        };

        // Act
        for (int i = 0; i < itemCount; i++)
        {
            _threadPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref completedCount);
                signal.Signal();
            }, priorities[i]);
        }

        // Wait for all actions to complete
        var allCompleted = signal.Wait(TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(allCompleted, "All actions should have completed within timeout");
        Assert.Equal(itemCount, completedCount);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _threadPool.Dispose();
    }
}