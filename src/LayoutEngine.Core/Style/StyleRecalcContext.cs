using AngleSharp.Dom;
using AngleSharp.Css.Dom;

namespace LayoutEngine.Core.Style;

using System;

/// <summary>
/// Context during style recalculation - carries parent style and document info
/// </summary>
public class StyleRecalcContext : IStyleRecalcContext
{
    public ICssStyleDeclaration? ParentStyle { get; }
    public IDocument Document { get; }
    public IElement? CurrentElement { get; set; }

    public StyleRecalcContext(IDocument? document, ICssStyleDeclaration? parentStyle = null)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        ParentStyle = parentStyle;
    }

    public IStyleRecalcContext WithParent(ICssStyleDeclaration parentStyle)
    {
        return new StyleRecalcContext(Document, parentStyle) { CurrentElement = CurrentElement };
    }

    public IStyleRecalcContext WithElement(IElement element)
    {
        return new StyleRecalcContext(Document, ParentStyle) { CurrentElement = element };
    }
}