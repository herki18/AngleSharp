namespace LayoutEngine.Core.Viewport;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Layout;

public class Viewport
{
    // Unique identifier for this viewport
    public string Id { get; }

    // Reference to DOM element (for sync purposes)
    public IElement DomElement { get; }

    // Visible rectangle in the coordinate space of the parent viewport
    public Rect ViewportRect { get; set; }

    // Total content size that can be scrolled
    public Size ContentSize { get; set; }

    // Current scroll position
    public Point ScrollOffset { get; set; }

    // Parent viewport (null for root)
    public Viewport? Parent { get; set; }

    // Child viewports
    public List<Viewport> Children { get; } = new List<Viewport>();

    // Fragment IDs contained in this viewport
    public HashSet<string> FragmentIds { get; } = new HashSet<string>();

    // Scroll properties
    public bool CanScrollHorizontally { get; set; }
    public bool CanScrollVertically { get; set; }
    public bool UseSmoothScrolling { get; set; }

    public Viewport(string id, IElement? domElement)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DomElement = domElement ?? throw new ArgumentNullException(nameof(domElement));
    }

    // Get absolute position in document space (accounting for parent scroll offsets)
    public Rect GetAbsoluteRect()
    {
        var x = ViewportRect.X;
        var y = ViewportRect.Y;
        var current = Parent;

        while (current != null)
        {
            x += current.ViewportRect.X - current.ScrollOffset.X;
            y += current.ViewportRect.Y - current.ScrollOffset.Y;
            current = current.Parent;
        }

        return new Rect(x, y, ViewportRect.Width, ViewportRect.Height);
    }

    // Transform document coordinates to local viewport coordinates
    public Point DocumentToLocal(Point documentPoint)
    {
        var absRect = GetAbsoluteRect();
        return new Point(
            documentPoint.X - absRect.X + ScrollOffset.X,
            documentPoint.Y - absRect.Y + ScrollOffset.Y
        );
    }

    // Transform local viewport coordinates to document coordinates
    public Point LocalToDocument(Point localPoint)
    {
        var absRect = GetAbsoluteRect();
        return new Point(
            localPoint.X + absRect.X - ScrollOffset.X,
            localPoint.Y + absRect.Y - ScrollOffset.Y
        );
    }
}