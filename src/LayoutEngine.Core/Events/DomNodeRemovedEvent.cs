namespace LayoutEngine.Core.Events;

using System;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

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