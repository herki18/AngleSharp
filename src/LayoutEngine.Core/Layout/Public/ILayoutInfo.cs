namespace LayoutEngine.Core.Layout.Public;

using System.Collections.Generic;
using Internal;
using Public;

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