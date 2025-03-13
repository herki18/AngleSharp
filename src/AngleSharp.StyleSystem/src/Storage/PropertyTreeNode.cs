namespace AngleSharp.StyleSystem.Storage;
using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.StyleSystem.Interfaces;

/// <summary>
/// Represents a node in the property tree that efficiently stores CSS property values.
/// </summary>
public class PropertyTreeNode : IPropertyTreeNode
{
    private readonly IPropertyTreeNode? _parent;
    private readonly Dictionary<string, ICssValue> _properties = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _computedValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IPropertyTreeNode> _children = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyTreeNode"/> class.
    /// </summary>
    /// <param name="parent">The parent node.</param>
    public PropertyTreeNode(IPropertyTreeNode? parent)
    {
        _parent = parent;
    }

    /// <inheritdoc />
    public IPropertyTreeNode? GetParent() => _parent;

    /// <inheritdoc />
    public void SetProperty(string name, ICssValue? value)
    {
        if (value != null)
        {
            _properties[name] = value;
            _computedValues.Remove(name);
        }
        else
        {
            _properties.Remove(name);
            _computedValues.Remove(name);
        }
        _children.Remove(name);
    }

    /// <inheritdoc />
    public void SetProperty(string name, string? value)
    {
        if (value != null)
        {
            // For color values and other CSS identifiers, we should use CssIdentifierValue
            // instead of CssStringValue to avoid the quotes
            if (IsLikelyCssIdentifier(name))
            {
                SetProperty(name, new CssIdentifierValue(value));
            }
            else
            {
                SetProperty(name, new CssStringValue(value));
            }
        }
        else
        {
            _properties.Remove(name);
            _computedValues.Remove(name);
            _children.Remove(name);
        }
    }

    private bool IsLikelyCssIdentifier(string propertyName)
    {
        // Common CSS properties that typically use identifiers rather than strings
        return propertyName.Equals("color", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("background-color", StringComparison.OrdinalIgnoreCase) ||
               propertyName.EndsWith("-color", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("display", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("position", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("font-family", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void RemoveProperty(string name)
    {
        _properties.Remove(name);
        _computedValues.Remove(name);
        _children.Remove(name);
    }

    /// <inheritdoc />
    public string GetPropertyValue(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return value.CssText;
        }
        if (_children.TryGetValue(name, out var childNode))
        {
            return childNode.GetPropertyValue(name);
        }
        return _parent?.GetPropertyValue(name) ?? string.Empty;
    }

    /// <inheritdoc />
    public string GetSelfPropertyValue(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return value.CssText;
        }
        if (_children.TryGetValue(name, out var childNode))
        {
            return childNode.GetPropertyValue(name);
        }
        return string.Empty;
    }

    /// <inheritdoc />
    public ICssValue? GetPropertyRawValue(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return value;
        }
        if (_children.TryGetValue(name, out var childNode))
        {
            return childNode.GetPropertyRawValue(name);
        }
        return _parent?.GetPropertyRawValue(name);
    }

    /// <inheritdoc />
    public object? GetPropertyCachedValue(string name)
    {
        if (_computedValues.TryGetValue(name, out var cachedValue))
        {
            return cachedValue;
        }
        if (_children.TryGetValue(name, out var childNode))
        {
            var childValue = childNode.GetPropertyCachedValue(name);
            if (childValue != null)
            {
                _computedValues[name] = childValue;
                return childValue;
            }
        }
        var rawValue = GetPropertyRawValue(name);
        if (rawValue != null)
        {
            _computedValues[name] = rawValue;
            return rawValue;
        }
        return null;
    }

    /// <inheritdoc />
    public bool HasProperty(string name)
    {
        // Only check the current node and its children, not parent
        if (_properties.ContainsKey(name))
        {
            return true;
        }
        if (_children.ContainsKey(name))
        {
            return true;
        }
        return false; // Don't check parent - that's handled by inheritance
    }

    /// <inheritdoc />
    public Dictionary<string, ICssValue> GetAllProperties()
    {
        var result = new Dictionary<string, ICssValue>(_properties, StringComparer.OrdinalIgnoreCase);
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

    /// <inheritdoc />
    public int GetPropertyCount()
    {
        return _properties.Count + _children.Count;
    }

    /// <inheritdoc />
    public void ReplaceSubtree(string property, IPropertyTreeNode sharedNode)
    {
        if (_properties.ContainsKey(property))
        {
            if (sharedNode is PropertyTreeNode concreteNode)
            {
                _children[property] = concreteNode;
                _properties.Remove(property);
                _computedValues.Remove(property);
            }
        }
    }

    /// <inheritdoc />
    public bool IsSharedWith(IPropertyTreeNode other, string propertyName)
    {
        if (!_children.TryGetValue(propertyName, out var childNode))
            return false;

        if (other is PropertyTreeNode otherNode &&
            !otherNode._children.TryGetValue(propertyName, out var otherChildNode))
            return false;

        return ReferenceEquals(childNode, other.GetPropertyCachedValue(propertyName));
    }
}