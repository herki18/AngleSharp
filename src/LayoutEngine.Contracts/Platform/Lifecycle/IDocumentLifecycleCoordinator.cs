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