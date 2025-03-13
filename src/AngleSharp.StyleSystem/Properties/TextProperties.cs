namespace AngleSharp.StyleSystem.Properties;

using System;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.StyleSystem.Computation;
using Interfaces;

/// <summary>
/// Represents text-related computed properties.
/// </summary>
public class TextProperties : ITextProperties
{
    private readonly ComputedStyle _owner;
    private readonly IRenderDevice _renderDevice;

    // Using AngleSharp's value types where appropriate
    private string _fontFamily = "sans-serif";
    private CssLengthValue _fontSize = CssLengthValue.Medium;
    private int _fontWeight = 400;
    private bool _isItalic = false;
    private CssLengthValue _lineHeight = CssLengthValue.Normal;
    private TextAlign _textAlign = TextAlign.Start;
    private CssColorValue _color = CssColorValue.Black;

    // Cached values for performance optimization
    private Double? _cachedFontSizePx;
    private Double? _cachedLineHeightPx;

    public TextProperties(ComputedStyle owner, IRenderDevice renderDevice)
    {
        _owner = owner;
        _renderDevice = renderDevice;
    }

    // Property getters
    public string FontFamily => _fontFamily;
    public int FontWeight => _fontWeight;
    public bool IsItalic => _isItalic;
    public CssLengthValue LineHeight => _lineHeight;
    public TextAlign TextAlign => _textAlign;
    public CssColorValue Color => _color;
    public CssLengthValue FontSize => _fontSize;

    /// <summary>
    /// Gets font size in pixels with caching for performance (layout optimization).
    /// </summary>
    public Double FontSizeInPixels
    {
        get
        {
            if (!_cachedFontSizePx.HasValue)
            {
                // Use AngleSharp's conversion with appropriate context
                _cachedFontSizePx = _fontSize.ToPixel(_renderDevice);
            }
            return _cachedFontSizePx ?? 16f; // Default to 16px if not calculable
        }
    }

    /// <summary>
    /// Gets line height in pixels with caching for performance (layout optimization).
    /// </summary>
    public Double LineHeightInPixels
    {
        get
        {
            if (!_cachedLineHeightPx.HasValue)
            {
                // For "normal" line-height, use standard browser calculation (typically 1.2 * font-size)
                if (_lineHeight == CssLengthValue.Normal)
                {
                    _cachedLineHeightPx = FontSizeInPixels * 1.2f;
                }
                else
                {
                    // Use AngleSharp's conversion with appropriate context
                    _cachedLineHeightPx = _lineHeight.ToPixel(_renderDevice);
                }
            }
            return _cachedLineHeightPx ?? (FontSizeInPixels * 1.2f);
        }
    }

    // RGBA color components with fast access for painting
    public byte Red => _color.R;
    public byte Green => _color.G;
    public byte Blue => _color.B;
    public float Alpha => _color.A;

    // Setter methods - with updated font-family handling
    public void SetFontFamily(string fontFamily)
    {
        // Strip quotes if present to ensure we store the raw font family name
        _fontFamily = StripFontFamilyQuotes(fontFamily ?? "sans-serif");
    }

    public void SetFontFamily(ICssValue value)
    {
        if (value != null)
        {
            // When setting from a CSS value object, we need to strip quotes from the CssText
            SetFontFamily(value.CssText);
        }
    }

    public void SetFontSize(CssLengthValue? fontSize)
    {
        _fontSize = fontSize ?? CssLengthValue.Medium;
        _cachedFontSizePx = null; // Invalidate cache
        _cachedLineHeightPx = null; // Line height may depend on font size
    }

    public void SetFontWeight(int weight) => _fontWeight = weight;

    public void SetIsItalic(bool isItalic) => _isItalic = isItalic;

    public void SetLineHeight(CssLengthValue? lineHeight)
    {
        _lineHeight = lineHeight ?? CssLengthValue.Normal;
        _cachedLineHeightPx = null; // Invalidate cache
    }

    public void SetTextAlign(TextAlign textAlign) => _textAlign = textAlign;

    public void SetColor(CssColorValue? color) => _color = color ?? CssColorValue.Black;

    public void SetColor(string colorStr)
    {
        // Try to use AngleSharp's color parsing methods

        // First try to parse as hex
        if (CssColorValue.TryFromHex(colorStr, out var parsedColor))
        {
            _color = parsedColor;
            return;
        }

        // Then try to get by known color name
        var namedColor = CssColorValue.FromName(colorStr);
        if (namedColor.HasValue)
        {
            _color = namedColor.Value;
            return;
        }

        // Handle common named colors directly
        _color = colorStr.ToLowerInvariant() switch
        {
            "black" => CssColorValue.Black,
            "white" => CssColorValue.White,
            "red" => CssColorValue.Red,
            "green" => CssColorValue.Green,
            "blue" => CssColorValue.Blue,
            "transparent" => CssColorValue.Transparent,
            _ => CssColorValue.Black // Fallback to black
        };
    }

    /// <summary>
    /// Invalidates any cached computed values.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedFontSizePx = null;
        _cachedLineHeightPx = null;
    }

    /// <summary>
    /// Creates a font string suitable for rendering APIs.
    /// </summary>
    public string GetFontString()
    {
        var style = IsItalic ? "italic " : string.Empty;
        return $"{style}{_fontWeight} {FontSizeInPixels}px {_fontFamily}";
    }

    /// <summary>
    /// Helper method to strip quotes from font-family values.
    /// </summary>
    private static string StripFontFamilyQuotes(string fontFamily)
    {
        if (string.IsNullOrEmpty(fontFamily))
            return string.Empty;

        // Strip outer quotes if present
        if ((fontFamily.StartsWith("\"") && fontFamily.EndsWith("\"")) ||
            (fontFamily.StartsWith("'") && fontFamily.EndsWith("'")))
        {
            return fontFamily.Substring(1, fontFamily.Length - 2);
        }

        return fontFamily;
    }
}