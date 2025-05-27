namespace LayoutEngine.Core.Core;

using System;
using AngleSharp.Dom;

/// <summary>
/// A factory interface for creating AngleSharp MutationObserver instances.
/// Uses AngleSharp's native callback signature.
/// </summary>
public interface IMutationObserverFactory
{
    /// <summary>
    /// Creates a new AngleSharp MutationObserver.
    /// </summary>
    /// <param name="callback">The callback to invoke when mutations occur, passing both the mutation records and the observer itself.</param>
    /// <returns>A new MutationObserver instance.</returns>
    IMutationObserver Create(Action<IMutationRecord[], IMutationObserver> callback);
}

/// <summary>
/// Configuration options for DOM mutation tracking.
/// </summary>
public class MutationTrackerOptions
{
    /// <summary>
    /// Whether to observe attributes. Default is true.
    /// </summary>
    public bool TrackAttributes { get; set; } = true;

    /// <summary>
    /// Whether to observe character data. Default is true.
    /// </summary>
    public bool TrackCharacterData { get; set; } = true;

    /// <summary>
    /// Whether to observe child nodes. Default is true.
    /// </summary>
    public bool TrackChildList { get; set; } = true;

    /// <summary>
    /// Whether to observe the subtree. Default is true.
    /// </summary>
    public bool TrackSubtree { get; set; } = true;

    /// <summary>
    /// Whether to record old attribute values. Default is true.
    /// </summary>
    public bool RecordAttributeOldValue { get; set; } = true;

    /// <summary>
    /// Whether to record old character data values. Default is true.
    /// </summary>
    public bool RecordCharacterDataOldValue { get; set; } = true;

    /// <summary>
    /// Which attributes to track. If null, all attributes are tracked.
    /// </summary>
    public string[]? AttributeFilter { get; set; }
}

/// <summary>
/// Interface for tracking DOM mutations.
/// This interface is simplified to focus on tracking elements with configuration options.
/// </summary>
public interface IDomMutationTracker : IDisposable
{
    /// <summary>
    /// Starts tracking mutations for the specified element with default options.
    /// </summary>
    /// <param name="element">The element to track.</param>
    /// <returns>A tracking ID that can be used to stop tracking.</returns>
    Guid TrackElement(IElement element);

    /// <summary>
    /// Starts tracking mutations for the specified element with custom options.
    /// </summary>
    /// <param name="element">The element to track.</param>
    /// <param name="options">Configuration options for tracking.</param>
    /// <returns>A tracking ID that can be used to stop tracking.</returns>
    Guid TrackElement(IElement element, MutationTrackerOptions options);

    /// <summary>
    /// Starts tracking mutations for the document's document element with default options.
    /// </summary>
    /// <param name="document">The document to track.</param>
    /// <returns>A tracking ID that can be used to stop tracking.</returns>
    Guid TrackDocument(IDocument document);

    /// <summary>
    /// Starts tracking mutations for the document's document element with custom options.
    /// </summary>
    /// <param name="document">The document to track.</param>
    /// <param name="options">Configuration options for tracking.</param>
    /// <returns>A tracking ID that can be used to stop tracking.</returns>
    Guid TrackDocument(IDocument document, MutationTrackerOptions options);

    /// <summary>
    /// Stops tracking the specified element.
    /// </summary>
    /// <param name="element">The element to stop tracking.</param>
    void StopTracking(IElement element);

    /// <summary>
    /// Stops tracking by tracking ID.
    /// </summary>
    /// <param name="trackingId">The tracking ID returned from a Track method.</param>
    void StopTracking(Guid trackingId);

    /// <summary>
    /// Stops all tracking.
    /// </summary>
    void StopAllTracking();

    /// <summary>
    /// Processes mutations manually if needed.
    /// </summary>
    /// <param name="mutations">The mutation records to process.</param>
    /// <param name="observer"></param>
    void ProcessMutations(IMutationRecord[] mutations, IMutationObserver? observer = null);
}