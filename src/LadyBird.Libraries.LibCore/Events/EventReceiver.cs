// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventReceiver.h
// Based: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/EventReceiver.cpp

namespace LadyBird.Libraries.LibCore.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using AK;

public enum TimerShouldFireWhenNotVisible
{
    No = 0,
    Yes
}

public abstract class EventReceiver : IDisposable
{
    // AK_MAKE_NONCOPYABLE(EventReceiver);
    // AK_MAKE_NONMOVABLE(EventReceiver);

    public EventReceiver(EventReceiver? parent = null)
    {
        _parent = parent;
        if (_parent != null)
            _parent.AddChild(this);
    }

    public virtual void Dispose()
    {
        // NOTE: We move our children out to a stack vector to prevent other
        //       code from trying to iterate over them.
        var children = _children.ToList();
        _children.Clear();

        // NOTE: We also unparent the children, so that they won't try to unparent
        //       themselves in their own destructors.
        foreach (var child in children)
            child._parent = null;

        StopTimer();
        _parent?.RemoveChild(this);
    }

    public virtual string ClassName()
    {
        return GetType().Name;
    }

    public virtual bool IsWidget => false;

    public string Name
    {
        get => _name;
        set => _name = value;
    }

    public List<EventReceiver> Children => _children;

    public void ForEachChild(Func<EventReceiver, IterationDecision> callback)
    {
        foreach (var child in _children)
        {
            if (callback(child) == IterationDecision.Break)
                return;
        }
    }

    public void ForEachChildOfType<T>(Func<T, IterationDecision> callback) where T : EventReceiver
    {
        ForEachChild(child =>
        {
            if (child is T typedChild)
                return callback(typedChild);
            return IterationDecision.Continue;
        });
    }

    public T? FindChildOfTypeNamed<T>(string name) where T : EventReceiver
    {
        T? foundChild = null;
        ForEachChildOfType<T>(child =>
        {
            if (child.Name == name)
            {
                foundChild = child;
                return IterationDecision.Break;
            }
            return IterationDecision.Continue;
        });
        return foundChild;
    }

    public T? FindDescendantOfTypeNamed<T>(string name) where T : EventReceiver
    {
        if (this is T typedThis && Name == name)
        {
            return typedThis;
        }

        T? foundChild = null;
        ForEachChild(child =>
        {
            foundChild = child.FindDescendantOfTypeNamed<T>(name);
            if (foundChild != null)
                return IterationDecision.Break;
            return IterationDecision.Continue;
        });
        return foundChild;
    }

    public bool IsAncestorOf(EventReceiver other)
    {
        if (other == this)
            return false;

        for (var ancestor = other.Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor == this)
                return true;
        }
        return false;
    }

    public EventReceiver? Parent => _parent;

    public void StartTimer(int ms, TimerShouldFireWhenNotVisible fireWhenNotVisible = TimerShouldFireWhenNotVisible.No)
    {
        if (_timerId != IntPtr.Zero)
        {
            Console.WriteLine($"{ClassName()} {this:X} already has a timer!");
            throw new InvalidOperationException("Already has a timer");
        }

        _timerId = EventLoop.RegisterTimer(this, ms, true, fireWhenNotVisible);
    }

    public void StopTimer()
    {
        if (_timerId == IntPtr.Zero)
            return;
        EventLoop.UnregisterTimer(_timerId);
        _timerId = IntPtr.Zero;
    }

    public bool HasTimer => _timerId != IntPtr.Zero;

    public Result<bool> TryAddChild(EventReceiver child)
    {
        // FIXME: Should we support reparenting objects?
        if (child.Parent != null && child.Parent != this)
            throw new InvalidOperationException("Cannot reparent object");

        try
        {
            _children.Add(child);
            child._parent = this;
            var childEvent = new ChildEvent(Events.Event.EventType.ChildAdded, child);
            Event(childEvent);
            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e);
        }
    }

    public void AddChild(EventReceiver child)
    {
        var result = TryAddChild(child);
        if (result.IsFailure)
            throw result.Error!;
    }

    public void InsertChildBefore(EventReceiver newChild, EventReceiver beforeChild)
    {
        // FIXME: Should we support reparenting objects?
        if (newChild.Parent != null && newChild.Parent != this)
            throw new InvalidOperationException("Cannot reparent object");

        newChild._parent = this;
        var index = _children.FindIndex(child => child == beforeChild);
        if (index >= 0)
            _children.Insert(index, newChild);
        else
            _children.Add(newChild);

        var childEvent = new ChildEvent(Events.Event.EventType.ChildAdded, newChild, beforeChild);
        Event(childEvent);
    }

    public void RemoveChild(EventReceiver child)
    {
        for (int i = 0; i < _children.Count; i++)
        {
            if (_children[i] == child)
            {
                // NOTE: We protect the child so it survives the handling of ChildRemoved.
                child._parent = null;
                _children.RemoveAt(i);
                var childEvent = new ChildEvent(Events.Event.EventType.ChildRemoved, child);
                Event(childEvent);
                return;
            }
        }
        throw new InvalidOperationException("Child not found");
    }

    public void RemoveAllChildren()
    {
        while (_children.Count > 0)
            _children[0].RemoveFromParent();
    }

    public void SetEventFilter(Func<Event, bool>? filter)
    {
        _eventFilter = filter;
    }

    public void DeferredInvoke(Action invokee)
    {
        EventLoopHelpers.DeferredInvoke(() => invokee());
    }

    public void DispatchEvent(Event e, EventReceiver? stayWithin = null)
    {
        if (stayWithin != null && stayWithin != this && !stayWithin.IsAncestorOf(this))
            throw new InvalidOperationException("Invalid stayWithin parameter");

        var target = this;
        do
        {
            // If there's an event filter on this target, ask if it wants to swallow this event.
            if (target._eventFilter != null && !target._eventFilter(e))
                return;

            target.Event(e);
            target = target.Parent;

            if (target == stayWithin)
            {
                // Prevent the event from bubbling any further.
                return;
            }
        } while (target != null && !e.IsAccepted);
    }

    public virtual bool IsVisibleForTimerPurposes()
    {
        if (Parent != null)
            return Parent.IsVisibleForTimerPurposes();
        return true;
    }

    public void RemoveFromParent()
    {
        _parent?.RemoveChild(this);
        // The call to `remove_child` may have deleted the object.
        // Do not dereference `this` from this point forward.
    }

    public T Add<T>(params object[] args) where T : EventReceiver
    {
        var child = (T)Activator.CreateInstance(typeof(T), args)!;
        AddChild(child);
        return child;
    }

    public Result<T> TryAdd<T>(params object[] args) where T : EventReceiver
    {
        try
        {
            var child = (T)Activator.CreateInstance(typeof(T), args)!;
            var result = TryAddChild(child);
            if (result.IsFailure)
                return Result<T>.Failure(result.Error!);
            return Result<T>.Success(child);
        }
        catch (Exception e)
        {
            return Result<T>.Failure(e);
        }
    }

    public virtual void Event(Event e)
    {
        switch (e.Type)
        {
            case (uint)Events.Event.EventType.Timer:
                if (_timerId == IntPtr.Zero)
                    break; // Too late, the timer was already stopped.
                TimerEvent((TimerEvent)e);
                return;

            case (uint)Events.Event.EventType.ChildAdded:
            case (uint)Events.Event.EventType.ChildRemoved:
                ChildEvent((ChildEvent)e);
                return;

            case (uint)Events.Event.EventType.Invalid:
                throw new InvalidOperationException("Invalid event type");

            case (uint)Events.Event.EventType.Custom:
                CustomEvent((CustomEvent)e);
                return;

            default:
                break;
        }
    }

    protected virtual void TimerEvent(TimerEvent e) { }

    // NOTE: You may get child events for children that are not yet fully constructed!
    protected virtual void ChildEvent(ChildEvent e) { }

    protected virtual void CustomEvent(CustomEvent e) { }

    private EventReceiver? _parent;
    private string _name = "";
    private IntPtr _timerId = IntPtr.Zero;
    private readonly List<EventReceiver> _children = new();
    private Func<Event, bool>? _eventFilter;
}