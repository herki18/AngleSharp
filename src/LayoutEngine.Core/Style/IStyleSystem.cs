namespace LayoutEngine.Core.Style;

using AngleSharp.Dom;

public interface IStyleSystem
{
    // Computes style for a specific element
    IComputedStyle ComputeStyle(IElement element);

    // Computes styles for the entire document
    void ComputeDocumentStyles(IDocument document);

    // Get computed style for an element (if already calculated)
    IComputedStyle? GetComputedStyle(IElement element);

    // Check if an element needs style recalculation
    bool NeedsStyleRecalc(IElement element);

    // Invalidate style for an element and its subtree
    void InvalidateStyle(IElement element, bool recursive = true);

    // Clear all cached styles
    void ClearStyles();
}