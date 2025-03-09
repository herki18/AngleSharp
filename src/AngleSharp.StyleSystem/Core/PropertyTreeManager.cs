namespace AngleSharp.StyleSystem.Core;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using Interfaces;

/// <summary>
/// Manages property trees for efficient style storage and sharing.
/// </summary>
public class PropertyTreeManager : IPropertyTreeManager
{
    private readonly Dictionary<string, PropertyTreeNode> _rootNodes = new Dictionary<string, PropertyTreeNode>();
    private readonly ConditionalWeakTable<IElement, PropertyTreeNode> _elementToPropertyTree = new ConditionalWeakTable<IElement, PropertyTreeNode>();

    /// <summary>
    /// Creates a new property tree node for the given element.
    /// </summary>
    public PropertyTreeNode CreateNode(IElement element, PropertyTreeNode? parent = null)
    {
        var node = new PropertyTreeNode(parent);
        _elementToPropertyTree.Add(element, node);
        return node;
    }

    /// <summary>
    /// Gets the property tree node for an element, creating one if it doesn't exist.
    /// </summary>
    public PropertyTreeNode GetOrCreateNode(IElement element, PropertyTreeNode? parent = null)
    {
        if (_elementToPropertyTree.TryGetValue(element, out var node))
        {
            return node;
        }

        return CreateNode(element, parent);
    }

    /// <summary>
    /// Creates a shared node for a specific property.
    /// </summary>
    public PropertyTreeNode GetSharedNode(string propertyName, ICssValue value)
    {
        var key = $"{propertyName}:{value?.CssText ?? "null"}";

        if (!_rootNodes.TryGetValue(key, out var node))
        {
            node = new PropertyTreeNode(null);
            node.SetProperty(propertyName, value);
            _rootNodes[key] = node;
        }

        return node;
    }

    /// <summary>
    /// Optimizes a property tree by sharing common subtrees.
    /// </summary>
    public void OptimizeTree(PropertyTreeNode node)
    {
        // Find subtrees with identical values and replace with shared nodes
        var properties = node.GetAllProperties();

        foreach (var property in properties)
        {
            var sharedNode = GetSharedNode(property.Key, property.Value);
            node.ReplaceSubtree(property.Key, sharedNode);
        }
    }
}