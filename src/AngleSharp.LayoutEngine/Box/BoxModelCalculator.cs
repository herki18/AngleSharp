namespace AngleSharp.LayoutEngine.Box;

using System;
using Css.Dom;

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