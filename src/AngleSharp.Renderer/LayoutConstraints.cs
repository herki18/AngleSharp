namespace AngleSharp.Renderer;

using System;
using Css.Dom;
using Css.Values;

#pragma warning disable CS8604, CS1591
public class LayoutConstraints
{
    public float MinWidth, MaxWidth, MinHeight, MaxHeight;

    public LayoutConstraints(ICssStyleDeclaration style)
    {
        // For min-width/min-height, default is 0.
        MinWidth = ParsePx(style, "min-width");
        MinHeight = ParsePx(style, "min-height");

        // For max-width/max-height, default to PositiveInfinity if not set or "auto".
        MaxWidth = ParsePxOrAuto(style, "max-width");
        MaxHeight = ParsePxOrAuto(style, "max-height");
    }

    public float ApplyWidth(float width)
    {
        return Math.Clamp(width, MinWidth, MaxWidth);
    }

    public float ApplyHeight(float height)
    {
        return Math.Clamp(height, MinHeight, MaxHeight);
    }

    private static float ParsePx(ICssStyleDeclaration style, string property)
    {
        var raw = style.GetProperty(property)?.RawValue;
        return raw is CssLengthValue lv ? (float)lv.Value : 0f;
    }

    private static float ParsePxOrAuto(ICssStyleDeclaration style, string property)
    {
        var raw = style.GetProperty(property)?.RawValue;
        // If the property is specified and is a length, return its value.
        if (raw is CssLengthValue lv)
            return (float)lv.Value;
        // Otherwise (unspecified or "auto"), treat it as no maximum.
        return float.PositiveInfinity;
    }
}