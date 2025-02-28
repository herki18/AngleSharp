namespace AngleSharp.LayoutEngine.FormattingContexts;

using System.Collections.Generic;
using Core;
using DOM;

#pragma warning disable CS8602, CS8603
/// <summary>
/// Base implementation of the formatting context interface, providing common functionality
/// for all formatting context types.
/// </summary>
public abstract class FormattingContext : IFormattingContext
{
    /// <summary>
    /// Collection of layout nodes that participate in this formatting context.
    /// </summary>
    protected readonly List<LayoutNode> _participants = new();

    /// <summary>
    /// Collection of child formatting contexts nested within this one.
    /// </summary>
    protected readonly List<IFormattingContext> _childContexts = new();

    /// <summary>
    /// Gets the layout node that establishes this formatting context.
    /// </summary>
    public LayoutNode EstablishingNode { get; }

    /// <summary>
    /// Creates a new formatting context with the specified establishing node.
    /// </summary>
    /// <param name="establishingNode">The node that establishes this formatting context.</param>
    protected FormattingContext(LayoutNode establishingNode)
    {
        EstablishingNode = establishingNode;
        establishingNode.FormattingContext = this;
    }

    /// <summary>
    /// Performs a complete layout of all elements within this formatting context.
    /// Each derived class must implement this method according to its specific layout rules.
    /// </summary>
    /// <param name="context">The layout context containing viewport information and constraints.</param>
    public abstract void Layout(LayoutContext context);

    /// <summary>
    /// Performs an incremental update of the layout for only the affected portions
    /// of this formatting context. Each derived class must implement this method according
    /// to its specific incremental layout strategy.
    /// </summary>
    /// <param name="context">The layout context containing viewport information and constraints.</param>
    public abstract void Reflow(LayoutContext context);

    /// <summary>
    /// Determines if the specified node is a participant in this formatting context.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node participates in this formatting context, otherwise false.</returns>
    public bool ContainsNode(LayoutNode node)
    {
        return _participants.Contains(node);
    }

    /// <summary>
    /// Gets all nodes participating in this formatting context.
    /// </summary>
    /// <returns>An enumerable collection of layout nodes.</returns>
    public IEnumerable<LayoutNode> GetParticipants()
    {
        return _participants;
    }

    /// <summary>
    /// Gets all child formatting contexts nested within this one.
    /// </summary>
    /// <returns>An enumerable collection of formatting contexts.</returns>
    public IEnumerable<IFormattingContext> GetChildFormattingContexts()
    {
        return _childContexts;
    }

    /// <summary>
    /// Adds a child formatting context to this formatting context.
    /// </summary>
    /// <param name="childContext">The child formatting context to add.</param>
    public void AddChildContext(IFormattingContext childContext)
    {
        _childContexts.Add(childContext);
    }

    /// <summary>
    /// Collects all nodes that participate in this formatting context.
    /// This method should be called during initialization to populate the _participants collection.
    /// </summary>
    /// <param name="node">The node to start collection from, typically the establishing node.</param>
    protected void CollectParticipants(LayoutNode node)
    {
        // Skip if node establishes its own formatting context and isn't the establishing node
        if (node != EstablishingNode && node.CreatesFormattingContext)
            return;

        // Add this node as a participant
        _participants.Add(node);

        // Process children
        foreach (var child in node.Children)
        {
            CollectParticipants(child);
        }
    }

    /// <summary>
    /// Checks if a node is in normal flow (not positioned absolutely or fixed, not floating).
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is in normal flow, otherwise false.</returns>
    protected bool IsInNormalFlow(LayoutNode node)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return true;

        string position = element.ComputedStyle.GetPropertyValue("position") ?? "static";
        if (position == "absolute" || position == "fixed")
            return false;

        string float_ = element.ComputedStyle.GetPropertyValue("float") ?? "none";
        if (float_ != "none")
            return false;

        return true;
    }

    /// <summary>
    /// Determines if a node is a block-level element.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is a block-level element, otherwise false.</returns>
    protected bool IsBlockLevel(LayoutNode node)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return false;

        string display = element.ComputedStyle.GetPropertyValue("display") ?? "inline";

        return display == "block" ||
               display == "flex" ||
               display == "grid" ||
               display == "flow-root" ||
               display == "table";
    }

    /// <summary>
    /// Determines if a node is an inline-level element.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is an inline-level element, otherwise false.</returns>
    protected bool IsInlineLevel(LayoutNode node)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return true; // Text nodes are inline by default

        string display = element.ComputedStyle.GetPropertyValue("display") ?? "inline";

        return display == "inline" ||
               display == "inline-block" ||
               display == "inline-flex" ||
               display == "inline-grid" ||
               display == "inline-table";
    }
}