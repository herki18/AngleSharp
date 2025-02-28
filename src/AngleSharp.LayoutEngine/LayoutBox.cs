namespace AngleSharp.LayoutEngine;

using System;
using Css.Dom;

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

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
public class Rect
{
    /// <summary>
    /// X coordinate of the rectangle.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y coordinate of the rectangle.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Height of the rectangle.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Gets the left edge of the rectangle.
    /// </summary>
    public float Left => X;

    /// <summary>
    /// Gets the right edge of the rectangle.
    /// </summary>
    public float Right => X + Width;

    /// <summary>
    /// Gets the top edge of the rectangle.
    /// </summary>
    public float Top => Y;

    /// <summary>
    /// Gets the bottom edge of the rectangle.
    /// </summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// Creates an empty rectangle.
    /// </summary>
    public Rect()
    {
    }

    /// <summary>
    /// Creates a rectangle with the specified position and size.
    /// </summary>
    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Determines if this rectangle contains a point.
    /// </summary>
    public bool Contains(float x, float y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

    /// <summary>
    /// Determines if this rectangle intersects with another rectangle.
    /// </summary>
    public bool IntersectsWith(Rect other)
    {
        return !(Right < other.Left || Left > other.Right ||
                Bottom < other.Top || Top > other.Bottom);
    }

    /// <summary>
    /// Creates a copy of this rectangle.
    /// </summary>
    public Rect Clone()
    {
        return new Rect(X, Y, Width, Height);
    }

    /// <summary>
    /// Returns a string representation of this rectangle.
    /// </summary>
    public override string ToString()
    {
        return $"Rect({X:F1},{Y:F1},{Width:F1},{Height:F1})";
    }
}

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

/// <summary>
/// Calculates box model values from CSS style properties.
/// </summary>
public class BoxModelCalculator
{
    private readonly BoxValues _boxValues = new BoxValues();

    /// <summary>
    /// Creates a box model calculator for the specified style and constraints.
    /// </summary>
    public BoxModelCalculator(ICssStyleDeclaration style, float availableWidth)
    {
        if (style == null) return;

        // Calculate margins
        _boxValues.MarginTop = ParseLength(style, "margin-top", 0);
        _boxValues.MarginRight = ParseLength(style, "margin-right", 0);
        _boxValues.MarginBottom = ParseLength(style, "margin-bottom", 0);
        _boxValues.MarginLeft = ParseLength(style, "margin-left", 0);

        // Calculate borders
        _boxValues.BorderTop = ParseLength(style, "border-top-width", 0);
        _boxValues.BorderRight = ParseLength(style, "border-right-width", 0);
        _boxValues.BorderBottom = ParseLength(style, "border-bottom-width", 0);
        _boxValues.BorderLeft = ParseLength(style, "border-left-width", 0);

        // Calculate paddings
        _boxValues.PaddingTop = ParseLength(style, "padding-top", 0);
        _boxValues.PaddingRight = ParseLength(style, "padding-right", 0);
        _boxValues.PaddingBottom = ParseLength(style, "padding-bottom", 0);
        _boxValues.PaddingLeft = ParseLength(style, "padding-left", 0);

        // Calculate content dimensions
        _boxValues.ContentWidth = ComputeWidth(style, availableWidth);
        _boxValues.ContentHeight = ComputeHeight(style);
    }

    /// <summary>
    /// Gets the calculated box model values.
    /// </summary>
    public BoxValues GetBoxValues() => _boxValues;

    /// <summary>
    /// Computes the content width based on CSS properties.
    /// </summary>
    private float ComputeWidth(ICssStyleDeclaration style, float availableWidth)
    {
        // Check if width is specified
        string widthValue = style.GetPropertyValue("width");
        string boxSizing = style.GetPropertyValue("box-sizing") ?? "content-box";

        float horizontalInsets = _boxValues.PaddingLeft + _boxValues.PaddingRight +
                               _boxValues.BorderLeft + _boxValues.BorderRight;

        if (string.IsNullOrEmpty(widthValue) || widthValue == "auto")
        {
            // Auto width fills the available space
            return boxSizing == "border-box"
                ? Math.Max(0, availableWidth - horizontalInsets)
                : availableWidth;
        }

        if (widthValue.EndsWith("%"))
        {
            // Percentage of available width
            if (float.TryParse(widthValue.TrimEnd('%'), out float percentage))
            {
                float computedWidth = availableWidth * percentage / 100f;

                return boxSizing == "border-box"
                    ? Math.Max(0, computedWidth - horizontalInsets)
                    : computedWidth;
            }
        }

        if (widthValue.EndsWith("px") && float.TryParse(widthValue.TrimEnd('p', 'x'), out float pixels))
        {
            return boxSizing == "border-box"
                ? Math.Max(0, pixels - horizontalInsets)
                : pixels;
        }

        // Default to available width
        return availableWidth;
    }

    /// <summary>
    /// Computes the content height based on CSS properties.
    /// </summary>
    private float ComputeHeight(ICssStyleDeclaration style)
    {
        // Check if height is specified
        string heightValue = style.GetPropertyValue("height");
        string boxSizing = style.GetPropertyValue("box-sizing") ?? "content-box";

        float verticalInsets = _boxValues.PaddingTop + _boxValues.PaddingBottom +
                             _boxValues.BorderTop + _boxValues.BorderBottom;

        if (string.IsNullOrEmpty(heightValue) || heightValue == "auto")
        {
            // Auto height will be determined by content
            return float.NaN;
        }

        if (heightValue.EndsWith("px") && float.TryParse(heightValue.TrimEnd('p', 'x'), out float pixels))
        {
            return boxSizing == "border-box"
                ? Math.Max(0, pixels - verticalInsets)
                : pixels;
        }

        // Default to auto height
        return float.NaN;
    }

    /// <summary>
    /// Parses a CSS length value from a style property.
    /// </summary>
    private float ParseLength(ICssStyleDeclaration style, string propertyName, float defaultValue)
    {
        string value = style.GetPropertyValue(propertyName);

        if (string.IsNullOrEmpty(value) || value == "auto")
            return defaultValue;

        if (value.EndsWith("px") && float.TryParse(value.TrimEnd('p', 'x'), out float pixels))
            return pixels;

        // Handle other units if needed

        return defaultValue;
    }
}