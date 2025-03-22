namespace LayoutEngine.Platform.Tests.Unit.Threading;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Platform.Threading;
using NSubstitute;
using Xunit;

public class ThreadingCoordinatorTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly IThreadPool _threadPool;
    private readonly ThreadingCoordinator _threadingCoordinator;
    private readonly List<IWorker> _testWorkers = new();

    public ThreadingCoordinatorTests()
    {
        _fixture = new Fixture();
        _threadPool = Substitute.For<IThreadPool>();
        _threadingCoordinator = new ThreadingCoordinator(_threadPool);
    }

    [Fact]
    public void MainThreadContext_ShouldNotBeNull()
    {
        // Act
        var context = _threadingCoordinator.MainThreadContext;

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void RenderThreadContext_ShouldNotBeNull()
    {
        // Act
        var context = _threadingCoordinator.RenderThreadContext;

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void EnableSynchronousMode_ShouldEnableSynchronousMode()
    {
        // Act
        _threadingCoordinator.EnableSynchronousMode(true);

        // Assert
        Assert.True(_threadingCoordinator.IsSynchronousModeEnabled);
    }

    [Fact]
    public void ScheduleOnMainThread_InSynchronousMode_ShouldQueueAction()
    {
        // Arrange
        _threadingCoordinator.EnableSynchronousMode(true);
        var actionExecuted = false;
        Action action = () => { actionExecuted = true; };

        // Act
        _threadingCoordinator.ScheduleOnMainThread(action);

        // Assert
        Assert.Equal(1, _threadingCoordinator.MainThreadQueueCount);
        Assert.False(actionExecuted);

        // Cleanup - Execute the queued action
        _threadingCoordinator.ExecuteQueuedActionsSync();
        Assert.True(actionExecuted);
    }

    [Fact]
    public void ScheduleOnRenderThread_InSynchronousMode_ShouldQueueAction()
    {
        // Arrange
        _threadingCoordinator.EnableSynchronousMode(true);
        var actionExecuted = false;
        Action action = () => { actionExecuted = true; };

        // Act
        _threadingCoordinator.ScheduleOnRenderThread(action);

        // Assert
        Assert.Equal(1, _threadingCoordinator.RenderThreadQueueCount);
        Assert.False(actionExecuted);

        // Cleanup - Execute the queued action
        _threadingCoordinator.ExecuteQueuedActionsSync();
        Assert.True(actionExecuted);
    }

    [Fact]
    public void ScheduleOnWorkerThread_InSynchronousMode_ShouldQueueAction()
    {
        // Arrange
        _threadingCoordinator.EnableSynchronousMode(true);
        var actionExecuted = false;
        Action action = () => { actionExecuted = true; };

        // Act
        _threadingCoordinator.ScheduleOnWorkerThread(action);

        // Assert
        Assert.Equal(1, _threadingCoordinator.WorkerThreadQueueCount);
        Assert.False(actionExecuted);

        // Cleanup - Execute the queued action
        _threadingCoordinator.ExecuteQueuedActionsSync();
        Assert.True(actionExecuted);
    }

    [Fact]
    public void ExecuteQueuedActionsSync_WithoutSynchronousMode_ShouldThrow()
    {
        // Arrange
        _threadingCoordinator.EnableSynchronousMode(false);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _threadingCoordinator.ExecuteQueuedActionsSync());
    }

    [Fact]
    public void ScheduleOnWorkerThread_InNormalMode_ShouldDelegateToThreadPool()
    {
        // Arrange
        _threadingCoordinator.EnableSynchronousMode(false);
        Action action = () => { };

        // Act
        _threadingCoordinator.ScheduleOnWorkerThread(action);

        // Assert
        _threadPool.Received(1).QueueWorkItem(action);
    }

    [Fact]
    public void CreateWorker_ShouldCreateValidWorker()
    {
        // Arrange
        WorkerType workerType = WorkerType.General;

        // Act
        var worker = _threadingCoordinator.CreateWorker(workerType);
        _testWorkers.Add(worker);

        // Assert
        Assert.NotNull(worker);
        Assert.Equal(workerType, worker.WorkerType);
        Assert.False(worker.IsBusy);
    }

    [Fact]
    public void Worker_PostWork_ShouldExecuteWork()
    {
        // Arrange
        _threadingCoordinator.EnableSynchronousMode(true);
        var worker = _threadingCoordinator.CreateWorker(WorkerType.General);
        _testWorkers.Add(worker);
        var actionExecuted = false;
        Action action = () => { actionExecuted = true; };
        Action<bool>? callback = (success) => { };

        // Act
        worker.PostWork(action, callback);
        ((ThreadingCoordinator.Worker)worker).ProcessWorkSynchronously();

        // Assert
        Assert.True(actionExecuted);
    }

    [Fact]
    public async Task Worker_PostWorkAsync_ShouldReturnCompletedTask()
    {
        // Arrange
        _threadingCoordinator.EnableSynchronousMode(true);
        var worker = _threadingCoordinator.CreateWorker(WorkerType.General);
        var value = 42;
        Action action = () => { value *= 2; };

        // Act
        var task = worker.PostWorkAsync(action);
        ((ThreadingCoordinator.Worker)worker).ProcessWorkSynchronously();
        await task;

        // Assert
        Assert.Equal(84, value);
    }

    [Fact]
    public void Worker_CancelPendingWork_ShouldClearQueue()
    {
        // Arrange
        _threadingCoordinator.EnableSynchronousMode(true);
        var worker = _threadingCoordinator.CreateWorker(WorkerType.General);
        _testWorkers.Add(worker);
        var callbackCalled = false;
        var callbackSuccess = true;
        Action action = () => { };
        Action<bool> callback = (success) => {
            callbackCalled = true;
            callbackSuccess = success;
        };

        // Act
        worker.PostWork(action, callback);

        // Verify work was queued
        Assert.Equal(1, ((ThreadingCoordinator.Worker)worker).PendingWorkItemCount);

        // Cancel the pending work
        worker.CancelPendingWork();

        // Assert
        Assert.True(callbackCalled, "Callback should be called when work is canceled");
        Assert.False(callbackSuccess, "Callback should receive 'false' when work is canceled");
        Assert.Equal(0, ((ThreadingCoordinator.Worker)worker).PendingWorkItemCount);

        // Do not call ProcessWorkSynchronously after canceling - there's nothing to process
    }

    public void Dispose()
    {
        // Dispose workers first to avoid race conditions
        foreach (var worker in _testWorkers)
        {
            try
            {
                worker.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Worker may already be disposed, which is fine
            }
        }
        _testWorkers.Clear();

        // Then dispose the coordinator
        try
        {
            _threadingCoordinator.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Coordinator may already be disposed, which is fine
        }
    }
}