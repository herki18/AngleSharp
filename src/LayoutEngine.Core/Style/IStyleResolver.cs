using AngleSharp.Dom;
using AngleSharp.Css.Dom;
using LayoutEngine.Core.Style;

/// <summary>
/// Main style resolution orchestrator - mirrors Blink's StyleResolver
/// </summary>
public interface IStyleResolver
{
    /// <summary>
    /// Resolves style for a single element (Blink: ResolveStyle)
    /// </summary>
    IComputedStyle ResolveStyle(IElement element, IStyleRecalcContext context);

    /// <summary>
    /// Computes styles for entire document tree (Blink: Document::RecalcStyle)
    /// </summary>
    void RecalcDocumentStyle(IDocument document);
}