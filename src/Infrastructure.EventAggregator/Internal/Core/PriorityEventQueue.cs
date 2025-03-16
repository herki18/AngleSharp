using System.Collections.Generic;
using Infrastructure.EventAggregator.API.Events;

namespace Infrastructure.EventAggregator.Internal.Core;

/// <summary>
/// A priority queue implementation for event processing.
/// Events with higher priority are dequeued before events with lower priority.
/// Events with the same priority are processed in FIFO order.
/// </summary>
internal class PriorityEventQueue
{
    private readonly Dictionary<EventPriority, Queue<PrioritizedEventWrapper>> _queues;
    private readonly object _lockObject = new object();

    /// <summary>
    /// Initializes a new instance of the PriorityEventQueue class.
    /// </summary>
    public PriorityEventQueue()
    {
        _queues = new Dictionary<EventPriority, Queue<PrioritizedEventWrapper>>();
        // Initialize queues for all priority levels
        foreach (EventPriority priority in System.Enum.GetValues(typeof(EventPriority)))
        {
            _queues[priority] = new Queue<PrioritizedEventWrapper>();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the queue is empty.
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            lock (_lockObject)
            {
                foreach (var queue in _queues.Values)
                {
                    if (queue.Count > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Enqueues an event with its associated priority.
    /// </summary>
    /// <param name="wrapper">The event wrapper to enqueue.</param>
    public void Enqueue(PrioritizedEventWrapper wrapper)
    {
        lock (_lockObject)
        {
            _queues[wrapper.Priority].Enqueue(wrapper);
        }
    }

    /// <summary>
    /// Dequeues the highest priority event available.
    /// </summary>
    /// <returns>The highest priority event, or null if the queue is empty.</returns>
    public PrioritizedEventWrapper Dequeue()
    {
        lock (_lockObject)
        {
            // Check queues from highest to lowest priority
            foreach (EventPriority priority in System.Enum.GetValues(typeof(EventPriority)))
            {
                // Check in reverse order (highest to lowest)
                EventPriority currentPriority = (EventPriority)((int)System.Enum.GetValues(typeof(EventPriority)).Length - 1 - (int)priority);

                if (_queues[currentPriority].Count > 0)
                {
                    return _queues[currentPriority].Dequeue();
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Peeks at the highest priority event without removing it.
    /// </summary>
    /// <returns>The highest priority event, or null if the queue is empty.</returns>
    public PrioritizedEventWrapper Peek()
    {
        lock (_lockObject)
        {
            // Check queues from highest to lowest priority
            foreach (EventPriority priority in System.Enum.GetValues(typeof(EventPriority)))
            {
                // Check in reverse order (highest to lowest)
                EventPriority currentPriority = (EventPriority)((int)System.Enum.GetValues(typeof(EventPriority)).Length - 1 - (int)priority);

                if (_queues[currentPriority].Count > 0)
                {
                    return _queues[currentPriority].Peek();
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Clears all queues.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            foreach (var queue in _queues.Values)
            {
                queue.Clear();
            }
        }
    }
}