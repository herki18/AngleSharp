namespace AngleSharp.StyleSystem.Interfaces;

using AngleSharp.Css;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;

/// <summary>
/// Main entry point for style computation.
/// </summary>
public interface IStyleEngine
{
    /// <summary>
    /// Computes the style for an element.
    /// </summary>
    /// <param name="element">The element to compute styles for.</param>
    /// <param name="pseudoElement">Optional pseudo-element selector.</param>
    /// <returns>The computed style for the element.</returns>
    IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null);

    /// <summary>
    /// Updates styles after a change to the DOM or stylesheets.
    /// </summary>
    /// <param name="root">The root element of the subtree to update.</param>
    void UpdateStyles(IElement root);

    /// <summary>
    /// Gets a factory for creating computed style objects.
    /// </summary>
    IComputedStyleFactory StyleFactory { get; }

    /// <summary>
    /// Gets the style invalidation tracker.
    /// </summary>
    IStyleInvalidationTracker InvalidationTracker { get; }

    IRenderDevice RenderDevice { get; }
    StyleSheetManager StylesheetManager { get; }
}