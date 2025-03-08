namespace AngleSharp.StyleSystem.Core;

using AngleSharp.Css.Dom;

/// <summary>
/// Uses bitfields to efficiently store boolean flags and enumeration values.
/// This is a memory optimization for frequently used boolean properties and small enums.
/// </summary>
public class SurrogateBitfields
{
    // Use AngleSharp's built-in enum types
    private DisplayMode _displayType = DisplayMode.Block;
    private PositionMode _positionType = PositionMode.Static;
    private OverflowMode _overflowX = OverflowMode.Visible;
    private OverflowMode _overflowY = OverflowMode.Visible;

    // Display and Position are frequently accessed, so they get direct enum storage
    public DisplayMode DisplayType
    {
        get => _displayType;
        set
        {
            _displayType = value;
            UpdateDisplayFlags();
        }
    }

    public PositionMode PositionType
    {
        get => _positionType;
        set
        {
            _positionType = value;
            UpdatePositionFlags();
        }
    }

    // Overflow properties are common enough to warrant dedicated storage
    public OverflowMode OverflowX
    {
        get => _overflowX;
        set
        {
            _overflowX = value;
            UpdateOverflowFlags();
        }
    }

    public OverflowMode OverflowY
    {
        get => _overflowY;
        set
        {
            _overflowY = value;
            UpdateOverflowFlags();
        }
    }

    // Boolean flags packed into bits for memory efficiency
    private uint _flags;

    // Flag positions for various boolean properties
    private const int IsVisibleFlag = 0;
    private const int HasTransformFlag = 1;
    private const int IsFloatingFlag = 2;
    private const int IsAbsolutelyPositionedFlag = 3;
    private const int HasBackgroundFlag = 4;
    private const int HasBorderFlag = 5;
    private const int IsInlineFlag = 6;
    private const int IsBlockFlag = 7;
    private const int IsClippedFlag = 8;
    private const int UsesBlendingFlag = 9;
    private const int HasFixedPositionFlag = 10;
    private const int HasStickyPositionFlag = 11;
    private const int IsFlexContainerFlag = 12;
    private const int IsFlexItemFlag = 13;
    private const int IsGridContainerFlag = 14;
    private const int IsGridItemFlag = 15;
    private const int HasOutlineFlag = 16;
    private const int HasBoxShadowFlag = 17;
    private const int HasTextShadowFlag = 18;
    private const int IsScrollableFlag = 19;
    private const int IsOverflowHiddenFlag = 20;

    // Commonly used flags with explicit property access
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

    public bool HasBackground
    {
        get => GetFlag(HasBackgroundFlag);
        set => SetFlag(HasBackgroundFlag, value);
    }

    public bool HasBorder
    {
        get => GetFlag(HasBorderFlag);
        set => SetFlag(HasBorderFlag, value);
    }

    public bool IsInline
    {
        get => GetFlag(IsInlineFlag);
        set => SetFlag(IsInlineFlag, value);
    }

    public bool IsBlock
    {
        get => GetFlag(IsBlockFlag);
        set => SetFlag(IsBlockFlag, value);
    }

    // Additional Display Type detection helpers
    public bool IsFlexContainer
    {
        get => GetFlag(IsFlexContainerFlag);
        set => SetFlag(IsFlexContainerFlag, value);
    }

    public bool IsGridContainer
    {
        get => GetFlag(IsGridContainerFlag);
        set => SetFlag(IsGridContainerFlag, value);
    }

    public bool IsScrollable
    {
        get => GetFlag(IsScrollableFlag);
        set => SetFlag(IsScrollableFlag, value);
    }

    // Private flag access
    private bool GetFlag(int position)
    {
        return (_flags & (1U << position)) != 0;
    }

    private void SetFlag(int position, bool value)
    {
        if (value)
            _flags |= (1U << position);
        else
            _flags &= ~(1U << position);
    }

    /// <summary>
    /// Updates display type and sets appropriate flags based on the display value.
    /// </summary>
    public void UpdateDisplayType(DisplayMode displayMode)
    {
        _displayType = displayMode;
        UpdateDisplayFlags();
    }

    /// <summary>
    /// Updates flags based on current display type.
    /// </summary>
    private void UpdateDisplayFlags()
    {
        IsInline = _displayType == DisplayMode.Inline ||
                   _displayType == DisplayMode.InlineBlock ||
                   _displayType == DisplayMode.InlineFlex ||
                   _displayType == DisplayMode.InlineGrid ||
                   _displayType == DisplayMode.InlineTable;

        IsBlock = _displayType == DisplayMode.Block ||
                  _displayType == DisplayMode.InlineBlock ||
                  _displayType == DisplayMode.Flex ||
                  _displayType == DisplayMode.Grid ||
                  _displayType == DisplayMode.Table;

        IsFlexContainer = _displayType == DisplayMode.Flex ||
                          _displayType == DisplayMode.InlineFlex;

        IsGridContainer = _displayType == DisplayMode.Grid ||
                          _displayType == DisplayMode.InlineGrid;
    }

    /// <summary>
    /// Updates position type and sets appropriate flags based on the position value.
    /// </summary>
    public void UpdatePositionType(PositionMode positionMode)
    {
        _positionType = positionMode;
        UpdatePositionFlags();
    }

    /// <summary>
    /// Updates flags based on current position type.
    /// </summary>
    private void UpdatePositionFlags()
    {
        IsAbsolutelyPositioned = _positionType == PositionMode.Absolute ||
                                 _positionType == PositionMode.Fixed;

        SetFlag(HasFixedPositionFlag, _positionType == PositionMode.Fixed);
        SetFlag(HasStickyPositionFlag, _positionType == PositionMode.Sticky);
    }

    /// <summary>
    /// Updates overflow properties and sets appropriate flags.
    /// </summary>
    public void UpdateOverflow(OverflowMode overflowX, OverflowMode overflowY)
    {
        _overflowX = overflowX;
        _overflowY = overflowY;
        UpdateOverflowFlags();
    }

    /// <summary>
    /// Updates flags based on current overflow settings.
    /// </summary>
    private void UpdateOverflowFlags()
    {
        IsScrollable = _overflowX == OverflowMode.Scroll || _overflowY == OverflowMode.Scroll ||
                       _overflowX == OverflowMode.Auto || _overflowY == OverflowMode.Auto;

        SetFlag(IsOverflowHiddenFlag, _overflowX == OverflowMode.Hidden || _overflowY == OverflowMode.Hidden);
        SetFlag(IsClippedFlag, _overflowX == OverflowMode.Hidden || _overflowY == OverflowMode.Hidden ||
                               _overflowX == OverflowMode.Clip || _overflowY == OverflowMode.Clip);
    }
}