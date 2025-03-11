namespace AngleSharp.StyleSystem.Computation;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using Integration;
using Interfaces;
using Models;
using Properties;
using Storage;

public class ComputedStyle : IComputedStyle
{
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
    private bool _isInitialized = false;

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

        // Process style properties to set up bitfields and computed values
        ProcessStyleProperties(declaration);

        _isInitialized = true;
    }

    public ICssStyleDeclaration Declaration { get; }

    public string GetPropertyValue(string propertyName)
    {
        // First check the property tree
        string value = _propertyTree.GetPropertyValue(propertyName);

        // If no value in the property tree, check the declaration
        if (string.IsNullOrEmpty(value) && _isInitialized)
        {
            value = Declaration.GetPropertyValue(propertyName);
        }

        // If still no value, get the initial value
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

    public T? GetValue<T>(string propertyName)
    {
        // First try to get the cached value from the property tree
        var value = _propertyTree.GetPropertyCachedValue(propertyName);

        if (value is T typedValue)
        {
            return typedValue;
        }

        // Try to convert the value if it's a known type
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

    public DisplayMode Display => _bitfields.DisplayType;
    public PositionMode Position => _bitfields.PositionType;
    public float Opacity => _rareProperties.Opacity;
    public int ZIndex => _rareProperties.ZIndex;
    public CssLengthValue FontSize => _textProperties.FontSize;
    public WritingMode WritingMode => _writingMode;
    public IBoxProperties Box => _boxProperties;
    public ITextProperties Text => _textProperties;

    internal PropertyTreeNode PropertyTreeNode => _propertyTree;

    private void ProcessStyleProperties(ICssStyleDeclaration declaration)
    {
        // First process values already in the property tree
        var propertiesFromTree = _propertyTree.GetAllProperties();
        foreach (var property in propertiesFromTree)
        {
            ProcessCssValue(property.Key, property.Value);
        }

        // Then process the declaration - but only for properties not already in the tree
        foreach (var property in declaration)
        {
            if (property.RawValue != null && !propertiesFromTree.ContainsKey(property.Name))
            {
                // Set the property in the tree to maintain consistency
                _propertyTree.SetProperty(property.Name, property.RawValue);
                ProcessCssValue(property.Name, property.RawValue);
            }
        }

        // Apply inheritance or initial values
        if (_parentStyle != null)
        {
            ApplyInheritance();
        }
        else
        {
            ApplyInitialValues();
        }
    }

    private void ProcessCssValue(string propertyName, ICssValue? value)
    {
        if (value == null)
            return;

        // Check if device-dependent for invalidation tracking
        if (value is CssLengthValue length && IsDeviceDependent(length))
        {
            if (_invalidationTracker is StyleInvalidationTracker tracker)
            {
                tracker.MarkAsDeviceDependent(_element);
            }
        }

        // Update the appropriate properties based on the property name
        switch (propertyName)
        {
            case "display":
                _bitfields.UpdateDisplayType(ParseDisplayType(value.CssText));
                break;
            case "position":
                _bitfields.UpdatePositionType(ParsePositionType(value.CssText));
                break;
            case "overflow":
                var overflow = ParseOverflowMode(value.CssText);
                _bitfields.UpdateOverflow(overflow, overflow);
                break;
            case "overflow-x":
                _bitfields.OverflowX = ParseOverflowMode(value.CssText);
                break;
            case "overflow-y":
                _bitfields.OverflowY = ParseOverflowMode(value.CssText);
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
                ProcessBoxProperty(propertyName, value);
                break;
            case "font-family":
            case "font-size":
            case "font-weight":
            case "font-style":
            case "line-height":
            case "text-align":
            case "color":
                ProcessTextProperty(propertyName, value);
                break;
            case "opacity":
                if (value.CssText != null && float.TryParse(value.CssText, out var opacity))
                {
                    _rareProperties.Opacity = opacity;
                    _bitfields.IsVisible = opacity > 0;
                }
                break;
            case "z-index":
                if (value.CssText != null && int.TryParse(value.CssText, out var zIndex))
                {
                    _rareProperties.ZIndex = zIndex;
                }
                break;
            case "background-color":
                if (value is CssColorValue bgcolor)
                {
                    _rareProperties.BackgroundColor = bgcolor;
                    _bitfields.HasBackground = !bgcolor.Equals(CssColorValue.Transparent);
                }
                break;
            case "border-color":
                if (value is CssColorValue borderColor)
                {
                    _rareProperties.BorderColor = borderColor;
                }
                break;
            default:
                _rareProperties.SetValue(propertyName, value);
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

    private void ProcessBoxProperty(string propertyName, ICssValue value)
    {
        CssLengthValue? length = null;
        if (value is CssLengthValue lengthValue)
        {
            length = lengthValue;
        }
        else if (value is ICssValue cssValue)
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
            switch (propertyName)
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

    private void ProcessTextProperty(string propertyName, ICssValue value)
    {
        switch (propertyName)
        {
            case "font-family":
                _textProperties.SetFontFamily(value.CssText);
                break;
            case "font-size":
                if (value is CssLengthValue fontSize)
                {
                    _textProperties.SetFontSize(fontSize);
                }
                break;
            case "font-weight":
                if (int.TryParse(value.CssText, out var fontWeight))
                {
                    _textProperties.SetFontWeight(fontWeight);
                }
                else if (value.CssText == "bold")
                {
                    _textProperties.SetFontWeight(700);
                }
                else if (value.CssText == "normal")
                {
                    _textProperties.SetFontWeight(400);
                }
                break;
            case "font-style":
                _textProperties.SetIsItalic(value.CssText == "italic");
                break;
            case "line-height":
                if (value is CssLengthValue lineHeight)
                {
                    _textProperties.SetLineHeight(lineHeight);
                }
                break;
            case "text-align":
                _textProperties.SetTextAlign(ParseTextAlign(value.CssText));
                break;
            case "color":
                if (value is CssColorValue colorValue)
                {
                    _textProperties.SetColor(colorValue);
                }
                else
                {
                    _textProperties.SetColor(value.CssText);
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

        // Inherit color if not specified
        if (!_propertyTree.HasProperty("color"))
        {
            var parentColor = _parentStyle.Text.Color;
            _textProperties.SetColor(parentColor);
            _propertyTree.SetProperty("color", parentColor);
        }

        // Inherit font-family if not specified
        if (!_propertyTree.HasProperty("font-family"))
        {
            var parentFontFamily = _parentStyle.Text.FontFamily;
            _textProperties.SetFontFamily(parentFontFamily);
            _propertyTree.SetProperty("font-family", parentFontFamily);
        }

        // Other inherited properties
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
                    // Also update the declaration for backward compatibility
                    Declaration.SetProperty(property, value);
                }
            }
        }
    }

    private void ApplyInitialValues()
    {
        // Apply default initial values for key properties if not specified
        if (!_propertyTree.HasProperty("color"))
        {
            _textProperties.SetColor(CssColorValue.Black);
            _propertyTree.SetProperty("color", CssColorValue.Black);
        }

        if (!_propertyTree.HasProperty("font-family"))
        {
            _textProperties.SetFontFamily("sans-serif");
            _propertyTree.SetProperty("font-family", "sans-serif");
        }

        if (!_propertyTree.HasProperty("font-size"))
        {
            _textProperties.SetFontSize(CssLengthValue.Medium);
            _propertyTree.SetProperty("font-size", CssLengthValue.Medium);
        }

        if (!_propertyTree.HasProperty("line-height"))
        {
            _textProperties.SetLineHeight(CssLengthValue.Normal);
            _propertyTree.SetProperty("line-height", CssLengthValue.Normal);
        }

        if (!_propertyTree.HasProperty("font-weight"))
        {
            _textProperties.SetFontWeight(400);
            _propertyTree.SetProperty("font-weight", "400");
        }

        if (!_propertyTree.HasProperty("font-style"))
        {
            _textProperties.SetIsItalic(false);
            _propertyTree.SetProperty("font-style", "normal");
        }

        if (!_propertyTree.HasProperty("text-align"))
        {
            _textProperties.SetTextAlign(TextAlign.Start);
            _propertyTree.SetProperty("text-align", "start");
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