// Base: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/ThreadEventQueue.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/Libraries/LibCore/ThreadEventQueue.cpp

// Copyright (c) 2023, Andreas Kling <andreas@ladybird.org>
//
// SPDX-License-Identifier: BSD-2-Clause

namespace LadyBird.Libraries.LibCore.Events;

using System;
using System.Collections.Generic;
using System.Threading;
using AK;

// Per-thread global event queue. This is where events are queued for the EventLoop to process.
// There is only one ThreadEventQueue per thread, and it is accessed via ThreadEventQueue.Current().
// It is allowed to post events to other threads' event queues.
public sealed class ThreadEventQueue
{
    // AK_MAKE_NONCOPYABLE(ThreadEventQueue);
    // AK_MAKE_NONMOVABLE(ThreadEventQueue);

    private static readonly ThreadLocal<ThreadEventQueue> _current =
        new ThreadLocal<ThreadEventQueue>(() => new ThreadEventQueue());

    private readonly Private _private;

    public static ThreadEventQueue Current()
    {
        // C++: pthread_once, pthread_key_create, pthread_getspecific, etc.
        // C#: Use ThreadLocal<T> for per-thread singleton.
        return _current.Value!;
    }

    // Process all queued events. Returns the number of events that were processed.
    public int Process()
    {
        // C++: decltype(m_private->queued_events) events;
        List<Private.QueuedEvent> events;
        lock (_private.Mutex)
        {
            events = new List<Private.QueuedEvent>(_private.QueuedEvents);
            _private.QueuedEvents.Clear();
            _private.PendingPromises.RemoveAll(job => job.IsResolved() || job.IsRejected());
        }

        int processedEvents = 0;
        for (int i = 0; i < events.Count; ++i)
        {
            var queuedEvent = events[i];
            // C++: WeakPtr<EventReceiver> -> C#: WeakReference<EventReceiver>
            if (!queuedEvent.Receiver.TryGetTarget(out var receiver))
            {
                switch ((Event.EventType)queuedEvent.Event.Type)
                {
                    case Event.EventType.Quit:
                        // C++: VERIFY_NOT_REACHED();
                        throw new InvalidOperationException("Quit event with no receiver.");
                    default:
                        // Receiver disappeared, drop the event on the floor.
                        break;
                }
            }
            else if (queuedEvent.Event.Type == (uint)Event.EventType.DeferredInvoke)
            {
                // C++: static_cast<DeferredInvocationEvent&>(event).m_invokee();
                ((DeferredInvocationEvent)queuedEvent.Event).Invokee();
            }
            else
            {
                // C++: NonnullRefPtr<EventReceiver> protector(*receiver);
                // C#: Not needed, just use receiver.
                receiver.DispatchEvent(queuedEvent.Event);
            }
            ++processedEvents;
        }

        lock (_private.Mutex)
        {
            if (_private.PendingPromises.Count > 30 && !_private.WarnedPromiseCount)
            {
                _private.WarnedPromiseCount = true;
                // C++: dbgln
                Console.WriteLine(
                    $"ThreadEventQueue.Process: Job queue wasn't designed for this load ({_private.PendingPromises.Count} promises)");
            }
        }
        return processedEvents;
    }

    // Posts an event to the event queue.
    public void PostEvent(EventReceiver receiver, Event @event)
    {
        lock (_private.Mutex)
        {
            _private.QueuedEvents.Add(new Private.QueuedEvent(receiver, @event));
        }
        EventLoopManager.The().DidPostEvent();
    }

    // Used by Threading.BackgroundAction.
    public void AddJob(Promise<EventReceiver> promise)
    {
        lock (_private.Mutex)
        {
            _private.PendingPromises.Add(promise);
        }
    }

    public void CancelAllPendingJobs()
    {
        lock (_private.Mutex)
        {
            foreach (var promise in _private.PendingPromises)
                promise.Reject(Error.FromErrno(ECanceled));
            _private.PendingPromises.Clear();
        }
    }

    // Returns true if there are events waiting to be flushed.
    public bool HasPendingEvents()
    {
        lock (_private.Mutex)
        {
            return _private.QueuedEvents.Count > 0;
        }
    }

    private ThreadEventQueue()
    {
        _private = new Private();
    }

    ~ThreadEventQueue()
    {
        // C++: default destructor
    }

    private class Private
    {
        public class QueuedEvent
        {
            // AK_MAKE_NONCOPYABLE(QueuedEvent);
            // AK_MAKE_DEFAULT_MOVABLE(QueuedEvent);

            public WeakReference<EventReceiver> Receiver { get; }
            public Event Event { get; }

            public QueuedEvent(EventReceiver receiver, Event @event)
            {
                Receiver = new WeakReference<EventReceiver>(receiver);
                Event = @event;
            }
        }

        public object Mutex { get; } = new object();
        public List<QueuedEvent> QueuedEvents { get; } = new List<QueuedEvent>();
        public List<Promise<EventReceiver>> PendingPromises { get; } = new List<Promise<EventReceiver>>(16);
        public bool WarnedPromiseCount { get; set; } = false;
    }

    // C++: ECANCELED
    private const int ECanceled = 125;
}