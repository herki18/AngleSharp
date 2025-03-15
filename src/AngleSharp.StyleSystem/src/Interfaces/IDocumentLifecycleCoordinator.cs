namespace AngleSharp.StyleSystem.Interfaces;

using System;
using AngleSharp.Dom;

/// <summary>
/// Coordinates document lifecycle with the style system.
/// </summary>
public interface IDocumentLifecycleCoordinator : IDisposable
{
    /// <summary>
    /// Gets the current document being coordinated.
    /// </summary>
    IDocument? CurrentDocument { get; }

    /// <summary>
    /// Attaches the style system to a document.
    /// </summary>
    /// <param name="document">The document to attach to.</param>
    void AttachToDocument(IDocument document);

    /// <summary>
    /// Detaches the style system from a document.
    /// </summary>
    /// <param name="document">The document to detach from.</param>
    void DetachFromDocument(IDocument document);

    /// <summary>
    /// Checks for document changes in the context and updates accordingly.
    /// </summary>
    void CheckForDocumentChange();

    /// <summary>
    /// Notifies observers of a DOM update.
    /// </summary>
    /// <param name="document">The document that was updated.</param>
    void NotifyDomUpdated(IDocument document);

    /// <summary>
    /// Notifies observers of a viewport change.
    /// </summary>
    /// <param name="width">The new viewport width.</param>
    /// <param name="height">The new viewport height.</param>
    void NotifyViewportChanged(int width, int height);
}