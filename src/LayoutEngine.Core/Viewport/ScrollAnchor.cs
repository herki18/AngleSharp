namespace LayoutEngine.Core.Viewport;

using System;
using AngleSharp.Dom;
using Layout;

public class ScrollAnchor
{
    // Element being used as an anchor
    public IElement Element { get; }

    // Relative position within the viewport (0-1 range)
    public float RelativeX { get; }
    public float RelativeY { get; }

    // Original scroll offset when anchor was created
    public Point OriginalScrollOffset { get; }

    public ScrollAnchor(IElement element, Viewport viewport)
    {
        Element = element;
        OriginalScrollOffset = viewport.ScrollOffset;

        // Calculate relative position if we have layout info
        // This is a simplified approach - in a real implementation
        // you'd need the element's bounds relative to the viewport
        RelativeX = 0.5f; // Center horizontally
        RelativeY = 0.5f; // Center vertically
    }

    // Apply this anchor to maintain relative position after layout changes
    public Point CalculateNewScrollOffset(Viewport viewport, ILayoutSystem layoutSystem)
    {
        // Get the element's new position after layout
        var layoutInfo = layoutSystem.GetLayoutInfo(Element);
        if (layoutInfo == null)
        {
            return OriginalScrollOffset; // Can't find element, retain original scroll
        }

        // Calculate where the anchor point should be
        var elementRect = layoutInfo.ContentRect;

        // Calculate new scroll position to keep the same relative element position visible
        float targetX = elementRect.X - (viewport.ViewportRect.Width * RelativeX);
        float targetY = elementRect.Y - (viewport.ViewportRect.Height * RelativeY);

        // Clamp to valid scroll range
        targetX = Math.Max(0, Math.Min(targetX, viewport.ContentSize.Width - viewport.ViewportRect.Width));
        targetY = Math.Max(0, Math.Min(targetY, viewport.ContentSize.Height - viewport.ViewportRect.Height));

        return new Point(targetX, targetY);
    }
}
