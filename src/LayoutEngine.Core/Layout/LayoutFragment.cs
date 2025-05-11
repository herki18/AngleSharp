namespace LayoutEngine.Core.Layout;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;

public class LayoutFragment : ILayoutFragment
{
    public Rect Bounds { get; set; }
    public IElement? Element { get; set; }
    public IReadOnlyList<ILayoutFragment> Children { get; set; } = Array.Empty<ILayoutFragment>();
    public IVisualProperties VisualProperties { get; set; } = new VisualProperties();
}