namespace LayoutEngine.Contracts.Platform.Dom;

using AngleSharp.Dom;

/// <summary>
/// Tracks DOM mutations and generates events.
/// </summary>
public interface IDomMutationTracker
{
    /// <summary>
    /// Begins tracking changes in the specified document.
    /// </summary>
    /// <param name="document">The document to track.</param>
    void TrackDocument(IDocument document);

    /// <summary>
    /// Begins tracking changes in the specified element and its descendants.
    /// </summary>
    /// <param name="element">The element to track.</param>
    void TrackElement(IElement element);

    /// <summary>
    /// Begins tracking changes to the specified attribute of the specified element.
    /// </summary>
    /// <param name="element">The element to track.</param>
    /// <param name="attributeName">The name of the attribute to track.</param>
    void TrackAttribute(IElement element, string attributeName);

    /// <summary>
    /// Stops tracking changes in the specified element and its descendants.
    /// </summary>
    /// <param name="element">The element to stop tracking.</param>
    void StopTracking(IElement element);

    /// <summary>
    /// Stops tracking changes in the specified document.
    /// </summary>
    /// <param name="document">The document to stop tracking.</param>
    void StopTracking(IDocument document);
}