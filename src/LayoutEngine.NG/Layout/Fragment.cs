namespace LayoutEngine.NG.Layout;

/// <summary>
/// Represents a layout fragment in the LayoutNG architecture.
/// A fragment is the output of the layout process for a LayoutObject.
/// In LayoutNG, fragments are immutable and represent the physical manifestation
/// of a LayoutObject in a particular context.
/// </summary>
public class Fragment
{
    /// <summary>
    /// The position of this fragment relative to its containing block.
    /// In LayoutNG, this is a PhysicalOffset.
    /// </summary>
    public PhysicalOffset Offset { get; set; }

    /// <summary>
    /// The size of this fragment.
    /// In LayoutNG, this is a PhysicalSize.
    /// </summary>
    public PhysicalSize Size { get; set; }

    /// <summary>
    /// Reference to the LayoutObject that generated this fragment.
    /// In LayoutNG, a single LayoutObject can generate multiple fragments
    /// (e.g., when broken across pages or columns).
    /// </summary>
    public LayoutObject? LayoutObject { get; set; }

    /// <summary>
    /// Whether this fragment is part of a fragmentation context.
    /// In LayoutNG, this indicates if the fragment is broken across pages/columns.
    /// </summary>
    public bool IsFragmented { get; set; }

    /// <summary>
    /// The fragment's break token, used for fragmentation.
    /// In LayoutNG, break tokens carry state between fragments of the same LayoutObject.
    /// </summary>
    // Note: In real LayoutNG, this would be a BreakToken object
    public object? BreakToken { get; set; }

    /// <summary>
    /// Gets the bounding box of this fragment in its container's coordinate space.
    /// In LayoutNG, this is commonly needed for paint and hit-testing.
    /// </summary>
    public (float left, float top, float right, float bottom) GetBoundingBox()
    {
        return (Offset.Left, Offset.Top,
                Offset.Left + Size.Width,
                Offset.Top + Size.Height);
    }

    /// <summary>
    /// Checks if a point is within this fragment.
    /// In LayoutNG, used for hit-testing.
    /// </summary>
    public bool ContainsPoint(float x, float y)
    {
        return x >= Offset.Left &&
               x < Offset.Left + Size.Width &&
               y >= Offset.Top &&
               y < Offset.Top + Size.Height;
    }
}