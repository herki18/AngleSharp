namespace AngleSharp.StyleSystem.Properties;

using System;
using AngleSharp.Css;
using AngleSharp.Css.Values;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Models;
using Interfaces;

/// <summary>
/// Represents box-related computed properties.
/// </summary>
public class BoxProperties : IBoxProperties
{
    private readonly ComputedStyle _owner;
    private readonly IRenderDevice _renderDevice;

    // Box dimensions - using AngleSharp's CssLengthValue
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

    // Cached pixel values for performance optimization
    private double? _cachedWidthPx;
    private double? _cachedHeightPx;

    public BoxProperties(ComputedStyle owner, IRenderDevice renderDevice)
    {
        _owner = owner;
        _renderDevice = renderDevice;
    }

    /// <summary>
    /// Gets the computed width as an AngleSharp CssLengthValue.
    /// </summary>
    public CssLengthValue Width => _width;

    /// <summary>
    /// Gets the computed height as an AngleSharp CssLengthValue.
    /// </summary>
    public CssLengthValue Height => _height;

    /// <summary>
    /// Gets the logical inline size (width or height depending on writing mode).
    /// </summary>
    public CssLengthValue InlineSize => _owner.WritingMode.IsHorizontal ? _width : _height;

    /// <summary>
    /// Gets the logical block size (height or width depending on writing mode).
    /// </summary>
    public CssLengthValue BlockSize => _owner.WritingMode.IsHorizontal ? _height : _width;

    public Double WidthInPixels
    {
        get
        {
            if (!_cachedWidthPx.HasValue && !_width.Equals(CssLengthValue.Auto))
            {
                // Call AngleSharp's conversion with appropriate context
                _cachedWidthPx = _width.ToPixel(_renderDevice);
            }
            return _cachedWidthPx ?? 0;
        }
    }

    public Double HeightInPixels
    {
        get
        {
            if (!_cachedHeightPx.HasValue && !_height.Equals(CssLengthValue.Auto))
            {
                // Call AngleSharp's conversion with appropriate context
                _cachedHeightPx = _height.ToPixel(_renderDevice);
            }
            return _cachedHeightPx ?? 0;
        }
    }

    // Edge geometry properties
    public Edges Margin => new Edges(_marginTop, _marginRight, _marginBottom, _marginLeft);
    public Edges Border => new Edges(_borderTop, _borderRight, _borderBottom, _borderLeft);
    public Edges Padding => new Edges(_paddingTop, _paddingRight, _paddingBottom, _paddingLeft);

    // Logical edge properties
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

    // Setter methods for property values
    public void SetWidth(CssLengthValue? value)
    {
        _width = value ?? CssLengthValue.Auto;
        _cachedWidthPx = null; // Invalidate cache
    }

    public void SetHeight(CssLengthValue? value)
    {
        _height = value ?? CssLengthValue.Auto;
        _cachedHeightPx = null; // Invalidate cache
    }

    // Margin setters
    public void SetMarginTop(CssLengthValue? value) => _marginTop = value ?? CssLengthValue.Zero;
    public void SetMarginRight(CssLengthValue? value) => _marginRight = value ?? CssLengthValue.Zero;
    public void SetMarginBottom(CssLengthValue? value) => _marginBottom = value ?? CssLengthValue.Zero;
    public void SetMarginLeft(CssLengthValue? value) => _marginLeft = value ?? CssLengthValue.Zero;

    // Border setters
    public void SetBorderTop(CssLengthValue? value) => _borderTop = value ?? CssLengthValue.Zero;
    public void SetBorderRight(CssLengthValue? value) => _borderRight = value ?? CssLengthValue.Zero;
    public void SetBorderBottom(CssLengthValue? value) => _borderBottom = value ?? CssLengthValue.Zero;
    public void SetBorderLeft(CssLengthValue? value) => _borderLeft = value ?? CssLengthValue.Zero;

    // Padding setters
    public void SetPaddingTop(CssLengthValue? value) => _paddingTop = value ?? CssLengthValue.Zero;
    public void SetPaddingRight(CssLengthValue? value) => _paddingRight = value ?? CssLengthValue.Zero;
    public void SetPaddingBottom(CssLengthValue? value) => _paddingBottom = value ?? CssLengthValue.Zero;
    public void SetPaddingLeft(CssLengthValue? value) => _paddingLeft = value ?? CssLengthValue.Zero;

    /// <summary>
    /// Invalidates any cached computed values.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedWidthPx = null;
        _cachedHeightPx = null;
    }
}