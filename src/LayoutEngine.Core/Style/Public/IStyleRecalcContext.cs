using AngleSharp.Css.Dom;
using AngleSharp.Dom;

namespace LayoutEngine.Core.Style.Public;

/// <summary>
/// Context during style recalculation - mirrors Blink's StyleRecalcContext
/// </summary>
public interface IStyleRecalcContext
{
    /// <summary>
    /// Parent element's computed style for inheritance
    /// </summary>
    ICssStyleDeclaration? ParentStyle { get; } // AngleSharp type

    /// <summary>
    /// Document being processed
    /// </summary>
    IDocument Document { get; }

    /// <summary>
    /// Current element being processed
    /// </summary>
    IElement? CurrentElement { get; }

    IStyleRecalcContext WithParent(ICssStyleDeclaration parentStyle);
    IStyleRecalcContext WithElement(IElement element);
}