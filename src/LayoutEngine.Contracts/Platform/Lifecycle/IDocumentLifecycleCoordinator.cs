namespace LayoutEngine.Contracts.Platform.Lifecycle;

using System;

/// <summary>
/// Coordinates the document lifecycle phases.
/// </summary>
public interface IDocumentLifecycleCoordinator
{
    /// <summary>
    /// Gets the current phase of the document lifecycle.
    /// </summary>
    DocumentLifecyclePhase CurrentPhase { get; }

    /// <summary>
    /// Transitions the document to the specified phase.
    /// </summary>
    /// <param name="phase">The phase to enter.</param>
    /// <exception cref="InvalidOperationException">Thrown if the transition is invalid.</exception>
    void EnterPhase(DocumentLifecyclePhase phase);

    /// <summary>
    /// Exits the current phase.
    /// </summary>
    /// <param name="phase">The phase to exit.</param>
    /// <exception cref="InvalidOperationException">Thrown if the current phase doesn't match.</exception>
    void ExitPhase(DocumentLifecyclePhase phase);

    /// <summary>
    /// Determines if the specified transition is valid.
    /// </summary>
    /// <param name="fromPhase">The phase to transition from.</param>
    /// <param name="toPhase">The phase to transition to.</param>
    /// <returns>True if the transition is valid, otherwise false.</returns>
    bool IsValidTransition(DocumentLifecyclePhase fromPhase, DocumentLifecyclePhase toPhase);

    /// <summary>
    /// Determines if the operation is allowed in the current phase.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>True if the operation is allowed, otherwise false.</returns>
    bool IsOperationAllowed(DocumentOperation operation);
}

/// <summary>
/// Validates lifecycle state transitions and operations.
/// </summary>
public interface ILifecycleStateValidator
{
    /// <summary>
    /// Determines if the specified transition is valid.
    /// </summary>
    /// <param name="fromPhase">The phase to transition from.</param>
    /// <param name="toPhase">The phase to transition to.</param>
    /// <returns>True if the transition is valid, otherwise false.</returns>
    bool IsValidTransition(DocumentLifecyclePhase fromPhase, DocumentLifecyclePhase toPhase);

    /// <summary>
    /// Determines if the operation is allowed in the specified phase.
    /// </summary>
    /// <param name="phase">The document lifecycle phase.</param>
    /// <param name="operation">The operation to check.</param>
    /// <returns>True if the operation is allowed, otherwise false.</returns>
    bool IsOperationAllowed(DocumentLifecyclePhase phase, DocumentOperation operation);
}

/// <summary>
/// Defines the phases of the document lifecycle.
/// </summary>
public enum DocumentLifecyclePhase
{
    /// <summary>
    /// Initial state after creation before active processing.
    /// </summary>
    Inactive,

    /// <summary>
    /// Document with clean (up-to-date) styles.
    /// </summary>
    StyleClean,

    /// <summary>
    /// Actively calculating styles.
    /// </summary>
    InStyleRecalc,

    /// <summary>
    /// Styles requiring recalculation.
    /// </summary>
    StyleDirty,

    /// <summary>
    /// Document with clean (up-to-date) layout.
    /// </summary>
    LayoutClean,

    /// <summary>
    /// Actively calculating layout.
    /// </summary>
    InLayout,

    /// <summary>
    /// Layout requiring recalculation.
    /// </summary>
    LayoutDirty,

    /// <summary>
    /// Document ready for rendering via external frameworks.
    /// </summary>
    RenderReady,

    /// <summary>
    /// Actively rendering via external framework.
    /// </summary>
    InRender,

    /// <summary>
    /// Render requiring update in framework.
    /// </summary>
    RenderDirty,

    /// <summary>
    /// Document has been shut down.
    /// </summary>
    Disposed,

    /// <summary>
    /// Unknown phase (used for error conditions).
    /// </summary>
    Unknown
}

/// <summary>
/// Defines operations that can be performed on a document.
/// </summary>
public enum DocumentOperation
{
    /// <summary>
    /// Reading styles.
    /// </summary>
    StyleReading,

    /// <summary>
    /// Modifying styles.
    /// </summary>
    StyleModification,

    /// <summary>
    /// Reading layout.
    /// </summary>
    LayoutReading,

    /// <summary>
    /// Calculating layout.
    /// </summary>
    LayoutCalculation,

    /// <summary>
    /// Reading render information.
    /// </summary>
    RenderReading,

    /// <summary>
    /// Performing rendering.
    /// </summary>
    Rendering,

    /// <summary>
    /// Reading DOM.
    /// </summary>
    DomReading,

    /// <summary>
    /// Modifying DOM.
    /// </summary>
    DomModification
}

/// <summary>
/// Defines the type of phase change event.
/// </summary>
public enum PhaseChangeType
{
    /// <summary>
    /// Entering a phase.
    /// </summary>
    Enter,

    /// <summary>
    /// Exiting a phase.
    /// </summary>
    Exit
}