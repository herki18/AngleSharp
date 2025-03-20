using System.Collections.Generic;

namespace LayoutEngine.Platform.Lifecycle;

using Contracts.Platform.Lifecycle;

/// <summary>
/// Validates lifecycle state transitions and operations.
/// Enforces the document lifecycle state machine rules.
/// </summary>
public sealed class LifecycleStateValidator : ILifecycleStateValidator
{
    // Valid state transitions dictionary
    private static readonly Dictionary<DocumentLifecyclePhase, HashSet<DocumentLifecyclePhase>> ValidTransitions = new()
    {
        // From Inactive
        [DocumentLifecyclePhase.Inactive] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.StyleClean,
            DocumentLifecyclePhase.Disposed
        },

        // From StyleClean
        [DocumentLifecyclePhase.StyleClean] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.InStyleRecalc,
            DocumentLifecyclePhase.LayoutClean,
            DocumentLifecyclePhase.Disposed
        },

        // From InStyleRecalc
        [DocumentLifecyclePhase.InStyleRecalc] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.StyleDirty,
            DocumentLifecyclePhase.StyleClean
        },

        // From StyleDirty
        [DocumentLifecyclePhase.StyleDirty] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.StyleClean
        },

        // From LayoutClean
        [DocumentLifecyclePhase.LayoutClean] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.InLayout,
            DocumentLifecyclePhase.RenderReady,
            DocumentLifecyclePhase.Disposed
        },

        // From InLayout
        [DocumentLifecyclePhase.InLayout] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.LayoutDirty,
            DocumentLifecyclePhase.LayoutClean
        },

        // From LayoutDirty
        [DocumentLifecyclePhase.LayoutDirty] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.LayoutClean
        },

        // From RenderReady
        [DocumentLifecyclePhase.RenderReady] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.InRender,
            DocumentLifecyclePhase.StyleClean, // Next frame
            DocumentLifecyclePhase.Disposed
        },

        // From InRender
        [DocumentLifecyclePhase.InRender] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.RenderDirty,
            DocumentLifecyclePhase.RenderReady
        },

        // From RenderDirty
        [DocumentLifecyclePhase.RenderDirty] = new HashSet<DocumentLifecyclePhase>
        {
            DocumentLifecyclePhase.RenderReady
        },

        // From Disposed (terminal state)
        [DocumentLifecyclePhase.Disposed] = new HashSet<DocumentLifecyclePhase>()
    };

    // Operation permissions by phase
    private static readonly Dictionary<DocumentLifecyclePhase, HashSet<DocumentOperation>> AllowedOperations = new()
    {
        // Inactive
        [DocumentLifecyclePhase.Inactive] = new HashSet<DocumentOperation>
        {
            DocumentOperation.DomReading,
            DocumentOperation.DomModification
        },

        // StyleClean
        [DocumentLifecyclePhase.StyleClean] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.StyleModification,
            DocumentOperation.DomReading,
            DocumentOperation.DomModification
        },

        // InStyleRecalc
        [DocumentLifecyclePhase.InStyleRecalc] = new HashSet<DocumentOperation>
        {
            DocumentOperation.DomReading
        },

        // StyleDirty
        [DocumentLifecyclePhase.StyleDirty] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleModification,
            DocumentOperation.DomReading
        },

        // LayoutClean
        [DocumentLifecyclePhase.LayoutClean] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutReading,
            DocumentOperation.DomReading
        },

        // InLayout
        [DocumentLifecyclePhase.InLayout] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutCalculation,
            DocumentOperation.DomReading
        },

        // LayoutDirty
        [DocumentLifecyclePhase.LayoutDirty] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutCalculation,
            DocumentOperation.DomReading
        },

        // RenderReady
        [DocumentLifecyclePhase.RenderReady] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutReading,
            DocumentOperation.RenderReading,
            DocumentOperation.DomReading
        },

        // InRender
        [DocumentLifecyclePhase.InRender] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutReading,
            DocumentOperation.RenderReading,
            DocumentOperation.Rendering,
            DocumentOperation.DomReading
        },

        // RenderDirty
        [DocumentLifecyclePhase.RenderDirty] = new HashSet<DocumentOperation>
        {
            DocumentOperation.StyleReading,
            DocumentOperation.LayoutReading,
            DocumentOperation.RenderReading,
            DocumentOperation.Rendering,
            DocumentOperation.DomReading
        },

        // Disposed
        [DocumentLifecyclePhase.Disposed] = new HashSet<DocumentOperation>()
    };

    /// <summary>
    /// Determines if the specified transition is valid.
    /// </summary>
    /// <param name="fromPhase">The phase to transition from.</param>
    /// <param name="toPhase">The phase to transition to.</param>
    /// <returns>True if the transition is valid, otherwise false.</returns>
    public bool IsValidTransition(DocumentLifecyclePhase fromPhase, DocumentLifecyclePhase toPhase)
    {
        // Check if the transition is defined in the valid transitions dictionary
        if (ValidTransitions.TryGetValue(fromPhase, out var validTargets))
        {
            return validTargets.Contains(toPhase);
        }

        // Unknown source phase, no valid transitions
        return false;
    }

    /// <summary>
    /// Determines if the operation is allowed in the specified phase.
    /// </summary>
    /// <param name="phase">The document lifecycle phase.</param>
    /// <param name="operation">The operation to check.</param>
    /// <returns>True if the operation is allowed, otherwise false.</returns>
    public bool IsOperationAllowed(DocumentLifecyclePhase phase, DocumentOperation operation)
    {
        // Check if the operation is allowed in the specified phase
        if (AllowedOperations.TryGetValue(phase, out var allowedOps))
        {
            return allowedOps.Contains(operation);
        }

        // Unknown phase, no allowed operations
        return false;
    }
}