namespace AngleSharp.StyleSystem.Core;

using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;

/// <summary>
/// Builds computed style objects from CSS declarations.
/// </summary>
public class ComputedStyleBuilder
{
    private readonly StyleEngine _engine;

    public ComputedStyleBuilder(StyleEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Builds a computed style from a CSS style declaration.
    /// </summary>
    public IComputedStyle? BuildComputedStyle(ICssStyleDeclaration style, IElement element, IComputedStyle? parentStyle)
    {
        // Create a computed style via the factory
        return (_engine.StyleFactory as ComputedStyleFactory)?.CreateComputedStyle(element, parentStyle, style);
    }
}