namespace AngleSharp.StyleSystem;

using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using LayoutEngine.StyleSystem;
using TextAlign = Css.Dom.TextAlign;

/// <summary>
/// A high-performance representation of computed style values optimized for layout.
/// </summary>
public class ComputedStyle : IComputedStyle
{
    #region Fields

    private readonly BoxProperties _boxProperties;
    private readonly TextProperties _textProperties;
    private readonly RareProperties _rareProperties;
    private readonly SurrogateBitfields _bitfields;
    private readonly PropertyTreeNode _propertyTree;
    private readonly IElement _element;
    private readonly IComputedStyle _parentStyle;
    private readonly WritingMode _writingMode;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new computed style.
    /// </summary>
    /// <param name="element">The element this style applies to.</param>
    /// <param name="parentStyle">The parent element's computed style.</param>
    /// <param name="declaration">The CSS declaration containing the style properties.</param>
    /// <param name="propertyTree">The property tree node for shared style storage.</param>
    public ComputedStyle(IElement element, IComputedStyle parentStyle, ICssStyleDeclaration declaration, PropertyTreeNode propertyTree)
    {
        _element = element;
        _parentStyle = parentStyle;
        _propertyTree = propertyTree;

        // Initialize property groups
        _boxProperties = new BoxProperties(this);
        _textProperties = new TextProperties(this);
        _rareProperties = new RareProperties(this);
        _bitfields = new SurrogateBitfields();

        // Compute writing mode early as it affects property mapping
        _writingMode = ComputeWritingMode(declaration);

        // Process style properties
        ProcessStyleProperties(declaration);
    }

    #endregion

    #region IComputedStyle Interface

    /// <summary>
    /// Gets a computed value by property name.
    /// </summary>
    public string GetPropertyValue(string propertyName)
    {
        return _propertyTree.GetPropertyValue(propertyName);
    }

    /// <summary>
    /// Gets a typed value for a specific property.
    /// </summary>
    public T GetValue<T>(string propertyName)
    {
        var value = _propertyTree.GetPropertyCachedValue(propertyName);

        if (value is T typedValue)
        {
            return typedValue;
        }

        if (typeof(T) == typeof(CssLengthValue) && value is ICssValue cssValue)
        {
            if (CssLengthValue.TryParse(cssValue.CssText, out var length))
            {
                return (T)(object)length;
            }
        }

        // Add more type conversion logic as needed

        return default;
    }

    /// <summary>
    /// Gets the display type of the element.
    /// </summary>
    public DisplayType Display => _bitfields.DisplayType;

    /// <summary>
    /// Gets the position type of the element.
    /// </summary>
    public PositionType Position => _bitfields.PositionType;

    /// <summary>
    /// Gets the computed value of the opacity property.
    /// </summary>
    public float Opacity => _rareProperties.Opacity;

    /// <summary>
    /// Gets the computed value of the z-index property.
    /// </summary>
    public int ZIndex => _rareProperties.ZIndex;

    /// <summary>
    /// Gets the computed value of the font-size property.
    /// </summary>
    public CssLengthValue FontSize => _textProperties.FontSize;

    /// <summary>
    /// Gets the writing mode for the element.
    /// </summary>
    public WritingMode WritingMode => _writingMode;

    /// <summary>
    /// Gets box-related properties.
    /// </summary>
    public IBoxProperties Box => _boxProperties;

    /// <summary>
    /// Gets text-related properties.
    /// </summary>
    public ITextProperties Text => _textProperties;

    #endregion

    #region Implementation Methods

    /// <summary>
    /// Processes all style properties from the declaration.
    /// </summary>
    private void ProcessStyleProperties(ICssStyleDeclaration declaration)
    {
        // Parse and compute values for all properties
        foreach (var property in declaration.Declarations)
        {
            ProcessProperty(property);
        }

        // Apply inheritance after all properties are processed
        if (_parentStyle != null)
        {
            ApplyInheritance();
        }
        else
        {
            ApplyInitialValues();
        }
    }

    /// <summary>
    /// Processes a single CSS property.
    /// </summary>
    private void ProcessProperty(ICssProperty property)
    {
        // Store property in the property tree
        _propertyTree.SetProperty(property.Name, property.RawValue);

        // Update specialized property groups
        switch (property.Name)
        {
            // Display and positioning properties
            case "display":
                _bitfields.DisplayType = ParseDisplayType(property.Value);
                break;
            case "position":
                _bitfields.PositionType = ParsePositionType(property.Value);
                break;

            // Box properties
            case "width":
            case "height":
            case "margin-top":
            case "margin-right":
            case "margin-bottom":
            case "margin-left":
            case "padding-top":
            case "padding-right":
            case "padding-bottom":
            case "padding-left":
            case "border-top-width":
            case "border-right-width":
            case "border-bottom-width":
            case "border-left-width":
                ProcessBoxProperty(property);
                break;

            // Text properties
            case "font-family":
            case "font-size":
            case "font-weight":
            case "font-style":
            case "line-height":
            case "text-align":
            case "color":
                ProcessTextProperty(property);
                break;

            // Other properties
            case "opacity":
                if (float.TryParse(property.Value, out var opacity))
                {
                    _rareProperties.Opacity = opacity;
                }
                break;
            case "z-index":
                if (int.TryParse(property.Value, out var zIndex))
                {
                    _rareProperties.ZIndex = zIndex;
                }
                break;
        }
    }

    /// <summary>
    /// Processes box-related properties.
    /// </summary>
    private void ProcessBoxProperty(ICssProperty property)
    {
        if (CssLengthValue.TryParse(property.Value, out var length))
        {
            switch (property.Name)
            {
                case "width":
                    _boxProperties.SetWidth(length);
                    break;
                case "height":
                    _boxProperties.SetHeight(length);
                    break;
                case "margin-top":
                    _boxProperties.SetMarginTop(length);
                    break;
                case "margin-right":
                    _boxProperties.SetMarginRight(length);
                    break;
                case "margin-bottom":
                    _boxProperties.SetMarginBottom(length);
                    break;
                case "margin-left":
                    _boxProperties.SetMarginLeft(length);
                    break;
                // Add more box properties as needed
            }
        }
    }

    /// <summary>
    /// Processes text-related properties.
    /// </summary>
    private void ProcessTextProperty(ICssProperty property)
    {
        switch (property.Name)
        {
            case "font-family":
                _textProperties.SetFontFamily(property.Value);
                break;
            case "font-size":
                if (CssLengthValue.TryParse(property.Value, out var fontSize))
                {
                    _textProperties.SetFontSize(fontSize);
                }
                break;
            case "font-weight":
                if (int.TryParse(property.Value, out var fontWeight))
                {
                    _textProperties.SetFontWeight(fontWeight);
                }
                else if (property.Value == "bold")
                {
                    _textProperties.SetFontWeight(700);
                }
                else if (property.Value == "normal")
                {
                    _textProperties.SetFontWeight(400);
                }
                break;
            case "font-style":
                _textProperties.SetIsItalic(property.Value == "italic");
                break;
            case "text-align":
                _textProperties.SetTextAlign(ParseTextAlign(property.Value));
                break;
            case "color":
                _textProperties.SetColor(property.Value);
                break;
            // Add more text properties as needed
        }
    }

    /// <summary>
    /// Computes the writing mode from the style declaration.
    /// </summary>
    private WritingMode ComputeWritingMode(ICssStyleDeclaration declaration)
    {
        var direction = Direction.Ltr;
        var mode = WritingModeType.HorizontalTopToBottom;

        var directionValue = declaration.GetPropertyValue("direction");
        if (directionValue == "rtl")
        {
            direction = Direction.Rtl;
        }

        var writingModeValue = declaration.GetPropertyValue("writing-mode");
        if (!string.IsNullOrEmpty(writingModeValue))
        {
            switch (writingModeValue)
            {
                case "vertical-rl":
                    mode = WritingModeType.VerticalRightToLeft;
                    break;
                case "vertical-lr":
                    mode = WritingModeType.VerticalLeftToRight;
                    break;
                case "sideways-rl":
                    mode = WritingModeType.SidewaysRightToLeft;
                    break;
                case "sideways-lr":
                    mode = WritingModeType.SidewaysLeftToRight;
                    break;
            }
        }

        return new WritingMode(direction, mode);
    }

    /// <summary>
    /// Applies inherited properties from the parent style.
    /// </summary>
    private void ApplyInheritance()
    {
        if (_parentStyle == null)
            return;

        // Inherit text properties if not specified
        if (!_propertyTree.HasProperty("color"))
        {
            _textProperties.SetColor(_parentStyle.Text.Color.CssText);
        }

        if (!_propertyTree.HasProperty("font-family"))
        {
            _textProperties.SetFontFamily(_parentStyle.Text.FontFamily);
        }

        // Inherit other inherited properties as needed
        var inheritedProperties = new[]
        {
            "line-height",
            "font-weight",
            "font-style",
            "text-align",
            // Add more inherited properties as needed
        };

        foreach (var property in inheritedProperties)
        {
            if (!_propertyTree.HasProperty(property))
            {
                _propertyTree.SetProperty(property, _parentStyle.GetPropertyValue(property));
            }
        }
    }

    /// <summary>
    /// Applies initial values for properties when no parent style exists.
    /// </summary>
    private void ApplyInitialValues()
    {
        // Set initial values for properties that need them
        if (!_propertyTree.HasProperty("color"))
        {
            _textProperties.SetColor("black");
        }

        if (!_propertyTree.HasProperty("font-family"))
        {
            _textProperties.SetFontFamily("sans-serif");
        }

        if (!_propertyTree.HasProperty("font-size"))
        {
            _textProperties.SetFontSize(CssLengthValue.Medium);
        }

        // Add more initial values as needed
    }

    /// <summary>
    /// Parses a display property value into a DisplayType enum.
    /// </summary>
    private DisplayType ParseDisplayType(string value)
    {
        return value switch
        {
            "none" => DisplayType.None,
            "block" => DisplayType.Block,
            "inline" => DisplayType.Inline,
            "inline-block" => DisplayType.InlineBlock,
            "flex" => DisplayType.Flex,
            "grid" => DisplayType.Grid,
            "table" => DisplayType.Table,
            _ => DisplayType.Block // Default
        };
    }

    /// <summary>
    /// Parses a position property value into a PositionType enum.
    /// </summary>
    private PositionType ParsePositionType(string value)
    {
        return value switch
        {
            "static" => PositionType.Static,
            "relative" => PositionType.Relative,
            "absolute" => PositionType.Absolute,
            "fixed" => PositionType.Fixed,
            "sticky" => PositionType.Sticky,
            _ => PositionType.Static // Default
        };
    }

    /// <summary>
    /// Parses a text-align property value into a TextAlign enum.
    /// </summary>
    private TextAlign ParseTextAlign(string value)
    {
        return value switch
        {
            "left" => TextAlign.Left,
            "right" => TextAlign.Right,
            "center" => TextAlign.Center,
            "justify" => TextAlign.Justify,
            "start" => TextAlign.Start,
            "end" => TextAlign.End,
            _ => TextAlign.Start // Default
        };
    }

    #endregion
}

/// <summary>
/// Represents box-related computed properties.
/// </summary>
public class BoxProperties : IBoxProperties
{
    private readonly ComputedStyle _owner;

    // Box dimensions
    private CssLengthValue _width = CssLengthValue.Auto;
    private CssLengthValue _height = CssLengthValue.Auto;

    // Margin edges
    private CssLengthValue _marginTop = CssLengthValue.Zero;
    private CssLengthValue _marginRight = CssLengthValue.Zero;
    private CssLengthValue _marginBottom = CssLengthValue.Zero;
    private CssLengthValue _marginLeft = CssLengthValue.Zero;

    // Border edges
    private CssLengthValue _borderTop = CssLengthValue.Zero;
    private CssLengthValue _borderRight = CssLengthValue.Zero;
    private CssLengthValue _borderBottom = CssLengthValue.Zero;
    private CssLengthValue _borderLeft = CssLengthValue.Zero;

    // Padding edges
    private CssLengthValue _paddingTop = CssLengthValue.Zero;
    private CssLengthValue _paddingRight = CssLengthValue.Zero;
    private CssLengthValue _paddingBottom = CssLengthValue.Zero;
    private CssLengthValue _paddingLeft = CssLengthValue.Zero;

    public BoxProperties(ComputedStyle owner)
    {
        _owner = owner;
    }

    public CssLengthValue Width => _width;
    public CssLengthValue Height => _height;

    public CssLengthValue InlineSize => _owner.WritingMode.IsHorizontal ? _width : _height;
    public CssLengthValue BlockSize => _owner.WritingMode.IsHorizontal ? _height : _width;

    public Edges Margin => new Edges(_marginTop, _marginRight, _marginBottom, _marginLeft);
    public Edges Border => new Edges(_borderTop, _borderRight, _borderBottom, _borderLeft);
    public Edges Padding => new Edges(_paddingTop, _paddingRight, _paddingBottom, _paddingLeft);

    public LogicalEdges LogicalMargin => new LogicalEdges(
        _owner.WritingMode.IsHorizontal ? _marginTop : _marginLeft,
        _owner.WritingMode.IsHorizontal ?
            (_owner.WritingMode.IsRightToLeft ? _marginLeft : _marginRight) :
            (_owner.WritingMode.IsRightToLeft ? _marginBottom : _marginTop),
        _owner.WritingMode.IsHorizontal ? _marginBottom : _marginRight,
        _owner.WritingMode.IsHorizontal ?
            (_owner.WritingMode.IsRightToLeft ? _marginRight : _marginLeft) :
            (_owner.WritingMode.IsRightToLeft ? _marginTop : _marginBottom)
    );

    public LogicalEdges LogicalBorder => new LogicalEdges(
        _owner.WritingMode.IsHorizontal ? _borderTop : _borderLeft,
        _owner.WritingMode.IsHorizontal ?
            (_owner.WritingMode.IsRightToLeft ? _borderLeft : _borderRight) :
            (_owner.WritingMode.IsRightToLeft ? _borderBottom : _borderTop),
        _owner.WritingMode.IsHorizontal ? _borderBottom : _borderRight,
        _owner.WritingMode.IsHorizontal ?
            (_owner.WritingMode.IsRightToLeft ? _borderRight : _borderLeft) :
            (_owner.WritingMode.IsRightToLeft ? _borderTop : _borderBottom)
    );

    public LogicalEdges LogicalPadding => new LogicalEdges(
        _owner.WritingMode.IsHorizontal ? _paddingTop : _paddingLeft,
        _owner.WritingMode.IsHorizontal ?
            (_owner.WritingMode.IsRightToLeft ? _paddingLeft : _paddingRight) :
            (_owner.WritingMode.IsRightToLeft ? _paddingBottom : _paddingTop),
        _owner.WritingMode.IsHorizontal ? _paddingBottom : _paddingRight,
        _owner.WritingMode.IsHorizontal ?
            (_owner.WritingMode.IsRightToLeft ? _paddingRight : _paddingLeft) :
            (_owner.WritingMode.IsRightToLeft ? _paddingTop : _paddingBottom)
    );

    public void SetWidth(CssLengthValue width) => _width = width;
    public void SetHeight(CssLengthValue height) => _height = height;
    public void SetMarginTop(CssLengthValue value) => _marginTop = value;
    public void SetMarginRight(CssLengthValue value) => _marginRight = value;
    public void SetMarginBottom(CssLengthValue value) => _marginBottom = value;
    public void SetMarginLeft(CssLengthValue value) => _marginLeft = value;
    public void SetBorderTop(CssLengthValue value) => _borderTop = value;
    public void SetBorderRight(CssLengthValue value) => _borderRight = value;
    public void SetBorderBottom(CssLengthValue value) => _borderBottom = value;
    public void SetBorderLeft(CssLengthValue value) => _borderLeft = value;
    public void SetPaddingTop(CssLengthValue value) => _paddingTop = value;
    public void SetPaddingRight(CssLengthValue value) => _paddingRight = value;
    public void SetPaddingBottom(CssLengthValue value) => _paddingBottom = value;
    public void SetPaddingLeft(CssLengthValue value) => _paddingLeft = value;
}

/// <summary>
/// Represents text-related computed properties.
/// </summary>
public class TextProperties : ITextProperties
{
    private string _fontFamily = "sans-serif";
    private CssLengthValue _fontSize = CssLengthValue.Medium;
    private int _fontWeight = 400;
    private bool _isItalic = false;
    private CssLengthValue _lineHeight = CssLengthValue.Normal;
    private TextAlign _textAlign = TextAlign.Start;
    private Color _color = new Color(0, 0, 0, 1.0f);

    public TextProperties(ComputedStyle owner)
    {
    }

    public string FontFamily => _fontFamily;
    public int FontWeight => _fontWeight;
    public bool IsItalic => _isItalic;
    public CssLengthValue LineHeight => _lineHeight;
    public TextAlign TextAlign => _textAlign;
    public Color Color => _color;
    public CssLengthValue FontSize => _fontSize;

    public void SetFontFamily(string fontFamily) => _fontFamily = fontFamily;
    public void SetFontSize(CssLengthValue fontSize) => _fontSize = fontSize;
    public void SetFontWeight(int weight) => _fontWeight = weight;
    public void SetIsItalic(bool isItalic) => _isItalic = isItalic;
    public void SetLineHeight(CssLengthValue lineHeight) => _lineHeight = lineHeight;
    public void SetTextAlign(TextAlign textAlign) => _textAlign = textAlign;
    public void SetColor(string colorStr) => _color = ParseColor(colorStr);

    private Color ParseColor(string colorStr)
    {
        // Simple color parsing for common named colors
        return colorStr switch
        {
            "black" => new Color(0, 0, 0, 1.0f),
            "white" => new Color(255, 255, 255, 1.0f),
            "red" => new Color(255, 0, 0, 1.0f),
            "green" => new Color(0, 128, 0, 1.0f),
            "blue" => new Color(0, 0, 255, 1.0f),
            // In a real implementation, parse RGB, RGBA, HSL, etc.
            _ => new Color(0, 0, 0, 1.0f) // Default to black
        };
    }
}

/// <summary>
/// Represents less-commonly used computed properties.
/// </summary>
public class RareProperties
{
    public float Opacity { get; set; } = 1.0f;
    public int ZIndex { get; set; } = 0;
    // Add other rare properties as needed
}

/// <summary>
/// Uses bitfields to store boolean flags and enumeration values efficiently.
/// </summary>
public class SurrogateBitfields
{
    // Store enums
    public DisplayType DisplayType { get; set; } = DisplayType.Block;
    public PositionType PositionType { get; set; } = PositionType.Static;

    // Boolean flags packed into bits
    private uint _flags;

    // Flag positions for various boolean properties
    private const int IsVisibleFlag = 0;
    private const int HasTransformFlag = 1;
    private const int IsFloatingFlag = 2;
    private const int IsAbsolutelyPositionedFlag = 3;
    // Add more flags as needed

    public bool IsVisible
    {
        get => GetFlag(IsVisibleFlag);
        set => SetFlag(IsVisibleFlag, value);
    }

    public bool HasTransform
    {
        get => GetFlag(HasTransformFlag);
        set => SetFlag(HasTransformFlag, value);
    }

    public bool IsFloating
    {
        get => GetFlag(IsFloatingFlag);
        set => SetFlag(IsFloatingFlag, value);
    }

    public bool IsAbsolutelyPositioned
    {
        get => GetFlag(IsAbsolutelyPositionedFlag);
        set => SetFlag(IsAbsolutelyPositionedFlag, value);
    }

    private bool GetFlag(int position) => (_flags & (1U << position)) != 0;

    private void SetFlag(int position, bool value)
    {
        if (value)
            _flags |= (1U << position);
        else
            _flags &= ~(1U << position);
    }
}

/// <summary>
/// Represents edges (margin, border, padding).
/// </summary>
public readonly struct Edges
{
    /// <summary>
    /// Gets the top edge.
    /// </summary>
    public CssLengthValue Top { get; }

    /// <summary>
    /// Gets the right edge.
    /// </summary>
    public CssLengthValue Right { get; }

    /// <summary>
    /// Gets the bottom edge.
    /// </summary>
    public CssLengthValue Bottom { get; }

    /// <summary>
    /// Gets the left edge.
    /// </summary>
    public CssLengthValue Left { get; }

    public Edges(CssLengthValue top, CssLengthValue right, CssLengthValue bottom, CssLengthValue left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }
}

/// <summary>
/// Represents logical edges independent of writing mode.
/// </summary>
public readonly struct LogicalEdges
{
    /// <summary>
    /// Gets the block-start edge.
    /// </summary>
    public CssLengthValue BlockStart { get; }

    /// <summary>
    /// Gets the inline-end edge.
    /// </summary>
    public CssLengthValue InlineEnd { get; }

    /// <summary>
    /// Gets the block-end edge.
    /// </summary>
    public CssLengthValue BlockEnd { get; }

    /// <summary>
    /// Gets the inline-start edge.
    /// </summary>
    public CssLengthValue InlineStart { get; }

    public LogicalEdges(CssLengthValue blockStart, CssLengthValue inlineEnd,
        CssLengthValue blockEnd, CssLengthValue inlineStart)
    {
        BlockStart = blockStart;
        InlineEnd = inlineEnd;
        BlockEnd = blockEnd;
        InlineStart = inlineStart;
    }

    /// <summary>
    /// Converts to physical edges based on writing mode.
    /// </summary>
    public Edges ToPhysical(WritingMode writingMode)
    {
        if (writingMode.IsHorizontal)
        {
            return writingMode.IsRightToLeft
                ? new Edges(BlockStart, InlineStart, BlockEnd, InlineEnd)
                : new Edges(BlockStart, InlineEnd, BlockEnd, InlineStart);
        }
        else
        {
            return writingMode.IsRightToLeft
                ? new Edges(InlineEnd, BlockEnd, InlineStart, BlockStart)
                : new Edges(InlineStart, BlockEnd, InlineEnd, BlockStart);
        }
    }
}

/// <summary>
/// Represents a color value.
/// </summary>
public readonly struct Color
{
    /// <summary>
    /// Gets the red component (0-255).
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Gets the green component (0-255).
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Gets the blue component (0-255).
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Gets the alpha component (0-1).
    /// </summary>
    public float A { get; }

    public Color(byte r, byte g, byte b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public string CssText
    {
        get
        {
            if (A < 1.0f)
            {
                return $"rgba({R}, {G}, {B}, {A})";
            }
            return $"rgb({R}, {G}, {B})";
        }
    }
}