namespace AngleSharp.StyleSystem.Interfaces;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Storage;

/// <summary>
/// Defines a manager for property tree nodes that optimizes CSS property storage and retrieval.
/// </summary>
public interface IPropertyTreeManager
{
    /// <summary>
    /// Creates a new property tree node for an element.
    /// </summary>
    /// <param name="element">The element that owns the node.</param>
    /// <param name="parent">The optional parent node.</param>
    /// <returns>A new property tree node.</returns>
    IPropertyTreeNode CreateNode(IElement element, IPropertyTreeNode? parent = null);

    /// <summary>
    /// Gets an existing property tree node for an element or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="element">The element that owns the node.</param>
    /// <param name="parent">The optional parent node.</param>
    /// <returns>The existing or new property tree node.</returns>
    IPropertyTreeNode GetOrCreateNode(IElement element, IPropertyTreeNode? parent = null);

    /// <summary>
    /// Gets a shared property tree node for a specific property value.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A shared property tree node.</returns>
    PropertyTreeNode GetSharedNode(string propertyName, ICssValue value);

    /// <summary>
    /// Optimizes a property tree node by sharing common values and reducing memory usage.
    /// </summary>
    /// <param name="node">The node to optimize.</param>
    void OptimizeTree(IPropertyTreeNode node);

    /// <summary>
    /// Gets metrics on property tree optimization performance.
    /// </summary>
    /// <returns>Optimization metrics.</returns>
    OptimizationMetrics GetOptimizationMetrics();

    /// <summary>
    /// Resets the optimization metrics.
    /// </summary>
    void ResetOptimizationMetrics();
}