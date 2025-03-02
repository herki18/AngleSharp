namespace AngleSharp.Dom;

using System;
using Attributes;
using Events;

[DomName("EventTarget")]
public interface IEventTarget
{
    [DomName("addEventListener")]
    void AddEventListener(String type, DomEventHandler? callback = null, Boolean capture = false);

    [DomName("removeEventListener")]
    void RemoveEventListener(String type, DomEventHandler? callback = null, Boolean capture = false);

    void InvokeEventListener(IEvent ev);

    [DomName("dispatchEvent")]
    Boolean Dispatch(IEvent ev);

    event EventHandler<EventSyncedArgs>? EventSynced;

    event EventHandler<EventUnregisteredArgs>? EventUnregistered;
}

public class EventSyncedArgs : EventArgs
{
    public EventSyncedArgs(String eventType, DomEventHandler callback)
    {
        EventType = eventType;
        Callback = callback;
    }

    public String EventType { get; }

    public DomEventHandler Callback { get; }
}

public class EventUnregisteredArgs : EventArgs
{
    public EventUnregisteredArgs(String eventType)
    {
        EventType = eventType;
    }

    public String EventType { get; }
}