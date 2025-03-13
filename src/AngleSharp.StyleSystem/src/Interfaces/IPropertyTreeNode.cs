namespace AngleSharp.StyleSystem.Interfaces;
using System.Collections.Generic;
using AngleSharp.Css.Dom;

/// <summary>
/// Defines an interface for a node in the property tree that efficiently stores CSS property values.
/// </summary>
public interface IPropertyTreeNode
{
    /// <summary>
    /// Gets the parent node in the property tree.
    /// </summary>
    /// <returns>The parent node, or null if this is a root node.</returns>
    IPropertyTreeNode? GetParent();

    /// <summary>
    /// Sets a property value on this node.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    void SetProperty(string name, ICssValue? value);

    /// <summary>
    /// Sets a property value using a string value.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value as a string.</param>
    void SetProperty(string name, string? value);

    /// <summary>
    /// Removes a property from this node.
    /// </summary>
    /// <param name="name">The property name to remove.</param>
    void RemoveProperty(string name);

    /// <summary>
    /// Gets the property value considering inheritance from parent nodes.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The property value as a string, or empty string if not found.</returns>
    string GetPropertyValue(string name);

    /// <summary>
    /// Gets the property value from this node only, without checking parent nodes.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The property value as a string, or empty string if not found.</returns>
    string GetSelfPropertyValue(string name);

    /// <summary>
    /// Gets the raw CSS value for a property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The raw CSS value, or null if not found.</returns>
    ICssValue? GetPropertyRawValue(string name);

    /// <summary>
    /// Gets a cached computed value for a property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The cached value, or null if not found.</returns>
    object? GetPropertyCachedValue(string name);

    /// <summary>
    /// Checks if this node or its parent has a specific property.
    /// </summary>
    /// <param name="name">The property name to check.</param>
    /// <returns>True if the property exists, otherwise false.</returns>
    bool HasProperty(string name);

    /// <summary>
    /// Gets all properties defined in this node and its children.
    /// </summary>
    /// <returns>A dictionary of property names and their values.</returns>
    Dictionary<string, ICssValue> GetAllProperties();

    /// <summary>
    /// Gets the total number of properties in this node.
    /// </summary>
    /// <returns>The property count.</returns>
    int GetPropertyCount();

    /// <summary>
    /// Replaces a property with a shared node for optimization.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="sharedNode">The shared node to use.</param>
    void ReplaceSubtree(string property, IPropertyTreeNode sharedNode);

    /// <summary>
    /// Checks if a property is shared with another node.
    /// </summary>
    /// <param name="other">The other node to compare with.</param>
    /// <param name="propertyName">The property name to check.</param>
    /// <returns>True if the property is shared, otherwise false.</returns>
    bool IsSharedWith(IPropertyTreeNode other, string propertyName);
}