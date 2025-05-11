namespace LayoutEngine.Core.Events;

using System;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Events;

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