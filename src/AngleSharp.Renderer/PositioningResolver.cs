namespace AngleSharp.Renderer;

using Css.Dom;
using Css.Values;

#pragma warning disable CS8604, CS1591
/// <summary>
/// Calculates the final X,Y coordinates of an element taking into account margins, positioning,
/// and parent container constraints following CSS layout rules.
/// </summary>
public static class PositioningResolver
{
    /// <summary>
    /// Calculates the final position of an element considering its margins, relative positioning,
    /// and parent container boundaries. Handles both fixed and auto margins according to CSS rules.
    /// </summary>
    /// <param name="style">CSS style declaration containing positioning and margin properties</param>
    /// <param name="parentX">Parent container's X coordinate</param>
    /// <param name="parentY">Parent container's Y coordinate</param>
    /// <param name="parentAvailableWidth">Available width within the parent container</param>
    /// <param name="elementContentWidth">Width of the element being positioned</param>
    /// <returns>Tuple containing final (X, Y) coordinates of the element</returns>
    public static (float X, float Y) CalculateElementPosition(
        ICssStyleDeclaration style,
        float parentX,
        float parentY,
        float parentAvailableWidth,
        float elementContentWidth)
    {
        // Extract margin values and check if they're set to 'auto'
        float marginLeftValue = ParseCssPixelValue(style, "margin-left");
        float marginRightValue = ParseCssPixelValue(style, "margin-right");
        bool isMarginLeftAuto = style.GetPropertyValue("margin-left") == "auto";
        bool isMarginRightAuto = style.GetPropertyValue("margin-right") == "auto";

        // Calculate final margins accounting for 'auto' values
        (float finalLeftMargin, float finalRightMargin) =
            AutoMarginResolver.CalculateAutoMargins(
                parentAvailableWidth,
                elementContentWidth,
                marginLeftValue,
                marginRightValue,
                isMarginLeftAuto,
                isMarginRightAuto);

        // Handle relative positioning offsets
        bool isRelativelyPositioned = style.GetPropertyValue("position") == "relative";
        float horizontalOffset = isRelativelyPositioned ? ParseCssPixelValue(style, "left") : 0;
        float verticalOffset = isRelativelyPositioned ? ParseCssPixelValue(style, "top") : 0;

        // Calculate final coordinates
        float finalX = parentX + finalLeftMargin + horizontalOffset;
        float finalY = parentY + verticalOffset;

        return (finalX, finalY);
    }

    /// <summary>
    /// Parses a CSS pixel value from a style property. Returns 0 if the property
    /// is not set or is not a valid length value.
    /// </summary>
    /// <param name="style">CSS style declaration containing the property</param>
    /// <param name="propertyName">Name of the CSS property to parse</param>
    /// <returns>Pixel value as float, or 0 if not found/invalid</returns>
    private static float ParseCssPixelValue(ICssStyleDeclaration style, string propertyName)
    {
        var propertyValue = style.GetProperty(propertyName)?.RawValue;
        return propertyValue is CssLengthValue lengthValue ? (float)lengthValue.Value : 0f;
    }
}