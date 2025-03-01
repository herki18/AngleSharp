#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace AngleSharp.LayoutEngine.Style
{
    using AngleSharp.LayoutEngine.Core;
    using AngleSharp.LayoutEngine.FormattingContexts.Enums;

    /// <summary>
    /// Contains the essential style properties needed for layout calculations.
    /// </summary>
    public class LayoutNodeStyle
    {
        /// <summary>
        /// Gets or sets the element's display type.
        /// </summary>
        public DisplayType Display { get; set; }

        /// <summary>
        /// Gets or sets the element's position type.
        /// </summary>
        public PositionType Position { get; set; }

        /// <summary>
        /// Gets or sets the element's float value.
        /// </summary>
        public FloatType Float { get; set; }

        /// <summary>
        /// Gets or sets the element's width value.
        /// </summary>
        public StyleValue Width { get; set; }

        /// <summary>
        /// Gets or sets the element's height value.
        /// </summary>
        public StyleValue Height { get; set; }

        /// <summary>
        /// Gets or sets the direct style resolver for this node.
        /// This provides access to computed values without string parsing.
        /// </summary>
        public DirectStyleResolver StyleResolver { get; set; }
    }
}