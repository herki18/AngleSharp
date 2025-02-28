namespace AngleSharp.LayoutEngine.API;

#pragma warning disable CS8604, CS8618, CS9264, CS8600, CS8602, CS8603, CS8625
/// <summary>
/// Contains layout information for an element.
/// </summary>
public class LayoutInfo
{
    /// <summary>
    /// Gets the X coordinate of the content box.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets the Y coordinate of the content box.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets the width of the content box.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Gets the height of the content box.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Gets the top margin.
    /// </summary>
    public float MarginTop { get; set; }

    /// <summary>
    /// Gets the right margin.
    /// </summary>
    public float MarginRight { get; set; }

    /// <summary>
    /// Gets the bottom margin.
    /// </summary>
    public float MarginBottom { get; set; }

    /// <summary>
    /// Gets the left margin.
    /// </summary>
    public float MarginLeft { get; set; }

    /// <summary>
    /// Gets the top padding.
    /// </summary>
    public float PaddingTop { get; set; }

    /// <summary>
    /// Gets the right padding.
    /// </summary>
    public float PaddingRight { get; set; }

    /// <summary>
    /// Gets the bottom padding.
    /// </summary>
    public float PaddingBottom { get; set; }

    /// <summary>
    /// Gets the left padding.
    /// </summary>
    public float PaddingLeft { get; set; }

    /// <summary>
    /// Gets the top border.
    /// </summary>
    public float BorderTop { get; set; }

    /// <summary>
    /// Gets the right border.
    /// </summary>
    public float BorderRight { get; set; }

    /// <summary>
    /// Gets the bottom border.
    /// </summary>
    public float BorderBottom { get; set; }

    /// <summary>
    /// Gets the left border.
    /// </summary>
    public float BorderLeft { get; set; }

    /// <summary>
    /// Gets the left edge of the border box.
    /// </summary>
    public float BorderBoxLeft => X - PaddingLeft - BorderLeft;

    /// <summary>
    /// Gets the top edge of the border box.
    /// </summary>
    public float BorderBoxTop => Y - PaddingTop - BorderTop;

    /// <summary>
    /// Gets the width of the border box.
    /// </summary>
    public float BorderBoxWidth => Width + PaddingLeft + PaddingRight + BorderLeft + BorderRight;

    /// <summary>
    /// Gets the height of the border box.
    /// </summary>
    public float BorderBoxHeight => Height + PaddingTop + PaddingBottom + BorderTop + BorderBottom;

    /// <summary>
    /// Gets the left edge of the margin box.
    /// </summary>
    public float MarginBoxLeft => BorderBoxLeft - MarginLeft;

    /// <summary>
    /// Gets the top edge of the margin box.
    /// </summary>
    public float MarginBoxTop => BorderBoxTop - MarginTop;

    /// <summary>
    /// Gets the width of the margin box.
    /// </summary>
    public float MarginBoxWidth => BorderBoxWidth + MarginLeft + MarginRight;

    /// <summary>
    /// Gets the height of the margin box.
    /// </summary>
    public float MarginBoxHeight => BorderBoxHeight + MarginTop + MarginBottom;
}