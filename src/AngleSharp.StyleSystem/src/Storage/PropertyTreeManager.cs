namespace AngleSharp.StyleSystem.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Interfaces;
public class PropertyTreeManager : IPropertyTreeManager
{
    private readonly Dictionary<string, PropertyTreeNode> _rootNodes = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<IElement, PropertyTreeNode> _elementToPropertyTree = new();
    private readonly Dictionary<string, HashSet<string>> _propertyGroups = new(StringComparer.OrdinalIgnoreCase);
    private long _totalPropertiesBeforeOptimization;
    private long _totalPropertiesAfterOptimization;
    private int _sharedNodeCount;
    private int _uniqueNodeCount;
    private readonly Dictionary<string, int> _propertyUsageCount = new(StringComparer.OrdinalIgnoreCase);
    public PropertyTreeManager()
    {
        InitializePropertyGroups();
    }
    private void InitializePropertyGroups()
    {
        _propertyGroups["margin"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "margin-top", "margin-right", "margin-bottom", "margin-left" };
        _propertyGroups["padding"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "padding-top", "padding-right", "padding-bottom", "padding-left" };
        _propertyGroups["border-width"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "border-top-width", "border-right-width", "border-bottom-width", "border-left-width" };
        _propertyGroups["border-color"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "border-top-color", "border-right-color", "border-bottom-color", "border-left-color" };
        _propertyGroups["font"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "font-family", "font-size", "font-weight", "font-style", "line-height" };
        _propertyGroups["text"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "color", "text-align", "text-decoration", "letter-spacing" };
    }
    public PropertyTreeNode CreateNode(IElement element, PropertyTreeNode? parent = null)
    {
        // First try to remove any existing entry for this element
        // This prevents the "An item with the same key has already been added" exception
        _elementToPropertyTree.Remove(element);

        var node = new PropertyTreeNode(parent);
        _elementToPropertyTree.Add(element, node);
        _uniqueNodeCount++;
        return node;
    }
    public PropertyTreeNode GetOrCreateNode(IElement element, PropertyTreeNode? parent = null)
    {
        if (_elementToPropertyTree.TryGetValue(element, out var node))
        {
            return node;
        }
        return CreateNode(element, parent);
    }
    public PropertyTreeNode GetSharedNode(string propertyName, ICssValue value)
    {
        var key = $"{propertyName}:{value?.CssText ?? "null"}";
        if (!_rootNodes.TryGetValue(key, out var node))
        {
            node = new PropertyTreeNode(null);
            node.SetProperty(propertyName, value);
            _rootNodes[key] = node;
            _sharedNodeCount++;
        }
        // Track property usage for optimization analysis
        if (!_propertyUsageCount.TryGetValue(propertyName, out _))
        {
            _propertyUsageCount[propertyName] = 0;
        }
        _propertyUsageCount[propertyName]++;
        return node;
    }
    public void OptimizeTree(PropertyTreeNode node)
    {
        // Get all properties before optimization for metrics
        var properties = node.GetAllProperties();
        _totalPropertiesBeforeOptimization += properties.Count;
        // Step 1: Basic property value sharing
        foreach (var property in properties)
        {
            var sharedNode = GetSharedNode(property.Key, property.Value);
            node.ReplaceSubtree(property.Key, sharedNode);
        }
        // Step 2: Property group optimization
        OptimizePropertyGroups(node, properties);
        // Step 3: Parent-child relationship optimization
        OptimizeWithParent(node);
        // Track metrics after optimization
        _totalPropertiesAfterOptimization += node.GetPropertyCount();
    }
    /// <summary>
    /// Optimizes groups of related properties by creating shared nodes for common patterns.
    /// </summary>
    private void OptimizePropertyGroups(PropertyTreeNode node, Dictionary<string, ICssValue> properties)
    {
        foreach (var group in _propertyGroups)
        {
            // Find properties from this group that are present in the node
            var presentProps = properties.Keys
                .Where(p => group.Value.Contains(p))
                .ToList();
            // Only optimize if multiple properties from the group are present
            if (presentProps.Count <= 1)
                continue;
            // Get values for all present properties in this group
            var groupValues = new Dictionary<string, ICssValue>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in presentProps)
            {
                if (properties.TryGetValue(prop, out var value))
                {
                    groupValues[prop] = value;
                }
            }
            // Create a unique key for this specific combination of properties and values
            var groupKey = $"group:{group.Key}:" + string.Join(";",
                groupValues.OrderBy(kv => kv.Key)
                          .Select(kv => $"{kv.Key}:{kv.Value?.CssText ?? "null"}"));
            if (!_rootNodes.TryGetValue(groupKey, out var groupNode))
            {
                groupNode = new PropertyTreeNode(null);
                foreach (var kv in groupValues)
                {
                    groupNode.SetProperty(kv.Key, kv.Value);
                }
                _rootNodes[groupKey] = groupNode;
                _sharedNodeCount++;
            }
            foreach (var prop in presentProps)
            {
                node.ReplaceSubtree(prop, groupNode);
            }
        }
    }
    private void OptimizeWithParent(PropertyTreeNode node)
    {
        var parent = node.GetParent();
        if (parent == null)
            return;
        var nodeProperties = node.GetAllProperties();
        foreach (var prop in nodeProperties)
        {
            var parentValue = parent.GetPropertyRawValue(prop.Key);
            if (parentValue != null && AreValuesEqual(prop.Value, parentValue))
            {
                node.RemoveProperty(prop.Key);
            }
        }
    }
    private bool AreValuesEqual(ICssValue value1, ICssValue value2)
    {
        if (ReferenceEquals(value1, value2))
            return true;
        if (value1 == null || value2 == null)
            return false;
        return string.Equals(value1.CssText, value2.CssText, StringComparison.Ordinal);
    }
    public OptimizationMetrics GetOptimizationMetrics()
    {
        return new OptimizationMetrics
        {
            TotalPropertiesBeforeOptimization = _totalPropertiesBeforeOptimization,
            TotalPropertiesAfterOptimization = _totalPropertiesAfterOptimization,
            SharedNodeCount = _sharedNodeCount,
            UniqueNodeCount = _uniqueNodeCount,
            MemorySavingsPercentage = _totalPropertiesBeforeOptimization == 0 ? 0 :
                100 - ((_totalPropertiesAfterOptimization * 100) / _totalPropertiesBeforeOptimization),
            MostFrequentProperties = _propertyUsageCount
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };
    }
    public void ResetOptimizationMetrics()
    {
        _totalPropertiesBeforeOptimization = 0;
        _totalPropertiesAfterOptimization = 0;
        _sharedNodeCount = 0;
        _uniqueNodeCount = 0;
        _propertyUsageCount.Clear();
    }
}