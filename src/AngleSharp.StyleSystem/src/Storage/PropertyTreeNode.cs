namespace AngleSharp.StyleSystem.Storage;
using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
public class PropertyTreeNode
{
    private readonly PropertyTreeNode? _parent;
    private readonly Dictionary<string, ICssValue> _properties = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _computedValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PropertyTreeNode> _children = new(StringComparer.OrdinalIgnoreCase);
    public PropertyTreeNode(PropertyTreeNode? parent)
    {
        _parent = parent;
    }
    public PropertyTreeNode? GetParent() => _parent;
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
    public void RemoveProperty(string name)
    {
        _properties.Remove(name);
        _computedValues.Remove(name);
        _children.Remove(name);
    }

    /// <summary>
    /// Gets the property value considering inheritance from parent nodes.
    /// </summary>
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

    /// <summary>
    /// Gets the property value from this node only, without checking parent nodes.
    /// </summary>
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
    public int GetPropertyCount()
    {
        return _properties.Count + _children.Count;
    }
    public void ReplaceSubtree(string property, PropertyTreeNode sharedNode)
    {
        if (_properties.ContainsKey(property))
        {
            _children[property] = sharedNode;
            _properties.Remove(property);
            _computedValues.Remove(property);
        }
    }
    public bool IsSharedWith(PropertyTreeNode other, string propertyName)
    {
        if (!_children.TryGetValue(propertyName, out var childNode))
            return false;
        if (!other._children.TryGetValue(propertyName, out var otherChildNode))
            return false;
        return ReferenceEquals(childNode, otherChildNode);
    }
}