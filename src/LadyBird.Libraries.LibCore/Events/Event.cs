// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/Event.h
// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/Event.cpp

namespace LadyBird.Libraries.LibCore.Events;

using System;

public class Event : IDisposable
{
    public enum EventType
    {
        Invalid = 0,
        Quit,
        Timer,
        NotifierActivation,
        DeferredInvoke,
        ChildAdded,
        ChildRemoved,
        Custom,
    }

    public Event()
    {
        _type = (uint)EventType.Invalid;
    }

    public Event(uint type)
    {
        _type = type;
    }

    public virtual void Dispose() { }

    public uint Type => _type;

    public bool IsAccepted => _accepted;
    public void Accept() => _accepted = true;
    public void Ignore() => _accepted = false;

    private uint _type = (uint)EventType.Invalid;
    private bool _accepted = true;
}

public class DeferredInvocationEvent : Event
{
    // friend class EventLoop;
    // friend class ThreadEventQueue;

    public DeferredInvocationEvent(DeferredInvocationContext context, Action invokee)
        : base((uint)EventType.DeferredInvoke)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _invokee = invokee ?? throw new ArgumentNullException(nameof(invokee));
    }

    internal DeferredInvocationContext Context => _context;
    internal Action Invokee => _invokee;

    private readonly DeferredInvocationContext _context;
    private readonly Action _invokee;
}

public class TimerEvent : Event
{
    public TimerEvent()
        : base((uint)EventType.Timer)
    {
    }
}

[Flags]
public enum NotificationType
{
    None = 0,
    Read = 1,
    Write = 2,
    HangUp = 4,
    Error = 8,
}

public class NotifierActivationEvent : Event
{
    public NotifierActivationEvent(int fd, NotificationType type)
        : base((uint)EventType.NotifierActivation)
    {
        _fd = fd;
        _type = type;
    }

    public int Fd => _fd;
    // TODO: Not sure if this should be overridden or not
    public new NotificationType Type => _type;

    private readonly int _fd;
    private readonly NotificationType _type;
}

public class ChildEvent : Event
{
    public ChildEvent(EventType type, EventReceiver child, EventReceiver? insertionBeforeChild = null)
        : base((uint)type)
    {
        // Converted from WeakPtr - using WeakReference<T> in C#
        _child = new WeakReference<EventReceiver>(child ?? throw new ArgumentNullException(nameof(child)));
        _insertionBeforeChild = insertionBeforeChild != null ? new WeakReference<EventReceiver>(insertionBeforeChild) : null;
    }

    public EventReceiver? Child
    {
        get
        {
            // Converted from WeakPtr::strong_ref() - using TryGetTarget in C#
            if (_child.TryGetTarget(out var child))
                return child;
            return null;
        }
    }

    public EventReceiver? InsertionBeforeChild
    {
        get
        {
            // Converted from WeakPtr::strong_ref() - using TryGetTarget in C#
            if (_insertionBeforeChild != null && _insertionBeforeChild.TryGetTarget(out var child))
                return child;
            return null;
        }
    }

    private readonly WeakReference<EventReceiver> _child;
    private readonly WeakReference<EventReceiver>? _insertionBeforeChild;
}

public class CustomEvent : Event
{
    public CustomEvent(int customType)
        : base((uint)EventType.Custom)
    {
        _customType = customType;
    }

    public int CustomType => _customType;

    private readonly int _customType = 0;
}