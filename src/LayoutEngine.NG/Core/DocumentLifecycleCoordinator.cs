namespace LayoutEngine.NG.Core;

using System;

/// <summary>
/// Pure state machine for the document lifecycle.
/// Tracks phase transitions following BlinkNG's document lifecycle model.
/// </summary>
public class DocumentLifecycleCoordinator : IDocumentLifecycleCoordinator
{
    private DocumentLifecyclePhase _currentPhase = DocumentLifecyclePhase.Inactive;

    /// <inheritdoc/>
    public DocumentLifecyclePhase CurrentPhase => _currentPhase;

    /// <inheritdoc/>
    public event Action<DocumentLifecyclePhase>? PhaseChanged;

    /// <inheritdoc/>
    public bool CanAdvancePhase()
    {
        // In BlinkNG, phase transitions follow specific rules
        // This simplified version allows most transitions except from Disposed
        return _currentPhase != DocumentLifecyclePhase.Disposed;
    }

    /// <inheritdoc/>
    public void AdvancePhase()
    {
        if (!CanAdvancePhase())
        {
            throw new InvalidOperationException($"Cannot advance from phase {_currentPhase}");
        }

        // Simplified phase advancement - in real BlinkNG this would be more complex
        var nextPhase = GetNextPhase(_currentPhase);
        TransitionTo(nextPhase);
    }

    /// <summary>
    /// Transitions to a specific phase.
    /// </summary>
    public void TransitionTo(DocumentLifecyclePhase newPhase)
    {
        if (_currentPhase == newPhase)
            return;

        var oldPhase = _currentPhase;
        _currentPhase = newPhase;
        PhaseChanged?.Invoke(newPhase);
    }

    /// <summary>
    /// Determines the next phase in the lifecycle.
    /// Following BlinkNG's lifecycle progression.
    /// </summary>
    private DocumentLifecyclePhase GetNextPhase(DocumentLifecyclePhase currentPhase)
    {
        return currentPhase switch
        {
            DocumentLifecyclePhase.Inactive => DocumentLifecyclePhase.InStyleRecalc,
            DocumentLifecyclePhase.InStyleRecalc => DocumentLifecyclePhase.StyleClean,
            DocumentLifecyclePhase.StyleClean => DocumentLifecyclePhase.InLayout,
            DocumentLifecyclePhase.InLayout => DocumentLifecyclePhase.LayoutClean,
            DocumentLifecyclePhase.LayoutClean => DocumentLifecyclePhase.RenderReady,
            DocumentLifecyclePhase.RenderReady => DocumentLifecyclePhase.InRender,
            DocumentLifecyclePhase.InRender => DocumentLifecyclePhase.StyleClean,
            _ => DocumentLifecyclePhase.Unknown
        };
    }
}
