namespace LayoutEngine.Core.LayoutNG.Public;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Internal;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manages the layout object tree and its relationship with the DOM tree.
/// </summary>
public class LayoutObjectTree
{
    private readonly LayoutTreeBuilder _treeBuilder;
    private readonly ILogger<LayoutObjectTree> _logger;
    private readonly Dictionary<INode, ILayoutObject> _nodeToLayoutObject = new();
    private ILayoutObject? _rootLayoutObject;

    public LayoutObjectTree(LayoutTreeBuilder treeBuilder, ILogger<LayoutObjectTree> logger)
    {
        _treeBuilder = treeBuilder ?? throw new ArgumentNullException(nameof(treeBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the root layout object.
    /// </summary>
    public ILayoutObject? RootLayoutObject => _rootLayoutObject;

    /// <summary>
    /// Builds the initial layout tree from a document.
    /// </summary>
    public void BuildFromDocument(IDocument document)
    {
        _logger.LogDebug("Building layout tree from document");

        Clear();

        if (document.DocumentElement != null)
        {
            _rootLayoutObject = _treeBuilder.BuildLayoutTree(document.DocumentElement);
            if (_rootLayoutObject != null)
            {
                RegisterLayoutObjectRecursive(_rootLayoutObject);
            }
        }

        _logger.LogDebug("Layout tree built with {Count} objects", _nodeToLayoutObject.Count);
    }

    /// <summary>
    /// Gets the layout object for a DOM node.
    /// </summary>
    public ILayoutObject? GetLayoutObject(INode node)
    {
        _nodeToLayoutObject.TryGetValue(node, out var layoutObject);
        return layoutObject;
    }

    /// <summary>
    /// Handles DOM node insertion.
    /// </summary>
    public void HandleNodeInserted(INode node, INode parent)
    {
        _logger.LogDebug("Handling node insertion: {NodeType} into {ParentType}",
            node.NodeType, parent.NodeType);

        // Find parent layout object
        var parentLayoutObject = GetLayoutObject(parent);
        if (parentLayoutObject == null || !(parentLayoutObject is ILayoutContainer parentContainer))
        {
            _logger.LogDebug("Parent has no layout object or is not a container");
            return;
        }

        // Build layout object for inserted node
        ILayoutObject? newLayoutObject = null;

        if (node is IElement element)
        {
            newLayoutObject = _treeBuilder.BuildLayoutTree(element);
        }
        else if (node is IText textNode)
        {
            newLayoutObject = new LayoutText(textNode);
            newLayoutObject.Style = parentLayoutObject.Style; // Inherit parent style
        }

        if (newLayoutObject != null)
        {
            // Find insertion point based on DOM order
            var beforeLayoutObject = FindInsertionPoint(node, parent, parentContainer);

            // Insert into layout tree
            parentContainer.InsertChild(newLayoutObject, beforeLayoutObject);

            // Register in mapping
            RegisterLayoutObjectRecursive(newLayoutObject);

            // Update anonymous wrappers
            parentContainer.CreateAnonymousWrappersIfNeeded();
        }
    }

    /// <summary>
    /// Handles DOM node removal.
    /// </summary>
    public void HandleNodeRemoved(INode node)
    {
        _logger.LogDebug("Handling node removal: {NodeType}", node.NodeType);

        var layoutObject = GetLayoutObject(node);
        if (layoutObject == null)
        {
            _logger.LogDebug("Node has no layout object");
            return;
        }

        // Remove from parent
        layoutObject.Remove();

        // Unregister from mapping
        UnregisterLayoutObjectRecursive(layoutObject);

        // Clean up anonymous wrappers if needed
        if (layoutObject.Parent is ILayoutContainer parentContainer)
        {
            parentContainer.RemoveUnnecessaryAnonymousWrappers();
        }

        // Destroy the layout object
        layoutObject.Destroy();
    }

    /// <summary>
    /// Handles DOM attribute changes.
    /// </summary>
    public void HandleAttributeChanged(IElement element, string attributeName)
    {
        _logger.LogDebug("Handling attribute change: {Attribute} on {TagName}",
            attributeName, element.TagName);

        var layoutObject = GetLayoutObject(element);
        if (layoutObject == null)
        {
            _logger.LogDebug("Element has no layout object");
            return;
        }

        // Style-related attributes require style recalculation
        if (IsStyleAttribute(attributeName))
        {
            layoutObject.SetNeedsStyleRecalc();
        }
        // Layout-affecting attributes
        else if (IsLayoutAttribute(attributeName))
        {
            layoutObject.SetNeedsLayout();
        }
        // Paint-only attributes
        else
        {
            layoutObject.SetNeedsPaintInvalidation();
        }
    }

    /// <summary>
    /// Handles text content changes.
    /// </summary>
    public void HandleTextChanged(IText textNode)
    {
        _logger.LogDebug("Handling text change");

        var layoutObject = GetLayoutObject(textNode);
        if (layoutObject is LayoutText layoutText)
        {
            layoutText.Text = textNode.TextContent ?? string.Empty;
            layoutText.SetNeedsLayout();
        }
    }

    /// <summary>
    /// Clears the entire layout tree.
    /// </summary>
    public void Clear()
    {
        if (_rootLayoutObject != null)
        {
            UnregisterLayoutObjectRecursive(_rootLayoutObject);
            _rootLayoutObject.Destroy();
            _rootLayoutObject = null;
        }

        _nodeToLayoutObject.Clear();
    }

    /// <summary>
    /// Traverses the layout tree in pre-order.
    /// </summary>
    public void TraversePreOrder(Action<ILayoutObject> action)
    {
        if (_rootLayoutObject != null)
        {
            TraversePreOrderRecursive(_rootLayoutObject, action);
        }
    }

    /// <summary>
    /// Traverses the layout tree in post-order.
    /// </summary>
    public void TraversePostOrder(Action<ILayoutObject> action)
    {
        if (_rootLayoutObject != null)
        {
            TraversePostOrderRecursive(_rootLayoutObject, action);
        }
    }

    // Private helper methods

    private void RegisterLayoutObjectRecursive(ILayoutObject layoutObject)
    {
        if (layoutObject.Node != null)
        {
            _nodeToLayoutObject[layoutObject.Node] = layoutObject;
        }

        if (layoutObject is ILayoutContainer container)
        {
            foreach (var child in container.Children)
            {
                RegisterLayoutObjectRecursive(child);
            }
        }
    }

    private void UnregisterLayoutObjectRecursive(ILayoutObject layoutObject)
    {
        if (layoutObject.Node != null)
        {
            _nodeToLayoutObject.Remove(layoutObject.Node);
        }

        if (layoutObject is ILayoutContainer container)
        {
            foreach (var child in container.Children)
            {
                UnregisterLayoutObjectRecursive(child);
            }
        }
    }

    private ILayoutObject? FindInsertionPoint(INode node, INode parent, ILayoutContainer parentContainer)
    {
        // Find the next sibling in DOM order that has a layout object
        var nextSibling = node.NextSibling;

        while (nextSibling != null)
        {
            var nextLayoutObject = GetLayoutObject(nextSibling);
            if (nextLayoutObject != null && nextLayoutObject.Parent == parentContainer)
            {
                return nextLayoutObject;
            }
            nextSibling = nextSibling.NextSibling;
        }

        return null; // Insert at end
    }

    private bool IsStyleAttribute(string attributeName)
    {
        return attributeName == "style" ||
               attributeName == "class" ||
               attributeName == "id";
    }

    private bool IsLayoutAttribute(string attributeName)
    {
        return attributeName == "width" ||
               attributeName == "height" ||
               attributeName == "hidden";
    }

    private void TraversePreOrderRecursive(ILayoutObject layoutObject, Action<ILayoutObject> action)
    {
        action(layoutObject);

        if (layoutObject is ILayoutContainer container)
        {
            foreach (var child in container.Children)
            {
                TraversePreOrderRecursive(child, action);
            }
        }
    }

    private void TraversePostOrderRecursive(ILayoutObject layoutObject, Action<ILayoutObject> action)
    {
        if (layoutObject is ILayoutContainer container)
        {
            foreach (var child in container.Children)
            {
                TraversePostOrderRecursive(child, action);
            }
        }

        action(layoutObject);
    }
}