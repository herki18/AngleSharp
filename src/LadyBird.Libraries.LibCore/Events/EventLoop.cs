// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventLoop.h
// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventLoop.cpp

namespace LadyBird.Libraries.LibCore.Events;

using System;
using System.Collections.Generic;
using AK;

// The event loop enables asynchronous (not parallel or multi-threaded) computing by efficiently handling events from various sources.
// Event loops are most important for GUI programs, where the various GUI updates and action callbacks run on the EventLoop,
// as well as services, where asynchronous remote procedure calls of multiple clients are handled.
// Event loops, through select(), allow programs to "go to sleep" for most of their runtime until some event happens.
// EventLoop is too expensive to use in realtime scenarios (read: audio) where even the time required by a single select() system call is too large and unpredictable.
//
// There is at most one running event loop per thread.
// Another event loop can be started while another event loop is already running; that new event loop will take over for the other event loop.
// This is mainly used in LibGUI, where each modal window stacks another event loop until it is closed.
// However, that means you need to be careful with storing the current event loop, as it might already be gone at the time of use.
// Event loops currently handle these kinds of events:
// - Deferred invocations caused by various objects. These are just a generic way of telling the EventLoop to run some function as soon as possible at a later point.
// - Timers, which repeatedly (or once after a delay) run a function on the EventLoop. Note that timers are not super accurate.
// - Filesystem notifications, i.e. whenever a file is read from, written to, etc.
// - POSIX signals, which allow the event loop to act as a signal handler and dispatch those signals in a more user-friendly way.
// - Fork events, because the child process event loop needs to clear its events and handlers.
// - Quit events, i.e. the event loop should exit.
// Any event that the event loop needs to wait on or needs to repeatedly handle is stored in a handle, e.g. s_timers.
public class EventLoop : IDisposable
{
    // AK_MAKE_NONMOVABLE(EventLoop);
    // AK_MAKE_NONCOPYABLE(EventLoop);

    // friend struct EventLoopPusher;

    public enum WaitMode
    {
        WaitForEvents,
        PollForEvents,
    }

    // Thread-local storage for event loop stack
    [ThreadStatic]
    private static List<EventLoop>? s_eventLoopStack = null;

    private static List<EventLoop> EventLoopStack()
    {
        if (s_eventLoopStack == null)
            s_eventLoopStack = new List<EventLoop>();
        return s_eventLoopStack;
    }

    public EventLoop()
    {
        _impl = EventLoopManager.The().MakeImplementation();
        if (EventLoopStack().Count == 0)
        {
            EventLoopStack().Add(this);
        }
    }

    public void Dispose()
    {
        var stack = EventLoopStack();
        if (stack.Count > 0 && stack[stack.Count - 1] == this)
        {
            stack.RemoveAt(stack.Count - 1);
        }
    }

    public static bool IsRunning()
    {
        var stack = s_eventLoopStack;
        return stack != null && stack.Count > 0;
    }

    public static EventLoop Current()
    {
        if (EventLoopStack().Count == 0)
            Console.WriteLine("No EventLoop is present, unable to return current one!");
        return EventLoopStack()[EventLoopStack().Count - 1];
    }

    public void Quit(int code)
    {
        ThreadEventQueue.Current().CancelAllPendingJobs();
        _impl.Quit(code);
    }

    public bool WasExitRequested()
    {
        return _impl.WasExitRequested();
    }

    // struct EventLoopPusher
    class EventLoopPusher : IDisposable
    {
        public EventLoopPusher(EventLoop eventLoop)
        {
            EventLoopStack().Add(eventLoop);
        }

        public void Dispose()
        {
            var stack = EventLoopStack();
            if (stack.Count > 0)
                stack.RemoveAt(stack.Count - 1);
        }
    }

    // Pump the event loop until its exit is requested.
    public int Exec()
    {
        using var pusher = new EventLoopPusher(this);
        return _impl.Exec();
    }

    // Pump the event loop until some condition is met.
    public void SpinUntil(Func<bool> goalCondition)
    {
        using var pusher = new EventLoopPusher(this);
        while (!goalCondition())
            Pump();
    }

    // Process events, generally called by exec() in a loop.
    // This should really only be used for integrating with other event loops.
    // The wait mode determines whether pump() uses select() to wait for the next event.
    public int Pump(WaitMode mode = WaitMode.WaitForEvents)
    {
        return _impl.Pump(mode == WaitMode.WaitForEvents
            ? EventLoopImplementation.PumpMode.WaitForEvents
            : EventLoopImplementation.PumpMode.DontWaitForEvents);
    }

    // Post an event to this event loop.
    public void PostEvent(EventReceiver receiver, Event @event)
    {
        _impl.PostEvent(receiver, @event);
    }

    public void AddJob(Promise<EventReceiver> jobPromise)
    {
        ThreadEventQueue.Current().AddJob(jobPromise);
    }

    public void DeferredInvoke(Action invokee)
    {
        var context = DeferredInvocationContext.Create();
        PostEvent(context, new DeferredInvocationEvent(context, invokee));
    }

    public void Wake()
    {
        _impl.Wake();
    }

    // The registration functions act upon the current loop of the current thread.
    public static IntPtr RegisterTimer(EventReceiver receiver, int milliseconds, bool shouldReload, TimerShouldFireWhenNotVisible fireWhenNotVisible)
    {
        return EventLoopManager.The().RegisterTimer(receiver, milliseconds, shouldReload, fireWhenNotVisible);
    }

    public static void UnregisterTimer(IntPtr timerId)
    {
        EventLoopManager.The().UnregisterTimer(timerId);
    }

    public static void RegisterNotifier(Badge<Notifier> badge, Notifier notifier)
    {
        EventLoopManager.The().RegisterNotifier(notifier);
    }

    public static void UnregisterNotifier(Badge<Notifier> badge, Notifier notifier)
    {
        EventLoopManager.The().UnregisterNotifier(notifier);
    }

    public static int RegisterSignal(int signo, Action<int> handler)
    {
        return EventLoopManager.The().RegisterSignal(signo, handler);
    }

    public static void UnregisterSignal(int handlerId)
    {
        EventLoopManager.The().UnregisterSignal(handlerId);
    }

    public EventLoopImplementation Impl => _impl;

    private readonly EventLoopImplementation _impl;
}

// Global function
public static class EventLoopHelpers
{
    public static void DeferredInvoke(Action invokee)
    {
        EventLoop.Current().DeferredInvoke(invokee);
    }
}