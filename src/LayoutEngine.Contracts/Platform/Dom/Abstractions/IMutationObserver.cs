using System;
using AngleSharp.Dom;

namespace LayoutEngine.Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Interface for an adapter around AngleSharp's MutationObserver.
/// </summary>
public interface IMutationObserver
{
    /// <summary>
    /// Registers the observer to receive notifications of DOM mutations on the specified node.
    /// </summary>
    /// <param name="target">The node to observe.</param>
    /// <param name="options">The options for the observation.</param>
    void Observe(INode target, MutationObserverInit options);

    /// <summary>
    /// Stops the observer from receiving notifications.
    /// </summary>
    void Disconnect();
}

/// <summary>
/// Interface for a factory that creates mutation observers.
/// </summary>
public interface IMutationObserverFactory
{
    /// <summary>
    /// Creates a mutation observer with the specified callback.
    /// </summary>
    /// <param name="callback">The callback to invoke when mutations occur.</param>
    /// <returns>A new mutation observer.</returns>
    IMutationObserver Create(Action<IMutationRecord[]> callback);
}

/// <summary>
/// Configuration options for a mutation observer.
/// </summary>
public class MutationObserverInit
{
    /// <summary>
    /// Gets or sets a value indicating whether to observe attribute changes.
    /// </summary>
    public bool Attributes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to observe character data changes.
    /// </summary>
    public bool CharacterData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to observe child list changes.
    /// </summary>
    public bool ChildList { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to observe the subtree.
    /// </summary>
    public bool Subtree { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to record the old value for attributes.
    /// </summary>
    public bool AttributeOldValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to record the old value for character data.
    /// </summary>
    public bool CharacterDataOldValue { get; set; }

    /// <summary>
    /// Gets or sets the filter for which attributes to observe.
    /// </summary>
    public string[]? AttributeFilter { get; set; }
}