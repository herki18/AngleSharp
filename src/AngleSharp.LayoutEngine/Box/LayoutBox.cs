namespace AngleSharp.LayoutEngine.Box;

/// <summary>
/// Represents the complete box model for an element, including content, padding, border and margin areas.
/// </summary>
public class LayoutBox
{
    // Content box (content area only)
    public Rect ContentRect { get; set; } = new Rect();

    // Padding edges (includes content + padding)
    public float PaddingTop { get; set; }
    public float PaddingRight { get; set; }
    public float PaddingBottom { get; set; }
    public float PaddingLeft { get; set; }

    // Border edges (includes content + padding + border)
    public float BorderTop { get; set; }
    public float BorderRight { get; set; }
    public float BorderBottom { get; set; }
    public float BorderLeft { get; set; }

    // Margin edges (includes content + padding + border + margin)
    public float MarginTop { get; set; }
    public float MarginRight { get; set; }
    public float MarginBottom { get; set; }
    public float MarginLeft { get; set; }

    // Flags for margin collapsing behavior
    public bool IsInMarginCollapsedChain { get; set; }
    public bool HasTopMarginCollapsed { get; set; }
    public bool HasBottomMarginCollapsed { get; set; }

    /// <summary>
    /// Gets or sets the effective top margin after collapsing.
    /// </summary>
    public float EffectiveTopMargin { get; set; }

    /// <summary>
    /// Gets or sets the effective bottom margin after collapsing.
    /// </summary>
    public float EffectiveBottomMargin { get; set; }

    /// <summary>
    /// Gets the X coordinate of the content box.
    /// </summary>
    public float X
    {
        get => ContentRect.X;
        set => ContentRect.X = value;
    }

    /// <summary>
    /// Gets the Y coordinate of the content box.
    /// </summary>
    public float Y
    {
        get => ContentRect.Y;
        set => ContentRect.Y = value;
    }

    /// <summary>
    /// Gets the width of the content box.
    /// </summary>
    public float Width
    {
        get => ContentRect.Width;
        set => ContentRect.Width = value;
    }

    /// <summary>
    /// Gets the height of the content box.
    /// </summary>
    public float Height
    {
        get => ContentRect.Height;
        set => ContentRect.Height = value;
    }

    /// <summary>
    /// Gets the total width of the box including padding and border (but not margin).
    /// </summary>
    public float BorderBoxWidth => ContentRect.Width + PaddingLeft + PaddingRight + BorderLeft + BorderRight;

    /// <summary>
    /// Gets the total height of the box including padding and border (but not margin).
    /// </summary>
    public float BorderBoxHeight => ContentRect.Height + PaddingTop + PaddingBottom + BorderTop + BorderBottom;

    /// <summary>
    /// Gets the total width of the box including padding, border, and margin.
    /// </summary>
    public float MarginBoxWidth => BorderBoxWidth + MarginLeft + MarginRight;

    /// <summary>
    /// Gets the total height of the box including padding, border, and margin.
    /// </summary>
    public float MarginBoxHeight => BorderBoxHeight + MarginTop + MarginBottom;

    /// <summary>
    /// Gets the left edge of the content box.
    /// </summary>
    public float Left => ContentRect.X;

    /// <summary>
    /// Gets the right edge of the content box.
    /// </summary>
    public float Right => ContentRect.X + ContentRect.Width;

    /// <summary>
    /// Gets the top edge of the content box.
    /// </summary>
    public float Top => ContentRect.Y;

    /// <summary>
    /// Gets the bottom edge of the content box.
    /// </summary>
    public float Bottom => ContentRect.Y + ContentRect.Height;

    /// <summary>
    /// Gets the left edge of the padding box.
    /// </summary>
    public float PaddingBoxLeft => ContentRect.X - PaddingLeft;

    /// <summary>
    /// Gets the right edge of the padding box.
    /// </summary>
    public float PaddingBoxRight => ContentRect.X + ContentRect.Width + PaddingRight;

    /// <summary>
    /// Gets the top edge of the padding box.
    /// </summary>
    public float PaddingBoxTop => ContentRect.Y - PaddingTop;

    /// <summary>
    /// Gets the bottom edge of the padding box.
    /// </summary>
    public float PaddingBoxBottom => ContentRect.Y + ContentRect.Height + PaddingBottom;

    /// <summary>
    /// Gets the left edge of the border box.
    /// </summary>
    public float BorderBoxLeft => PaddingBoxLeft - BorderLeft;

    /// <summary>
    /// Gets the right edge of the border box.
    /// </summary>
    public float BorderBoxRight => PaddingBoxRight + BorderRight;

    /// <summary>
    /// Gets the top edge of the border box.
    /// </summary>
    public float BorderBoxTop => PaddingBoxTop - BorderTop;

    /// <summary>
    /// Gets the bottom edge of the border box.
    /// </summary>
    public float BorderBoxBottom => PaddingBoxBottom + BorderBottom;

    /// <summary>
    /// Gets the left edge of the margin box.
    /// </summary>
    public float MarginBoxLeft => BorderBoxLeft - MarginLeft;

    /// <summary>
    /// Gets the right edge of the margin box.
    /// </summary>
    public float MarginBoxRight => BorderBoxRight + MarginRight;

    /// <summary>
    /// Gets the top edge of the margin box.
    /// </summary>
    public float MarginBoxTop => BorderBoxTop - MarginTop;

    /// <summary>
    /// Gets the bottom edge of the margin box.
    /// </summary>
    public float MarginBoxBottom => BorderBoxBottom + MarginBottom;

    /// <summary>
    /// Gets the padding box as a rectangle.
    /// </summary>
    public Rect GetPaddingBox()
    {
        return new Rect(
            ContentRect.X - PaddingLeft,
            ContentRect.Y - PaddingTop,
            ContentRect.Width + PaddingLeft + PaddingRight,
            ContentRect.Height + PaddingTop + PaddingBottom
        );
    }

    /// <summary>
    /// Gets the border box as a rectangle.
    /// </summary>
    public Rect GetBorderBox()
    {
        return new Rect(
            BorderBoxLeft,
            BorderBoxTop,
            BorderBoxRight - BorderBoxLeft,
            BorderBoxBottom - BorderBoxTop
        );
    }

    /// <summary>
    /// Gets the margin box as a rectangle.
    /// </summary>
    public Rect GetMarginBox()
    {
        return new Rect(
            MarginBoxLeft,
            MarginBoxTop,
            MarginBoxRight - MarginBoxLeft,
            MarginBoxBottom - MarginBoxTop
        );
    }

    /// <summary>
    /// Gets the sum of all vertical insets (padding + border).
    /// </summary>
    public float VerticalInsets => PaddingTop + PaddingBottom + BorderTop + BorderBottom;

    /// <summary>
    /// Gets the sum of all horizontal insets (padding + border).
    /// </summary>
    public float HorizontalInsets => PaddingLeft + PaddingRight + BorderLeft + BorderRight;

    /// <summary>
    /// Determines if this box overlaps with another box.
    /// </summary>
    public bool Overlaps(LayoutBox other)
    {
        return !(Right < other.Left || Left > other.Right ||
                Bottom < other.Top || Top > other.Bottom);
    }

    /// <summary>
    /// Determines if this box contains a point.
    /// </summary>
    public bool ContainsPoint(float x, float y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

    /// <summary>
    /// Creates a copy of this layout box.
    /// </summary>
    public LayoutBox Clone()
    {
        return new LayoutBox
        {
            ContentRect = ContentRect.Clone(),

            PaddingTop = PaddingTop,
            PaddingRight = PaddingRight,
            PaddingBottom = PaddingBottom,
            PaddingLeft = PaddingLeft,

            BorderTop = BorderTop,
            BorderRight = BorderRight,
            BorderBottom = BorderBottom,
            BorderLeft = BorderLeft,

            MarginTop = MarginTop,
            MarginRight = MarginRight,
            MarginBottom = MarginBottom,
            MarginLeft = MarginLeft,

            IsInMarginCollapsedChain = IsInMarginCollapsedChain,
            HasTopMarginCollapsed = HasTopMarginCollapsed,
            HasBottomMarginCollapsed = HasBottomMarginCollapsed,
            EffectiveTopMargin = EffectiveTopMargin,
            EffectiveBottomMargin = EffectiveBottomMargin
        };
    }

    /// <summary>
    /// Updates this box based on the specified derived values.
    /// </summary>
    public void UpdateFromBoxValues(BoxValues boxValues)
    {
        ContentRect.Width = boxValues.ContentWidth;
        ContentRect.Height = boxValues.ContentHeight;

        PaddingTop = boxValues.PaddingTop;
        PaddingRight = boxValues.PaddingRight;
        PaddingBottom = boxValues.PaddingBottom;
        PaddingLeft = boxValues.PaddingLeft;

        BorderTop = boxValues.BorderTop;
        BorderRight = boxValues.BorderRight;
        BorderBottom = boxValues.BorderBottom;
        BorderLeft = boxValues.BorderLeft;

        MarginTop = boxValues.MarginTop;
        MarginRight = boxValues.MarginRight;
        MarginBottom = boxValues.MarginBottom;
        MarginLeft = boxValues.MarginLeft;
    }

    /// <summary>
    /// Compresses all the box model values into a string for debugging.
    /// </summary>
    /// <returns>A string representation of the box.</returns>
    public override string ToString()
    {
        return $"Position=({X:F1},{Y:F1}), " +
               $"Content=({Width:F1}x{Height:F1}), " +
               $"Margin=({MarginTop:F1},{MarginRight:F1},{MarginBottom:F1},{MarginLeft:F1})";
    }
}