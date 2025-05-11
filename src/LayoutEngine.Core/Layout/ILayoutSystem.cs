namespace LayoutEngine.Core.Layout;

using AngleSharp.Dom;

public interface ILayoutSystem
{
    // Perform layout for the document
    ILayoutResult PerformLayout(IDocument document);

    // Check if an element needs layout
    bool NeedsLayout(IElement element);

    // Invalidate layout for an element (typically triggered by style changes)
    void InvalidateLayout(IElement element, bool recursive = true);

    // Get the current fragment tree (after layout is performed)
    IFragmentTree GetFragmentTree();

    // Get layout information for a specific element
    ILayoutInfo? GetLayoutInfo(IElement element);
}


// Interface for layout results