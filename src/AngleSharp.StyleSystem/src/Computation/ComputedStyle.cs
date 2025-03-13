namespace AngleSharp.StyleSystem.Computation;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Storage;
using System;
using System.Collections.Generic;
using Properties;
using StyleSystem.Properties;

public class ComputedStyle : IComputedStyle
{
    #region Fields

    internal readonly IElement _element;
    internal readonly IComputedStyle? _parentStyle;
    private readonly IBoxProperties _boxProperties;
    private readonly ITextProperties _textProperties;
    private readonly RareProperties _rareProperties;
    private readonly SurrogateBitfields _bitfields;
    private readonly IPropertyTreeNode _propertyTree;
    private readonly WritingMode _writingMode;
    private readonly IRenderDevice _renderDevice;
    private readonly IStyleInvalidationTracker _invalidationTracker;
    private readonly IBrowsingContext _context;
    private readonly Dictionary<string, object> _computedValueCache;
    private bool _isInitialized = false;

    private static readonly HashSet<string> _nonInheritedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PropertyNames.Width, PropertyNames.Height,
        PropertyNames.Margin, PropertyNames.MarginTop, PropertyNames.MarginRight, PropertyNames.MarginBottom, PropertyNames.MarginLeft,
        PropertyNames.Padding, PropertyNames.PaddingTop, PropertyNames.PaddingRight, PropertyNames.PaddingBottom, PropertyNames.PaddingLeft,
        PropertyNames.Border, PropertyNames.BorderTop, PropertyNames.BorderRight, PropertyNames.BorderBottom, PropertyNames.BorderLeft,
        PropertyNames.BorderWidth, PropertyNames.BorderTopWidth, PropertyNames.BorderRightWidth, PropertyNames.BorderBottomWidth, PropertyNames.BorderLeftWidth,
        PropertyNames.BorderColor, PropertyNames.BorderTopColor, PropertyNames.BorderRightColor, PropertyNames.BorderBottomColor, PropertyNames.BorderLeftColor,
        PropertyNames.BorderStyle, PropertyNames.BorderTopStyle, PropertyNames.BorderRightStyle, PropertyNames.BorderBottomStyle, PropertyNames.BorderLeftStyle,
        PropertyNames.Background, PropertyNames.BackgroundColor, PropertyNames.BackgroundImage, PropertyNames.BackgroundPosition, PropertyNames.BackgroundRepeat,
        PropertyNames.Display, PropertyNames.Position, PropertyNames.Float, PropertyNames.Clear, PropertyNames.ZIndex,
        PropertyNames.Overflow, PropertyNames.OverflowX, PropertyNames.OverflowY,
        PropertyNames.Opacity, PropertyNames.BoxSizing, PropertyNames.BoxShadow, PropertyNames.MinWidth, PropertyNames.MinHeight,
        PropertyNames.MaxWidth, PropertyNames.MaxHeight,
        PropertyNames.Top, PropertyNames.Right, PropertyNames.Bottom, PropertyNames.Left,
        PropertyNames.Transform, PropertyNames.TransformOrigin, PropertyNames.Transition, PropertyNames.Animation,
        PropertyNames.VerticalAlign, PropertyNames.PageBreakBefore, PropertyNames.PageBreakAfter, PropertyNames.PageBreakInside
    };

    #endregion

    #region Constructor

    public ComputedStyle(
        IElement element,
        IComputedStyle? parentStyle,
        ICssStyleDeclaration declaration,
        IPropertyTreeNode propertyTree,
        IRenderDevice renderDevice,
        IStyleInvalidationTracker invalidationTracker,
        IBrowsingContext context)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        _parentStyle = parentStyle;
        _propertyTree = propertyTree ?? throw new ArgumentNullException(nameof(propertyTree));
        _renderDevice = renderDevice ?? throw new ArgumentNullException(nameof(renderDevice));
        _invalidationTracker = invalidationTracker ?? throw new ArgumentNullException(nameof(invalidationTracker));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _boxProperties = new BoxProperties(this, _renderDevice);
        _textProperties = new TextProperties(this, _renderDevice);
        _rareProperties = new RareProperties();
        _bitfields = new SurrogateBitfields();
        _computedValueCache = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        _writingMode = ComputeWritingMode(declaration);
        ProcessStyleProperties(declaration);
        _isInitialized = true;
    }

    #endregion

    #region Public Properties and Methods

    public ICssStyleDeclaration Declaration { get; }

    public string GetPropertyValue(string propertyName)
    {
        string value = string.Empty;
        if (_propertyTree is PropertyTreeNode concreteNode)
        {
            value = concreteNode.GetSelfPropertyValue(propertyName);
        }
        else
        {
            ICssValue? rawValue = _propertyTree.GetPropertyRawValue(propertyName);
            if (rawValue != null)
            {
                value = rawValue.CssText;
            }
        }
        if (string.IsNullOrEmpty(value) && _isInitialized)
        {
            value = Declaration.GetPropertyValue(propertyName);
            if (string.IsNullOrEmpty(value) && _parentStyle != null && !_nonInheritedProperties.Contains(propertyName))
            {
                value = _parentStyle.GetPropertyValue(propertyName);
            }
        }
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
        if (_computedValueCache.TryGetValue(propertyName, out var cachedValue) && cachedValue is T typedCachedValue)
        {
            return typedCachedValue;
        }
        var value = _propertyTree.GetPropertyCachedValue(propertyName);
        if (value is T typedValue)
        {
            _computedValueCache[propertyName] = typedValue;
            return typedValue;
        }
        if (typeof(T) == typeof(CssLengthValue) && value is ICssValue cssValue)
        {
            if (cssValue is CssLengthValue length)
            {
                _computedValueCache[propertyName] = length;
                return (T)(object)length;
            }
        }
        else if (typeof(T) == typeof(CssColorValue) && value is ICssValue colorValue)
        {
            if (colorValue is CssColorValue color)
            {
                _computedValueCache[propertyName] = color;
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
    internal IPropertyTreeNode PropertyTreeNode => _propertyTree;

    #endregion

    #region Style Processing

    private void ProcessStyleProperties(ICssStyleDeclaration declaration)
    {
        var propertiesFromTree = _propertyTree.GetAllProperties();
        foreach (var property in propertiesFromTree)
        {
            ProcessCssValue(property.Key, property.Value);
        }
        foreach (var property in declaration)
        {
            if (property.RawValue != null && !propertiesFromTree.ContainsKey(property.Name))
            {
                _propertyTree.SetProperty(property.Name, property.RawValue);
                ProcessCssValue(property.Name, property.RawValue);
            }
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

    private void ProcessCssValue(string propertyName, ICssValue? value)
    {
        if (value == null)
            return;

        if (value is CssLengthValue length && IsDeviceDependent(length))
        {
            if (_invalidationTracker is StyleInvalidationTracker tracker)
            {
                tracker.MarkAsDeviceDependent(_element);
            }
        }

        switch (propertyName)
        {
            case PropertyNames.Display:
                _bitfields.UpdateDisplayType(ParseDisplayType(value.CssText));
                break;
            case PropertyNames.Position:
                _bitfields.UpdatePositionType(ParsePositionType(value.CssText));
                break;
            case PropertyNames.Overflow:
                var overflow = ParseOverflowMode(value.CssText);
                _bitfields.UpdateOverflow(overflow, overflow);
                break;
            case PropertyNames.OverflowX:
                _bitfields.OverflowX = ParseOverflowMode(value.CssText);
                break;
            case PropertyNames.OverflowY:
                _bitfields.OverflowY = ParseOverflowMode(value.CssText);
                break;
            case PropertyNames.Width:
            case PropertyNames.Height:
            case PropertyNames.MarginTop:
            case PropertyNames.MarginRight:
            case PropertyNames.MarginBottom:
            case PropertyNames.MarginLeft:
            case PropertyNames.PaddingTop:
            case PropertyNames.PaddingRight:
            case PropertyNames.PaddingBottom:
            case PropertyNames.PaddingLeft:
            case PropertyNames.BorderTopWidth:
            case PropertyNames.BorderRightWidth:
            case PropertyNames.BorderBottomWidth:
            case PropertyNames.BorderLeftWidth:
                ProcessBoxProperty(propertyName, value);
                break;
            case PropertyNames.FontFamily:
            case PropertyNames.FontSize:
            case PropertyNames.FontWeight:
            case PropertyNames.FontStyle:
            case PropertyNames.LineHeight:
            case PropertyNames.TextAlign:
            case PropertyNames.Color:
                ProcessTextProperty(propertyName, value);
                break;
            case PropertyNames.Opacity:
                if (float.TryParse(value.CssText, out var opacity))
                {
                    _rareProperties.Opacity = opacity;
                    _bitfields.IsVisible = opacity > 0;
                }
                break;
            case PropertyNames.ZIndex:
                if (int.TryParse(value.CssText, out var zIndex))
                {
                    _rareProperties.ZIndex = zIndex;
                }
                break;
            case PropertyNames.BackgroundColor:
                if (value is CssColorValue bgcolor)
                {
                    _rareProperties.SetValue(PropertyNames.BackgroundColor, bgcolor);
                    _bitfields.HasBackground = !bgcolor.Equals(CssColorValue.Transparent);
                }
                break;
            case PropertyNames.BorderColor:
                if (value is CssColorValue borderColor)
                {
                    _rareProperties.SetValue(PropertyNames.BorderColor, borderColor);
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
        return unit == CssLengthValue.Unit.Em ||
               unit == CssLengthValue.Unit.Rem ||
               unit == CssLengthValue.Unit.Vh ||
               unit == CssLengthValue.Unit.Vw ||
               unit == CssLengthValue.Unit.Vmin ||
               unit == CssLengthValue.Unit.Vmax ||
               unit == CssLengthValue.Unit.Percent;
    }

    #endregion

    #region Box Properties Processing

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
                case PropertyNames.Width:
                    _boxProperties.SetWidth(length);
                    break;
                case PropertyNames.Height:
                    _boxProperties.SetHeight(length);
                    break;
                case PropertyNames.MarginTop:
                    _boxProperties.SetMarginTop(length);
                    break;
                case PropertyNames.MarginRight:
                    _boxProperties.SetMarginRight(length);
                    break;
                case PropertyNames.MarginBottom:
                    _boxProperties.SetMarginBottom(length);
                    break;
                case PropertyNames.MarginLeft:
                    _boxProperties.SetMarginLeft(length);
                    break;
                case PropertyNames.BorderTopWidth:
                    _boxProperties.SetBorderTop(length);
                    _bitfields.HasBorder = true;
                    break;
                case PropertyNames.BorderRightWidth:
                    _boxProperties.SetBorderRight(length);
                    _bitfields.HasBorder = true;
                    break;
                case PropertyNames.BorderBottomWidth:
                    _boxProperties.SetBorderBottom(length);
                    _bitfields.HasBorder = true;
                    break;
                case PropertyNames.BorderLeftWidth:
                    _boxProperties.SetBorderLeft(length);
                    _bitfields.HasBorder = true;
                    break;
                case PropertyNames.PaddingTop:
                    _boxProperties.SetPaddingTop(length);
                    break;
                case PropertyNames.PaddingRight:
                    _boxProperties.SetPaddingRight(length);
                    break;
                case PropertyNames.PaddingBottom:
                    _boxProperties.SetPaddingBottom(length);
                    break;
                case PropertyNames.PaddingLeft:
                    _boxProperties.SetPaddingLeft(length);
                    break;
            }
        }
    }

    #endregion

    #region Text Properties Processing

    private void ProcessTextProperty(string propertyName, ICssValue value)
    {
        switch (propertyName)
        {
            case PropertyNames.FontFamily:
                _textProperties.SetFontFamily(value.CssText);
                break;
            case PropertyNames.FontSize:
                if (value is CssLengthValue fontSize)
                {
                    _textProperties.SetFontSize(fontSize);
                }
                break;
            case PropertyNames.FontWeight:
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
            case PropertyNames.FontStyle:
                _textProperties.SetIsItalic(value.CssText == "italic");
                break;
            case PropertyNames.LineHeight:
                if (value is CssLengthValue lineHeight)
                {
                    _textProperties.SetLineHeight(lineHeight);
                }
                break;
            case PropertyNames.TextAlign:
                _textProperties.SetTextAlign(ParseTextAlign(value.CssText));
                break;
            case PropertyNames.Color:
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

    #endregion

    #region Writing Mode and Inheritance

    private WritingMode ComputeWritingMode(ICssStyleDeclaration declaration)
    {
        var direction = DirectionMode.Ltr;
        var mode = WritingModeType.HorizontalTopToBottom;

        var directionValue = declaration.GetPropertyValue(PropertyNames.Direction);
        if (directionValue == "rtl")
        {
            direction = DirectionMode.Rtl;
        }

        var writingModeValue = declaration.GetPropertyValue(PropertyNames.WritingMode);
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

        if (!_propertyTree.HasProperty(PropertyNames.Color))
        {
            var parentColor = _parentStyle.Text.Color;
            _textProperties.SetColor(parentColor);
            _propertyTree.SetProperty(PropertyNames.Color, parentColor);
        }

        if (!_propertyTree.HasProperty(PropertyNames.FontFamily))
        {
            var parentFontFamily = _parentStyle.Text.FontFamily;
            _textProperties.SetFontFamily(parentFontFamily);
            _propertyTree.SetProperty(PropertyNames.FontFamily, parentFontFamily);
        }

        var inheritedProperties = new[]
        {
            PropertyNames.LineHeight,
            PropertyNames.FontWeight,
            PropertyNames.FontStyle,
            PropertyNames.TextAlign,
            PropertyNames.Visibility,
            PropertyNames.LetterSpacing,
            PropertyNames.WordSpacing,
            PropertyNames.WhiteSpace,
            PropertyNames.Direction,
            PropertyNames.TextTransform,
            PropertyNames.TextIndent,
            PropertyNames.Orphans,
            PropertyNames.Widows,
            PropertyNames.ListStyleType,
            PropertyNames.ListStylePosition,
            PropertyNames.ListStyleImage,
            PropertyNames.ListStyle,
            PropertyNames.Quotes,
            PropertyNames.Cursor,
            PropertyNames.FontVariant,
            PropertyNames.FontStretch,
            PropertyNames.FontSizeAdjust
        };

        foreach (var property in inheritedProperties)
        {
            if (!_propertyTree.HasProperty(property))
            {
                var value = _parentStyle.GetPropertyValue(property);
                if (!string.IsNullOrEmpty(value))
                {
                    _propertyTree.SetProperty(property, value);
                    Declaration.SetProperty(property, value);
                }
            }
        }
    }

    private void ApplyInitialValues()
    {
        if (!_propertyTree.HasProperty(PropertyNames.Color))
        {
            _textProperties.SetColor(CssColorValue.Black);
            _propertyTree.SetProperty(PropertyNames.Color, CssColorValue.Black);
        }

        if (!_propertyTree.HasProperty(PropertyNames.FontFamily))
        {
            _textProperties.SetFontFamily("Times New Roman");
            _propertyTree.SetProperty(PropertyNames.FontFamily, "Times New Roman");
        }

        if (!_propertyTree.HasProperty(PropertyNames.FontSize))
        {
            _textProperties.SetFontSize(CssLengthValue.Medium);
            _propertyTree.SetProperty(PropertyNames.FontSize, CssLengthValue.Medium);
        }

        if (!_propertyTree.HasProperty(PropertyNames.LineHeight))
        {
            _textProperties.SetLineHeight(CssLengthValue.Normal);
            _propertyTree.SetProperty(PropertyNames.LineHeight, CssLengthValue.Normal);
        }

        if (!_propertyTree.HasProperty(PropertyNames.FontWeight))
        {
            _textProperties.SetFontWeight(400);
            _propertyTree.SetProperty(PropertyNames.FontWeight, "400");
        }

        if (!_propertyTree.HasProperty(PropertyNames.FontStyle))
        {
            _textProperties.SetIsItalic(false);
            _propertyTree.SetProperty(PropertyNames.FontStyle, "normal");
        }

        if (!_propertyTree.HasProperty(PropertyNames.TextAlign))
        {
            _textProperties.SetTextAlign(TextAlign.Start);
            _propertyTree.SetProperty(PropertyNames.TextAlign, "start");
        }
    }

    #endregion

    #region Parsing Utilities

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

    #endregion
}