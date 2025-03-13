namespace AngleSharp.StyleSystem.Interfaces
{
    using AngleSharp.Css.Values;
    using AngleSharp.StyleSystem.Models;

    /// <summary>
    /// Box-related computed properties.
    /// </summary>
    public interface IBoxProperties
    {
        /// <summary>
        /// Gets the computed width.
        /// </summary>
        CssLengthValue Width { get; }

        /// <summary>
        /// Gets the computed height.
        /// </summary>
        CssLengthValue Height { get; }

        /// <summary>
        /// Gets the logical inline size (width or height depending on writing mode).
        /// </summary>
        CssLengthValue InlineSize { get; }

        /// <summary>
        /// Gets the logical block size (height or width depending on writing mode).
        /// </summary>
        CssLengthValue BlockSize { get; }

        /// <summary>
        /// Gets the margin edges.
        /// </summary>
        Edges Margin { get; }

        /// <summary>
        /// Gets the border edges.
        /// </summary>
        Edges Border { get; }

        /// <summary>
        /// Gets the padding edges.
        /// </summary>
        Edges Padding { get; }

        /// <summary>
        /// Gets the logical margin edges.
        /// </summary>
        LogicalEdges LogicalMargin { get; }

        /// <summary>
        /// Gets the logical border edges.
        /// </summary>
        LogicalEdges LogicalBorder { get; }

        /// <summary>
        /// Gets the logical padding edges.
        /// </summary>
        LogicalEdges LogicalPadding { get; }

        /// <summary>
        /// Sets the width value.
        /// </summary>
        /// <param name="value">The width value to set.</param>
        void SetWidth(CssLengthValue? value);

        /// <summary>
        /// Sets the height value.
        /// </summary>
        /// <param name="value">The height value to set.</param>
        void SetHeight(CssLengthValue? value);

        /// <summary>
        /// Sets the margin top value.
        /// </summary>
        /// <param name="value">The margin top value to set.</param>
        void SetMarginTop(CssLengthValue? value);

        /// <summary>
        /// Sets the margin right value.
        /// </summary>
        /// <param name="value">The margin right value to set.</param>
        void SetMarginRight(CssLengthValue? value);

        /// <summary>
        /// Sets the margin bottom value.
        /// </summary>
        /// <param name="value">The margin bottom value to set.</param>
        void SetMarginBottom(CssLengthValue? value);

        /// <summary>
        /// Sets the margin left value.
        /// </summary>
        /// <param name="value">The margin left value to set.</param>
        void SetMarginLeft(CssLengthValue? value);

        /// <summary>
        /// Sets the border top width value.
        /// </summary>
        /// <param name="value">The border top width value to set.</param>
        void SetBorderTop(CssLengthValue? value);

        /// <summary>
        /// Sets the border right width value.
        /// </summary>
        /// <param name="value">The border right width value to set.</param>
        void SetBorderRight(CssLengthValue? value);

        /// <summary>
        /// Sets the border bottom width value.
        /// </summary>
        /// <param name="value">The border bottom width value to set.</param>
        void SetBorderBottom(CssLengthValue? value);

        /// <summary>
        /// Sets the border left width value.
        /// </summary>
        /// <param name="value">The border left width value to set.</param>
        void SetBorderLeft(CssLengthValue? value);

        /// <summary>
        /// Sets the padding top value.
        /// </summary>
        /// <param name="value">The padding top value to set.</param>
        void SetPaddingTop(CssLengthValue? value);

        /// <summary>
        /// Sets the padding right value.
        /// </summary>
        /// <param name="value">The padding right value to set.</param>
        void SetPaddingRight(CssLengthValue? value);

        /// <summary>
        /// Sets the padding bottom value.
        /// </summary>
        /// <param name="value">The padding bottom value to set.</param>
        void SetPaddingBottom(CssLengthValue? value);

        /// <summary>
        /// Sets the padding left value.
        /// </summary>
        /// <param name="value">The padding left value to set.</param>
        void SetPaddingLeft(CssLengthValue? value);
    }
}