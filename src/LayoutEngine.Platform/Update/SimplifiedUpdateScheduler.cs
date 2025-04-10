using System;
using System.Collections.Generic;
using System.Diagnostics;
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;

namespace LayoutEngine.Platform.Update;

/// <summary>
/// Simplified update scheduler that processes visual updates in a single thread.
/// </summary>
public class SimplifiedUpdateScheduler : IUpdateScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly Dictionary<UpdatePriority, Queue<IVisualUpdate>> _updateQueues = new();
    private readonly UpdatePriority[] _priorityLevels;

    private bool _isPaused;
    private bool _isDisposed;
    private readonly Stopwatch _performanceTimer = new();

    /// <summary>
    /// Initializes a new instance of the SimplifiedUpdateScheduler.
    /// </summary>
    public SimplifiedUpdateScheduler(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        // Define priority levels in processing order
        _priorityLevels = new[]
        {
            UpdatePriority.Critical,
            UpdatePriority.High,
            UpdatePriority.Normal,
            UpdatePriority.Low
        };

        // Initialize queues for each priority
        foreach (var priority in _priorityLevels)
        {
            _updateQueues[priority] = new Queue<IVisualUpdate>();
        }
    }

    /// <summary>
    /// Schedules a visual update with normal priority.
    /// </summary>
    public void ScheduleUpdate(IVisualUpdate update)
    {
        ScheduleUpdate(update, UpdatePriority.Normal);
    }

    /// <summary>
    /// Schedules a visual update with specified priority.
    /// </summary>
    public void ScheduleUpdate(IVisualUpdate update, UpdatePriority priority)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));

        if (update == null)
            throw new ArgumentNullException(nameof(update));

        // Add update to appropriate queue
        _updateQueues[priority].Enqueue(update);

        // Process updates immediately if we're not paused
        if (!_isPaused)
        {
            ProcessUpdates();
        }
    }

    /// <summary>
    /// Cancels a previously scheduled update if it hasn't been processed yet.
    /// </summary>
    public bool CancelUpdate(IVisualUpdate update)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));

        if (update == null)
            throw new ArgumentNullException(nameof(update));

        bool found = false;

        // We need to rebuild each queue without the canceled update
        foreach (var priority in _priorityLevels)
        {
            var queue = _updateQueues[priority];
            var newQueue = new Queue<IVisualUpdate>();

            while (queue.Count > 0)
            {
                var queuedUpdate = queue.Dequeue();

                // If this is the update to cancel, skip it
                if (queuedUpdate.Id == update.Id)
                {
                    found = true;
                    continue;
                }

                // Otherwise, keep it in the queue
                newQueue.Enqueue(queuedUpdate);
            }

            // Replace the old queue with the new one
            _updateQueues[priority] = newQueue;
        }

        return found;
    }

    /// <summary>
    /// Pauses processing of updates.
    /// </summary>
    public void PauseUpdates()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));

        _isPaused = true;
    }

    /// <summary>
    /// Resumes processing of updates.
    /// </summary>
    public void ResumeUpdates()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));

        if (_isPaused)
        {
            _isPaused = false;

            // Process any queued updates immediately
            ProcessUpdates();
        }
    }

    /// <summary>
    /// Processes pending updates.
    /// </summary>
    /// <param name="maxUpdatesPerCall">Maximum number of updates to process per call. Default: 20.</param>
    public void ProcessUpdates(int maxUpdatesPerCall = 20)
    {
        if (_isDisposed || _isPaused)
            return;

        int updatesProcessed = 0;

        // Process updates by priority until we hit the limit
        foreach (var priority in _priorityLevels)
        {
            var queue = _updateQueues[priority];

            while (queue.Count > 0 && updatesProcessed < maxUpdatesPerCall)
            {
                var update = queue.Dequeue();
                ProcessUpdate(update);
                updatesProcessed++;
            }

            // If we've hit the limit, stop processing
            if (updatesProcessed >= maxUpdatesPerCall)
                break;
        }
    }

    /// <summary>
    /// Processes a single update and publishes the appropriate events.
    /// </summary>
    private void ProcessUpdate(IVisualUpdate update)
    {
        try
        {
            _performanceTimer.Restart();

            // Process the update based on type
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
                    // Handle resource updates if needed
                    break;

                case UpdateType.Full:
                    // For full updates, we trigger style updates first
                    // Layout and render will be triggered by the lifecycle coordinator
                    _eventAggregator.Publish(new StyleInvalidatedEvent(new[] { update.Element }));
                    break;
            }

            _performanceTimer.Stop();

            // Publish an event that the update was processed
            _eventAggregator.Publish(
                new UpdateProcessedEvent(update, _performanceTimer.Elapsed.TotalMilliseconds),
                EventPriority.Normal);
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.Error.WriteLine($"Error processing update: {ex}");
        }
    }

    /// <summary>
    /// Gets the count of pending updates across all priority queues.
    /// </summary>
    public int GetPendingUpdateCount()
    {
        int count = 0;
        foreach (var queue in _updateQueues.Values)
        {
            count += queue.Count;
        }
        return count;
    }

    /// <summary>
    /// Disposes the update scheduler and clears all queues.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Clear all queues
        foreach (var queue in _updateQueues.Values)
        {
            queue.Clear();
        }
    }
}