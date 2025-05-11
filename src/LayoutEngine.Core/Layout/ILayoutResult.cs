namespace LayoutEngine.Core.Layout;

using AngleSharp.Dom;

public interface ILayoutResult
{
    // Root fragment of the layout
    ILayoutFragment RootFragment { get; }

    // Get layout box model information for an element
    ILayoutInfo? GetLayoutInfo(IElement element);
}

// Interface for fragments