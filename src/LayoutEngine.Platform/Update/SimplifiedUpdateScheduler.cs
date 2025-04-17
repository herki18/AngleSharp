using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks; // Added for Task
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.StyleSystem;
using LayoutEngine.Contracts.LayoutSystem;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LayoutEngine.Platform.Update;

/// <summary>
/// Simplified update scheduler that processes visual updates sequentially in a single thread,
/// respecting a time budget per processing call. It directly calls subsystem processing methods.
/// </summary>
public class SimplifiedUpdateScheduler : IUpdateScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IDocumentLifecycleCoordinator _lifecycleCoordinator;
    private readonly IStyleEngine _styleEngine;
    private readonly ILayoutEngine _layoutEngine;
    private readonly ILogger<SimplifiedUpdateScheduler> _logger;
    private readonly Dictionary<UpdatePriority, Queue<IVisualUpdate>> _updateQueues = new();
    private readonly UpdatePriority[] _priorityLevels; // Highest to lowest

    private bool _isProcessing = false; // Simple re-entrancy guard for sync processing
    private readonly object _processingLock = new object();
    private bool _isPaused;
    private bool _isDisposed;
    private readonly Stopwatch _batchTimer = new(); // Timer for the overall budget

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

        _priorityLevels = new[] { UpdatePriority.Critical, UpdatePriority.High, UpdatePriority.Normal, UpdatePriority.Low };
        foreach (var priority in _priorityLevels) { _updateQueues[priority] = new Queue<IVisualUpdate>(); }
        _logger.LogInformation("SimplifiedUpdateScheduler initialized.");
    }

    // --- Scheduling Methods ---
    public void ScheduleUpdate(IVisualUpdate update) => ScheduleUpdate(update, UpdatePriority.Normal);

    public void ScheduleUpdate(IVisualUpdate update, UpdatePriority priority)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));
        if (update == null) throw new ArgumentNullException(nameof(update));

        _logger.LogTrace("Scheduling update {Id} ({Type}) Priority: {Priority}", update.Id, update.Type, priority);
        // Enqueue based on priority
        _updateQueues[priority].Enqueue(update);
        // Note: Processing is NOT triggered here directly. It's driven by the host's calls to ProcessUpdates.
    }

    // CancelUpdate, PauseUpdates, ResumeUpdates remain the same...
    public bool CancelUpdate(IVisualUpdate update)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));
        if (update == null) throw new ArgumentNullException(nameof(update));
        bool found = false;
        foreach (var priority in _priorityLevels) {
            var queue = _updateQueues[priority];
            var newQueue = new Queue<IVisualUpdate>(queue.Where(u => {
                if (u.Id == update.Id) { found = true; return false; }
                return true;
            }));
            _updateQueues[priority] = newQueue;
        }
        if (found) _logger.LogTrace("Cancelled update {Id}", update.Id);
        return found;
    }
    public void PauseUpdates()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));
        _isPaused = true;
        _logger.LogInformation("Update processing paused.");
    }
    public void ResumeUpdates()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));
        _isPaused = false;
        _logger.LogInformation("Update processing resumed.");
    }

    // --- Processing Methods ---

    /// <summary>
    /// Processes pending updates asynchronously within the budget.
    /// Calls the synchronous version in this simplified implementation.
    /// </summary>
    public Task ProcessUpdatesAsync(double timeBudgetMilliseconds)
    {
        ProcessUpdates(timeBudgetMilliseconds);
        return Task.CompletedTask; // Return completed task as processing is sync here
    }

    /// <summary>
    /// Processes pending updates synchronously based on priority until the time budget is exceeded
    /// or all queues are empty. This is the primary method called by the host loop.
    /// </summary>
    public void ProcessUpdates(double timeBudgetMilliseconds)
    {
        if (_isDisposed || _isPaused) return;

        // Prevent re-entrancy if an update processing triggers another ProcessUpdates call indirectly
        lock (_processingLock)
        {
            if (_isProcessing)
            {
                _logger.LogTrace("Processing already in progress, skipping redundant call.");
                return;
            }
            _isProcessing = true;
        }

        _logger.LogTrace("Starting processing batch with budget: {Budget}ms. Pending: {PendingCount}", timeBudgetMilliseconds, GetPendingUpdateCount());
        _batchTimer.Restart(); // Start timing the whole batch
        int updatesProcessedThisBatch = 0;
        var stopwatch = Stopwatch.StartNew(); // For individual update timing

        try
        {
            foreach (var priority in _priorityLevels)
            {
                var queue = _updateQueues[priority];
                while (queue.Count > 0)
                {
                    // Check budget *before* processing the next item
                    if (_batchTimer.Elapsed.TotalMilliseconds >= timeBudgetMilliseconds && updatesProcessedThisBatch > 0)
                    {
                        _logger.LogDebug("Time budget ({Budget}ms) exceeded after processing {Count} updates. Stopping batch.", timeBudgetMilliseconds, updatesProcessedThisBatch);
                        goto EndProcessing; // Exit loops cleanly
                    }

                    var update = queue.Dequeue();
                    stopwatch.Restart();
                    bool errorHandled = false; // Flag specific to this update iteration

                    try
                    {
                        // --- Process the single update ---
                        ProcessSingleUpdate(update); // Call the synchronous helper
                        // If ProcessSingleUpdate completes without throwing, it's considered successful
                    }
                    catch (Exception ex) // Catch errors specifically from ProcessSingleUpdate
                    {
                        errorHandled = true; // Mark that we handled an error for this update
                        _logger.LogError(ex, "Error processing update {UpdateId} ({UpdateType})", update.Id, update.Type);
                        stopwatch.Stop(); // Stop timer before publishing error event
                        _eventAggregator.Publish(
                            new UpdateProcessedEvent(update, stopwatch.Elapsed.TotalMilliseconds, false, ex), // Publish ERROR event
                            EventPriority.Normal);
                    }
                    finally // Runs after try or catch for the single update
                    {
                        // Publish SUCCESS event only if no error was caught and handled above
                        if (!errorHandled)
                        {
                            stopwatch.Stop(); // Stop timer before publishing success event
                            _eventAggregator.Publish(
                                new UpdateProcessedEvent(update, stopwatch.Elapsed.TotalMilliseconds, true, null), // Publish SUCCESS event
                                EventPriority.Normal);
                        }
                    }
                    // ----------------------------------

                    updatesProcessedThisBatch++;
                }
            }
            EndProcessing:; // Label for budget break
            _batchTimer.Stop(); // Stop timing the batch
            if (updatesProcessedThisBatch > 0) {
                _logger.LogTrace("Finished processing batch. {Count} updates processed in {Duration:F2}ms.", updatesProcessedThisBatch, _batchTimer.Elapsed.TotalMilliseconds);
            }
        }
        catch(Exception ex) // Catch unexpected errors during the outer loop/budget check etc.
        {
             _logger.LogError(ex, "Unhandled exception during ProcessUpdates outer loop.");
             _batchTimer.Stop();
        }
        finally
        {
            lock (_processingLock)
            {
                _isProcessing = false; // Release processing flag
            }
        }
    }

    /// <summary>
    /// Helper method to process a single update. Manages lifecycle state entries and calls subsystems.
    /// </summary>
    private void ProcessSingleUpdate(IVisualUpdate update)
    {
         // Note: Individual update timing is now handled within the ProcessUpdates loop
         _logger.LogDebug("Processing update {Id} ({Type}) for element {ElemId}", update.Id, update.Type, update.Element?.Id ?? "doc");

        // No internal try-catch here; let exceptions bubble up to the main loop's catch
        // This prevents hiding errors that might stop the entire batch correctly.
        switch (update.Type)
        {
            case UpdateType.Full: // Fall through to Style
            case UpdateType.Style:
                if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InStyleRecalc &&
                    _lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InStyleRecalc))
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
                }
                if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
                {
                    _logger.LogTrace("Calling StyleEngine.ProcessUpdatesAsync...");
                    _styleEngine.ProcessUpdatesAsync().GetAwaiter().GetResult();
                    // Exit handled by LayoutEngineMain based on StyleComputedEvent
                } else _logger.LogWarning("Skipped style processing: Invalid phase ({Phase})", _lifecycleCoordinator.CurrentPhase);
                break;

            case UpdateType.Layout:
                if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InLayout &&
                    _lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InLayout))
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InLayout);
                }
                if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InLayout)
                {
                    _logger.LogTrace("Calling LayoutEngine.ProcessUpdatesAsync...");
                    _layoutEngine.ProcessUpdatesAsync().GetAwaiter().GetResult();
                    // Exit handled by LayoutEngineMain based on LayoutUpdatedEvent
                } else _logger.LogWarning("Skipped layout processing: Invalid phase ({Phase})", _lifecycleCoordinator.CurrentPhase);
                break;

            case UpdateType.Render:
                if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InRender)
                {
                    if(_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.RenderReady))
                         _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                    if(_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InRender))
                         _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InRender);
                }
                if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InRender)
                {
                    _logger.LogTrace("Publishing RenderInvalidatedEvent...");
                    _eventAggregator.Publish(new RenderInvalidatedEvent(null));
                    // Simulate render completion for mock
                    _eventAggregator.Publish(new RenderCompletedEvent());
                     if(_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InRender, DocumentLifecyclePhase.RenderReady))
                         _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                } else _logger.LogWarning("Skipped render processing: Invalid phase ({Phase})", _lifecycleCoordinator.CurrentPhase);
                break;

            case UpdateType.Resource:
                 _logger.LogTrace("Processing Resource update (No specific action).");
                break;
        }
        // Note: UpdateProcessedEvent is published in the calling loop (ProcessUpdates)
    }

    // GetPendingUpdateCount remains the same
    public int GetPendingUpdateCount()
    {
        int count = 0;
        foreach (var queue in _updateQueues.Values) count += queue.Count;
        return count;
    }

    // Dispose remains the same
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _logger.LogInformation("SimplifiedUpdateScheduler disposing.");
        foreach (var queue in _updateQueues.Values) queue.Clear();
    }
}