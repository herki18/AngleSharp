namespace LayoutEngine.Contracts.StyleSystem; // Changed Namespace

using AngleSharp.Dom;

/// <summary>
/// Represents the writing mode and direction of text.
/// </summary>
public readonly struct WritingMode
{
    /// <summary>Gets the text direction (e.g., LTR or RTL).</summary>
    public DirectionMode Direction { get; }

    /// <summary>Gets the writing mode type (e.g., horizontal or vertical).</summary>
    public WritingModeType Mode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WritingMode"/> struct.
    /// </summary>
    /// <param name="direction">The text direction.</param>
    /// <param name="mode">The writing mode type.</param>
    public WritingMode(DirectionMode direction, WritingModeType mode)
    {
        Direction = direction;
        Mode = mode;
    }

    /// <summary>Gets a value indicating whether the writing mode is horizontal.</summary>
    public bool IsHorizontal => Mode == WritingModeType.HorizontalTopToBottom;

    /// <summary>Gets a value indicating whether the writing mode is vertical.</summary>
    public bool IsVertical => !IsHorizontal;

    /// <summary>Gets a value indicating whether the direction is right-to-left.</summary>
    public bool IsRightToLeft => Direction == DirectionMode.Rtl;

    // Static instances can be kept if useful
    public static WritingMode HorizontalLtr => new(DirectionMode.Ltr, WritingModeType.HorizontalTopToBottom);
    public static WritingMode HorizontalRtl => new(DirectionMode.Rtl, WritingModeType.HorizontalTopToBottom);
    public static WritingMode VerticalRl => new(DirectionMode.Ltr, WritingModeType.VerticalRightToLeft);
    public static WritingMode VerticalLr => new(DirectionMode.Ltr, WritingModeType.VerticalLeftToRight);
}

/// <summary>
/// Defines the different types of writing modes available.
/// </summary>
public enum WritingModeType
{
    HorizontalTopToBottom,
    VerticalRightToLeft,
    VerticalLeftToRight,
    SidewaysRightToLeft,
    SidewaysLeftToRight
}