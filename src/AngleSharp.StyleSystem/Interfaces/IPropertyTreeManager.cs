using AngleSharp.Css.Dom;
using AngleSharp.Dom;

namespace AngleSharp.StyleSystem.Core.Interfaces;

/// <summary>
/// Manages property tree nodes for optimal style sharing.
/// </summary>
public interface IPropertyTreeManager
{
    /// <summary>
    /// Creates a new property tree node.
    /// </summary>
    /// <param name="element">The element to associate with the node.</param>
    /// <param name="parent">Optional parent node.</param>
    /// <returns>A new property tree node.</returns>
    PropertyTreeNode CreateNode(IElement element, PropertyTreeNode? parent = null);

    /// <summary>
    /// Gets or creates a property tree node for an element.
    /// </summary>
    /// <param name="element">The element to get a node for.</param>
    /// <param name="parent">Optional parent node.</param>
    /// <returns>An existing or new property tree node.</returns>
    PropertyTreeNode GetOrCreateNode(IElement element, PropertyTreeNode? parent = null);

    /// <summary>
    /// Gets a shared node for a specific property value.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A shared node containing just that property.</returns>
    PropertyTreeNode GetSharedNode(string propertyName, ICssValue value);

    /// <summary>
    /// Optimizes a property tree for memory usage.
    /// </summary>
    /// <param name="node">The node to optimize.</param>
    void OptimizeTree(PropertyTreeNode node);
}