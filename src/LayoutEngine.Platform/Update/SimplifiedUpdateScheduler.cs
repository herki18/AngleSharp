using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

using System.Threading;

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
    private readonly UpdatePriority[] _priorityLevels;
    private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1, 1);
    private bool _isPaused;
    private bool _isDisposed;
    private readonly Stopwatch _batchTimer = new();

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
        foreach (var priority in _priorityLevels)
        {
            _updateQueues[priority] = new Queue<IVisualUpdate>();
        }

        _logger.LogInformation("SimplifiedUpdateScheduler initialized.");
    }

    public void ScheduleUpdate(IVisualUpdate update) => ScheduleUpdate(update, UpdatePriority.Normal);

    public void ScheduleUpdate(IVisualUpdate update, UpdatePriority priority)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));
        if (update == null) throw new ArgumentNullException(nameof(update));

        _logger.LogTrace("Scheduling update {Id} ({Type}) Priority: {Priority}", update.Id, update.Type, priority);
        _updateQueues[priority].Enqueue(update);
    }

    public bool CancelUpdate(IVisualUpdate update)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimplifiedUpdateScheduler));
        if (update == null) throw new ArgumentNullException(nameof(update));
        bool found = false;
        foreach (var priority in _priorityLevels)
        {
            var queue = _updateQueues[priority];
            var newQueue = new Queue<IVisualUpdate>(queue.Where(u =>
            {
                if (u.Id == update.Id)
                {
                    found = true;
                    return false;
                }

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

    /// <summary>
    /// Asynchronously processes pending updates based on priority until the time budget is exceeded
    /// or all queues are empty.
    /// </summary>
    public async Task ProcessUpdatesAsync(double timeBudgetMilliseconds, CancellationToken cancellationToken = default)
    {
        if (_isDisposed || _isPaused) return;

        // Attempt to acquire the semaphore asynchronously, return if already processing
        if (!await _processingSemaphore.WaitAsync(0, cancellationToken)) // 0 timeout = try immediately
        {
            _logger.LogTrace("Processing already in progress, skipping redundant async call.");
            return;
        }

        _logger.LogTrace("Starting async processing batch with budget: {Budget}ms. Pending: {PendingCount}", timeBudgetMilliseconds, GetPendingUpdateCount());
        _batchTimer.Restart();
        int updatesProcessedThisBatch = 0;
        var stopwatch = Stopwatch.StartNew(); // For individual timing

        try
        {
            foreach (var priority in _priorityLevels)
            {
                var queue = _updateQueues[priority];
                while (queue.Count > 0)
                {
                    // Check budget *before* processing
                    if (_batchTimer.Elapsed.TotalMilliseconds >= timeBudgetMilliseconds && updatesProcessedThisBatch > 0)
                    {
                        _logger.LogDebug("Time budget ({Budget}ms) exceeded after processing {Count} updates. Stopping async batch.", timeBudgetMilliseconds,
                            updatesProcessedThisBatch);
                        goto EndProcessing; // Use goto for cleaner exit from nested loops
                    }

                    // Check for cancellation request
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Update processing cancelled.");
                        goto EndProcessing;
                    }

                    var update = queue.Dequeue();
                    stopwatch.Restart();
                    bool errorHandled = false;

                    try
                    {
                        // Process the single update ASYNCHRONOUSLY
                        await ProcessSingleUpdateAsync(update, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Processing of update {Id} cancelled.", update.Id);
                        errorHandled = true; // Mark as handled (cancelled)
                        // Optionally re-queue the cancelled item if needed, or just drop it
                        _updateQueues[priority].Enqueue(update); // Requeue example
                    }
                    catch (Exception ex)
                    {
                        errorHandled = true;
                        _logger.LogError(ex, "Error processing update {UpdateId} ({UpdateType})", update.Id, update.Type);
                        stopwatch.Stop();
                        _eventAggregator.Publish(
                            new UpdateProcessedEvent(update, stopwatch.Elapsed.TotalMilliseconds, false, ex),
                            EventPriority.Normal);
                    }
                    finally
                    {
                        // Publish SUCCESS only if no error/cancellation was handled
                        if (!errorHandled)
                        {
                            stopwatch.Stop();
                            _eventAggregator.Publish(
                                new UpdateProcessedEvent(update, stopwatch.Elapsed.TotalMilliseconds, true, null),
                                EventPriority.Normal);
                        }
                    }

                    updatesProcessedThisBatch++;
                } // end while queue.Count > 0
            } // end foreach priority

            EndProcessing: ; // Label for budget break / cancellation

            _batchTimer.Stop();
            if (updatesProcessedThisBatch > 0)
            {
                _logger.LogTrace("Finished async processing batch. {Count} updates processed in {Duration:F2}ms.", updatesProcessedThisBatch,
                    _batchTimer.Elapsed.TotalMilliseconds);
            }
        }
        catch (Exception ex) // Catch unexpected errors during the outer loop
        {
            _logger.LogError(ex, "Unhandled exception during ProcessUpdatesAsync outer loop.");
            _batchTimer.Stop();
        }
        finally
        {
            _processingSemaphore.Release(); // Release the semaphore
        }
    }

    /// <summary>
    /// Synchronous wrapper for ProcessUpdatesAsync. Blocks until completion. Use with caution.
    /// </summary>
    public void ProcessUpdates(double timeBudgetMilliseconds)
    {
        // Block on the async version
        ProcessUpdatesAsync(timeBudgetMilliseconds).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Processes a single update asynchronously, triggers subsystem processing, and handles lifecycle.
    /// </summary>
    private async Task ProcessSingleUpdateAsync(IVisualUpdate update, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing update {Id} ({Type}) for element {ElemId}", update.Id, update.Type, update.Element?.Id ?? "doc");

        cancellationToken.ThrowIfCancellationRequested(); // Check cancellation at start

        switch (update.Type)
        {
            case UpdateType.Full:
            case UpdateType.Style:
                if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InStyleRecalc &&
                    _lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InStyleRecalc))
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
                }

                if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
                {
                    _logger.LogTrace("Awaiting StyleEngine.ProcessUpdatesAsync for update {Id}", update.Id);
                    await _styleEngine.ProcessUpdatesAsync(); // Await the subsystem
                }
                else _logger.LogWarning("Skipped style processing: Invalid phase ({Phase})", _lifecycleCoordinator.CurrentPhase);

                break;

            case UpdateType.Layout:
                if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InLayout &&
                    _lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InLayout))
                {
                    _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InLayout);
                }

                if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InLayout)
                {
                    _logger.LogTrace("Awaiting LayoutEngine.ProcessUpdatesAsync for update {Id}", update.Id);
                    await _layoutEngine.ProcessUpdatesAsync(); // Await the subsystem
                }
                else _logger.LogWarning("Skipped layout processing: Invalid phase ({Phase})", _lifecycleCoordinator.CurrentPhase);

                break;

            case UpdateType.Render:
                if (_lifecycleCoordinator.CurrentPhase != DocumentLifecyclePhase.InRender)
                {
                    if (_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.RenderReady))
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                    if (_lifecycleCoordinator.IsValidTransition(_lifecycleCoordinator.CurrentPhase, DocumentLifecyclePhase.InRender))
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.InRender);
                }

                if (_lifecycleCoordinator.CurrentPhase == DocumentLifecyclePhase.InRender)
                {
                    _logger.LogTrace("Publishing RenderInvalidatedEvent for update {Id}", update.Id);
                    _eventAggregator.Publish(new RenderInvalidatedEvent(null));
                    // Simulate completion
                    _eventAggregator.Publish(new RenderCompletedEvent());
                    if (_lifecycleCoordinator.IsValidTransition(DocumentLifecyclePhase.InRender, DocumentLifecyclePhase.RenderReady))
                        _lifecycleCoordinator.EnterPhase(DocumentLifecyclePhase.RenderReady);
                }
                else _logger.LogWarning("Skipped render processing: Invalid phase ({Phase})", _lifecycleCoordinator.CurrentPhase);

                break;

            case UpdateType.Resource:
                _logger.LogTrace("Processing Resource update {Id} (No specific action).", update.Id);
                break;
        }
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