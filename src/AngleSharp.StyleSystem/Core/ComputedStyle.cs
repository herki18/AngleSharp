namespace AngleSharp.StyleSystem.Core;

using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using Css;
using Interfaces;

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
    private readonly IComputedStyle? _parentStyle;
    private readonly WritingMode _writingMode;
    private readonly IRenderDevice _renderDevice;
    private readonly IStyleInvalidationTracker _invalidationTracker;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new computed style.
    /// </summary>
    /// <param name="element">The element this style applies to.</param>
    /// <param name="parentStyle">The parent element's computed style.</param>
    /// <param name="declaration">The CSS declaration containing the style properties.</param>
    /// <param name="propertyTree">The property tree node for shared style storage.</param>
    public ComputedStyle(IElement element, IComputedStyle? parentStyle, ICssStyleDeclaration declaration, PropertyTreeNode propertyTree, IRenderDevice renderDevice, IStyleInvalidationTracker invalidationTracker)
    {
        _element = element;
        _parentStyle = parentStyle;
        _propertyTree = propertyTree;
        _renderDevice = renderDevice;
        _invalidationTracker = invalidationTracker;

        // Initialize property groups
        _boxProperties = new BoxProperties(this, _renderDevice);
        _textProperties = new TextProperties(this, _renderDevice);
        _rareProperties = new RareProperties();
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
    public T? GetValue<T>(string propertyName)
    {
        var value = _propertyTree.GetPropertyCachedValue(propertyName);

        if (value is T typedValue)
        {
            return typedValue;
        }

        // Try to handle specific common conversions
        if (typeof(T) == typeof(CssLengthValue) && value is ICssValue cssValue)
        {
            if (cssValue is CssLengthValue length)
            {
                return (T)(object)length;
            }
        }
        else if (typeof(T) == typeof(CssColorValue) && value is ICssValue colorValue)
        {
            if (colorValue is CssColorValue color)
            {
                return (T)(object)color;
            }
        }

        return default;
    }

    /// <summary>
    /// Gets the display type of the element.
    /// </summary>
    public DisplayMode Display => _bitfields.DisplayType;

    /// <summary>
    /// Gets the position type of the element.
    /// </summary>
    public PositionMode Position => _bitfields.PositionType;

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

    /// <summary>
    /// Gets the underlying property tree node.
    /// </summary>
    internal PropertyTreeNode PropertyTreeNode => _propertyTree;

    #endregion

    #region Implementation Methods

    /// <summary>
    /// Processes all style properties from the declaration.
    /// </summary>
    private void ProcessStyleProperties(ICssStyleDeclaration declaration)
    {
        // Parse and compute values for all properties
        foreach (var property in declaration)
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

        // Track device-dependent properties
        if (property.RawValue is CssLengthValue length && IsDeviceDependent(length))
        {
            if (_invalidationTracker is StyleInvalidationTracker tracker)
            {
                tracker.MarkAsDeviceDependent(_element);
            }
        }

        // Update specialized property groups
        switch (property.Name)
        {
            // Display and positioning properties
            case "display":
                _bitfields.UpdateDisplayType(ParseDisplayType(property.Value));
                break;
            case "position":
                _bitfields.UpdatePositionType(ParsePositionType(property.Value));
                break;
            case "overflow":
                var overflow = ParseOverflowMode(property.Value);
                _bitfields.UpdateOverflow(overflow, overflow);
                break;
            case "overflow-x":
                _bitfields.OverflowX = ParseOverflowMode(property.Value);
                break;
            case "overflow-y":
                _bitfields.OverflowY = ParseOverflowMode(property.Value);
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
                    _bitfields.IsVisible = opacity > 0;
                }
                break;
            case "z-index":
                if (int.TryParse(property.Value, out var zIndex))
                {
                    _rareProperties.ZIndex = zIndex;
                }
                break;
            case "background-color":
                if (property.RawValue is CssColorValue bgcolor)
                {
                    _rareProperties.BackgroundColor = bgcolor;
                    _bitfields.HasBackground = !bgcolor.Equals(CssColorValue.Transparent);
                }
                break;
            case "border-color":
                if (property.RawValue is CssColorValue borderColor)
                {
                    _rareProperties.BorderColor = borderColor;
                }
                break;
            default:
                // Store other properties in RareProperties
                if (property.RawValue != null)
                {
                    _rareProperties.SetValue(property.Name, property.RawValue);
                }
                break;
        }
    }

    private bool IsDeviceDependent(CssLengthValue length)
    {
        // Check if the unit depends on the render device
        var unit = length.Type;
        return unit == CssLengthValue.Unit.Em || unit == CssLengthValue.Unit.Rem ||
               unit == CssLengthValue.Unit.Vh || unit == CssLengthValue.Unit.Vw ||
               unit == CssLengthValue.Unit.Vmin || unit == CssLengthValue.Unit.Vmax ||
               unit == CssLengthValue.Unit.Percent;
    }

    /// <summary>
    /// Processes box-related properties.
    /// </summary>
    private void ProcessBoxProperty(ICssProperty property)
    {
        // Extract CSS length value
        CssLengthValue? length = null;

        if (property.RawValue is CssLengthValue lengthValue)
        {
            length = lengthValue;
        }
        else if (property.RawValue is ICssValue cssValue)
        {
            // If it's a different CSS value type, try to handle common cases
            if (cssValue.CssText == "auto")
            {
                length = CssLengthValue.Auto;
            }
            else if (cssValue.CssText == "0" || cssValue.CssText == "0px")
            {
                length = CssLengthValue.Zero;
            }
        }

        if (length != null)
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
                case "border-top-width":
                    _boxProperties.SetBorderTop(length);
                    _bitfields.HasBorder = true;
                    break;
                case "border-right-width":
                    _boxProperties.SetBorderRight(length);
                    _bitfields.HasBorder = true;
                    break;
                case "border-bottom-width":
                    _boxProperties.SetBorderBottom(length);
                    _bitfields.HasBorder = true;
                    break;
                case "border-left-width":
                    _boxProperties.SetBorderLeft(length);
                    _bitfields.HasBorder = true;
                    break;
                case "padding-top":
                    _boxProperties.SetPaddingTop(length);
                    break;
                case "padding-right":
                    _boxProperties.SetPaddingRight(length);
                    break;
                case "padding-bottom":
                    _boxProperties.SetPaddingBottom(length);
                    break;
                case "padding-left":
                    _boxProperties.SetPaddingLeft(length);
                    break;
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
                if (property.RawValue is CssLengthValue fontSize)
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
            case "line-height":
                if (property.RawValue is CssLengthValue lineHeight)
                {
                    _textProperties.SetLineHeight(lineHeight);
                }
                break;
            case "text-align":
                _textProperties.SetTextAlign(ParseTextAlign(property.Value));
                break;
            case "color":
                if (property.RawValue is CssColorValue colorValue)
                {
                    _textProperties.SetColor(colorValue);
                }
                else
                {
                    _textProperties.SetColor(property.Value);
                }
                break;
        }
    }

    /// <summary>
    /// Computes the writing mode from the style declaration.
    /// </summary>
    private WritingMode ComputeWritingMode(ICssStyleDeclaration declaration)
    {
        var direction = DirectionMode.Ltr;
        var mode = WritingModeType.HorizontalTopToBottom;

        var directionValue = declaration.GetPropertyValue("direction");
        if (directionValue == "rtl")
        {
            direction = DirectionMode.Rtl;
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
            _textProperties.SetColor(_parentStyle.Text.Color);
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
            "visibility",
            "letter-spacing",
            "word-spacing",
            "white-space",
            "direction",
            "text-transform",
            "text-indent",
            "orphans",
            "widows",
            "list-style-type",
            "list-style-position",
            "list-style-image",
            "list-style",
        };

        foreach (var property in inheritedProperties)
        {
            if (!_propertyTree.HasProperty(property))
            {
                var value = _parentStyle.GetPropertyValue(property);
                if (!string.IsNullOrEmpty(value))
                {
                    _propertyTree.SetProperty(property, value);
                }
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
            _textProperties.SetColor(CssColorValue.Black);
        }

        if (!_propertyTree.HasProperty("font-family"))
        {
            _textProperties.SetFontFamily("sans-serif");
        }

        if (!_propertyTree.HasProperty("font-size"))
        {
            _textProperties.SetFontSize(CssLengthValue.Medium);
        }

        if (!_propertyTree.HasProperty("line-height"))
        {
            _textProperties.SetLineHeight(CssLengthValue.Normal);
        }

        if (!_propertyTree.HasProperty("font-weight"))
        {
            _textProperties.SetFontWeight(400);
        }

        if (!_propertyTree.HasProperty("font-style"))
        {
            _textProperties.SetIsItalic(false);
        }

        if (!_propertyTree.HasProperty("text-align"))
        {
            _textProperties.SetTextAlign(TextAlign.Start);
        }
    }

    /// <summary>
    /// Parses a display property value into a DisplayType enum.
    /// Use AngleSharp's built-in DisplayMode enum.
    /// </summary>
    private DisplayMode ParseDisplayType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "none" => DisplayMode.None,
            "block" => DisplayMode.Block,
            "inline" => DisplayMode.Inline,
            "inline-block" => DisplayMode.InlineBlock,
            "flex" => DisplayMode.Flex,
            "inline-flex" => DisplayMode.InlineFlex,
            "grid" => DisplayMode.Grid,
            "inline-grid" => DisplayMode.InlineGrid,
            "table" => DisplayMode.Table,
            "table-row" => DisplayMode.TableRow,
            "table-cell" => DisplayMode.TableCell,
            "table-caption" => DisplayMode.TableCaption,
            "list-item" => DisplayMode.ListItem,
            _ => DisplayMode.Block // Default
        };
    }

    /// <summary>
    /// Parses a position property value into a PositionType enum.
    /// </summary>
    private PositionMode ParsePositionType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "static" => PositionMode.Static,
            "relative" => PositionMode.Relative,
            "absolute" => PositionMode.Absolute,
            "fixed" => PositionMode.Fixed,
            "sticky" => PositionMode.Sticky,
            _ => PositionMode.Static // Default
        };
    }

    /// <summary>
    /// Parses an overflow property value into an OverflowMode enum.
    /// Use AngleSharp's built-in OverflowMode enum.
    /// </summary>
    private OverflowMode ParseOverflowMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "visible" => OverflowMode.Visible,
            "hidden" => OverflowMode.Hidden,
            "scroll" => OverflowMode.Scroll,
            "auto" => OverflowMode.Auto,
            "clip" => OverflowMode.Clip,
            _ => OverflowMode.Visible // Default
        };
    }

    /// <summary>
    /// Parses a text-align property value into a TextAlign enum.
    /// Use AngleSharp's built-in TextAlign enum.
    /// </summary>
    private TextAlign ParseTextAlign(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "left" => TextAlign.Left,
            "right" => TextAlign.Right,
            "center" => TextAlign.Center,
            "justify" => TextAlign.Justify,
            "start" => TextAlign.Start,
            "end" => TextAlign.End,
            "justify-all" => TextAlign.JustifyAll,
            "match-parent" => TextAlign.MatchParent,
            _ => TextAlign.Start // Default
        };
    }

    #endregion
}