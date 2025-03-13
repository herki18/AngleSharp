namespace AngleSharp.StyleSystem.Interfaces
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;

    /// <summary>
    /// Represents less commonly used properties in a computed style.
    /// </summary>
    public interface IRareProperties
    {
        /// <summary>
        /// Gets the opacity value.
        /// </summary>
        float Opacity { get; }

        /// <summary>
        /// Gets the z-index value.
        /// </summary>
        int ZIndex { get; }

        /// <summary>
        /// Gets the background color.
        /// </summary>
        CssColorValue BackgroundColor { get; }

        /// <summary>
        /// Gets the border color.
        /// </summary>
        CssColorValue BorderColor { get; }

        /// <summary>
        /// Gets a property value by name.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <returns>The property value, or null if not set.</returns>
        ICssValue? GetValue(string propertyName);

        /// <summary>
        /// Sets a property value.
        /// </summary>
        /// <param name="propertyName">The name of the property to set.</param>
        /// <param name="value">The value to set.</param>
        void SetValue(string propertyName, ICssValue value);
    }
}