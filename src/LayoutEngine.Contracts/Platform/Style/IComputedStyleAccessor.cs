using AngleSharp.Dom;

namespace LayoutEngine.Contracts.Platform.Style;

/// <summary>
/// Accesses computed styles for elements.
/// </summary>
public interface IComputedStyleAccessor
{
    /// <summary>
    /// Gets the computed style for the specified element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The computed style.</returns>
    IComputedStyle GetComputedStyle(IElement element);
}