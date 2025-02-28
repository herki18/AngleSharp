namespace AngleSharp.LayoutEngine.FormattingContexts;

using System;
using System.Collections.Generic;
using Core;
using DOM;
using Managers;

#pragma warning disable CS8602, CS8603
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
            context = new FlexFormattingContext.FlexFormattingContext(node);
        }
        else if (display == "grid" || display == "inline-grid")
        {
            throw new NotImplementedException("Grid layout not yet implemented.");
        }
        else if (display.StartsWith("inline") && display != "inline-block")
        {
            context = new InlineFormattingContext.InlineFormattingContext(node);
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