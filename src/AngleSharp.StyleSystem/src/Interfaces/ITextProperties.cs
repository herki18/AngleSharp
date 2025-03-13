namespace AngleSharp.StyleSystem.Interfaces
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;

    /// <summary>
    /// Interface for text-related computed style properties.
    /// </summary>
    public interface ITextProperties
    {
        /// <summary>
        /// Gets the computed font family.
        /// </summary>
        string FontFamily { get; }

        /// <summary>
        /// Gets the computed font weight.
        /// </summary>
        int FontWeight { get; }

        /// <summary>
        /// Gets whether the font style is italic.
        /// </summary>
        bool IsItalic { get; }

        /// <summary>
        /// Gets the computed line height.
        /// </summary>
        CssLengthValue LineHeight { get; }

        /// <summary>
        /// Gets the computed text align property.
        /// </summary>
        TextAlign TextAlign { get; }

        /// <summary>
        /// Gets the computed color.
        /// </summary>
        CssColorValue Color { get; }

        /// <summary>
        /// Gets the computed font size.
        /// </summary>
        CssLengthValue FontSize { get; }

        /// <summary>
        /// Gets the font size in pixels.
        /// </summary>
        double FontSizeInPixels { get; }

        /// <summary>
        /// Gets the line height in pixels.
        /// </summary>
        double LineHeightInPixels { get; }

        /// <summary>
        /// Sets the font family from a string value.
        /// </summary>
        /// <param name="fontFamily">The font family value.</param>
        void SetFontFamily(string fontFamily);

        /// <summary>
        /// Sets the font family from a CSS value.
        /// </summary>
        /// <param name="value">The CSS value containing the font family.</param>
        void SetFontFamily(ICssValue value);

        /// <summary>
        /// Sets the font size.
        /// </summary>
        /// <param name="fontSize">The font size value.</param>
        void SetFontSize(CssLengthValue? fontSize);

        /// <summary>
        /// Sets the font weight.
        /// </summary>
        /// <param name="weight">The font weight value.</param>
        void SetFontWeight(int weight);

        /// <summary>
        /// Sets whether the font style is italic.
        /// </summary>
        /// <param name="isItalic">true for italic; otherwise, false.</param>
        void SetIsItalic(bool isItalic);

        /// <summary>
        /// Sets the line height.
        /// </summary>
        /// <param name="lineHeight">The line height value.</param>
        void SetLineHeight(CssLengthValue? lineHeight);

        /// <summary>
        /// Sets the text alignment.
        /// </summary>
        /// <param name="textAlign">The text alignment value.</param>
        void SetTextAlign(TextAlign textAlign);

        /// <summary>
        /// Sets the color from a CSS color value.
        /// </summary>
        /// <param name="color">The color value.</param>
        void SetColor(CssColorValue? color);

        /// <summary>
        /// Sets the color from a string representation.
        /// </summary>
        /// <param name="colorStr">The color string.</param>
        void SetColor(string colorStr);

        /// <summary>
        /// Invalidates any cached computed values.
        /// </summary>
        void InvalidateCache();

        /// <summary>
        /// Creates a font string suitable for rendering APIs.
        /// </summary>
        /// <returns>A formatted font string.</returns>
        string GetFontString();
    }
}