namespace AngleSharp.StyleSystem.Interfaces;

using AngleSharp.Css.Dom;

/// <summary>
/// Processes style inheritance according to CSS specification rules.
/// </summary>
public interface IInheritanceProcessor
{
    /// <summary>
    /// Applies inheritance to the element's style based on the parent's computed style.
    /// </summary>
    /// <param name="elementStyle">The element's own style declaration.</param>
    /// <param name="parentComputedStyle">The parent element's computed style, if available.</param>
    /// <returns>A new style declaration with inherited properties applied.</returns>
    ICssStyleDeclaration ApplyInheritance(ICssStyleDeclaration elementStyle, IComputedStyle? parentComputedStyle);
}