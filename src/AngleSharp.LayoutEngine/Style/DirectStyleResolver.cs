namespace AngleSharp.LayoutEngine.Style
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.LayoutEngine.Adapters;
    using System;
    using Css;

    /// <summary>
    /// Resolves CSS values directly from AngleSharp's CSS value system,
    /// avoiding string parsing and ensuring consistent calculations.
    /// </summary>
    public class DirectStyleResolver
    {
        private readonly ICssStyleDeclaration _style;
        private readonly AngleSharpRenderDimensionsAdapter _dimensions;

        /// <summary>
        /// Creates a new direct style resolver.
        /// </summary>
        /// <param name="style">The computed style to resolve values from.</param>
        /// <param name="dimensions">The render dimensions to use for unit conversion.</param>
        public DirectStyleResolver(ICssStyleDeclaration style, AngleSharpRenderDimensionsAdapter dimensions)
        {
            _style = style ?? throw new ArgumentNullException(nameof(style));
            _dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
        }

        /// <summary>
        /// Resolves a length property to pixels.
        /// </summary>
        /// <param name="propertyName">The name of the CSS property.</param>
        /// <param name="defaultValue">The default value if the property is not specified or cannot be resolved.</param>
        /// <param name="mode">The render mode (horizontal or vertical) for unit conversion.</param>
        /// <returns>The resolved value in pixels.</returns>
        public float ResolveLengthValue(string propertyName, float defaultValue = 0, RenderMode mode = RenderMode.Horizontal)
        {
            // Try to get the property
            var property = _style.GetProperty(propertyName) as ICssProperty;
            if (property == null || !string.IsNullOrEmpty(property.Value))
                return defaultValue;

            // Check if the raw value is a length
            if (property.RawValue is CssLengthValue lengthValue)
            {
                try
                {
                    return (float)lengthValue.ToPixel(_dimensions, mode);
                }
                catch (Exception)
                {
                    // If conversion fails, use default value
                    return defaultValue;
                }
            }

            // Fall back to string parsing for other cases
            var value = property.Value;
            if (string.IsNullOrEmpty(value) || value == "auto" || value == "none")
                return defaultValue;

            if (value.EndsWith("px") && float.TryParse(value.TrimEnd('p', 'x'), out float pixels))
                return pixels;

            if (value.EndsWith("%") && float.TryParse(value.TrimEnd('%'), out float percentage))
            {
                var containerSize = mode == RenderMode.Horizontal ? _dimensions.RenderWidth : _dimensions.RenderHeight;
                return (float)(percentage * containerSize * 0.01);
            }

            return defaultValue;
        }

        /// <summary>
        /// Checks if a property has the 'auto' value.
        /// </summary>
        /// <param name="propertyName">The name of the CSS property.</param>
        /// <returns>True if the property has the 'auto' value, otherwise false.</returns>
        public bool IsAuto(string propertyName)
        {
            var property = _style.GetProperty(propertyName) as ICssProperty;
            if (property?.RawValue is CssLengthValue lengthValue && lengthValue.Equals(CssLengthValue.Auto))
            {
                return true;
            }

            var value = _style.GetPropertyValue(propertyName);
            return value == "auto";
        }

        /// <summary>
        /// Resolves a set of box values (margin, padding, border).
        /// </summary>
        /// <param name="topProperty">The top property name.</param>
        /// <param name="rightProperty">The right property name.</param>
        /// <param name="bottomProperty">The bottom property name.</param>
        /// <param name="leftProperty">The left property name.</param>
        /// <returns>A tuple containing the resolved values (top, right, bottom, left).</returns>
        public (float Top, float Right, float Bottom, float Left) ResolveBoxValues(
            string topProperty, string rightProperty, string bottomProperty, string leftProperty)
        {
            return (
                ResolveLengthValue(topProperty),
                ResolveLengthValue(rightProperty),
                ResolveLengthValue(bottomProperty),
                ResolveLengthValue(leftProperty)
            );
        }
    }
}