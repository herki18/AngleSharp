namespace AngleSharp.StyleSystem.Interfaces;

using System.Collections.Generic;
using AngleSharp.Dom;

/// <summary>
/// Handles the traversal of element trees for style resolution
/// and coordinates style computation scheduling.
/// </summary>
public interface IStyleTreeResolver
{
    /// <summary>
    /// Computes styles for a specific element and its children.
    /// </summary>
    /// <param name="element">The root element of the subtree to resolve styles for.</param>
    /// <param name="forceRecalculate">When true, forces recalculation even if styles are considered up-to-date.</param>
    void ResolveStylesForSubtree(IElement element, bool forceRecalculate = false);

    /// <summary>
    /// Computes the style for an individual element.
    /// </summary>
    /// <param name="element">The element to compute styles for.</param>
    /// <param name="parentStyle">The parent element's computed style, if available.</param>
    /// <param name="pseudoElement">Optional pseudo-element selector.</param>
    /// <returns>The computed style for the element.</returns>
    IComputedStyle ResolveElementStyle(IElement element, IComputedStyle? parentStyle = null, string? pseudoElement = null);

    /// <summary>
    /// Gets all elements that need style recalculation within a subtree.
    /// </summary>
    /// <param name="root">The root element of the subtree to check.</param>
    /// <returns>A collection of elements requiring style recalculation.</returns>
    IEnumerable<IElement> GetElementsNeedingStyleResolution(IElement root);

    /// <summary>
    /// Clears any cached style resolution data.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Determines if an element is eligible for style sharing with another element.
    /// </summary>
    /// <param name="element">The element to check for style sharing opportunities.</param>
    /// <returns>True if the element's style can potentially be shared; otherwise, false.</returns>
    bool CanShareStyle(IElement element);

    /// <summary>
    /// Gets the element that is currently being processed.
    /// </summary>
    IElement? CurrentElement { get; }

    /// <summary>
    /// Gets a value indicating whether style resolution is currently in progress.
    /// </summary>
    bool IsResolving { get; }
}