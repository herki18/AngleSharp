namespace LayoutEngine.Core.Layout;

using System.Collections.Generic;

public interface ILayoutInfo
{
    // Box model rectangles
    Rect ContentRect { get; }
    Rect PaddingRect { get; }
    Rect BorderRect { get; }
    Rect MarginRect { get; }
    
    // Position in parent coordinate space
    Point Position { get; }
    
    // All fragments for this element (could be multiple, e.g., for text)
    IReadOnlyList<ILayoutFragment> Fragments { get; }
}