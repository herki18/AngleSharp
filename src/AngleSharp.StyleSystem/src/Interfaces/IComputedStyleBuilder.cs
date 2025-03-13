namespace AngleSharp.StyleSystem.Interfaces;

using AngleSharp.Css.Dom;
using AngleSharp.Dom;

/// <summary>
/// Builds computed style objects from CSS declarations.
/// </summary>
public interface IComputedStyleBuilder
{
    /// <summary>
    /// Builds a computed style from a CSS style declaration.
    /// </summary>
    /// <param name="declaration">The CSS style declaration to process.</param>
    /// <param name="element">The element being styled.</param>
    /// <param name="parentStyle">The parent element's computed style.</param>
    /// <returns>A computed style object.</returns>
    IComputedStyle? BuildComputedStyle(ICssStyleDeclaration declaration, IElement element, IComputedStyle? parentStyle);
}