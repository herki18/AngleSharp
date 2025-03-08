namespace AngleSharp.StyleSystem.Core
{
    using AngleSharp.Dom;

    /// <summary>
    /// Represents a writing mode for text direction and flow.
    /// </summary>
    public readonly struct WritingMode
    {
        /// <summary>
        /// Gets the text direction (LTR or RTL).
        /// </summary>
        public DirectionMode Direction { get; }

        /// <summary>
        /// Gets the writing mode type.
        /// </summary>
        public WritingModeType Mode { get; }

        /// <summary>
        /// Creates a new WritingMode.
        /// </summary>
        /// <param name="direction">The text direction.</param>
        /// <param name="mode">The writing mode type.</param>
        public WritingMode(DirectionMode direction, WritingModeType mode)
        {
            Direction = direction;
            Mode = mode;
        }

        /// <summary>
        /// Gets whether the writing mode is horizontal.
        /// </summary>
        public bool IsHorizontal => Mode == WritingModeType.HorizontalTopToBottom;

        /// <summary>
        /// Gets whether the writing mode is vertical.
        /// </summary>
        public bool IsVertical => !IsHorizontal;

        /// <summary>
        /// Gets whether the text direction is right-to-left.
        /// </summary>
        public bool IsRightToLeft => Direction == DirectionMode.Rtl;

        /// <summary>
        /// Creates a standard horizontal left-to-right writing mode.
        /// </summary>
        public static WritingMode HorizontalLtr => new WritingMode(DirectionMode.Ltr, WritingModeType.HorizontalTopToBottom);

        /// <summary>
        /// Creates a horizontal right-to-left writing mode.
        /// </summary>
        public static WritingMode HorizontalRtl => new WritingMode(DirectionMode.Rtl, WritingModeType.HorizontalTopToBottom);

        /// <summary>
        /// Creates a vertical right-to-left writing mode.
        /// </summary>
        public static WritingMode VerticalRl => new WritingMode(DirectionMode.Ltr, WritingModeType.VerticalRightToLeft);

        /// <summary>
        /// Creates a vertical left-to-right writing mode.
        /// </summary>
        public static WritingMode VerticalLr => new WritingMode(DirectionMode.Ltr, WritingModeType.VerticalLeftToRight);
    }

    /// <summary>
    /// Writing mode types.
    /// </summary>
    public enum WritingModeType
    {
        /// <summary>
        /// Standard horizontal writing from top to bottom.
        /// </summary>
        HorizontalTopToBottom,

        /// <summary>
        /// Vertical writing from right to left.
        /// </summary>
        VerticalRightToLeft,

        /// <summary>
        /// Vertical writing from left to right.
        /// </summary>
        VerticalLeftToRight,

        /// <summary>
        /// Sideways writing from right to left.
        /// </summary>
        SidewaysRightToLeft,

        /// <summary>
        /// Sideways writing from left to right.
        /// </summary>
        SidewaysLeftToRight
    }
}