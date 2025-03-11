namespace AngleSharp.StyleSystem.Models;

using Dom;

/// <summary>
/// Represents a change to the DOM that may affect styling.
/// This object contains information about the change without making any
/// decisions about which elements need style invalidation.
/// </summary>
public class DomChange
{
    /// <summary>
    /// Gets or sets the type of DOM change.
    /// </summary>
    public DomChangeType Type { get; set; }

    /// <summary>
    /// Gets or sets the target element or node of the change.
    /// This is typically the element whose attribute changed, or the parent element for childList mutations.
    /// </summary>
    public INode? Target { get; set; }

    /// <summary>
    /// Gets or sets the node that was added or removed (for NodeAdded/NodeRemoved changes).
    /// </summary>
    public INode? Node { get; set; }

    /// <summary>
    /// Gets or sets the name of the attribute that changed (for AttributeChanged changes).
    /// </summary>
    public string? AttributeName { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the attribute that changed (for AttributeChanged changes).
    /// </summary>
    public string? AttributeNamespace { get; set; }

    /// <summary>
    /// Gets or sets the old value of the changed attribute or text.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value of the changed attribute or text.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the previous sibling node (for NodeAdded/NodeRemoved changes).
    /// </summary>
    public INode? PreviousSibling { get; set; }

    /// <summary>
    /// Gets or sets the next sibling node (for NodeAdded/NodeRemoved changes).
    /// </summary>
    public INode? NextSibling { get; set; }
}