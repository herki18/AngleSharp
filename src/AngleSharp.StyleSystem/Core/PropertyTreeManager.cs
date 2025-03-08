namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;

/// <summary>
/// Manages property trees for efficient style storage and sharing.
/// </summary>
public class PropertyTreeManager
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

/// <summary>
/// Represents a node in the property tree that efficiently stores computed style values.
/// </summary>
public class PropertyTreeNode
{
    private readonly PropertyTreeNode? _parent;
    private readonly Dictionary<string, ICssValue> _properties = new Dictionary<string, ICssValue>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _computedValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PropertyTreeNode> _children = new Dictionary<string, PropertyTreeNode>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new property tree node.
    /// </summary>
    /// <param name="parent">The parent node, or null for a root node.</param>
    public PropertyTreeNode(PropertyTreeNode? parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Sets a property value in this node.
    /// </summary>
    public void SetProperty(string name, ICssValue? value)
    {
        if (value != null)
        {
            _properties[name] = value;
            _computedValues.Remove(name); // Clear cached computed value
        }
        else
        {
            _properties.Remove(name);
            _computedValues.Remove(name); // Clear cached computed value
        }
    }

    /// <summary>
    /// Sets a property value in this node.
    /// </summary>
    public void SetProperty(string name, string value)
    {
        if (value != null)
        {
            // In a real implementation, we would parse the value and create a proper ICssValue
            SetProperty(name, new CssStringValue(value));
        }
        else
        {
            _properties.Remove(name);
            _computedValues.Remove(name);
        }
    }

    /// <summary>
    /// Gets a property value, looking up the tree if necessary.
    /// </summary>
    public string GetPropertyValue(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return value.CssText;
        }

        return _parent?.GetPropertyValue(name) ?? string.Empty;
    }

    /// <summary>
    /// Gets a property value as an ICssValue, looking up the tree if necessary.
    /// </summary>
    public ICssValue? GetPropertyRawValue(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return value;
        }

        return _parent?.GetPropertyRawValue(name);
    }

    /// <summary>
    /// Gets a computed cached value for a property, or computes and caches it if needed.
    /// </summary>
    public object? GetPropertyCachedValue(string name)
    {
        if (_computedValues.TryGetValue(name, out var cachedValue))
        {
            return cachedValue;
        }

        var rawValue = GetPropertyRawValue(name);
        if (rawValue != null)
        {
            // In a real implementation, we would do more complex value conversion
            var computed = rawValue;
            _computedValues[name] = computed;
            return computed;
        }

        return null;
    }

    /// <summary>
    /// Checks if this node or any parent has a property.
    /// </summary>
    public bool HasProperty(string name)
    {
        if (_properties.ContainsKey(name))
        {
            return true;
        }

        return _parent?.HasProperty(name) ?? false;
    }

    /// <summary>
    /// Gets all properties in this node (not including inherited ones).
    /// </summary>
    public Dictionary<string, ICssValue> GetAllProperties()
    {
        return new Dictionary<string, ICssValue>(_properties, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Replaces a property subtree with a shared node.
    /// </summary>
    public void ReplaceSubtree(string property, PropertyTreeNode sharedNode)
    {
        if (_properties.ContainsKey(property))
        {
            _children[property] = sharedNode;
            _properties.Remove(property);
            _computedValues.Remove(property);
        }
    }
}

/// <summary>
/// A simple ICssValue implementation for string values.
/// </summary>
internal class CssStringValue : ICssValue
{
    private readonly string _value;

    public CssStringValue(string value)
    {
        _value = value;
    }

    public string CssText => _value;

    public ICssValue Compute(ICssComputeContext context)
    {
        return this;
    }

    public bool Equals(ICssValue other)
    {
        return other is CssStringValue otherValue && _value == otherValue._value;
    }
}