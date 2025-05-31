namespace LayoutEngine.Core.LayoutNG.Public;

using LayoutEngine.Core.Layout.Internal;

/// <summary>
/// Interface for layout objects that have a box model (margins, borders, padding).
/// </summary>
public interface ILayoutBox : ILayoutContainer
{
    /// <summary>
    /// Gets or sets the content box rectangle (excludes padding, border, margin).
    /// </summary>
    Rect ContentRect { get; set; }

    /// <summary>
    /// Gets or sets the padding box rectangle (content + padding).
    /// </summary>
    Rect PaddingRect { get; set; }

    /// <summary>
    /// Gets or sets the border box rectangle (content + padding + border).
    /// </summary>
    Rect BorderRect { get; set; }

    /// <summary>
    /// Gets or sets the margin box rectangle (content + padding + border + margin).
    /// </summary>
    Rect MarginRect { get; set; }

    /// <summary>
    /// Gets the computed margin values.
    /// </summary>
    EdgeValues Margins { get; set; }

    /// <summary>
    /// Gets the computed border widths.
    /// </summary>
    EdgeValues Borders { get; set; }

    /// <summary>
    /// Gets the computed padding values.
    /// </summary>
    EdgeValues Paddings { get; set; }

    /// <summary>
    /// Gets or sets the logical width (respecting writing mode).
    /// </summary>
    float LogicalWidth { get; set; }

    /// <summary>
    /// Gets or sets the logical height (respecting writing mode).
    /// </summary>
    float LogicalHeight { get; set; }

    /// <summary>
    /// Gets or sets the position offset from the containing block.
    /// </summary>
    Point LocationOffset { get; set; }

    /// <summary>
    /// Gets whether this box shrinks to fit its content.
    /// </summary>
    bool ShrinkToFit { get; }

    /// <summary>
    /// Gets whether this box's width depends on its containing block.
    /// </summary>
    bool WidthDependsOnContainingBlock { get; }

    /// <summary>
    /// Computes the logical width of this box.
    /// </summary>
    void ComputeLogicalWidth();

    /// <summary>
    /// Computes the logical height of this box.
    /// </summary>
    void ComputeLogicalHeight();

    /// <summary>
    /// Updates the position of this box relative to its containing block.
    /// </summary>
    void UpdateLocation();
}

/// <summary>
/// Represents edge values (top, right, bottom, left) for margins, borders, or padding.
/// </summary>
public struct EdgeValues
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }

    public EdgeValues(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public EdgeValues(float all) : this(all, all, all, all) { }

    public EdgeValues(float vertical, float horizontal) : this(vertical, horizontal, vertical, horizontal) { }
}