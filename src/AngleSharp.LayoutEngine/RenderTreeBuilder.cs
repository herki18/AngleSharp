#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8600, CS8602, CS8603, CS8625
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Css.Dom;
using AngleSharp.Html.Dom;

/// <summary>
/// Builds a render tree from an AngleSharp DOM tree. The render tree contains only
/// elements that need to be laid out and rendered, with computed styles.
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
            // Get styles
            var specifiedStyle = GetSpecifiedStyle(element);
            var computedStyle = GetComputedStyle(element);

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

            // Create element render node
            renderNode = new ElementNode(element, children, specifiedStyle, computedStyle);
        }
        else if (node is IText text && !string.IsNullOrWhiteSpace(text.Data))
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

        // Create child render nodes and update their parent references
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
            // Check display:none
            var style = element.GetComputedStyle();
            if (style != null && style.GetPropertyValue("display") == "none")
            {
                return false;
            }

            // Always render elements with renderable content
            return true;
        }

        if (node is IText text)
        {
            // Only render non-empty text nodes
            return !string.IsNullOrWhiteSpace(text.Data);
        }

        // Don't render comments, document types, etc.
        return false;
    }

    /// <summary>
    /// Gets the specified style for an element.
    /// </summary>
    /// <param name="element">The element to get the style for.</param>
    /// <returns>The specified style declaration.</returns>
    private ICssStyleDeclaration GetSpecifiedStyle(IElement element)
    {
        // Get inline style
        var styleAttr = element.GetAttribute("style");
        if (!string.IsNullOrEmpty(styleAttr))
        {
            // In a real implementation, this would parse the style attribute
            // and create a style declaration. Here we'll use a simple implementation.
            var style = new SimpleStyleDeclaration();
            ParseInlineStyle(styleAttr, style);
            return style;
        }

        // Return empty style if no inline style
        return new SimpleStyleDeclaration();
    }

    /// <summary>
    /// Gets the computed style for an element.
    /// </summary>
    /// <param name="element">The element to get the style for.</param>
    /// <returns>The computed style declaration.</returns>
    private ICssStyleDeclaration GetComputedStyle(IElement element)
    {
        // In a real implementation, this would use the cascade to compute
        // the final styles. Here we'll just use the element's computed style
        // if available from AngleSharp.
        var computedStyle = element.GetComputedStyle();
        if (computedStyle != null)
        {
            return computedStyle;
        }

        // If not available, use a simplified approach
        return new SimpleStyleDeclaration();
    }

    /// <summary>
    /// Parses an inline style string into a style declaration.
    /// </summary>
    /// <param name="styleText">The style text to parse.</param>
    /// <param name="style">The style declaration to populate.</param>
    private void ParseInlineStyle(string styleText, SimpleStyleDeclaration style)
    {
        if (string.IsNullOrEmpty(styleText))
            return;

        // Simple parser: split by semicolons, then by colons
        var declarations = styleText.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var declaration in declarations)
        {
            var parts = declaration.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var property = parts[0].Trim();
                var value = parts[1].Trim();
                style.SetProperty(property, value);
            }
        }
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

        // For elements, check styles and attributes
        if (oldNode is ElementNode oldElement && newNode is ElementNode newElement)
        {
            // Check if styles have changed
            if (HaveStylesChanged(oldElement.SpecifiedStyle, newElement.SpecifiedStyle))
                return true;

            // Check if attributes have changed
            if (HaveAttributesChanged(oldElement.Ref, newElement.Ref))
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
    /// Determines if styles have changed between two style declarations.
    /// </summary>
    /// <param name="oldStyle">The old style declaration.</param>
    /// <param name="newStyle">The new style declaration.</param>
    /// <returns>True if styles have changed, false otherwise.</returns>
    private bool HaveStylesChanged(ICssStyleDeclaration oldStyle, ICssStyleDeclaration newStyle)
    {
        if (oldStyle == null && newStyle == null)
            return false;

        if (oldStyle == null || newStyle == null)
            return true;

        // For simplicity, we'll consider styles changed if the number of properties changed
        if (oldStyle.Length != newStyle.Length)
            return true;

        // Check if any property values have changed
        foreach (var property in oldStyle)
        {
            var oldValue = oldStyle.GetPropertyValue(property.Name);
            var newValue = newStyle.GetPropertyValue(property.Name);

            if (oldValue != newValue)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if attributes have changed between two elements.
    /// </summary>
    /// <param name="oldElement">The old element.</param>
    /// <param name="newElement">The new element.</param>
    /// <returns>True if attributes have changed, false otherwise.</returns>
    private bool HaveAttributesChanged(IElement oldElement, IElement newElement)
    {
        if (oldElement == null || newElement == null)
            return true;

        // Check if the number of attributes has changed
        if (oldElement.Attributes.Length != newElement.Attributes.Length)
            return true;

        // Check if any attribute values have changed
        foreach (var attr in oldElement.Attributes)
        {
            var newValue = newElement.GetAttribute(attr.Name);
            if (attr.Value != newValue)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Represents a render tree built from a DOM tree.
/// </summary>
public class RenderTree
{
    private readonly Dictionary<INode, IRenderNode> _nodeMap = new Dictionary<INode, IRenderNode>();

    /// <summary>
    /// Creates a new render tree with the specified root node.
    /// </summary>
    /// <param name="root">The root node of the render tree.</param>
    public RenderTree(IRenderNode root)
    {
        Root = root;

        // Build node map for quick lookups
        if (root != null)
        {
            BuildNodeMap(root);
        }
    }

    /// <summary>
    /// Gets the root node of the render tree.
    /// </summary>
    public IRenderNode Root { get; }

    /// <summary>
    /// Finds a render node by its corresponding DOM node.
    /// </summary>
    /// <param name="node">The DOM node to find the render node for.</param>
    /// <returns>The corresponding render node, or null if not found.</returns>
    public IRenderNode FindNodeByDomNode(INode node)
    {
        if (_nodeMap.TryGetValue(node, out var renderNode))
        {
            return renderNode;
        }
        return null;
    }

    /// <summary>
    /// Gets all nodes in the render tree.
    /// </summary>
    /// <returns>A collection of all render nodes.</returns>
    public IEnumerable<IRenderNode> GetAllNodes()
    {
        if (Root == null)
            return Enumerable.Empty<IRenderNode>();

        return TraverseNodes(Root);
    }

    /// <summary>
    /// Gets the nodes that have changed since the last render tree build.
    /// </summary>
    /// <returns>A collection of changed render nodes.</returns>
    public IEnumerable<IRenderNode> GetChangedNodes()
    {
        // This is handled by the RenderTreeBuilder
        return Enumerable.Empty<IRenderNode>();
    }

    /// <summary>
    /// Gets the nodes that have been added since the last render tree build.
    /// </summary>
    /// <returns>A collection of added render nodes.</returns>
    public IEnumerable<IRenderNode> GetAddedNodes()
    {
        // This is handled by the RenderTreeBuilder
        return Enumerable.Empty<IRenderNode>();
    }

    /// <summary>
    /// Gets the nodes that have been removed since the last render tree build.
    /// </summary>
    /// <returns>A collection of removed render nodes.</returns>
    public IEnumerable<IRenderNode> GetRemovedNodes()
    {
        // This is handled by the RenderTreeBuilder
        return Enumerable.Empty<IRenderNode>();
    }

    /// <summary>
    /// Builds a map of DOM nodes to render nodes for quick lookup.
    /// </summary>
    /// <param name="node">The root node to start building from.</param>
    private void BuildNodeMap(IRenderNode node)
    {
        _nodeMap[node.Ref] = node;

        foreach (var child in node.Children)
        {
            BuildNodeMap(child);
        }
    }

    /// <summary>
    /// Traverses all nodes in the render tree.
    /// </summary>
    /// <param name="node">The node to start traversing from.</param>
    /// <returns>A collection of all render nodes.</returns>
    private IEnumerable<IRenderNode> TraverseNodes(IRenderNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in TraverseNodes(child))
            {
                yield return descendant;
            }
        }
    }
}

/// <summary>
/// A simple implementation of ICssStyleDeclaration.
/// </summary>
public class SimpleStyleDeclaration : ICssStyleDeclaration
{
    private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _priorities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sets a style property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="priority">The property priority.</param>
    public void SetProperty(string name, string value, string priority = "")
    {
        _properties[name] = value;
        _priorities[name] = priority;
    }

    /// <summary>
    /// Gets a style property value.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The property value, or null if not set.</returns>
    public string GetPropertyValue(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return value;
        }
        return null;
    }

    /// <summary>
    /// Gets a style property priority.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The property priority, or empty string if not set.</returns>
    public string GetPropertyPriority(string name)
    {
        if (_priorities.TryGetValue(name, out var priority))
        {
            return priority;
        }
        return "";
    }

    /// <summary>
    /// Removes a style property.
    /// </summary>
    /// <param name="name">The property name to remove.</param>
    public void RemoveProperty(string name)
    {
        _properties.Remove(name);
        _priorities.Remove(name);
    }

    /// <summary>
    /// Gets a style property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The property, or null if not set.</returns>
    public ICssProperty GetProperty(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return new SimpleCssProperty
            {
                Name = name,
                Value = value,
                Priority = _priorities.GetValueOrDefault(name, "")
            };
        }
        return null;
    }

    /// <summary>
    /// Gets all properties.
    /// </summary>
    /// <returns>A collection of all properties.</returns>
    public IEnumerable<ICssProperty> GetAllProperties()
    {
        return _properties.Select(p => new SimpleCssProperty
        {
            Name = p.Key,
            Value = p.Value,
            Priority = _priorities.GetValueOrDefault(p.Key, "")
        });
    }

    /// <summary>
    /// Gets an enumerator over all properties.
    /// </summary>
    /// <returns>An enumerator over all properties.</returns>
    public IEnumerator<ICssProperty> GetEnumerator()
    {
        return GetAllProperties().GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator over all properties.
    /// </summary>
    /// <returns>An enumerator over all properties.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Gets the number of properties.
    /// </summary>
    public int Length => _properties.Count;

    /// <summary>
    /// Gets a property by index.
    /// </summary>
    /// <param name="index">The index of the property.</param>
    /// <returns>The property at the specified index.</returns>
    public ICssProperty this[int index] => GetAllProperties().ElementAtOrDefault(index);

    /// <summary>
    /// Creates a clone of this style declaration.
    /// </summary>
    /// <returns>A clone of this style declaration.</returns>
    public ICssStyleDeclaration Clone()
    {
        var clone = new SimpleStyleDeclaration();
        foreach (var property in _properties)
        {
            clone.SetProperty(property.Key, property.Value, _priorities.GetValueOrDefault(property.Key, ""));
        }
        return clone;
    }
}

/// <summary>
/// A simple implementation of ICssProperty.
/// </summary>
public class SimpleCssProperty : ICssProperty
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the property value.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Gets or sets the property priority.
    /// </summary>
    public string Priority { get; set; }

    /// <summary>
    /// Gets the raw property value.
    /// </summary>
    public object RawValue => Value;
}