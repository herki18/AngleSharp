// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventLoopImplementation.h

namespace LadyBird.Libraries.LibCore.Events;

using System;
using System.Collections.Concurrent;

/// <summary>
/// Pure .NET EventLoopManager - no platform-specific code needed
/// Maintains LadyBird's API structure for easy porting of other components
/// </summary>
public class EventLoopManager
{
    private static EventLoopManager? s_eventLoopManager;

    // Track which implementation owns which timer/notifier
    private readonly ConcurrentDictionary<IntPtr, EventLoopImplementation> _timerOwners = new();
    private readonly ConcurrentDictionary<Notifier, EventLoopImplementation> _notifierOwners = new();

    public static EventLoopManager The()
    {
        if (s_eventLoopManager == null)
            s_eventLoopManager = new EventLoopManager();
        return s_eventLoopManager;
    }

    public static void Install(EventLoopManager manager)
    {
        s_eventLoopManager = manager;
    }

    protected EventLoopManager() { }

    public virtual EventLoopImplementation MakeImplementation()
    {
        return new EventLoopImplementation();
    }

    public virtual IntPtr RegisterTimer(EventReceiver receiver, int milliseconds, bool shouldReload,
        TimerShouldFireWhenNotVisible fireWhenNotVisible)
    {
        var impl = EventLoop.Current().Impl;
        var timerId = impl.RegisterTimer(receiver, milliseconds, shouldReload, fireWhenNotVisible);
        _timerOwners[timerId] = impl;
        return timerId;
    }

    public virtual void UnregisterTimer(IntPtr timerId)
    {
        if (_timerOwners.TryRemove(timerId, out var impl))
        {
            impl.UnregisterTimer(timerId);
        }
    }

    public virtual void RegisterNotifier(Notifier notifier)
    {
        var impl = EventLoop.Current().Impl;
        impl.RegisterNotifier(notifier);
        _notifierOwners[notifier] = impl;
    }

    public virtual void UnregisterNotifier(Notifier notifier)
    {
        if (_notifierOwners.TryRemove(notifier, out var impl))
        {
            impl.UnregisterNotifier(notifier);
        }
    }

    public virtual void DidPostEvent()
    {
        // Wake up the current event loop if it exists
        if (EventLoop.IsRunning())
        {
            EventLoop.Current().Wake();
        }
    }

    // Signal handling - not really applicable in pure .NET environments
    // Keeping the API for compatibility with LadyBird architecture
    public virtual int RegisterSignal(int signalNumber, Action<int> handler)
    {
        // In .NET, we would use different mechanisms:
        // - Console.CancelKeyPress for Ctrl+C
        // - AppDomain.ProcessExit for process termination
        // - Framework-specific events for GUI apps
        Console.WriteLine($"RegisterSignal({signalNumber}) - not implemented in .NET environment");
        return -1;
    }

    public virtual void UnregisterSignal(int handlerId)
    {
        Console.WriteLine($"UnregisterSignal({handlerId}) - not implemented in .NET environment");
    }
}

// No platform-specific typedef needed anymore!
// Just use EventLoopManager directly