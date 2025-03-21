namespace LayoutEngine.Platform.Tests.Unit.Threading;

using AutoFixture;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Platform.Threading;
using NSubstitute;

public class ThreadingCoordinatorTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly IThreadPool _threadPool;
    private readonly ThreadingCoordinator _threadingCoordinator;

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
        var actionExecuted = false;
        Action action = () => { actionExecuted = false; };
        Action<bool> callback = (success) => { actionExecuted = success; };

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
        var callbackExecuted = false;
        Action action = () => { };
        Action<bool> callback = (success) => { callbackExecuted = true; };

        // Act
        worker.PostWork(action, callback);
        worker.CancelPendingWork();
        ((ThreadingCoordinator.Worker)worker).ProcessWorkSynchronously();

        // Assert
        Assert.True(callbackExecuted);
        Assert.Equal(0, ((ThreadingCoordinator.Worker)worker).PendingWorkItemCount);
    }

    public void Dispose()
    {
        _threadingCoordinator.Dispose();
    }
}