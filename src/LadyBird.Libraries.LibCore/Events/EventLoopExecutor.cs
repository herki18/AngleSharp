// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventLoopExecutor.swift

namespace LadyBird.Libraries.LibCore.Events;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// C# adaptation of Swift EventLoopExecutor
// This provides integration with Task/async-await patterns
public class EventLoopExecutor : TaskScheduler
{
    private readonly EventLoop _eventLoop;

    public EventLoopExecutor()
    {
        _eventLoop = EventLoop.Current();
    }

    public EventLoopExecutor(EventLoop eventLoop)
    {
        _eventLoop = eventLoop ?? throw new ArgumentNullException(nameof(eventLoop));
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        // Not supported for this scheduler
        return Array.Empty<Task>();
    }

    protected override void QueueTask(Task task)
    {
        _eventLoop.DeferredInvoke(() =>
        {
            TryExecuteTask(task);
        });
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // Only allow inline execution if we're on the event loop thread
        if (EventLoop.Current() == _eventLoop)
        {
            return TryExecuteTask(task);
        }
        return false;
    }

    public void CheckIsolated()
    {
        if (EventLoop.Current() != _eventLoop)
            throw new InvalidOperationException("Not on the expected event loop");
    }
}

// Extension methods for async/await integration
public static class EventLoopExtensions
{
    public static TaskScheduler AsTaskScheduler(this EventLoop eventLoop)
    {
        return new EventLoopExecutor(eventLoop);
    }

    public static Task RunAsync(this EventLoop eventLoop, Action action)
    {
        return Task.Factory.StartNew(
            action,
            CancellationToken.None,
            TaskCreationOptions.None,
            new EventLoopExecutor(eventLoop)
        );
    }

    public static Task<T> RunAsync<T>(this EventLoop eventLoop, Func<T> function)
    {
        return Task.Factory.StartNew(
            function,
            CancellationToken.None,
            TaskCreationOptions.None,
            new EventLoopExecutor(eventLoop)
        );
    }
}