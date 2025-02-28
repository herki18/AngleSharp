namespace AngleSharp.LayoutEngine.Box;

using System;

/// <summary>
/// Holds the box model values derived from style properties.
/// </summary>
public class BoxValues
{
    // Content dimensions
    public float ContentWidth { get; set; }
    public float ContentHeight { get; set; }

    // Padding values
    public float PaddingTop { get; set; }
    public float PaddingRight { get; set; }
    public float PaddingBottom { get; set; }
    public float PaddingLeft { get; set; }

    // Border values
    public float BorderTop { get; set; }
    public float BorderRight { get; set; }
    public float BorderBottom { get; set; }
    public float BorderLeft { get; set; }

    // Margin values
    public float MarginTop { get; set; }
    public float MarginRight { get; set; }
    public float MarginBottom { get; set; }
    public float MarginLeft { get; set; }

    /// <summary>
    /// Gets the total width of the content, padding, and border combined.
    /// </summary>
    public float BorderBoxWidth => ContentWidth + PaddingLeft + PaddingRight + BorderLeft + BorderRight;

    /// <summary>
    /// Gets the total height of the content, padding, and border combined.
    /// </summary>
    public float BorderBoxHeight => ContentHeight + PaddingTop + PaddingBottom + BorderTop + BorderBottom;

    /// <summary>
    /// Gets the total horizontal padding and border.
    /// </summary>
    public float HorizontalInsets => PaddingLeft + PaddingRight + BorderLeft + BorderRight;

    /// <summary>
    /// Gets the total vertical padding and border.
    /// </summary>
    public float VerticalInsets => PaddingTop + PaddingBottom + BorderTop + BorderBottom;

    /// <summary>
    /// Gets the total width including margins.
    /// </summary>
    public float MarginBoxWidth => BorderBoxWidth + MarginLeft + MarginRight;

    /// <summary>
    /// Gets the total height including margins.
    /// </summary>
    public float MarginBoxHeight => BorderBoxHeight + MarginTop + MarginBottom;

    /// <summary>
    /// Calculates content width from border-box width (for border-box sizing).
    /// </summary>
    public void CalculateContentWidthFromBorderBox(float borderBoxWidth)
    {
        ContentWidth = Math.Max(0, borderBoxWidth - HorizontalInsets);
    }

    /// <summary>
    /// Calculates content height from border-box height (for border-box sizing).
    /// </summary>
    public void CalculateContentHeightFromBorderBox(float borderBoxHeight)
    {
        ContentHeight = Math.Max(0, borderBoxHeight - VerticalInsets);
    }
}