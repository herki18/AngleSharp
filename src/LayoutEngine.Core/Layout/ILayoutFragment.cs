namespace LayoutEngine.Core.Layout;

using System.Collections.Generic;
using AngleSharp.Dom;

public interface ILayoutFragment
{
    // Geometry of this fragment
    Rect Bounds { get; }
    
    // Source element (if any)
    IElement? Element { get; }
    
    // Child fragments
    IReadOnlyList<ILayoutFragment> Children { get; }
    
    // Visual properties needed for rendering
    IVisualProperties VisualProperties { get; }
}