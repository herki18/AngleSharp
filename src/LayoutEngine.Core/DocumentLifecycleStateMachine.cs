namespace LayoutEngine.Core;

using System;
using System.Collections.Generic;
using Events;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using Stateless;

/// <summary>
/// Manages the document lifecycle state machine with automatic transitions.
/// </summary>
public sealed class DocumentLifecycleStateMachine : IDisposable
{
    private readonly StateMachine<DocumentLifecyclePhase, LifecycleTrigger> _stateMachine;
    private readonly IEventAggregator _eventAggregator;

    // Static mapping of states to allowed operations
    private static readonly Dictionary<DocumentLifecyclePhase, HashSet<DocumentOperation>> AllowedOperations = new()
    {
        [DocumentLifecyclePhase.Inactive] = new HashSet<DocumentOperation>
        {
            DocumentOperation.DomReading,
            DocumentOperation.DomModification
        },
        [DocumentLifecyclePhase.StyleClean] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.StyleModification,
            DocumentOperation.DomReading,
            DocumentOperation.DomModification
        },
        [DocumentLifecyclePhase.InStyleRecalc] = new HashSet<DocumentOperation>
        {
            DocumentOperation.DomReading
        },
        [DocumentLifecyclePhase.StyleDirty] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleModification,
            DocumentOperation.DomReading
        },
        [DocumentLifecyclePhase.LayoutClean] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutReading,
            DocumentOperation.DomReading
        },
        [DocumentLifecyclePhase.InLayout] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutCalculation,
            DocumentOperation.DomReading
        },
        [DocumentLifecyclePhase.LayoutDirty] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutCalculation,
            DocumentOperation.DomReading
        },
        [DocumentLifecyclePhase.RenderReady] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutReading,
            DocumentOperation.RenderReading,
            DocumentOperation.DomReading
        },
        [DocumentLifecyclePhase.InRender] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutReading,
            DocumentOperation.RenderReading,
            DocumentOperation.Rendering,
            DocumentOperation.DomReading
        },
        [DocumentLifecyclePhase.RenderDirty] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutReading,
            DocumentOperation.RenderReading,
            DocumentOperation.Rendering,
            DocumentOperation.DomReading
        },
        [DocumentLifecyclePhase.Disposed] = new HashSet<DocumentOperation>()
    };

    // Mapping of exit triggers to the corresponding next phase trigger
    private static readonly Dictionary<LifecycleTrigger, LifecycleTrigger> ExitToNextTrigger = new()
    {
        // Style phase
        [LifecycleTrigger.ExitInStyleRecalc] = LifecycleTrigger.StyleChanged,
        [LifecycleTrigger.ExitStyleDirty] = LifecycleTrigger.StyleComplete,

        // Layout phase
        [LifecycleTrigger.ExitInLayout] = LifecycleTrigger.LayoutChanged,
        [LifecycleTrigger.ExitLayoutDirty] = LifecycleTrigger.LayoutComplete,

        // Render phase
        [LifecycleTrigger.ExitInRender] = LifecycleTrigger.RenderChanged,
        [LifecycleTrigger.ExitRenderDirty] = LifecycleTrigger.RenderComplete
    };

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentLifecycleStateMachine"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing state change events.</param>
    public DocumentLifecycleStateMachine(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        // Create the state machine starting in Inactive state
        _stateMachine = new StateMachine<DocumentLifecyclePhase, LifecycleTrigger>(DocumentLifecyclePhase.Inactive, Stateless.FiringMode.Immediate);

        // Configure state machine with valid transitions and events
        ConfigureStateMachine();
    }

    /// <summary>
    /// Gets the current phase of the document lifecycle.
    /// </summary>
    public DocumentLifecyclePhase CurrentPhase => _stateMachine.State;

    /// <summary>
    /// Checks if a transition from the current state to the target state is possible.
    /// </summary>
    /// <param name="targetState">The desired target state.</param>
    /// <returns>True if the transition is valid, otherwise false.</returns>
    public bool CanTransitionTo(DocumentLifecyclePhase targetState)
    {
        return _stateMachine.CanFire(GetTriggerForTargetState(CurrentPhase, targetState));
    }

    /// <summary>
    /// Transitions to the target state if a valid trigger exists.
    /// </summary>
    /// <param name="targetState">The state to transition to.</param>
    /// <returns>True if the transition was successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the transition is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
    public bool TryTransitionTo(DocumentLifecyclePhase targetState)
    {
        ThrowIfDisposed();

        var trigger = GetTriggerForTargetState(CurrentPhase, targetState);
        if (!_stateMachine.CanFire(trigger))
        {
            return false;
        }

        _stateMachine.Fire(trigger);
        return true;
    }

    /// <summary>
    /// Signals that the current phase is exiting.
    /// </summary>
    /// <returns>True if an exit trigger was fired and handled.</returns>
    public bool SignalPhaseExit()
    {
        ThrowIfDisposed();

        var exitTrigger = GetExitTriggerForState(CurrentPhase);
        if (exitTrigger == null || !_stateMachine.CanFire(exitTrigger.Value))
        {
            return false;
        }

        _stateMachine.Fire(exitTrigger.Value);
        return true;
    }

    /// <summary>
    /// Checks whether the specified operation is allowed in the current state.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>True if the operation is allowed, otherwise false.</returns>
    public bool IsOperationAllowed(DocumentOperation operation)
    {
        return AllowedOperations.TryGetValue(CurrentPhase, out var allowedOps) &&
               allowedOps.Contains(operation);
    }

    /// <summary>
    /// Configures the state machine with all valid transitions and entry/exit behaviors.
    /// </summary>
    private void ConfigureStateMachine()
    {
        // Configure the Inactive state
        _stateMachine.Configure(DocumentLifecyclePhase.Inactive)
            .Permit(LifecycleTrigger.EnterStyleClean, DocumentLifecyclePhase.StyleClean)
            .Permit(LifecycleTrigger.Dispose, DocumentLifecyclePhase.Disposed)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.Inactive, PhaseChangeType.Enter));

        // Configure the StyleClean state
        _stateMachine.Configure(DocumentLifecyclePhase.StyleClean)
            .Permit(LifecycleTrigger.BeginStyleCalculation, DocumentLifecyclePhase.InStyleRecalc)
            .Permit(LifecycleTrigger.EnterLayoutClean, DocumentLifecyclePhase.LayoutClean)
            .Permit(LifecycleTrigger.Dispose, DocumentLifecyclePhase.Disposed)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.StyleClean, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.StyleClean, PhaseChangeType.Exit));

        // Configure the InStyleRecalc state
        _stateMachine.Configure(DocumentLifecyclePhase.InStyleRecalc)
            .Permit(LifecycleTrigger.StyleChanged, DocumentLifecyclePhase.StyleDirty)
            .Permit(LifecycleTrigger.NoStyleChanges, DocumentLifecyclePhase.StyleClean)
            .Permit(LifecycleTrigger.ExitInStyleRecalc, DocumentLifecyclePhase.StyleDirty)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.InStyleRecalc, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.InStyleRecalc, PhaseChangeType.Exit));

        // Configure the StyleDirty state
        _stateMachine.Configure(DocumentLifecyclePhase.StyleDirty)
            .Permit(LifecycleTrigger.StyleComplete, DocumentLifecyclePhase.StyleClean)
            .Permit(LifecycleTrigger.ExitStyleDirty, DocumentLifecyclePhase.StyleClean)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.StyleDirty, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.StyleDirty, PhaseChangeType.Exit));

        // Configure the LayoutClean state
        _stateMachine.Configure(DocumentLifecyclePhase.LayoutClean)
            .Permit(LifecycleTrigger.BeginLayoutCalculation, DocumentLifecyclePhase.InLayout)
            .Permit(LifecycleTrigger.EnterRenderReady, DocumentLifecyclePhase.RenderReady)
            .Permit(LifecycleTrigger.Dispose, DocumentLifecyclePhase.Disposed)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.LayoutClean, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.LayoutClean, PhaseChangeType.Exit));

        // Configure the InLayout state
        _stateMachine.Configure(DocumentLifecyclePhase.InLayout)
            .Permit(LifecycleTrigger.LayoutChanged, DocumentLifecyclePhase.LayoutDirty)
            .Permit(LifecycleTrigger.NoLayoutChanges, DocumentLifecyclePhase.LayoutClean)
            .Permit(LifecycleTrigger.ExitInLayout, DocumentLifecyclePhase.LayoutDirty)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.InLayout, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.InLayout, PhaseChangeType.Exit));

        // Configure the LayoutDirty state
        _stateMachine.Configure(DocumentLifecyclePhase.LayoutDirty)
            .Permit(LifecycleTrigger.LayoutComplete, DocumentLifecyclePhase.LayoutClean)
            .Permit(LifecycleTrigger.ExitLayoutDirty, DocumentLifecyclePhase.LayoutClean)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.LayoutDirty, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.LayoutDirty, PhaseChangeType.Exit));

        // Configure the RenderReady state
        _stateMachine.Configure(DocumentLifecyclePhase.RenderReady)
            .Permit(LifecycleTrigger.BeginRendering, DocumentLifecyclePhase.InRender)
            .Permit(LifecycleTrigger.NextFrame, DocumentLifecyclePhase.StyleClean)
            .Permit(LifecycleTrigger.Dispose, DocumentLifecyclePhase.Disposed)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.RenderReady, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.RenderReady, PhaseChangeType.Exit));

        // Configure the InRender state
        _stateMachine.Configure(DocumentLifecyclePhase.InRender)
            .Permit(LifecycleTrigger.RenderChanged, DocumentLifecyclePhase.RenderDirty)
            .Permit(LifecycleTrigger.NoRenderChanges, DocumentLifecyclePhase.RenderReady)
            .Permit(LifecycleTrigger.ExitInRender, DocumentLifecyclePhase.RenderDirty)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.InRender, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.InRender, PhaseChangeType.Exit));

        // Configure the RenderDirty state
        _stateMachine.Configure(DocumentLifecyclePhase.RenderDirty)
            .Permit(LifecycleTrigger.RenderComplete, DocumentLifecyclePhase.RenderReady)
            .Permit(LifecycleTrigger.ExitRenderDirty, DocumentLifecyclePhase.RenderReady)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.RenderDirty, PhaseChangeType.Enter))
            .OnExit(() => PublishPhaseEvent(DocumentLifecyclePhase.RenderDirty, PhaseChangeType.Exit));

        // Configure the Disposed state (terminal state)
        _stateMachine.Configure(DocumentLifecyclePhase.Disposed)
            .OnEntry(() => PublishPhaseEvent(DocumentLifecyclePhase.Disposed, PhaseChangeType.Enter));
    }

    /// <summary>
    /// Publishes a phase event.
    /// </summary>
    private void PublishPhaseEvent(DocumentLifecyclePhase phase, PhaseChangeType changeType)
    {
        _eventAggregator.Publish(
            new PhaseChangedEvent(phase, changeType),
            EventPriority.High);
    }

    /// <summary>
    /// Gets the appropriate trigger for transitioning to the specified target state.
    /// </summary>
    private LifecycleTrigger GetTriggerForTargetState(DocumentLifecyclePhase currentState, DocumentLifecyclePhase targetState)
    {
        return (currentState, targetState) switch
        {
            // From Inactive
            (DocumentLifecyclePhase.Inactive, DocumentLifecyclePhase.StyleClean) => LifecycleTrigger.EnterStyleClean,
            (DocumentLifecyclePhase.Inactive, DocumentLifecyclePhase.Disposed) => LifecycleTrigger.Dispose,

            // From StyleClean
            (DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.InStyleRecalc) => LifecycleTrigger.BeginStyleCalculation,
            (DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.LayoutClean) => LifecycleTrigger.EnterLayoutClean,
            (DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.Disposed) => LifecycleTrigger.Dispose,

            // From InStyleRecalc
            (DocumentLifecyclePhase.InStyleRecalc, DocumentLifecyclePhase.StyleClean) => LifecycleTrigger.NoStyleChanges,
            (DocumentLifecyclePhase.InStyleRecalc, DocumentLifecyclePhase.StyleDirty) => LifecycleTrigger.StyleChanged,

            // From StyleDirty
            (DocumentLifecyclePhase.StyleDirty, DocumentLifecyclePhase.StyleClean) => LifecycleTrigger.StyleComplete,

            // From LayoutClean
            (DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.InLayout) => LifecycleTrigger.BeginLayoutCalculation,
            (DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.RenderReady) => LifecycleTrigger.EnterRenderReady,
            (DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.Disposed) => LifecycleTrigger.Dispose,

            // From InLayout
            (DocumentLifecyclePhase.InLayout, DocumentLifecyclePhase.LayoutClean) => LifecycleTrigger.NoLayoutChanges,
            (DocumentLifecyclePhase.InLayout, DocumentLifecyclePhase.LayoutDirty) => LifecycleTrigger.LayoutChanged,

            // From LayoutDirty
            (DocumentLifecyclePhase.LayoutDirty, DocumentLifecyclePhase.LayoutClean) => LifecycleTrigger.LayoutComplete,

            // From RenderReady
            (DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.InRender) => LifecycleTrigger.BeginRendering,
            (DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.StyleClean) => LifecycleTrigger.NextFrame,
            (DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.Disposed) => LifecycleTrigger.Dispose,

            // From InRender
            (DocumentLifecyclePhase.InRender, DocumentLifecyclePhase.RenderReady) => LifecycleTrigger.NoRenderChanges,
            (DocumentLifecyclePhase.InRender, DocumentLifecyclePhase.RenderDirty) => LifecycleTrigger.RenderChanged,

            // From RenderDirty
            (DocumentLifecyclePhase.RenderDirty, DocumentLifecyclePhase.RenderReady) => LifecycleTrigger.RenderComplete,

            // Default case for unsupported transitions
            _ => throw new InvalidOperationException($"No valid trigger defined for transition from {currentState} to {targetState}")
        };
    }

    /// <summary>
    /// Gets the exit trigger for the given state.
    /// </summary>
    private LifecycleTrigger? GetExitTriggerForState(DocumentLifecyclePhase state)
    {
        return state switch
        {
            DocumentLifecyclePhase.InStyleRecalc => LifecycleTrigger.ExitInStyleRecalc,
            DocumentLifecyclePhase.StyleDirty => LifecycleTrigger.ExitStyleDirty,
            DocumentLifecyclePhase.InLayout => LifecycleTrigger.ExitInLayout,
            DocumentLifecyclePhase.LayoutDirty => LifecycleTrigger.ExitLayoutDirty,
            DocumentLifecyclePhase.InRender => LifecycleTrigger.ExitInRender,
            DocumentLifecyclePhase.RenderDirty => LifecycleTrigger.ExitRenderDirty,
            _ => null
        };
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(DocumentLifecycleStateMachine));
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
    }
}