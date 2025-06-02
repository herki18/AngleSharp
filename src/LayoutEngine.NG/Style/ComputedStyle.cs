namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;
using System.Collections.Generic;
using AngleSharp.Css.Values;

/// <summary>
/// CSS white-space property values.
/// </summary>
public enum WhiteSpaceType
{
    Normal,
    NoWrap,
    Pre,
    PreWrap,
    PreLine,
    BreakSpaces
}

/// <summary>
/// Represents the computed CSS style for a node, following BlinkNG's ComputedStyle.
/// In BlinkNG, ComputedStyle is immutable after style recalc and contains all resolved CSS values.
/// </summary>
public class ComputedStyle
{
    private readonly Dictionary<string, object> _properties = new();

    /// <summary>
    /// The DOM element associated with this computed style.
    /// In BlinkNG, this connection helps with inheritance and invalidation.
    /// </summary>
    public IElement? Element { get; set; }

    /// <summary>
    /// Parent computed style for inheritance.
    /// In BlinkNG, many properties inherit from parent styles.
    /// </summary>
    public ComputedStyle? ParentComputedStyle { get; set; }

    /// <summary>
    /// Display type following CSS Display Module.
    /// In BlinkNG, this determines the layout algorithm to use.
    /// </summary>
    public DisplayType Display { get; set; } = DisplayType.Block;

    /// <summary>
    /// Position type following CSS Positioning Module.
    /// </summary>
    public PositionType Position { get; set; } = PositionType.Static;

    /// <summary>
    /// White-space handling for text content.
    /// In BlinkNG, affects text layout and whitespace collapsing.
    /// </summary>
    public WhiteSpaceType WhiteSpace { get; set; } = WhiteSpaceType.Normal;

    /// <summary>
    /// Whether this style creates a new stacking context.
    /// In BlinkNG, computed from opacity, transform, and other properties.
    /// </summary>
    public bool CreatesStackingContext { get; set; }

    /// <summary>
    /// Whether this style creates a new containing block.
    /// In BlinkNG, computed from position and other properties.
    /// </summary>
    public bool CreatesContainingBlock { get; set; }

    /// <summary>
    /// Whether this element needs layout.
    /// In BlinkNG, this is part of the dirty bit tracking.
    /// </summary>
    public bool NeedsLayout { get; set; }

    public LengthBox Margin { get; set; } = LengthBox.Zero;
    public LengthBox Padding { get; set; } = LengthBox.Zero;
    public BorderBox Border { get; set; } = BorderBox.Zero;
    public CssColorValue BackgroundColor { get; set; } = CssColorValue.Transparent;
    public CssColorValue Color { get; set; } = CssColorValue.Black;

    /// <summary>
    /// Gets a CSS property value by name.
    /// In BlinkNG, this would access the internal property storage.
    /// </summary>
    public object? GetPropertyValue(string propertyName)
    {
        return _properties.TryGetValue(propertyName, out var value) ? value : null;
    }

    /// <summary>
    /// Sets a CSS property value.
    /// Note: In real BlinkNG, ComputedStyle is immutable after creation.
    /// This is simplified for the skeleton implementation.
    /// </summary>
    public void SetPropertyValue(string propertyName, object value)
    {
        _properties[propertyName] = value;
    }

    /// <summary>
    /// Creates a copy of this ComputedStyle.
    /// In BlinkNG, used for style sharing and caching.
    /// </summary>
    public ComputedStyle Clone()
    {
        var clone = new ComputedStyle
        {
            Element = Element,
            ParentComputedStyle = ParentComputedStyle,
            Display = Display,
            Position = Position,
            WhiteSpace = WhiteSpace,
            CreatesStackingContext = CreatesStackingContext,
            CreatesContainingBlock = CreatesContainingBlock,
            NeedsLayout = NeedsLayout,
            Margin = Margin,
            Padding = Padding,
            Border = Border,
            BackgroundColor = BackgroundColor,
            Color = Color
        };

        foreach (var kvp in _properties)
        {
            clone._properties[kvp.Key] = kvp.Value;
        }

        return clone;
    }

    /// <summary>
    /// Checks if this style can be shared with another element.
    /// In BlinkNG, style sharing is an important optimization.
    /// </summary>
    public bool CanShare(IElement element)
    {
        // Skeleton implementation - actual logic would be complex
        return false;
    }

    // Container query dependency methods (simplified stubs)
    public bool DependsOnSizeContainerQueries() => false;
    public bool DependsOnStyleContainerQueries() => false;
    public bool DependsOnScrollStateContainerQueries() => false;
    public bool DependsOnAnchoredContainerQueries() => false;
    public bool HighlightPseudoElementStylesDependOnContainerUnits() => false;
    public bool CanMatchSizeContainerQueries(IElement element) => false;
}

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

    public static LengthBox Zero => new(CssLengthValue.Zero, CssLengthValue.Zero, CssLengthValue.Zero, CssLengthValue.Zero);
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

    public static BorderBox Zero => new(CssLengthValue.Zero, CssLengthValue.Zero, CssLengthValue.Zero, CssLengthValue.Zero);
}