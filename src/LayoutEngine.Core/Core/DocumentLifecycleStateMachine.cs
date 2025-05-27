namespace LayoutEngine.Core.Core;

using System;
using System.Collections.Generic;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using LayoutEngine.Core.Events;
using Microsoft.Extensions.Logging;
using Stateless;
// Added for ILogger

/// <summary>
/// Manages the document lifecycle state machine with automatic transitions.
/// </summary>
public sealed class DocumentLifecycleStateMachine : IDisposable
{
    private readonly StateMachine<DocumentLifecyclePhase, LifecycleTrigger> _stateMachine;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<DocumentLifecycleStateMachine> _logger;

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
        [DocumentLifecyclePhase.Disposed] = new HashSet<DocumentOperation>()
    };

    // Mapping of generic exit triggers (used by SignalPhaseExit) to the specific
    // trigger that signifies the completion of that phase's work.
    private static readonly Dictionary<LifecycleTrigger, LifecycleTrigger> ExitToCompletionTrigger = new()
    {
        // Style phase
        [LifecycleTrigger.ExitInStyleRecalc] = LifecycleTrigger.StyleComplete,

        // Layout phase
        [LifecycleTrigger.ExitInLayout] = LifecycleTrigger.LayoutComplete,

        // Render phase
        [LifecycleTrigger.ExitInRender] = LifecycleTrigger.RenderComplete,
    };

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentLifecycleStateMachine"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing state change events.</param>
    /// <param name="logger">The logger for logging state machine activities.</param>
    public DocumentLifecycleStateMachine(IEventAggregator eventAggregator, ILogger<DocumentLifecycleStateMachine> logger)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _stateMachine = new StateMachine<DocumentLifecyclePhase, LifecycleTrigger>(DocumentLifecyclePhase.Inactive, FiringMode.Immediate);
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
        var trigger = GetTriggerForTargetState(CurrentPhase, targetState);
        return trigger.HasValue && _stateMachine.CanFire(trigger.Value);
    }

    /// <summary>
    /// Transitions to the target state if a valid trigger exists.
    /// </summary>
    /// <param name="targetState">The state to transition to.</param>
    /// <returns>True if the transition was successful, otherwise false.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
    public bool TryTransitionTo(DocumentLifecyclePhase targetState)
    {
        ThrowIfDisposed();
        var phaseBeforeTransition = CurrentPhase; // Capture current phase before attempting transition

        var triggerNullable = GetTriggerForTargetState(phaseBeforeTransition, targetState);
        if (!triggerNullable.HasValue)
        {
            _logger.LogWarning("No trigger defined for transition from {CurrentPhase} to {TargetState}.", phaseBeforeTransition, targetState);
            return false;
        }

        LifecycleTrigger trigger = triggerNullable.Value;
        if (!_stateMachine.CanFire(trigger))
        {
            _logger.LogWarning("Cannot fire trigger {Trigger} for transition from {CurrentPhase} to {TargetState}. State machine configuration may not permit this.", trigger, phaseBeforeTransition, targetState);
            return false;
        }

        _stateMachine.Fire(trigger);
        // CurrentPhase will now reflect the new state
        _logger.LogDebug("Successfully transitioned from {PreviousPhase} to {CurrentPhase} using trigger {Trigger}.", phaseBeforeTransition, CurrentPhase, trigger);
        return true;
    }

    /// <summary>
    /// Signals that the current "In..." phase (e.g., InLayout) has completed its work and should transition to its corresponding "Clean" or "Ready" state.
    /// </summary>
    /// <returns>True if an exit trigger was successfully mapped to a completion trigger and fired.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
    public bool SignalPhaseExit()
    {
        ThrowIfDisposed();
        var phaseBeforeExit = CurrentPhase; // Capture current phase before attempting exit
        var exitTriggerEnum = GetExitTriggerForState(phaseBeforeExit);

        if (exitTriggerEnum == null)
        {
            _logger.LogWarning("No exit trigger defined for current state {CurrentPhase} in SignalPhaseExit.", phaseBeforeExit);
            return false;
        }

        LifecycleTrigger exitTrigger = exitTriggerEnum.Value;

        if (ExitToCompletionTrigger.TryGetValue(exitTrigger, out var completionTrigger))
        {
            if (!_stateMachine.CanFire(completionTrigger))
            {
                _logger.LogWarning("Cannot fire completion trigger {CompletionTrigger} (mapped from {ExitTrigger}) for current state {CurrentPhase} in SignalPhaseExit.", completionTrigger, exitTrigger, phaseBeforeExit);
                return false;
            }
            _stateMachine.Fire(completionTrigger);
            _logger.LogDebug("Successfully exited {PreviousPhase} to {CurrentPhase} using completion trigger {CompletionTrigger} (from exit trigger {ExitTrigger}).", phaseBeforeExit, CurrentPhase, completionTrigger, exitTrigger);
            return true;
        }
        else if (!_stateMachine.CanFire(exitTrigger)) // Fallback for direct exit trigger if no mapping
        {
            _logger.LogWarning("Cannot fire direct exit trigger {ExitTrigger} for current state {CurrentPhase} in SignalPhaseExit (no completion mapping found or direct fire failed).", exitTrigger, phaseBeforeExit);
            return false;
        }

        _stateMachine.Fire(exitTrigger); // Firing direct exit trigger
        _logger.LogDebug("Successfully exited {PreviousPhase} to {CurrentPhase} using direct exit trigger {ExitTrigger}.", phaseBeforeExit, CurrentPhase, exitTrigger);
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
            .OnEntry(LogPhaseChangeAndPublish);

        // Configure the StyleClean state
        _stateMachine.Configure(DocumentLifecyclePhase.StyleClean)
            .Permit(LifecycleTrigger.BeginStyleCalculation, DocumentLifecyclePhase.InStyleRecalc)
            .Permit(LifecycleTrigger.EnterLayoutClean, DocumentLifecyclePhase.LayoutClean)
            .Permit(LifecycleTrigger.Dispose, DocumentLifecyclePhase.Disposed)
            .OnEntry(LogPhaseChangeAndPublish)
            .OnExit(LogPhaseChangeAndPublish);

        // Configure the InStyleRecalc state
        _stateMachine.Configure(DocumentLifecyclePhase.InStyleRecalc)
            .Permit(LifecycleTrigger.StyleComplete, DocumentLifecyclePhase.StyleClean)
            .Permit(LifecycleTrigger.ExitInStyleRecalc, DocumentLifecyclePhase.StyleClean)
            .OnEntry(LogPhaseChangeAndPublish)
            .OnExit(LogPhaseChangeAndPublish);

        // Configure the LayoutClean state
        _stateMachine.Configure(DocumentLifecyclePhase.LayoutClean)
            .Permit(LifecycleTrigger.BeginLayoutCalculation, DocumentLifecyclePhase.InLayout)
            .Permit(LifecycleTrigger.EnterRenderReady, DocumentLifecyclePhase.RenderReady)
            .Permit(LifecycleTrigger.Dispose, DocumentLifecyclePhase.Disposed)
            .OnEntry(LogPhaseChangeAndPublish)
            .OnExit(LogPhaseChangeAndPublish);

        // Configure the InLayout state
        _stateMachine.Configure(DocumentLifecyclePhase.InLayout)
            .Permit(LifecycleTrigger.LayoutComplete, DocumentLifecyclePhase.LayoutClean)
            .Permit(LifecycleTrigger.ExitInLayout, DocumentLifecyclePhase.LayoutClean)
            .OnEntry(LogPhaseChangeAndPublish)
            .OnExit(LogPhaseChangeAndPublish);

        // Configure the RenderReady state
        _stateMachine.Configure(DocumentLifecyclePhase.RenderReady)
            .Permit(LifecycleTrigger.BeginRendering, DocumentLifecyclePhase.InRender)
            .Permit(LifecycleTrigger.NextFrame, DocumentLifecyclePhase.StyleClean)
            .Permit(LifecycleTrigger.Dispose, DocumentLifecyclePhase.Disposed)
            .OnEntry(LogPhaseChangeAndPublish)
            .OnExit(LogPhaseChangeAndPublish);

        // Configure the InRender state
        _stateMachine.Configure(DocumentLifecyclePhase.InRender)
            .Permit(LifecycleTrigger.RenderComplete, DocumentLifecyclePhase.RenderReady)
            .Permit(LifecycleTrigger.ExitInRender, DocumentLifecyclePhase.RenderReady)
            .OnEntry(LogPhaseChangeAndPublish)
            .OnExit(LogPhaseChangeAndPublish);

        // Configure the Disposed state (terminal state)
        _stateMachine.Configure(DocumentLifecyclePhase.Disposed)
            .OnEntry(LogPhaseChangeAndPublish);
    }

    /// <summary>
    /// Helper method to log phase changes and publish an event.
    /// Used for OnEntry and OnExit handlers in state machine configuration.
    /// </summary>
    /// <param name="transition">The state machine transition details.</param>
    private void LogPhaseChangeAndPublish(StateMachine<DocumentLifecyclePhase, LifecycleTrigger>.Transition transition)
    {
        // For OnEntry, transition.Destination is the state being entered.
        // For OnExit, transition.Source is the state being exited.
        // We need to determine if this is an entry or exit to correctly identify the "active" phase for the event.

        DocumentLifecyclePhase phaseForEvent;
        PhaseChangeType changeType;

        // Heuristic: If the current state of the machine matches the transition's destination, it's likely an OnEntry.
        // If it matches the source, and source is not destination, it's likely an OnExit (already transitioned).
        // This relies on Stateless firing OnExit *after* the state has changed if not using deferred firing.
        // With Immediate firing, OnExit for S1 fires, then state becomes S2, then OnEntry for S2 fires.
        // So, in OnExit(S1->S2), _stateMachine.State is S2. transition.Source is S1.
        // In OnEntry(S1->S2), _stateMachine.State is S2. transition.Destination is S2.

        if (_stateMachine.State == transition.Destination && transition.Source != transition.Destination)
        {
            // This is most likely an OnEntry action or an OnExit action where state has already changed to destination
             if(IsEntering(transition)) // Check if we are truly entering the destination
             {
                phaseForEvent = transition.Destination;
                changeType = PhaseChangeType.Enter;
             }
             else // If not entering destination, it must be exiting the source
             {
                phaseForEvent = transition.Source;
                changeType = PhaseChangeType.Exit;
             }
        }
        else if (transition.Source == transition.Destination && transition.IsReentry) // Handle re-entry if configured
        {
            phaseForEvent = transition.Destination;
            changeType = PhaseChangeType.Enter; // Or a specific Reentry type
        }
        else // Default to source for exit if destination is different
        {
             phaseForEvent = transition.Source; // This is an OnExit action
             changeType = PhaseChangeType.Exit;
        }
         // A more direct way if OnEntry and OnExit were separate lambdas:
         // OnEntry: phaseForEvent = transition.Destination, changeType = PhaseChangeType.Enter
         // OnExit:  phaseForEvent = transition.Source,    changeType = PhaseChangeType.Exit

        _logger.LogInformation("DocumentLifecyclePhase changed: {Phase} ({ChangeType}). Source: {SourceState}, Destination: {DestinationState}, Trigger: {Trigger}",
            phaseForEvent, changeType, transition.Source, transition.Destination, transition.Trigger);

        _eventAggregator.Publish(
            new PhaseChangedEvent(phaseForEvent, changeType),
            EventPriority.High);
    }

    /// <summary>
    /// Determines if the transition represents an entry into the destination state.
    /// This is a simplified helper; Stateless's OnEntry/OnExit are more direct.
    /// </summary>
    private bool IsEntering(StateMachine<DocumentLifecyclePhase, LifecycleTrigger>.Transition transition)
    {
        // This is tricky because OnExit handlers are called *after* the state has already changed to the destination
        // when using FiringMode.Immediate.
        // A reliable way to distinguish OnEntry from OnExit when using a shared handler
        // is often to pass an explicit parameter to the handler or use separate handlers.
        // Given the current shared handler, we make an assumption.
        // If the state machine's current state IS the destination of the transition,
        // AND the source is different, it's an entry into the NEW state.
        // If the source IS the destination, it's a re-entry.
        if (transition.Source == transition.Destination) return transition.IsReentry; // True for re-entry

        // At the point OnEntry(S->D) is called, _stateMachine.State == D
        // At the point OnExit(S->D) is called, _stateMachine.State == D
        // This makes it hard to distinguish in a shared handler without more context.
        // For simplicity, if this method is called from OnEntry, transition.Destination is the key.
        // If called from OnExit, transition.Source is the key.
        // The current logic in LogPhaseChangeAndPublish tries to infer this.
        // Let's assume for this helper, if the machine is in the destination, it's an entry for event purposes.
        return _stateMachine.State == transition.Destination;
    }


    /// <summary>
    /// Gets the appropriate trigger for transitioning from the current state to the specified target state.
    /// Returns null if no direct trigger is defined for the state combination.
    /// </summary>
    private LifecycleTrigger? GetTriggerForTargetState(DocumentLifecyclePhase currentState, DocumentLifecyclePhase targetState)
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
            (DocumentLifecyclePhase.InStyleRecalc, DocumentLifecyclePhase.StyleClean) => LifecycleTrigger.StyleComplete,

            // From LayoutClean
            (DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.InLayout) => LifecycleTrigger.BeginLayoutCalculation,
            (DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.RenderReady) => LifecycleTrigger.EnterRenderReady,
            (DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.Disposed) => LifecycleTrigger.Dispose,

            // From InLayout
            (DocumentLifecyclePhase.InLayout, DocumentLifecyclePhase.LayoutClean) => LifecycleTrigger.LayoutComplete,

            // From RenderReady
            (DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.InRender) => LifecycleTrigger.BeginRendering,
            (DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.StyleClean) => LifecycleTrigger.NextFrame,
            (DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.Disposed) => LifecycleTrigger.Dispose,

            // From InRender
            (DocumentLifecyclePhase.InRender, DocumentLifecyclePhase.RenderReady) => LifecycleTrigger.RenderComplete,

            // Default case for unsupported transitions
            _ => null // Return null instead of throwing an exception
        };
    }

    /// <summary>
    /// Gets the generic exit trigger for the given "In..." state.
    /// This trigger is then mapped by SignalPhaseExit to a specific completion trigger.
    /// </summary>
    private LifecycleTrigger? GetExitTriggerForState(DocumentLifecyclePhase state)
    {
        return state switch
        {
            DocumentLifecyclePhase.InStyleRecalc => LifecycleTrigger.ExitInStyleRecalc,
            DocumentLifecyclePhase.InLayout => LifecycleTrigger.ExitInLayout,
            DocumentLifecyclePhase.InRender => LifecycleTrigger.ExitInRender,
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