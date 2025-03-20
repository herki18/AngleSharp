using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LayoutEngine.Contracts.Threading;

namespace LayoutEngine.Platform.Update;

using Contracts.Platform.Events;
using Contracts.Platform.Updates;
using Contracts.Resource;
using Infrastructure.EventAggregator.API.Aggregation;

/// <summary>
/// Schedules and prioritizes visual updates.
/// Manages the update queue and ensures updates are processed in priority order.
/// </summary>
public sealed class UpdateScheduler : IUpdateScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly ConcurrentDictionary<Guid, IVisualUpdate> _pendingUpdates = new();
    private readonly ConcurrentDictionary<UpdatePriority, ConcurrentQueue<IVisualUpdate>> _priorityQueues = new();
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private readonly UpdatePriority[] _priorityLevels;
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private bool _isPaused;
    private bool _isProcessing;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateScheduler"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing update events.</param>
    /// <param name="threadingCoordinator">The threading coordinator for thread scheduling.</param>
    public UpdateScheduler(
        IEventAggregator eventAggregator,
        IThreadingCoordinator threadingCoordinator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));

        // Initialize priority levels from highest to lowest
        _priorityLevels = new[]
        {
            UpdatePriority.Critical,
            UpdatePriority.High,
            UpdatePriority.Normal,
            UpdatePriority.Low
        };

        // Initialize priority queues
        foreach (var priority in _priorityLevels)
        {
            _priorityQueues[priority] = new ConcurrentQueue<IVisualUpdate>();
        }

        // Subscribe to memory pressure events
        _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));

        // Subscribe to frame events
        _subscriptions.Add(_eventAggregator.Subscribe<BeginFrameEvent>(OnBeginFrame));
    }

    /// <summary>
    /// Schedules a visual update.
    /// </summary>
    /// <param name="update">The update to schedule.</param>
    public void ScheduleUpdate(IVisualUpdate update)
    {
        ScheduleUpdate(update, UpdatePriority.Normal);
    }

    /// <summary>
    /// Schedules a visual update with a specific priority.
    /// </summary>
    /// <param name="update">The update to schedule.</param>
    /// <param name="priority">The priority level for the update.</param>
    public void ScheduleUpdate(IVisualUpdate update, UpdatePriority priority)
    {
        ThrowIfDisposed();

        if (update == null)
            throw new ArgumentNullException(nameof(update));

        // Add to pending updates
        if (!_pendingUpdates.TryAdd(update.Id, update))
        {
            // Update already pending, return
            return;
        }

        // Add to priority queue
        var queue = _priorityQueues[priority];
        queue.Enqueue(update);

        // Schedule processing if not already processing and not paused
        if (!_isProcessing && !_isPaused)
        {
            ScheduleProcessing();
        }
    }

    /// <summary>
    /// Cancels a pending update.
    /// </summary>
    /// <param name="update">The update to cancel.</param>
    /// <returns>True if the update was canceled, otherwise false.</returns>
    public bool CancelUpdate(IVisualUpdate update)
    {
        ThrowIfDisposed();

        if (update == null)
            throw new ArgumentNullException(nameof(update));

        // Remove from pending updates
        return _pendingUpdates.TryRemove(update.Id, out _);
    }

    /// <summary>
    /// Pauses all update processing.
    /// </summary>
    public void PauseUpdates()
    {
        ThrowIfDisposed();
        _isPaused = true;
    }

    /// <summary>
    /// Resumes update processing.
    /// </summary>
    public void ResumeUpdates()
    {
        ThrowIfDisposed();

        if (_isPaused)
        {
            _isPaused = false;

            // Schedule processing if we have pending updates
            if (_pendingUpdates.Count > 0 && !_isProcessing)
            {
                ScheduleProcessing();
            }
        }
    }

    /// <summary>
    /// Handles memory pressure events by adjusting update processing.
    /// </summary>
    private void OnMemoryPressure(MemoryPressureEvent e)
    {
        // Under high memory pressure, cancel non-critical updates
        if (e.Severity >= MemoryPressureSeverity.High)
        {
            CancelNonCriticalUpdates();
        }
    }

    /// <summary>
    /// Handles begin frame events by processing pending updates.
    /// </summary>
    private void OnBeginFrame(BeginFrameEvent e)
    {
        // Process updates if not already processing and not paused
        if (!_isProcessing && !_isPaused)
        {
            ScheduleProcessing();
        }
    }

    /// <summary>
    /// Cancels all non-critical updates to reduce memory pressure.
    /// </summary>
    private void CancelNonCriticalUpdates()
    {
        // Get copy of pending updates
        var updates = new List<IVisualUpdate>(_pendingUpdates.Values);

        // Cancel all non-critical updates
        foreach (var update in updates)
        {
            if (update.Type != UpdateType.Full)
            {
                CancelUpdate(update);
            }
        }
    }

    /// <summary>
    /// Schedules update processing on the appropriate thread.
    /// </summary>
    private void ScheduleProcessing()
    {
        _isProcessing = true;

        // Schedule processing on main thread
        _threadingCoordinator.ScheduleOnMainThread(ProcessUpdates);
    }

    /// <summary>
    /// Processes all pending updates in priority order.
    /// </summary>
    private async void ProcessUpdates()
    {
        if (_isDisposed || _isPaused)
        {
            _isProcessing = false;
            return;
        }

        try
        {
            // Ensure only one thread is processing updates
            await _processingLock.WaitAsync();

            try
            {
                // Process updates in priority order
                foreach (var priority in _priorityLevels)
                {
                    ProcessPriorityQueue(priority);
                }
            }
            finally
            {
                _processingLock.Release();
            }
        }
        catch (Exception)
        {
            // Log exception
        }
        finally
        {
            _isProcessing = false;

            // If we still have updates, schedule another processing pass
            if (_pendingUpdates.Count > 0 && !_isPaused)
            {
                ScheduleProcessing();
            }
        }
    }

    /// <summary>
    /// Processes all updates in a specific priority queue.
    /// </summary>
    /// <param name="priority">The priority queue to process.</param>
    private void ProcessPriorityQueue(UpdatePriority priority)
    {
        var queue = _priorityQueues[priority];

        // Process all updates in this queue
        while (queue.TryDequeue(out var update))
        {
            // Check if update is still pending
            if (_pendingUpdates.TryRemove(update.Id, out _))
            {
                try
                {
                    // Process the update
                    ProcessUpdate(update);
                }
                catch (Exception)
                {
                    // Log exception
                }
            }
        }
    }

    /// <summary>
    /// Processes a single update.
    /// </summary>
    /// <param name="update">The update to process.</param>
    private void ProcessUpdate(IVisualUpdate update)
    {
        // Publish event based on update type
        switch (update.Type)
        {
            case UpdateType.Style:
                _eventAggregator.Publish(new StyleInvalidatedEvent(new[] { update.Element }));
                break;

            case UpdateType.Layout:
                _eventAggregator.Publish(new LayoutInvalidatedEvent(new[] { update.Element }));
                break;

            case UpdateType.Render:
                _eventAggregator.Publish(new RenderInvalidatedEvent(null));
                break;

            case UpdateType.Resource:
                // Handle resource update
                break;

            case UpdateType.Full:
                // Full update - invalidate all
                _eventAggregator.Publish(new StyleInvalidatedEvent(new[] { update.Element }));
                break;
        }

        // Publish update processed event
        _eventAggregator.Publish(new UpdateProcessedEvent(update));
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(UpdateScheduler));
        }
    }

    /// <summary>
    /// Disposes the UpdateScheduler and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Unsubscribe from events
        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }

        _subscriptions.Clear();

        // Clear queues
        _pendingUpdates.Clear();

        foreach (var queue in _priorityQueues.Values)
        {
            while (queue.TryDequeue(out _)) { }
        }

        _priorityQueues.Clear();

        // Dispose processing lock
        _processingLock.Dispose();
    }
}