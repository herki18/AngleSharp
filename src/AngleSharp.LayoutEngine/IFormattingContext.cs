#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;

/// <summary>
/// Defines the interface for a formatting context, which determines how elements are laid out
/// within a specific region of the document. Formatting contexts are a fundamental concept in CSS
/// where each context manages its own internal layout rules and provides isolation for layout behaviors.
/// </summary>
public interface IFormattingContext
{
    /// <summary>
    /// Gets the layout node that establishes this formatting context.
    /// </summary>
    LayoutNode EstablishingNode { get; }

    /// <summary>
    /// Performs a complete layout of all elements within this formatting context.
    /// </summary>
    /// <param name="context">The layout context containing viewport information and constraints.</param>
    void Layout(LayoutContext context);

    /// <summary>
    /// Performs an incremental update of the layout for only the affected portions
    /// of this formatting context, typically used when a subset of elements has changed.
    /// </summary>
    /// <param name="context">The layout context containing viewport information and constraints.</param>
    void Reflow(LayoutContext context);

    /// <summary>
    /// Determines if the specified node is a participant in this formatting context.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node participates in this formatting context, otherwise false.</returns>
    bool ContainsNode(LayoutNode node);

    /// <summary>
    /// Gets all nodes participating in this formatting context.
    /// </summary>
    /// <returns>An enumerable collection of layout nodes.</returns>
    IEnumerable<LayoutNode> GetParticipants();

    /// <summary>
    /// Gets all child formatting contexts nested within this one.
    /// </summary>
    /// <returns>An enumerable collection of formatting contexts.</returns>
    IEnumerable<IFormattingContext> GetChildFormattingContexts();

    /// <summary>
    /// Adds a child formatting context to this formatting context.
    /// </summary>
    /// <param name="childContext">The child formatting context to add.</param>
    void AddChildContext(IFormattingContext childContext);
}

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

/// <summary>
/// Factory for creating formatting contexts based on node types and CSS properties.
/// </summary>
public class FormattingContextFactory
{
    private readonly IMarginManager _marginManager;

    /// <summary>
    /// Creates a new formatting context factory.
    /// </summary>
    /// <param name="marginManager">The margin manager to use for margin calculations.</param>
    public FormattingContextFactory(IMarginManager marginManager)
    {
        _marginManager = marginManager;
    }

    /// <summary>
    /// Creates formatting contexts for all nodes in the layout tree.
    /// </summary>
    /// <param name="tree">The layout tree to process.</param>
    public void CreateFormattingContexts(LayoutTree tree)
    {
        // Start with the root node
        CreateFormattingContextsRecursive(tree.Root);
    }

    /// <summary>
    /// Gets all formatting contexts affected by changes to the specified nodes.
    /// </summary>
    /// <param name="dirtyNodes">The nodes that have changed.</param>
    /// <returns>A list of affected formatting contexts.</returns>
    public List<IFormattingContext> GetAffectedContexts(IEnumerable<LayoutNode> dirtyNodes)
    {
        var affectedContexts = new HashSet<IFormattingContext>();

        foreach (var node in dirtyNodes)
        {
            // Find the nearest formatting context for this node
            var context = FindNearestFormattingContext(node);
            if (context != null)
            {
                affectedContexts.Add(context);
            }
        }

        return new List<IFormattingContext>(affectedContexts);
    }

    /// <summary>
    /// Recursively creates formatting contexts for a node and its descendants.
    /// </summary>
    /// <param name="node">The node to process.</param>
    private void CreateFormattingContextsRecursive(LayoutNode node)
    {
        // Check if this node establishes a formatting context
        if (NodeEstablishesFormattingContext(node))
        {
            CreateFormattingContext(node);
        }

        // Process children
        foreach (var child in node.Children)
        {
            CreateFormattingContextsRecursive(child);
        }
    }

    /// <summary>
    /// Determines if a node establishes its own formatting context.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node establishes a formatting context, otherwise false.</returns>
    private bool NodeEstablishesFormattingContext(LayoutNode node)
    {
        // Get element style
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return false;

        // Determine if the node creates a formatting context based on CSS properties
        node.CreatesFormattingContext = DetermineIfCreatesFormattingContext(element);
        return node.CreatesFormattingContext;
    }

    /// <summary>
    /// Creates the appropriate formatting context for a node.
    /// </summary>
    /// <param name="node">The node that establishes the formatting context.</param>
    /// <returns>The created formatting context.</returns>
    private IFormattingContext CreateFormattingContext(LayoutNode node)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return null;

        string display = element.ComputedStyle.GetPropertyValue("display") ?? "inline";

        // Create the appropriate formatting context based on display type
        IFormattingContext context;

        if (display == "flex" || display == "inline-flex")
        {
            context = new FlexFormattingContext(node);
        }
        else if (display == "grid" || display == "inline-grid")
        {
            throw new NotImplementedException("Grid layout not yet implemented.");
        }
        else if (display.StartsWith("inline") && display != "inline-block")
        {
            context = new InlineFormattingContext(node);
        }
        else
        {
            // Default to block formatting context
            context = new BlockFormattingContext(node, _marginManager);
        }

        // Connect to parent context
        if (node.Parent?.FormattingContext != null)
        {
            node.Parent.FormattingContext.AddChildContext(context);
        }

        return context;
    }

    /// <summary>
    /// Determines if an element creates its own formatting context based on CSS properties.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>True if the element creates a formatting context, otherwise false.</returns>
    private bool DetermineIfCreatesFormattingContext(ElementNode element)
    {
        var style = element.ComputedStyle;
        string display = style.GetPropertyValue("display") ?? "inline";
        string position = style.GetPropertyValue("position") ?? "static";
        string float_ = style.GetPropertyValue("float") ?? "none";
        string overflow = style.GetPropertyValue("overflow") ?? "visible";

        // According to CSS spec, these create a new formatting context:
        return display == "flow-root" ||
               display == "flex" ||
               display == "inline-flex" ||
               display == "grid" ||
               display == "inline-grid" ||
               display == "table-cell" ||
               position == "absolute" ||
               position == "fixed" ||
               float_ != "none" ||
               overflow != "visible" ||
               display == "inline-block";
    }

    /// <summary>
    /// Finds the nearest formatting context for a node.
    /// </summary>
    /// <param name="node">The node to find the context for.</param>
    /// <returns>The nearest formatting context.</returns>
    private IFormattingContext FindNearestFormattingContext(LayoutNode node)
    {
        var current = node;
        while (current != null)
        {
            if (current.FormattingContext != null)
                return current.FormattingContext;
            current = current.Parent;
        }
        return null;
    }
}