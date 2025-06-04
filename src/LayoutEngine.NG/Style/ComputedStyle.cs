namespace LayoutEngine.NG.Style;

using System;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using Layout;
using ScrollSnapAlign = AngleSharp.Css.Dom.ScrollSnapAlign;

/// <summary>
/// ComputedStyle is an immutable, typed, read-only wrapper around AngleSharp's ICssStyleDeclaration.
/// It provides ergonomic, browser-like access to computed CSS property values for a DOM element.
///
/// - All property values are resolved via AngleSharp's cascade, inheritance, and initial value logic.
/// - No property is ever missing: if not set by cascade or inheritance, the CSS initial value is returned.
/// - Typed accessors (e.g., Display, Position, Color, Margin) are provided for engine ergonomics.
/// - The underlying ICssStyleDeclaration is exposed via the Raw property for advanced use.
/// - This class is fully immutable and should not be mutated after construction.
/// </summary>
public class ComputedStyle
{
    private readonly ICssStyleDeclaration _declaration;

    public ComputedStyle(ICssStyleDeclaration declaration)
    {
        _declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
    }

    /// <summary>
    /// Exposes the underlying ICssStyleDeclaration for advanced use.
    /// </summary>
    public ICssStyleDeclaration Raw => _declaration;

    /// <summary>
    /// Gets the display type as AngleSharp's DisplayMode enum.
    /// </summary>
    public DisplayMode Display
    {
        get
        {
            var value = _declaration.GetPropertyValue(PropertyNames.Display);
            if (Enum.TryParse<DisplayMode>(value, true, out var result))
                return result;
            return DisplayMode.Inline;
        }
    }

    /// <summary>
    /// Gets the position type as AngleSharp's PositionMode enum.
    /// </summary>
    public PositionMode Position
    {
        get
        {
            var value = _declaration.GetPropertyValue(PropertyNames.Position);
            if (Enum.TryParse<PositionMode>(value, true, out var result))
                return result;
            return PositionMode.Static;
        }
    }

    /// <summary>
    /// Gets the color as a CssColorValue, if possible.
    /// </summary>
    public CssColorValue Color
    {
        get
        {
            var prop = _declaration.GetProperty(PropertyNames.Color);
            if (prop?.RawValue is CssColorValue color)
                return color;
            return CssColorValue.Black;
        }
    }

    /// <summary>
    /// Gets the background color as a CssColorValue, if possible.
    /// </summary>
    public CssColorValue BackgroundColor
    {
        get
        {
            var prop = _declaration.GetProperty(PropertyNames.BackgroundColor);
            if (prop?.RawValue is CssColorValue color)
                return color;
            return CssColorValue.Transparent;
        }
    }

    public Whitespace WhiteSpace
    {
        get
        {
            var value = _declaration.GetPropertyValue("white-space");
            if (Enum.TryParse<Whitespace>(value, true, out var result))
                return result;
            return Whitespace.Normal;
        }
    }

    /// <summary>
    /// Gets the margin as a LengthBox (top, right, bottom, left).
    /// </summary>
    public LengthBox Margin => GetBox(PropertyNames.Margin);

    /// <summary>
    /// Gets the padding as a LengthBox (top, right, bottom, left).
    /// </summary>
    public LengthBox Padding => GetBox(PropertyNames.Padding);

    /// <summary>
    /// Gets the border width as a BorderBox (top, right, bottom, left).
    /// </summary>
    public BorderBox Border => GetBorderBox();

    private LengthBox GetBox(string property)
    {
        var top = GetLength($"{property}-top");
        var right = GetLength($"{property}-right");
        var bottom = GetLength($"{property}-bottom");
        var left = GetLength($"{property}-left");
        return new LengthBox(top, right, bottom, left);
    }

    private BorderBox GetBorderBox()
    {
        var top = GetLength("border-top-width");
        var right = GetLength("border-right-width");
        var bottom = GetLength("border-bottom-width");
        var left = GetLength("border-left-width");
        return new BorderBox(top, right, bottom, left);
    }

    private CssLengthValue GetLength(string property)
    {
        var prop = _declaration.GetProperty(property);
        if (prop?.RawValue is CssLengthValue len)
            return len;
        var str = _declaration.GetPropertyValue(property);
        if (CssLengthValue.TryParse(str, out var parsed))
            return parsed;
        return CssLengthValue.Zero;
    }

    /// <summary>
    /// Gets a raw property value by name.
    /// </summary>
    public string? GetPropertyValue(string propertyName) => _declaration.GetPropertyValue(propertyName);

    /// <summary>
    /// Gets a raw property object by name.
    /// </summary>
    public ICssProperty? GetProperty(string propertyName) => _declaration.GetProperty(propertyName);

    public bool HasStickyConstrainedPosition()
    {
        // TODO: Implement HasStickyConstrainedPosition
        return false;
    }

    public ScrollSnapAlign GetScrollSnapAlign()
    {
        // TODO: Implement GetScrollSnapAlign
        return ScrollSnapAlign.None;
    }

    public string? AnchorName()
    {
        // TODO: Implement AnchorName
        return null;
    }

    public bool IsHorizontalWritingMode()
    {
        // TODO: Implement IsHorizontalWritingMode
        return true;
    }

    public WritingDirection GetWritingDirection()
    {
        // TODO: Implement GetWritingDirection
        return new WritingDirection();
    }
}

// Example LengthBox and BorderBox structs (adjust as needed for your codebase)
public struct LengthBox
{
    public CssLengthValue Top { get; }
    public CssLengthValue Right { get; }
    public CssLengthValue Bottom { get; }
    public CssLengthValue Left { get; }

    public LengthBox(CssLengthValue top, CssLengthValue right, CssLengthValue bottom, CssLengthValue left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public static LengthBox Zero =>
        new(CssLengthValue.Zero, CssLengthValue.Zero, CssLengthValue.Zero, CssLengthValue.Zero);
}

public struct BorderBox
{
    public CssLengthValue Top { get; }
    public CssLengthValue Right { get; }
    public CssLengthValue Bottom { get; }
    public CssLengthValue Left { get; }

    public BorderBox(CssLengthValue top, CssLengthValue right, CssLengthValue bottom, CssLengthValue left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public static BorderBox Zero =>
        new(CssLengthValue.Zero, CssLengthValue.Zero, CssLengthValue.Zero, CssLengthValue.Zero);
}