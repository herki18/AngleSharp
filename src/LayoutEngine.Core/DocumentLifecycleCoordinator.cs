namespace LayoutEngine.Core;

using System;
using System.Collections.Generic;
using Events;
using Infrastructure.EventAggregator.API.Aggregation;
// Note: Infrastructure.EventAggregator.API.Events is not directly used here, but kept if other parts rely on it.
using Microsoft.Extensions.Logging;

/// <summary>
/// Coordinates the document lifecycle phases using a state machine.
/// Ensures valid state transitions and responds to system events.
/// </summary>
public sealed class DocumentLifecycleCoordinator : IDocumentLifecycleCoordinator, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<DocumentLifecycleCoordinator> _logger;
    private readonly DocumentLifecycleStateMachine _stateMachine; // Injected dependency
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentLifecycleCoordinator"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing and subscribing to events.</param>
    /// <param name="stateMachine">The document lifecycle state machine.</param>
    /// <param name="logger">The logger for this coordinator.</param>
    /// <exception cref="ArgumentNullException">Thrown if any required dependency is null.</exception>
    public DocumentLifecycleCoordinator(
        IEventAggregator eventAggregator,
        DocumentLifecycleStateMachine stateMachine, // Added stateMachine parameter
        ILogger<DocumentLifecycleCoordinator> logger)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine)); // Assign injected stateMachine
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to system events
        SubscribeToEvents();
    }

    /// <summary>
    /// Gets the current phase of the document lifecycle.
    /// </summary>
    public DocumentLifecyclePhase CurrentPhase => _stateMachine.CurrentPhase;

    /// <summary>
    /// Transitions the document to the specified phase.
    /// </summary>
    /// <param name="phase">The phase to enter.</param>
    /// <exception cref="InvalidOperationException">Thrown if the transition is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
    public void EnterPhase(DocumentLifecyclePhase phase)
    {
        ThrowIfDisposed();
        _logger.LogDebug("Attempting to enter phase: {TargetPhase} from {CurrentPhase}", phase, CurrentPhase);

        // Use the state machine to perform the transition
        bool success = _stateMachine.TryTransitionTo(phase);

        if (!success)
        {
            _logger.LogError("Invalid phase transition attempted from {CurrentPhase} to {TargetPhase}", CurrentPhase, phase);
            throw new InvalidOperationException(
                $"Invalid phase transition from {CurrentPhase} to {phase}");
        }
        _logger.LogInformation("Successfully entered phase: {CurrentPhase}", CurrentPhase);
    }

    /// <summary>
    /// Exits the current phase.
    /// </summary>
    /// <param name="phase">The phase to exit. This should match the current phase.</param>
    /// <exception cref="InvalidOperationException">Thrown if the current phase doesn't match the phase parameter or if exit fails.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
    public void ExitPhase(DocumentLifecyclePhase phase)
    {
        ThrowIfDisposed();
        _logger.LogDebug("Attempting to exit phase: {PhaseParameter} (Current: {CurrentPhase})", phase, CurrentPhase);

        // Validate current phase
        if (CurrentPhase != phase)
        {
            _logger.LogError("Cannot exit phase {PhaseParameter} because current phase is {CurrentPhase}", phase, CurrentPhase);
            throw new InvalidOperationException(
                $"Cannot exit phase {phase} when current phase is {CurrentPhase}");
        }

        // Signal the exit to the state machine, which handles transitions and events
        bool success = _stateMachine.SignalPhaseExit();

        if (!success)
        {
            _logger.LogError("Failed to exit phase {PhaseParameter} (Current after attempt: {CurrentPhase})", phase, CurrentPhase);
            throw new InvalidOperationException(
                $"Failed to exit phase {phase}");
        }
        _logger.LogInformation("Successfully exited phase {PhaseParameter}, new phase is {CurrentPhase}", phase, CurrentPhase);
    }

    /// <summary>
    /// Determines if the specified transition is valid from the current phase.
    /// </summary>
    /// <param name="fromPhase">The phase to transition from (must be the current phase).</param>
    /// <param name="toPhase">The phase to transition to.</param>
    /// <returns>True if the transition is valid, otherwise false.</returns>
    public bool IsValidTransition(DocumentLifecyclePhase fromPhase, DocumentLifecyclePhase toPhase)
    {
        // We can only determine transitions from the current state
        if (fromPhase != CurrentPhase)
        {
            _logger.LogWarning("IsValidTransition check for non-current 'fromPhase'. From: {FromPhase}, Current: {CurrentPhase}, To: {ToPhase}", fromPhase, CurrentPhase, toPhase);
            return false;
        }
        return _stateMachine.CanTransitionTo(toPhase);
    }

    /// <summary>
    /// Determines if the operation is allowed in the current phase.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>True if the operation is allowed, otherwise false.</returns>
    public bool IsOperationAllowed(DocumentOperation operation)
    {
        return _stateMachine.IsOperationAllowed(operation);
    }

    /// <summary>
    /// Subscribes to all relevant system events.
    /// </summary>
    private void SubscribeToEvents()
    {
        _logger.LogDebug("Subscribing to document lifecycle events.");
        // Subscribe to completion events
        _subscriptions.Add(_eventAggregator.Subscribe<StyleComputedEvent>(OnStyleComputed));
        _subscriptions.Add(_eventAggregator.Subscribe<FragmentTreeUpdatedEvent>(OnFragmentTreeUpdated));
        _subscriptions.Add(_eventAggregator.Subscribe<RenderCompletedEvent>(OnRenderCompleted));

        // Subscribe to invalidation events
        _subscriptions.Add(_eventAggregator.Subscribe<StyleInvalidatedEvent>(OnStyleInvalidated));
        _subscriptions.Add(_eventAggregator.Subscribe<LayoutInvalidatedEvent>(OnLayoutInvalidated));
        _subscriptions.Add(_eventAggregator.Subscribe<RenderInvalidatedEvent>(OnRenderInvalidated));
    }

    /// <summary>
    /// Handles StyleComputed events to transition out of style calculation.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnStyleComputed(StyleComputedEvent e)
    {
        _logger.LogDebug("StyleComputedEvent received. Current phase: {CurrentPhase}", CurrentPhase);
        if (CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
        {
            ExitPhase(DocumentLifecyclePhase.InStyleRecalc);
        }
        else
        {
            _logger.LogWarning("StyleComputedEvent received but not in InStyleRecalc phase (Current: {CurrentPhase}). Ignoring.", CurrentPhase);
        }
    }

    /// <summary>
    /// Handles FragmentTreeUpdated events to transition out of layout calculation.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnFragmentTreeUpdated(FragmentTreeUpdatedEvent e)
    {
        _logger.LogDebug("FragmentTreeUpdatedEvent received. Current phase: {CurrentPhase}", CurrentPhase);
        if (CurrentPhase == DocumentLifecyclePhase.InLayout)
        {
            ExitPhase(DocumentLifecyclePhase.InLayout);
        }
        else
        {
            _logger.LogWarning("FragmentTreeUpdatedEvent received but not in InLayout phase (Current: {CurrentPhase}). Ignoring.", CurrentPhase);
        }
    }

    /// <summary>
    /// Handles RenderCompleted events to transition out of rendering.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnRenderCompleted(RenderCompletedEvent e)
    {
        _logger.LogDebug("RenderCompletedEvent received. Current phase: {CurrentPhase}", CurrentPhase);
        if (CurrentPhase == DocumentLifecyclePhase.InRender)
        {
            ExitPhase(DocumentLifecyclePhase.InRender);
        }
        else
        {
            _logger.LogWarning("RenderCompletedEvent received but not in InRender phase (Current: {CurrentPhase}). Ignoring.", CurrentPhase);
        }
    }

    /// <summary>
    /// Handles StyleInvalidated events to begin style recalculation if needed.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnStyleInvalidated(StyleInvalidatedEvent e)
    {
        _logger.LogDebug("StyleInvalidatedEvent received. Current phase: {CurrentPhase}", CurrentPhase);
        // Only transition to InStyleRecalc if we're currently in StyleClean
        if (CurrentPhase == DocumentLifecyclePhase.StyleClean)
        {
            EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
        }
        else
        {
            _logger.LogInformation("StyleInvalidatedEvent received but not in StyleClean phase (Current: {CurrentPhase}). Style recalc will not be triggered by coordinator at this time.", CurrentPhase);
        }
    }

    /// <summary>
    /// Handles LayoutInvalidated events to begin layout calculation if needed.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnLayoutInvalidated(LayoutInvalidatedEvent e)
    {
        _logger.LogDebug("LayoutInvalidatedEvent received. Current phase: {CurrentPhase}", CurrentPhase);
        // Only transition to InLayout if we're currently in LayoutClean
        if (CurrentPhase == DocumentLifecyclePhase.LayoutClean)
        {
            EnterPhase(DocumentLifecyclePhase.InLayout);
        }
        else
        {
            _logger.LogInformation("LayoutInvalidatedEvent received but not in LayoutClean phase (Current: {CurrentPhase}). Layout calculation will not be triggered by coordinator at this time.", CurrentPhase);
        }
    }

    /// <summary>
    /// Handles RenderInvalidated events to begin rendering if needed.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnRenderInvalidated(RenderInvalidatedEvent e)
    {
        _logger.LogDebug("RenderInvalidatedEvent received. Current phase: {CurrentPhase}", CurrentPhase);
        // Only transition to InRender if we're currently in RenderReady
        if (CurrentPhase == DocumentLifecyclePhase.RenderReady)
        {
            EnterPhase(DocumentLifecyclePhase.InRender);
        }
        else
        {
            _logger.LogInformation("RenderInvalidatedEvent received but not in RenderReady phase (Current: {CurrentPhase}). Rendering will not be triggered by coordinator at this time.", CurrentPhase);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            _logger.LogError("Operation attempted on disposed DocumentLifecycleCoordinator.");
            throw new ObjectDisposedException(nameof(DocumentLifecycleCoordinator));
        }
    }

    /// <summary>
    /// Disposes the DocumentLifecycleCoordinator and unsubscribes all event subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _logger.LogInformation("Disposing DocumentLifecycleCoordinator.");
        _isDisposed = true;

        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }
        _subscriptions.Clear();
        _logger.LogDebug("Unsubscribed from all events.");

        // Assuming DocumentLifecycleStateMachine has a Dispose method.
        // If it doesn't, this line might cause a compile error or is unnecessary.
        // Based on the provided DocumentLifecycleStateMachine, it does have Dispose().
        (_stateMachine as IDisposable)?.Dispose();
        _logger.LogDebug("Disposed the state machine.");
    }
}
