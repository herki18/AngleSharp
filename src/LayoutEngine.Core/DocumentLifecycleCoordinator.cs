namespace LayoutEngine.Core;

using System;
using System.Collections.Generic;
using Events;
using Infrastructure.EventAggregator.API.Aggregation;
using Microsoft.Extensions.Logging;

/// <summary>
/// Coordinates the document lifecycle phases using a state machine.
/// Ensures valid state transitions and responds to system events.
/// </summary>
public sealed class DocumentLifecycleCoordinator : IDocumentLifecycleCoordinator, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<DocumentLifecycleCoordinator> _logger;
    private readonly DocumentLifecycleStateMachine _stateMachine;
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentLifecycleCoordinator"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing and subscribing to events.</param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException">Thrown if any required dependency is null.</exception>
    public DocumentLifecycleCoordinator(IEventAggregator eventAggregator, ILogger<DocumentLifecycleCoordinator> logger)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger;
        _stateMachine = new DocumentLifecycleStateMachine(eventAggregator);

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

        // Use the state machine to perform the transition
        bool success = _stateMachine.TryTransitionTo(phase);

        if (!success)
        {
            throw new InvalidOperationException(
                $"Invalid phase transition from {CurrentPhase} to {phase}");
        }
    }

    /// <summary>
    /// Exits the current phase.
    /// </summary>
    /// <param name="phase">The phase to exit.</param>
    /// <exception cref="InvalidOperationException">Thrown if the current phase doesn't match.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
    public void ExitPhase(DocumentLifecyclePhase phase)
    {
        ThrowIfDisposed();

        // Validate current phase
        if (CurrentPhase != phase)
        {
            throw new InvalidOperationException(
                $"Cannot exit phase {phase} when current phase is {CurrentPhase}");
        }

        // Signal the exit to the state machine, which handles transitions and events
        bool success = _stateMachine.SignalPhaseExit();

        if (!success)
        {
            throw new InvalidOperationException(
                $"Failed to exit phase {phase}");
        }
    }

    /// <summary>
    /// Determines if the specified transition is valid.
    /// </summary>
    /// <param name="fromPhase">The phase to transition from.</param>
    /// <param name="toPhase">The phase to transition to.</param>
    /// <returns>True if the transition is valid, otherwise false.</returns>
    public bool IsValidTransition(DocumentLifecyclePhase fromPhase, DocumentLifecyclePhase toPhase)
    {
        // We can only determine transitions from the current state
        if (fromPhase != CurrentPhase)
            return false;

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
        _logger.LogDebug("[DocumentLifecycleCoordinator] StyleComputedEvent received");
        if (CurrentPhase == DocumentLifecyclePhase.InStyleRecalc)
        {
            ExitPhase(DocumentLifecyclePhase.InStyleRecalc);
        }
    }

    /// <summary>
    /// Handles FragmentTreeUpdated events to transition out of layout calculation.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnFragmentTreeUpdated(FragmentTreeUpdatedEvent e)
    {
        if (CurrentPhase == DocumentLifecyclePhase.InLayout)
        {
            ExitPhase(DocumentLifecyclePhase.InLayout);
        }
    }

    /// <summary>
    /// Handles RenderCompleted events to transition out of rendering.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnRenderCompleted(RenderCompletedEvent e)
    {
        if (CurrentPhase == DocumentLifecyclePhase.InRender)
        {
            ExitPhase(DocumentLifecyclePhase.InRender);
        }
    }

    /// <summary>
    /// Handles StyleInvalidated events to begin style recalculation if needed.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnStyleInvalidated(StyleInvalidatedEvent e)
    {
        // Only transition to InStyleRecalc if we're currently in StyleClean
        if (CurrentPhase == DocumentLifecyclePhase.StyleClean)
        {
            EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
        }
    }

    /// <summary>
    /// Handles LayoutInvalidated events to begin layout calculation if needed.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnLayoutInvalidated(LayoutInvalidatedEvent e)
    {
        // Only transition to InLayout if we're currently in LayoutClean
        if (CurrentPhase == DocumentLifecyclePhase.LayoutClean)
        {
            EnterPhase(DocumentLifecyclePhase.InLayout);
        }
    }

    /// <summary>
    /// Handles RenderInvalidated events to begin rendering if needed.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private void OnRenderInvalidated(RenderInvalidatedEvent e)
    {
        // Only transition to InRender if we're currently in RenderReady
        if (CurrentPhase == DocumentLifecyclePhase.RenderReady)
        {
            EnterPhase(DocumentLifecyclePhase.InRender);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
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

        _isDisposed = true;

        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }

        _subscriptions.Clear();
        _stateMachine.Dispose();
    }
}