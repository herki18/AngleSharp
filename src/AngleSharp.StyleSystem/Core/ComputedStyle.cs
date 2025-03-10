namespace AngleSharp.StyleSystem.Core;

using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using Css;
using Interfaces;

/// <summary>
/// Represents a computed style with optimized property access.
/// </summary>
public class ComputedStyle : IComputedStyle
{
    // These fields are internal to allow access by ComputedStyleFactory
    internal readonly IElement _element;
    internal readonly IComputedStyle? _parentStyle;

    private readonly BoxProperties _boxProperties;
    private readonly TextProperties _textProperties;
    private readonly RareProperties _rareProperties;
    private readonly SurrogateBitfields _bitfields;
    private readonly PropertyTreeNode _propertyTree;
    private readonly WritingMode _writingMode;
    private readonly IRenderDevice _renderDevice;
    private readonly IStyleInvalidationTracker _invalidationTracker;
    private readonly IBrowsingContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputedStyle"/> class.
    /// </summary>
    public ComputedStyle(
        IElement element,
        IComputedStyle? parentStyle,
        ICssStyleDeclaration declaration,
        PropertyTreeNode propertyTree,
        IRenderDevice renderDevice,
        IStyleInvalidationTracker invalidationTracker,
        IBrowsingContext context)
    {
        _element = element;
        _parentStyle = parentStyle;
        _propertyTree = propertyTree;
        _renderDevice = renderDevice;
        _invalidationTracker = invalidationTracker;
        _context = context;

        _boxProperties = new BoxProperties(this, _renderDevice);
        _textProperties = new TextProperties(this, _renderDevice);
        _rareProperties = new RareProperties();
        _bitfields = new SurrogateBitfields();

        Declaration = declaration;
        _writingMode = ComputeWritingMode(declaration);

        ProcessStyleProperties(declaration);
    }

    /// <summary>
    /// Gets the declaration that was used to create this computed style.
    /// </summary>
    public ICssStyleDeclaration Declaration { get; }

    /// <summary>
    /// Gets a property value by name.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property value.</returns>
    public string GetPropertyValue(string propertyName)
    {
        string value = _propertyTree.GetPropertyValue(propertyName);
        if (string.IsNullOrEmpty(value))
        {
            var factory = _context.GetFactory<IDeclarationFactory>();
            var declarationInfo = factory?.Create(propertyName);
            if (declarationInfo?.InitialValue != null)
            {
                return declarationInfo.InitialValue.CssText;
            }
        }

        return value;
    }

    /// <summary>
    /// Gets a typed property value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The typed value.</returns>
    public T? GetValue<T>(string propertyName)
    {
        var value = _propertyTree.GetPropertyCachedValue(propertyName);

        if (value is T typedValue)
        {
            return typedValue;
        }

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
    /// Gets the display type.
    /// </summary>
    public DisplayMode Display => _bitfields.DisplayType;

    /// <summary>
    /// Gets the position type.
    /// </summary>
    public PositionMode Position => _bitfields.PositionType;

    /// <summary>
    /// Gets the opacity value.
    /// </summary>
    public float Opacity => _rareProperties.Opacity;

    /// <summary>
    /// Gets the z-index value.
    /// </summary>
    public int ZIndex => _rareProperties.ZIndex;

    /// <summary>
    /// Gets the font size.
    /// </summary>
    public CssLengthValue FontSize => _textProperties.FontSize;

    /// <summary>
    /// Gets the writing mode.
    /// </summary>
    public WritingMode WritingMode => _writingMode;

    /// <summary>
    /// Gets the box properties.
    /// </summary>
    public IBoxProperties Box => _boxProperties;

    /// <summary>
    /// Gets the text properties.
    /// </summary>
    public ITextProperties Text => _textProperties;

    /// <summary>
    /// Gets the property tree node.
    /// </summary>
    internal PropertyTreeNode PropertyTreeNode => _propertyTree;

    private void ProcessStyleProperties(ICssStyleDeclaration declaration)
    {
        foreach (var property in declaration)
        {
            ProcessProperty(property);
        }

        if (_parentStyle != null)
        {
            ApplyInheritance();
        }
        else
        {
            ApplyInitialValues();
        }
    }

    private void ProcessProperty(ICssProperty property)
    {
        _propertyTree.SetProperty(property.Name, property.RawValue);

        if (property.RawValue is CssLengthValue length && IsDeviceDependent(length))
        {
            if (_invalidationTracker is StyleInvalidationTracker tracker)
            {
                tracker.MarkAsDeviceDependent(_element);
            }
        }

        switch (property.Name)
        {
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
            case "font-family":
            case "font-size":
            case "font-weight":
            case "font-style":
            case "line-height":
            case "text-align":
            case "color":
                ProcessTextProperty(property);
                break;
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
                if (property.RawValue != null)
                {
                    _rareProperties.SetValue(property.Name, property.RawValue);
                }
                break;
        }
    }

    private bool IsDeviceDependent(CssLengthValue length)
    {
        var unit = length.Type;
        return unit == CssLengthValue.Unit.Em || unit == CssLengthValue.Unit.Rem ||
               unit == CssLengthValue.Unit.Vh || unit == CssLengthValue.Unit.Vw ||
               unit == CssLengthValue.Unit.Vmin || unit == CssLengthValue.Unit.Vmax ||
               unit == CssLengthValue.Unit.Percent;
    }

    private void ProcessBoxProperty(ICssProperty property)
    {
        CssLengthValue? length = null;

        if (property.RawValue is CssLengthValue lengthValue)
        {
            length = lengthValue;
        }
        else if (property.RawValue is ICssValue cssValue)
        {
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

    private void ApplyInheritance()
    {
        if (_parentStyle == null)
            return;

        if (!_propertyTree.HasProperty("color"))
        {
            _textProperties.SetColor(_parentStyle.Text.Color);
        }

        if (!_propertyTree.HasProperty("font-family"))
        {
            _textProperties.SetFontFamily(_parentStyle.Text.FontFamily);
        }

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

    private void ApplyInitialValues()
    {
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
            _ => DisplayMode.Block
        };
    }

    private PositionMode ParsePositionType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "static" => PositionMode.Static,
            "relative" => PositionMode.Relative,
            "absolute" => PositionMode.Absolute,
            "fixed" => PositionMode.Fixed,
            "sticky" => PositionMode.Sticky,
            _ => PositionMode.Static
        };
    }

    private OverflowMode ParseOverflowMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "visible" => OverflowMode.Visible,
            "hidden" => OverflowMode.Hidden,
            "scroll" => OverflowMode.Scroll,
            "auto" => OverflowMode.Auto,
            "clip" => OverflowMode.Clip,
            _ => OverflowMode.Visible
        };
    }

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
            _ => TextAlign.Start
        };
    }
}