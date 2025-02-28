namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

/// <summary>
/// Builds a render tree from an AngleSharp DOM tree, optimized to leverage AngleSharp's
/// capabilities and minimize duplication.
/// </summary>
public class RenderTreeBuilder
{
    private readonly Dictionary<INode, IRenderNode> _nodeMap = new Dictionary<INode, IRenderNode>();
    private readonly HashSet<IRenderNode> _addedNodes = new HashSet<IRenderNode>();
    private readonly HashSet<IRenderNode> _removedNodes = new HashSet<IRenderNode>();
    private readonly HashSet<IRenderNode> _changedNodes = new HashSet<IRenderNode>();
    private RenderTree _lastRenderTree;

    /// <summary>
    /// Builds a render tree from the specified document.
    /// </summary>
    /// <param name="document">The document to build the render tree from.</param>
    /// <returns>The built render tree.</returns>
    public RenderTree BuildRenderTree(IDocument document)
    {
        _nodeMap.Clear();
        _addedNodes.Clear();
        _removedNodes.Clear();
        _changedNodes.Clear();

        var root = document.DocumentElement;
        if (root == null)
        {
            return new RenderTree(null);
        }

        // Create render tree
        var renderTree = new RenderTree(BuildRenderNode(root, null));

        // Store render tree for change tracking
        _lastRenderTree = renderTree;

        return renderTree;
    }

    /// <summary>
    /// Finds a render node for the specified DOM node.
    /// </summary>
    /// <param name="node">The DOM node to find the render node for.</param>
    /// <returns>The corresponding render node, or null if not found.</returns>
    public IRenderNode FindRenderNode(INode node)
    {
        if (_nodeMap.TryGetValue(node, out var renderNode))
        {
            return renderNode;
        }
        return null;
    }

    /// <summary>
    /// Builds a render node for the specified DOM node.
    /// </summary>
    /// <param name="node">The DOM node to build a render node for.</param>
    /// <param name="parent">The parent render node, if any.</param>
    /// <returns>The built render node.</returns>
    private IRenderNode BuildRenderNode(INode node, IRenderNode parent)
    {
        // Check if this node should be rendered
        if (!ShouldRender(node))
        {
            return null;
        }

        // Create appropriate render node based on node type
        IRenderNode renderNode;

        if (node is IElement element)
        {
            // Get computed style directly from AngleSharp
            var computedStyle = element.GetComputedStyle();

            // Create child render nodes
            var children = new List<IRenderNode>();
            foreach (var child in node.ChildNodes)
            {
                var childRenderNode = BuildRenderNode(child, null);
                if (childRenderNode != null)
                {
                    children.Add(childRenderNode);
                    childRenderNode.Parent = null; // Will be set later
                }
            }

            // Use AngleSharp's IStyle interface for specified style (inline styles)
            var specifiedStyle = element.GetStyle();

            // Create element render node
            renderNode = new ElementNode(element, children, specifiedStyle, computedStyle);
        }
        else if (node is IText text && !string.IsNullOrWhiteSpace(text.TextContent))
        {
            // Create text render node
            renderNode = new TextNode(text);
        }
        else if (node is IComment || node is IDocumentType)
        {
            // Non-renderable nodes
            renderNode = new NonRenderableNode(node, Array.Empty<IRenderNode>());
        }
        else
        {
            // Unknown or non-renderable node type
            return null;
        }

        // Set parent relationship
        renderNode.Parent = parent;

        // Store in node map
        _nodeMap[node] = renderNode;

        // Track changes
        if (_lastRenderTree != null)
        {
            var previousRenderNode = _lastRenderTree.FindNodeByDomNode(node);
            if (previousRenderNode == null)
            {
                _addedNodes.Add(renderNode);
            }
            else if (HasNodeChanged(previousRenderNode, renderNode))
            {
                _changedNodes.Add(renderNode);
            }
        }

        // Update parent references for children
        if (renderNode is ElementNode elementNode)
        {
            foreach (var child in elementNode.Children)
            {
                child.Parent = renderNode;
            }
        }

        return renderNode;
    }

    /// <summary>
    /// Determines if a DOM node should be rendered.
    /// </summary>
    /// <param name="node">The DOM node to check.</param>
    /// <returns>True if the node should be rendered, false otherwise.</returns>
    private bool ShouldRender(INode node)
    {
        if (node is IElement element)
        {
            // Check display:none using AngleSharp's computed style
            var computedStyle = element.GetComputedStyle();
            if (computedStyle != null && computedStyle.Display == "none")
            {
                return false;
            }

            return true;
        }

        if (node is IText text)
        {
            // Only render non-empty text nodes
            return !string.IsNullOrWhiteSpace(text.TextContent);
        }

        // Don't render comments, document types, etc.
        return false;
    }

    /// <summary>
    /// Determines if a render node has changed compared to a previous version.
    /// </summary>
    /// <param name="oldNode">The old render node.</param>
    /// <param name="newNode">The new render node.</param>
    /// <returns>True if the node has changed, false otherwise.</returns>
    private bool HasNodeChanged(IRenderNode oldNode, IRenderNode newNode)
    {
        // Check if node types differ
        if (oldNode.GetType() != newNode.GetType())
            return true;

        // For elements, compare computed styles
        if (oldNode is ElementNode oldElement && newNode is ElementNode newElement)
        {
            // Compare by checking key layout properties
            // AngleSharp doesn't provide a good way to compare full computed styles at once
            var oldComputed = oldElement.ComputedStyle;
            var newComputed = newElement.ComputedStyle;

            if (oldComputed == null || newComputed == null)
                return oldComputed != newComputed; // One is null, one isn't

            // Check key layout-affecting properties
            string[] keyProperties = {
                "display", "position", "float", "width", "height",
                "margin-top", "margin-right", "margin-bottom", "margin-left",
                "padding-top", "padding-right", "padding-bottom", "padding-left",
                "border-top-width", "border-right-width", "border-bottom-width", "border-left-width"
            };

            foreach (var prop in keyProperties)
            {
                if (oldComputed.GetPropertyValue(prop) != newComputed.GetPropertyValue(prop))
                    return true;
            }

            // Check if attributes that affect layout have changed
            // Class and ID changes could affect styles
            if (oldElement.Ref.ClassName != newElement.Ref.ClassName ||
                oldElement.Ref.Id != newElement.Ref.Id)
                return true;
        }

        // For text nodes, check content
        if (oldNode is TextNode oldText && newNode is TextNode newText)
        {
            return oldText.Ref.TextContent != newText.Ref.TextContent;
        }

        // No changes detected
        return false;
    }

    /// <summary>
    /// Gets render nodes that have been added since the last render tree build.
    /// </summary>
    /// <returns>A collection of added render nodes.</returns>
    public IEnumerable<IRenderNode> GetAddedNodes()
    {
        return _addedNodes;
    }

    /// <summary>
    /// Gets render nodes that have been removed since the last render tree build.
    /// </summary>
    /// <returns>A collection of removed render nodes.</returns>
    public IEnumerable<IRenderNode> GetRemovedNodes()
    {
        return _removedNodes;
    }

    /// <summary>
    /// Gets render nodes that have changed since the last render tree build.
    /// </summary>
    /// <returns>A collection of changed render nodes.</returns>
    public IEnumerable<IRenderNode> GetChangedNodes()
    {
        return _changedNodes;
    }
}