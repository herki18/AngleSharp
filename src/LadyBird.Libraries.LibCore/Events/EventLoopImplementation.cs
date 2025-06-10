// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventLoopImplementation.h
// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventLoopImplementation.cpp

namespace LadyBird.Libraries.LibCore.Events;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
/// Pure .NET event loop implementation
/// Maintains LadyBird's API while using .NET primitives
/// </summary>
public class EventLoopImplementation : IDisposable
{
    public enum PumpMode
    {
        WaitForEvents,
        DontWaitForEvents,
    }

    private readonly ManualResetEventSlim _wakeEvent = new ManualResetEventSlim(false);
    private readonly ConcurrentDictionary<int, TimerState> _timers = new();
    private readonly ConcurrentDictionary<int, NotifierState> _notifiers = new();
    private volatile bool _exitRequested = false;
    private int _exitCode = 0;
    private int _nextId = 1;
    protected ThreadEventQueue _threadEventQueue;

    private class TimerState
    {
        public System.Threading.Timer? Timer { get; set; }
        public WeakReference<EventReceiver> Owner { get; set; } = null!;
        public TimerShouldFireWhenNotVisible FireWhenNotVisible { get; set; }
        public AutoResetEvent ReadyEvent { get; set; } = new AutoResetEvent(false);
        public int Id { get; set; }
        public bool ShouldReload { get; set; }
        public int Interval { get; set; }
    }

    private class NotifierState
    {
        public Notifier Notifier { get; set; } = null!;
        public Thread? WatcherThread { get; set; }
        public AutoResetEvent ReadyEvent { get; set; } = new AutoResetEvent(false);
        public CancellationTokenSource Cancellation { get; set; } = new();
    }

    public EventLoopImplementation()
    {
        _threadEventQueue = ThreadEventQueue.Current();
    }

    public int Exec()
    {
        while (!_exitRequested)
        {
            Pump(PumpMode.WaitForEvents);
        }
        return _exitCode;
    }

    public int Pump(PumpMode mode)
    {
        var timeout = mode == PumpMode.WaitForEvents && !_threadEventQueue.HasPendingEvents()
            ? -1 // Infinite wait
            : 0;  // Don't wait

        // Collect all wait handles
        var waitHandles = new List<WaitHandle> { _wakeEvent.WaitHandle };
        var handleActions = new List<Action> { () => _wakeEvent.Reset() };

        // Add timer events
        foreach (var timer in _timers.Values)
        {
            waitHandles.Add(timer.ReadyEvent);
            var timerId = timer.Id;
            handleActions.Add(() => ProcessTimer(timerId));
        }

        // Add notifier events
        foreach (var notifier in _notifiers.Values)
        {
            waitHandles.Add(notifier.ReadyEvent);
            var notifierId = notifier.Notifier.GetHashCode();
            handleActions.Add(() => ProcessNotifier(notifierId));
        }

        // Wait for any event
        if (waitHandles.Count > 0 && timeout != 0)
        {
            var index = WaitHandle.WaitAny(waitHandles.ToArray(), timeout);
            if (index >= 0 && index < handleActions.Count)
            {
                handleActions[index]();
            }
        }

        // Process the thread event queue
        return _threadEventQueue.Process();
    }

    private void ProcessTimer(int timerId)
    {
        if (_timers.TryGetValue(timerId, out var timer))
        {
            if (timer.Owner.TryGetTarget(out var owner))
            {
                if (timer.FireWhenNotVisible == TimerShouldFireWhenNotVisible.Yes ||
                    owner.IsVisibleForTimerPurposes())
                {
                    _threadEventQueue.PostEvent(owner, new TimerEvent());
                }
            }
        }
    }

    private void ProcessNotifier(int notifierId)
    {
        var notifier = _notifiers.Values.FirstOrDefault(n => n.Notifier.GetHashCode() == notifierId);
        if (notifier != null)
        {
            _threadEventQueue.PostEvent(notifier.Notifier,
                new NotifierActivationEvent(notifier.Notifier.Fd, notifier.Notifier.Type));
        }
    }

    public void Quit(int code)
    {
        _exitRequested = true;
        _exitCode = code;
        Wake();
    }

    public void Wake()
    {
        _wakeEvent.Set();
    }

    public bool WasExitRequested() => _exitRequested;

    public void PostEvent(EventReceiver receiver, Event @event)
    {
        _threadEventQueue.PostEvent(receiver, @event);
        if (_threadEventQueue != ThreadEventQueue.Current())
            Wake();
    }

    public IntPtr RegisterTimer(EventReceiver receiver, int milliseconds, bool shouldReload,
        TimerShouldFireWhenNotVisible fireWhenNotVisible)
    {
        var timerId = Interlocked.Increment(ref _nextId);
        var state = new TimerState
        {
            Owner = new WeakReference<EventReceiver>(receiver),
            FireWhenNotVisible = fireWhenNotVisible,
            Id = timerId,
            ShouldReload = shouldReload,
            Interval = milliseconds
        };

        // Create timer that signals our event
        state.Timer = new System.Threading.Timer(_ =>
        {
            state.ReadyEvent.Set();
            Wake();
        }, null, milliseconds, shouldReload ? milliseconds : Timeout.Infinite);

        _timers[timerId] = state;
        return new IntPtr(timerId);
    }

    public void UnregisterTimer(IntPtr timerId)
    {
        if (_timers.TryRemove(timerId.ToInt32(), out var state))
        {
            state.Timer?.Dispose();
            state.ReadyEvent?.Dispose();
        }
    }

    public void RegisterNotifier(Notifier notifier)
    {
        var notifierId = notifier.GetHashCode();
        var state = new NotifierState
        {
            Notifier = notifier
        };

        // For .NET environments, we'll simulate file descriptor readiness
        // In practice, this would integrate with the specific UI framework
        state.WatcherThread = new Thread(() =>
        {
            while (!state.Cancellation.Token.IsCancellationRequested)
            {
                // This is where framework-specific integration would happen
                // For now, we just simulate periodic checks
                Thread.Sleep(100);

                // In real implementation:
                // - WinForms: integrate with Application.Idle
                // - WPF: use Dispatcher
                // - Unity: use coroutines or Update loop

                if (CheckNotifierReady(notifier))
                {
                    state.ReadyEvent.Set();
                    Wake();
                }
            }
        })
        {
            IsBackground = true,
            Name = $"Notifier_{notifierId}"
        };

        _notifiers[notifierId] = state;
        state.WatcherThread.Start();
    }

    private bool CheckNotifierReady(Notifier notifier)
    {
        // Placeholder - in real implementation would check actual file descriptor
        // For now, return false to avoid spinning
        return false;
    }

    public void UnregisterNotifier(Notifier notifier)
    {
        var key = _notifiers.Keys.FirstOrDefault(k =>
            _notifiers.TryGetValue(k, out var state) && state.Notifier == notifier);

        if (key != 0 && _notifiers.TryRemove(key, out var notifierState))
        {
            notifierState.Cancellation.Cancel();
            notifierState.WatcherThread?.Join(100);
            notifierState.ReadyEvent?.Dispose();
            notifierState.Cancellation?.Dispose();
        }
    }

    public virtual void Dispose()
    {
        // Clean up all timers
        foreach (var state in _timers.Values)
        {
            state.Timer?.Dispose();
            state.ReadyEvent?.Dispose();
        }
        _timers.Clear();

        // Clean up all notifiers
        foreach (var state in _notifiers.Values)
        {
            state.Cancellation?.Cancel();
            state.WatcherThread?.Join(100);
            state.ReadyEvent?.Dispose();
            state.Cancellation?.Dispose();
        }
        _notifiers.Clear();

        _wakeEvent?.Dispose();
    }
}