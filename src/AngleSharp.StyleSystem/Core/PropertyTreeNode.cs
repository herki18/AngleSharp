namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using Css.Dom;
using Css.Values;

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
    public void SetProperty(string name, string? value)
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