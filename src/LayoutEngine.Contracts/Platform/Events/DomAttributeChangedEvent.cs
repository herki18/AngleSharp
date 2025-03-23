using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;
using System;

namespace LayoutEngine.Contracts.Platform.Events;

/// <summary>
/// Event that is published when a DOM attribute is changed.
/// </summary>
public class DomAttributeChangedEvent : EventBase
{
    /// <summary>
    /// The element whose attribute changed.
    /// </summary>
    public IElement Element { get; }

    /// <summary>
    /// The name of the attribute that changed.
    /// </summary>
    public string AttributeName { get; }

    /// <summary>
    /// The old value of the attribute.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// The new value of the attribute.
    /// </summary>
    public string? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomAttributeChangedEvent"/> class.
    /// </summary>
    public DomAttributeChangedEvent(IElement element, string attributeName, string? oldValue, string? newValue)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Event that is published when a DOM node is added.
/// </summary>
public class DomNodeAddedEvent : EventBase
{
    /// <summary>
    /// The node that was added.
    /// </summary>
    public INode Node { get; }

    /// <summary>
    /// The parent node that the node was added to.
    /// </summary>
    public INode Parent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomNodeAddedEvent"/> class.
    /// </summary>
    public DomNodeAddedEvent(INode node, INode parent)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }
}

/// <summary>
/// Event that is published when a DOM node is removed.
/// </summary>
public class DomNodeRemovedEvent : EventBase
{
    /// <summary>
    /// The node that was removed.
    /// </summary>
    public INode Node { get; }

    /// <summary>
    /// The parent node that the node was removed from.
    /// </summary>
    public INode Parent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomNodeRemovedEvent"/> class.
    /// </summary>
    public DomNodeRemovedEvent(INode node, INode parent)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }
}

/// <summary>
/// Event that is published when the text of a DOM text node is changed.
/// </summary>
public class DomTextChangedEvent : EventBase
{
    /// <summary>
    /// The text node whose content changed.
    /// </summary>
    public IText TextNode { get; }

    /// <summary>
    /// The old value of the text.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// The new value of the text.
    /// </summary>
    public string? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomTextChangedEvent"/> class.
    /// </summary>
    public DomTextChangedEvent(IText textNode, string? oldValue, string? newValue)
    {
        TextNode = textNode ?? throw new ArgumentNullException(nameof(textNode));
        OldValue = oldValue;
        NewValue = newValue;
    }
}