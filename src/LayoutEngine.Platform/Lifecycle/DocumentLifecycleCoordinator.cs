using System;
using System.Collections.Generic;

namespace LayoutEngine.Platform.Lifecycle;

using Contracts.Platform.Events;
using Contracts.Platform.Lifecycle;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Coordinates the document lifecycle phases and ensures valid state transitions.
/// Publishes lifecycle events and responds to system completion events.
/// </summary>
public sealed class DocumentLifecycleCoordinator : IDocumentLifecycleCoordinator, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILifecycleStateValidator _stateValidator;
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private DocumentLifecyclePhase _currentPhase = DocumentLifecyclePhase.Inactive;
    private bool _isDisposed;

    public DocumentLifecycleCoordinator(IEventAggregator eventAggregator, ILifecycleStateValidator stateValidator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _stateValidator = stateValidator ?? throw new ArgumentNullException(nameof(stateValidator));

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
    /// Gets the current phase of the document lifecycle.
    /// </summary>
    public DocumentLifecyclePhase CurrentPhase => _currentPhase;

    /// <summary>
    /// Transitions the document to the specified phase.
    /// </summary>
    /// <param name="phase">The phase to enter.</param>
    /// <exception cref="InvalidOperationException">Thrown if the transition is invalid.</exception>
    public void EnterPhase(DocumentLifecyclePhase phase)
    {
        ThrowIfDisposed();

        // Validate the transition
        if (!IsValidTransition(_currentPhase, phase))
        {
            throw new InvalidOperationException(
                $"Invalid phase transition from {_currentPhase} to {phase}");
        }

        var previousPhase = _currentPhase;
        _currentPhase = phase;

        // Publish phase changed event with HIGH priority
        _eventAggregator.Publish(
            new PhaseChangedEvent(phase, PhaseChangeType.Enter),
            EventPriority.High);

        // Log the transition
        // _logger?.LogInformation("Document lifecycle phase changed from {PreviousPhase} to {CurrentPhase}", previousPhase, _currentPhase);
    }

    /// <summary>
    /// Exits the current phase.
    /// </summary>
    /// <param name="phase">The phase to exit.</param>
    /// <exception cref="InvalidOperationException">Thrown if the current phase doesn't match.</exception>
    public void ExitPhase(DocumentLifecyclePhase phase)
    {
        ThrowIfDisposed();

        // Validate current phase
        if (_currentPhase != phase)
        {
            throw new InvalidOperationException(
                $"Cannot exit phase {phase} when current phase is {_currentPhase}");
        }

        // Publish phase changed event with HIGH priority
        _eventAggregator.Publish(
            new PhaseChangedEvent(phase, PhaseChangeType.Exit),
            EventPriority.High);

        // Determine and enter next phase
        var nextPhase = DetermineNextPhase(phase);
        if (nextPhase != DocumentLifecyclePhase.Unknown)
        {
            EnterPhase(nextPhase);
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
        return _stateValidator.IsValidTransition(fromPhase, toPhase);
    }

    /// <summary>
    /// Determines if the operation is allowed in the current phase.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>True if the operation is allowed, otherwise false.</returns>
    public bool IsOperationAllowed(DocumentOperation operation)
    {
        return _stateValidator.IsOperationAllowed(_currentPhase, operation);
    }

    /// <summary>
    /// Handles StyleComputed events to transition out of style calculation.
    /// </summary>
    private void OnStyleComputed(StyleComputedEvent e)
    {
        if (_currentPhase == DocumentLifecyclePhase.InStyleRecalc)
        {
            ExitPhase(DocumentLifecyclePhase.InStyleRecalc);
        }
    }

    /// <summary>
    /// Handles FragmentTreeUpdated events to transition out of layout calculation.
    /// </summary>
    private void OnFragmentTreeUpdated(FragmentTreeUpdatedEvent e)
    {
        if (_currentPhase == DocumentLifecyclePhase.InLayout)
        {
            ExitPhase(DocumentLifecyclePhase.InLayout);
        }
    }

    /// <summary>
    /// Handles RenderCompleted events to transition out of rendering.
    /// </summary>
    private void OnRenderCompleted(RenderCompletedEvent e)
    {
        if (_currentPhase == DocumentLifecyclePhase.InRender)
        {
            ExitPhase(DocumentLifecyclePhase.InRender);
        }
    }

    /// <summary>
    /// Handles StyleInvalidated events to begin style recalculation if needed.
    /// </summary>
    private void OnStyleInvalidated(StyleInvalidatedEvent e)
    {
        // Only transition to InStyleRecalc if we're currently in StyleClean
        if (_currentPhase == DocumentLifecyclePhase.StyleClean)
        {
            EnterPhase(DocumentLifecyclePhase.InStyleRecalc);
        }
    }

    /// <summary>
    /// Handles LayoutInvalidated events to begin layout calculation if needed.
    /// </summary>
    private void OnLayoutInvalidated(LayoutInvalidatedEvent e)
    {
        // Only transition to InLayout if we're currently in LayoutClean
        if (_currentPhase == DocumentLifecyclePhase.LayoutClean)
        {
            EnterPhase(DocumentLifecyclePhase.InLayout);
        }
    }

    /// <summary>
    /// Handles RenderInvalidated events to begin rendering if needed.
    /// </summary>
    private void OnRenderInvalidated(RenderInvalidatedEvent e)
    {
        // Only transition to InRender if we're currently in RenderReady
        if (_currentPhase == DocumentLifecyclePhase.RenderReady)
        {
            EnterPhase(DocumentLifecyclePhase.InRender);
        }
    }

    /// <summary>
    /// Determines the next phase based on the current phase.
    /// </summary>
    private DocumentLifecyclePhase DetermineNextPhase(DocumentLifecyclePhase currentPhase)
    {
        return currentPhase switch
        {
            DocumentLifecyclePhase.InStyleRecalc => DocumentLifecyclePhase.StyleDirty,
            DocumentLifecyclePhase.StyleDirty => DocumentLifecyclePhase.StyleClean,
            DocumentLifecyclePhase.InLayout => DocumentLifecyclePhase.LayoutDirty,
            DocumentLifecyclePhase.LayoutDirty => DocumentLifecyclePhase.LayoutClean,
            DocumentLifecyclePhase.InRender => DocumentLifecyclePhase.RenderDirty,
            DocumentLifecyclePhase.RenderDirty => DocumentLifecyclePhase.RenderReady,
            _ => DocumentLifecyclePhase.Unknown
        };
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
    }
}