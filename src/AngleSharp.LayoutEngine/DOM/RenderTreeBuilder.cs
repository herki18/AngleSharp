#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8600, CS8602, CS8603, CS8625
namespace AngleSharp.LayoutEngine.DOM;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
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
            // Get computed style - AngleSharp requires window and element
            ICssStyleDeclaration computedStyle = null;
            ICssStyleDeclaration specifiedStyle = null;

            if (element.OwnerDocument?.DefaultView != null)
            {
                computedStyle = element.OwnerDocument.DefaultView.GetComputedStyle(element, null);

                // Get inline style directly from the element using AngleSharp's APIs
                if (element is IHtmlElement htmlElement)
                {
                    specifiedStyle = (ICssStyleDeclaration?)htmlElement.Style;
                }
            }

            // Create child render nodes
            var children = new List<IRenderNode>();
            foreach (var child in node.ChildNodes)
            {
                var childRenderNode = BuildRenderNode(child, null);
                if (childRenderNode != null)
                {
                    children.Add(childRenderNode);
                }
            }

            // Create element render node
            renderNode = new ElementNode(element, children, specifiedStyle, computedStyle);

            // Set parent relationship for children
            foreach (var child in children)
            {
                if (child is ElementNode childElement)
                {
                    childElement.Parent = renderNode;
                }
                else if (child is DOM.TextNode childText)
                {
                    childText.Parent = renderNode;
                }
                else if (child is NonRenderableNode childNonRenderable)
                {
                    childNonRenderable.Parent = renderNode;
                }
            }
        }
        else if (node is IText text && !string.IsNullOrWhiteSpace(text.TextContent))
        {
            // Create text render node
            renderNode = new DOM.TextNode(text);
        }
        else
        {
            // Create non-renderable node for comments, document types, etc.
            renderNode = new NonRenderableNode(node, Array.Empty<IRenderNode>());
        }

        // Set parent relationship
        if (parent != null)
        {
            if (renderNode is ElementNode elementNode)
            {
                elementNode.Parent = parent;
            }
            else if (renderNode is DOM.TextNode textNode)
            {
                textNode.Parent = parent;
            }
            else if (renderNode is NonRenderableNode nonRenderableNode)
            {
                nonRenderableNode.Parent = parent;
            }
        }

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
            if (element.OwnerDocument?.DefaultView != null)
            {
                var computedStyle = element.OwnerDocument.DefaultView.GetComputedStyle(element, null);
                if (computedStyle != null && computedStyle.Display == "none")
                {
                    return false;
                }
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

        // For elements, compare styles and attributes
        if (oldNode is ElementNode oldElement && newNode is ElementNode newElement)
        {
            // Compare by checking key attributes
            if (oldElement.Id != newElement.Id ||
                oldElement.Ref.ClassName != newElement.Ref.ClassName)
                return true;

            // Compare computed styles
            var oldComputed = oldElement.ComputedStyle;
            var newComputed = newElement.ComputedStyle;

            if ((oldComputed == null) != (newComputed == null))
                return true; // One is null, one isn't

            if (oldComputed != null && newComputed != null)
            {
                // Check key layout-affecting properties
                string[] keyProperties = {
                    // Display and positioning
                    "display", "position", "float", "z-index",

                    // Box model
                    "width", "height", "min-width", "min-height", "max-width", "max-height",
                    "margin-top", "margin-right", "margin-bottom", "margin-left",
                    "padding-top", "padding-right", "padding-bottom", "padding-left",
                    "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
                    "box-sizing",

                    // Flexbox
                    "flex", "flex-direction", "flex-wrap", "flex-flow",
                    "justify-content", "align-items", "align-content",
                    "order", "flex-grow", "flex-shrink", "flex-basis", "align-self",

                    // Others
                    "overflow", "visibility", "top", "right", "bottom", "left"
                };

                foreach (var prop in keyProperties)
                {
                    string oldValue = oldComputed.GetPropertyValue(prop);
                    string newValue = newComputed.GetPropertyValue(prop);

                    if (oldValue != newValue)
                        return true;
                }
            }

            // Check other element attributes that could affect layout
            string[] layoutAttrs = { "width", "height", "align", "valign", "colspan", "rowspan" };

            foreach (var attr in layoutAttrs)
            {
                var oldValue = oldElement.Ref.GetAttribute(attr);
                var newValue = newElement.Ref.GetAttribute(attr);

                if (oldValue != newValue)
                    return true;
            }
        }
        // For text nodes, check content
        else if (oldNode is DOM.TextNode oldText && newNode is DOM.TextNode newText)
        {
            return oldText.Ref.TextContent != newText.Ref.TextContent;
        }

        // No changes detected
        return false;
    }
}