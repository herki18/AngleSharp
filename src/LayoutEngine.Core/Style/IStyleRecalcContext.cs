using AngleSharp.Dom;
using AngleSharp.Css.Dom;

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
}