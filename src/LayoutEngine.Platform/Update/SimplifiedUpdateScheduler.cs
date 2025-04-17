using System;
using System.Collections.Generic;
using System.Diagnostics;
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;

namespace LayoutEngine.Platform.Update;

using System.Linq;
using System.Threading.Tasks;
using Contracts.LayoutSystem;
using Contracts.Platform.Lifecycle;
using Contracts.StyleSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Simplified update scheduler that processes visual updates in a single thread.
/// </summary>
public class SimplifiedUpdateScheduler : IUpdateScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator; // Added
    private readonly IStyleEngine _styleEngine;                         // Added
    private readonly ILayoutEngine _layoutEngine;                       // Added
    private readonly ILogger<SimplifiedUpdateScheduler> _logger;        // Added
    private readonly Dictionary<UpdatePriority, Queue<IVisualUpdate>> _updateQueues = new();
    private readonly UpdatePriority[] _priorityLevels;

    private bool _isPaused;
    private bool _isDisposed;
    private readonly Stopwatch _performanceTimer = new();

    /// <summary>
    /// Initializes a new instance of the SimplifiedUpdateScheduler.
    /// </summary>
    public SimplifiedUpdateScheduler(
        IEventAggregator eventAggregator,
        IDocumentLifecycleCoordinator lifecycleCoordinator,
        IStyleEngine styleEngine,
        ILayoutEngine layoutEngine,
        ILogger<SimplifiedUpdateScheduler>? logger = null)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _lifecycleCoordinator = lifecycleCoordinator ?? throw new ArgumentNullException(nameof(lifecycleCoordinator));
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
        _logger = logger ?? NullLogger<SimplifiedUpdateScheduler>.Instance;

        _priorityLevels = new[]
        {
            UpdatePriority.Critical, UpdatePriority.High, UpdatePriority.Normal, UpdatePriority.Low
        };

        foreach (var priority in _priorityLevels)
        {
            _updateQueues[priority] = new Queue<IVisualUpdate>();
        }
        _logger.LogInformation("SimplifiedUpdateScheduler initialized.");
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

        _logger.LogTrace("Scheduling update {UpdateId} ({UpdateType}) for element {ElementId} with priority {Priority}",
            update.Id, update.Type, update.Element?.Id ?? "document", priority);

        _updateQueues[priority].Enqueue(update);

        // Process updates immediately if not paused
        if (!_isPaused)
        {
            // Check if already processing to prevent re-entrancy if ProcessUpdate schedules more updates
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
        foreach (var priority in _priorityLevels)
        {
            var queue = _updateQueues[priority];
            var newQueue = new Queue<IVisualUpdate>(queue.Where(u => {
                if (u.Id == update.Id) {
                    found = true;
                    return false; // Exclude the update
                }
                return true; // Keep others
            }));
            _updateQueues[priority] = newQueue;
        }

        if (found) {
            _logger.LogTrace("Cancelled update {UpdateId}", update.Id);
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
        _logger.LogInformation("Update processing paused.");
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
            _logger.LogInformation("Update processing resumed.");
            ProcessUpdates(); // Process queued updates
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

        _logger.LogTrace("Processing up to {MaxUpdates} pending updates.", maxUpdatesPerCall);
        int updatesProcessed = 0;

        foreach (var priority in _priorityLevels)
        {
            var queue = _updateQueues[priority];
            while (queue.Count > 0 && updatesProcessed < maxUpdatesPerCall)
            {
                var update = queue.Dequeue();
                ProcessUpdate(update);
                updatesProcessed++;
            }
            if (updatesProcessed >= maxUpdatesPerCall)
            {
                _logger.LogTrace("Reached max updates per call ({MaxUpdates}). Will process more later.", maxUpdatesPerCall);
                // Schedule next processing batch if more updates exist
                if (GetPendingUpdateCount() > 0 && !_isPaused) {
                    // Simple immediate reschedule in simplified model
                    // In a real scheduler, this might post to the next frame/event loop iteration
                    ScheduleNextProcessing();
                }
                break;
            }
        }
        _logger.LogTrace("Finished processing batch. {UpdatesProcessed} updates processed.", updatesProcessed);
    }

    private void ScheduleNextProcessing() {
        // In this simplified scheduler, we just call ProcessUpdates again.
        // A real implementation might post to the UI thread or use Task.Run depending on threading model.
        _logger.LogTrace("Scheduling next batch of update processing.");
        // Using Task.Run to avoid potential stack overflow if updates schedule more updates immediately
        Task.Run(() => ProcessUpdates());
    }

    /// <summary>
    /// Processes a single update and publishes the appropriate events.
    /// </summary>
    private void ProcessUpdate(IVisualUpdate update)
    {
        _logger.LogDebug("Processing update {UpdateId} ({UpdateType}) for element {ElementId}",
            update.Id, update.Type, update.Element?.Id ?? "document");

        try
        {
            _performanceTimer.Restart();
            bool needsLayout = false;
            bool needsRender = false;

            switch (update.Type)
            {
                case UpdateType.Full:
                case UpdateType.Style:
                    if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InStyleRecalc)
                    {
                         if(_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InStyleRecalc))
                            _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
                         else
                              _logger.LogWarning("Cannot enter InStyleRecalc phase from {CurrentPhase}", _lifecycleCoordinator.CurrentPhase);
                    }
                    // Call the actual style processing method
                    _styleEngine.ProcessUpdatesAsync().GetAwaiter().GetResult(); // Use GetResult in simplified model
                    needsLayout = true; // Style changes usually require layout
                    break;

                case UpdateType.Layout:
                     if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InLayout)
                    {
                        if(_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InLayout))
                            _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InLayout);
                        else
                            _logger.LogWarning("Cannot enter InLayout phase from {CurrentPhase}", _lifecycleCoordinator.CurrentPhase);
                    }
                    // Call the actual layout processing method
                    _layoutEngine.ProcessUpdatesAsync().GetAwaiter().GetResult(); // Use GetResult in simplified model
                    needsRender = true; // Layout changes require render
                    break;

                case UpdateType.Render:
                     if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InRender)
                     {
                        if(_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.RenderReady))
                            _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);

                         if(_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InRender))
                            _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InRender);
                         else
                            _logger.LogWarning("Cannot enter InRender phase from {CurrentPhase}", _lifecycleCoordinator.CurrentPhase);
                     }
                    // Trigger render (in a real engine, this would call a render system)
                    _eventAggregator.Publish(new RenderInvalidatedEvent(null)); // Simplified: just publish event
                    // In a real system, RenderCompletedEvent would be published by the renderer
                    // For the mock, we might simulate completion here or in LayoutEngineMain
                    _eventAggregator.Publish(new RenderCompletedEvent());
                    if(_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.RenderReady))
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady); // Assume render completes quickly
                    break;

                case UpdateType.Resource:
                    _logger.LogTrace("Processing Resource update (no specific action taken in simplified scheduler).");
                    // Handle resource updates if needed
                    break;
            }

            _performanceTimer.Stop();

            // Publish event that this specific update was processed
            _eventAggregator.Publish(
                new UpdateProcessedEvent(update, _performanceTimer.Elapsed.TotalMilliseconds),
                EventPriority.Normal);

            // --- Lifecycle transitions based on processing completion (Simplified) ---
            // These would normally happen in response to StyleComputed/LayoutUpdated events handled by LayoutEngineMain/Coordinator
            if (update.Type == UpdateType.Style || update.Type == UpdateType.Full)
            {
                if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
                {
                    if(_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InStyleRecalc, DocumentLifecyclePhase.StyleClean))
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.StyleClean);
                }
            }
             if (update.Type == UpdateType.Layout || (update.Type == UpdateType.Full && needsLayout)) // If full implies layout
            {
                if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InLayout)
                {
                     if(_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InLayout, DocumentLifecyclePhase.LayoutClean))
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.LayoutClean);
                }
            }
             if (update.Type == UpdateType.Render || (update.Type == UpdateType.Layout && needsRender) || (update.Type == UpdateType.Full && needsRender)) // If layout/full implies render
             {
                  if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InRender)
                  {
                      if(_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InRender, DocumentLifecyclePhase.RenderReady))
                          _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                  }
             }
             // --- End Lifecycle ---


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update {UpdateId} ({UpdateType})", update.Id, update.Type);
            // Optionally publish an error event
             _eventAggregator.Publish(
                new UpdateProcessedEvent(update, _performanceTimer.Elapsed.TotalMilliseconds, false, ex),
                EventPriority.Normal);
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
        _logger.LogInformation("SimplifiedUpdateScheduler disposing.");
        foreach (var queue in _updateQueues.Values)
        {
            queue.Clear();
        }
    }
}