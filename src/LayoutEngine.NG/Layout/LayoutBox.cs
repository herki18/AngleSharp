namespace LayoutEngine.NG.Layout;

/// <summary>
/// Represents a box in the layout tree.
/// In LayoutNG, LayoutBox is the base class for all layout objects that generate a principal box.
/// This follows the actual LayoutNG naming convention (not LayoutBlock).
/// </summary>
public class LayoutBox : LayoutObject
{
    /// <summary>
    /// The position of this box.
    /// In LayoutNG, boxes store their physical location.
    /// Note: This is separate from Fragment.Offset which is relative to the containing block.
    /// </summary>
    public PhysicalOffset Location { get; set; }

    /// <summary>
    /// The size of this box's content area.
    /// In LayoutNG, this is the content box size (excludes padding and borders).
    /// </summary>
    public PhysicalSize ContentSize { get; set; }

    /// <summary>
    /// The box's padding area.
    /// In LayoutNG, padding is part of the box model.
    /// </summary>
    public BoxSpacing Padding { get; set; }

    /// <summary>
    /// The box's border widths.
    /// In LayoutNG, borders affect box sizing.
    /// </summary>
    public BoxSpacing Border { get; set; }

    /// <summary>
    /// The box's margin area.
    /// In LayoutNG, margins are outside the border box.
    /// </summary>
    public BoxSpacing Margin { get; set; }

    /// <summary>
    /// The box's overflow behavior.
    /// In LayoutNG, determines clipping and scrolling.
    /// </summary>
    public Overflow OverflowX { get; set; } = Overflow.Visible;
    public Overflow OverflowY { get; set; } = Overflow.Visible;

    /// <summary>
    /// Whether this box has been positioned.
    /// In LayoutNG, used during layout to track progress.
    /// </summary>
    public bool IsPositioned { get; set; }

    /// <summary>
    /// Gets the border box size (content + padding + border).
    /// In LayoutNG, this is a commonly needed measurement.
    /// </summary>
    public PhysicalSize BorderBoxSize
    {
        get
        {
            return new PhysicalSize(
                ContentSize.Width + Padding.Left + Padding.Right + Border.Left + Border.Right,
                ContentSize.Height + Padding.Top + Padding.Bottom + Border.Top + Border.Bottom
            );
        }
    }

    /// <summary>
    /// Gets the padding box size (content + padding).
    /// </summary>
    public PhysicalSize PaddingBoxSize
    {
        get
        {
            return new PhysicalSize(
                ContentSize.Width + Padding.Left + Padding.Right,
                ContentSize.Height + Padding.Top + Padding.Bottom
            );
        }
    }

    /// <summary>
    /// Creates a fragment for this box during layout.
    /// In LayoutNG, this would be part of the layout algorithm's output.
    /// </summary>
    public Fragment CreateFragment()
    {
        return new Fragment
        {
            LayoutObject = this,
            Offset = Location,
            Size = BorderBoxSize,
            IsFragmented = false,
            BreakToken = null
        };
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Box;
    }
}

/// <summary>
/// Represents spacing values for box model calculations.
/// In LayoutNG, used for margin, padding, and border.
/// </summary>
public struct BoxSpacing
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
}

/// <summary>
/// CSS overflow values.
/// </summary>
public enum Overflow
{
    Visible,
    Hidden,
    Scroll,
    Auto,
    Clip
}