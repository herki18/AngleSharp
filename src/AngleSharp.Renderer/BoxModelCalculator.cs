namespace AngleSharp.Renderer;

using Css.Dom;
using Css.Values;

#pragma warning disable CS8604, CS1591
public class BoxModelCalculator
{
    public float MarginTop, MarginRight, MarginBottom, MarginLeft;
    public float BorderTop, BorderRight, BorderBottom, BorderLeft;
    public float PaddingTop, PaddingRight, PaddingBottom, PaddingLeft;
    public float ContentWidth, ContentHeight;
    public float BoxWidth, BoxHeight;

    public BoxModelCalculator(ICssStyleDeclaration style, float availableWidth)
    {
        // Parse margins
        MarginTop = ParsePx(style, "margin-top");
        MarginRight = ParsePx(style, "margin-right");
        MarginBottom = ParsePx(style, "margin-bottom");
        MarginLeft = ParsePx(style, "margin-left");

        // Parse borders
        BorderTop = ParsePx(style, "border-top-width");
        BorderRight = ParsePx(style, "border-right-width");
        BorderBottom = ParsePx(style, "border-bottom-width");
        BorderLeft = ParsePx(style, "border-left-width");

        // Parse paddings
        PaddingTop = ParsePx(style, "padding-top");
        PaddingRight = ParsePx(style, "padding-right");
        PaddingBottom = ParsePx(style, "padding-bottom");
        PaddingLeft = ParsePx(style, "padding-left");

        // Compute content dimensions
        ContentWidth = ComputeWidth(style, availableWidth);
        ContentHeight = ComputeHeight(style);

        // Compute full box size
        BoxWidth = ContentWidth + PaddingLeft + PaddingRight + BorderLeft + BorderRight;
        BoxHeight = ContentHeight + PaddingTop + PaddingBottom + BorderTop + BorderBottom;
    }

    /// <summary>
    /// Computes the content width based on the specified width and box-sizing.
    /// </summary>
    private float ComputeWidth(ICssStyleDeclaration style, float availableWidth)
    {
        // Get the specified width.
        float specifiedWidth = ParsePx(style, "width");

        // Check the box-sizing property.
        bool isBorderBox = style.GetPropertyValue("box-sizing") == "border-box";

        if (specifiedWidth > 0)
        {
            if (isBorderBox)
            {
                // For border-box, the specified width includes padding and border.
                // Subtract them to get the content width.
                float computed = specifiedWidth - (PaddingLeft + PaddingRight + BorderLeft + BorderRight);
                return computed > 0 ? computed : 0;
            }
            else
            {
                // For content-box, the specified width is the content width.
                return specifiedWidth;
            }
        }
        else
        {
            // If width is "auto", use the parent's available width.
            if (isBorderBox)
            {
                float computed = availableWidth - (PaddingLeft + PaddingRight + BorderLeft + BorderRight);
                return computed > 0 ? computed : 0;
            }

            return availableWidth;
        }
    }

    /// <summary>
    /// Computes the content height based on the specified height and box-sizing.
    /// </summary>
    private float ComputeHeight(ICssStyleDeclaration style)
    {
        float specifiedHeight = ParsePx(style, "height");
        bool isBorderBox = style.GetPropertyValue("box-sizing") == "border-box";

        if (specifiedHeight > 0)
        {
            if (isBorderBox)
            {
                float computed = specifiedHeight - (PaddingTop + PaddingBottom + BorderTop + BorderBottom);
                return computed > 0 ? computed : 0;
            }
            else
            {
                return specifiedHeight;
            }
        }
        else
        {
            // For "auto" height, we signal that the content height will be determined by the content.
            return float.NaN;
        }
    }

    private static float ParsePx(ICssStyleDeclaration style, string property)
    {
        var raw = style.GetProperty(property)?.RawValue;
        return raw is CssLengthValue lv ? (float)lv.Value : 0f;
    }
}