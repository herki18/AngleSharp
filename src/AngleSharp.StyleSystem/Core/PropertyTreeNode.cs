namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using Css.Dom;
using Css.Values;

/// <summary>
/// Represents a node in the property tree for efficient style storage.
/// </summary>
public class PropertyTreeNode
{
    private readonly PropertyTreeNode? _parent;
    private readonly Dictionary<string, ICssValue> _properties = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _computedValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PropertyTreeNode> _children = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyTreeNode"/> class.
    /// </summary>
    /// <param name="parent">The parent node.</param>
    public PropertyTreeNode(PropertyTreeNode? parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Gets the parent node of this node.
    /// </summary>
    public PropertyTreeNode? GetParent() => _parent;

    /// <summary>
    /// Sets a property value on this node.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    public void SetProperty(string name, ICssValue? value)
    {
        if (value != null)
        {
            _properties[name] = value;
            _computedValues.Remove(name); // Invalidate computed value cache
        }
        else
        {
            _properties.Remove(name);
            _computedValues.Remove(name);
        }

        // Remove any shared node for this property when setting a direct value
        _children.Remove(name);
    }

    /// <summary>
    /// Sets a property string value on this node.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property string value.</param>
    public void SetProperty(string name, string? value)
    {
        if (value != null)
        {
            SetProperty(name, new CssStringValue(value));
        }
        else
        {
            _properties.Remove(name);
            _computedValues.Remove(name);
            _children.Remove(name);
        }
    }

    /// <summary>
    /// Removes a property from this node.
    /// </summary>
    /// <param name="name">The property name to remove.</param>
    public void RemoveProperty(string name)
    {
        _properties.Remove(name);
        _computedValues.Remove(name);
        _children.Remove(name);
    }

    /// <summary>
    /// Gets the CSS text value of a property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The CSS text value or an empty string if not found.</returns>
    public string GetPropertyValue(string name)
    {
        // Check local properties first
        if (_properties.TryGetValue(name, out var value))
        {
            return value.CssText;
        }

        // Check shared nodes
        if (_children.TryGetValue(name, out var childNode))
        {
            return childNode.GetPropertyValue(name);
        }

        // Fall back to parent
        return _parent?.GetPropertyValue(name) ?? string.Empty;
    }

    /// <summary>
    /// Gets the raw CSS value of a property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The raw CSS value or null if not found.</returns>
    public ICssValue? GetPropertyRawValue(string name)
    {
        // Check local properties first
        if (_properties.TryGetValue(name, out var value))
        {
            return value;
        }

        // Check shared nodes
        if (_children.TryGetValue(name, out var childNode))
        {
            return childNode.GetPropertyRawValue(name);
        }

        // Fall back to parent
        return _parent?.GetPropertyRawValue(name);
    }

    /// <summary>
    /// Gets the cached computed value of a property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The cached computed value or null if not found.</returns>
    public object? GetPropertyCachedValue(string name)
    {
        // Check cached computed values first
        if (_computedValues.TryGetValue(name, out var cachedValue))
        {
            return cachedValue;
        }

        // Check shared nodes
        if (_children.TryGetValue(name, out var childNode))
        {
            var childValue = childNode.GetPropertyCachedValue(name);
            if (childValue != null)
            {
                // Cache the result from the shared node
                _computedValues[name] = childValue;
                return childValue;
            }
        }

        // Get the raw value and cache it
        var rawValue = GetPropertyRawValue(name);
        if (rawValue != null)
        {
            _computedValues[name] = rawValue;
            return rawValue;
        }

        return null;
    }

    /// <summary>
    /// Checks if a property is defined on this node or its ancestors.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>True if the property is defined; otherwise, false.</returns>
    public bool HasProperty(string name)
    {
        if (_properties.ContainsKey(name))
        {
            return true;
        }

        if (_children.ContainsKey(name))
        {
            return true;
        }

        return _parent?.HasProperty(name) ?? false;
    }

    /// <summary>
    /// Gets all properties defined on this node, including those from shared nodes.
    /// </summary>
    /// <returns>A dictionary of all properties.</returns>
    public Dictionary<string, ICssValue> GetAllProperties()
    {
        // Start with properties directly on this node
        var result = new Dictionary<string, ICssValue>(_properties, StringComparer.OrdinalIgnoreCase);

        // Add properties from shared nodes
        foreach (var child in _children)
        {
            var childValue = child.Value.GetPropertyRawValue(child.Key);
            if (childValue != null)
            {
                result[child.Key] = childValue;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the total count of properties on this node.
    /// </summary>
    /// <returns>The property count.</returns>
    public int GetPropertyCount()
    {
        return _properties.Count + _children.Count;
    }

    /// <summary>
    /// Replaces a property with a shared subtree node.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="sharedNode">The shared node.</param>
    public void ReplaceSubtree(string property, PropertyTreeNode sharedNode)
    {
        if (_properties.ContainsKey(property))
        {
            _children[property] = sharedNode;
            _properties.Remove(property);
            _computedValues.Remove(property);
        }
    }

    /// <summary>
    /// Checks if a property is shared with another node.
    /// </summary>
    /// <param name="other">The other node.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>True if the property is shared; otherwise, false.</returns>
    public bool IsSharedWith(PropertyTreeNode other, string propertyName)
    {
        if (!_children.TryGetValue(propertyName, out var childNode))
            return false;

        if (!other._children.TryGetValue(propertyName, out var otherChildNode))
            return false;

        return ReferenceEquals(childNode, otherChildNode);
    }
}