namespace AngleSharp.StyleSystem.Interfaces;

using System.Collections.Generic;
using AngleSharp.Dom;

/// <summary>
/// Tracks style invalidation and dependencies.
/// </summary>
public interface IStyleInvalidationTracker
{
    /// <summary>
    /// Marks an element as needing style recalculation.
    /// </summary>
    /// <param name="element">The element to invalidate.</param>
    void InvalidateElement(IElement element);

    /// <summary>
    /// Marks specific properties as needing recalculation.
    /// </summary>
    /// <param name="element">The element with properties to invalidate.</param>
    /// <param name="properties">The names of the properties to invalidate.</param>
    void InvalidateProperties(IElement element, IEnumerable<string> properties);

    /// <summary>
    /// Determines if an element needs style recalculation.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>True if the element needs recalculation; otherwise, false.</returns>
    bool NeedsStyleRecalculation(IElement element);

    /// <summary>
    /// Tracks a style dependency between elements.
    /// </summary>
    /// <param name="dependent">The element that depends on the source.</param>
    /// <param name="source">The element that affects the dependent.</param>
    void TrackDependency(IElement dependent, IElement source);
}