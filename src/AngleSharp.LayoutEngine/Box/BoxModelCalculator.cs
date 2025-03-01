namespace AngleSharp.LayoutEngine.Box
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.LayoutEngine.Adapters;
    using AngleSharp.LayoutEngine.Core;
    using AngleSharp.LayoutEngine.Style;
    using System;
    using Css;

    /// <summary>
    /// Calculates box model values from CSS style properties.
    /// Enhanced version that uses AngleSharp's native unit conversion.
    /// </summary>
    public class BoxModelCalculator
    {
        private readonly BoxValues _boxValues = new BoxValues();
        private readonly ICssStyleDeclaration _style;
        private readonly DirectStyleResolver _styleResolver;

        /// <summary>
        /// Creates a box model calculator for the specified style and constraints.
        /// Legacy constructor for backward compatibility.
        /// </summary>
        /// <param name="style">The style to calculate values from.</param>
        /// <param name="availableWidth">The available width for percentage calculations.</param>
        public BoxModelCalculator(ICssStyleDeclaration style, float availableWidth)
        {
            _style = style ?? throw new ArgumentNullException(nameof(style));

            // Create a simple adapter for the legacy constructor
            var dimensions = new LegacyRenderDimensionsAdapter(availableWidth);
            _styleResolver = new DirectStyleResolver(style, dimensions);

            CalculateBoxValues();
        }

        /// <summary>
        /// Creates a box model calculator using the provided style and layout context.
        /// </summary>
        /// <param name="style">The style to calculate values from.</param>
        /// <param name="context">The layout context for unit conversion.</param>
        public BoxModelCalculator(ICssStyleDeclaration style, LayoutContext context)
        {
            _style = style ?? throw new ArgumentNullException(nameof(style));

            var dimensions = new AngleSharpRenderDimensionsAdapter(context);
            _styleResolver = new DirectStyleResolver(style, dimensions);

            CalculateBoxValues();
        }

        /// <summary>
        /// Creates a box model calculator using direct style resolution.
        /// </summary>
        /// <param name="style">The style to calculate values from.</param>
        /// <param name="styleResolver">The style resolver to use.</param>
        public BoxModelCalculator(ICssStyleDeclaration style, DirectStyleResolver styleResolver)
        {
            _style = style ?? throw new ArgumentNullException(nameof(style));
            _styleResolver = styleResolver ?? throw new ArgumentNullException(nameof(styleResolver));

            CalculateBoxValues();
        }

        /// <summary>
        /// Gets the calculated box model values.
        /// </summary>
        public BoxValues GetBoxValues() => _boxValues;

        /// <summary>
        /// Calculates all box model values.
        /// </summary>
        private void CalculateBoxValues()
        {
            // Calculate margins
            var margins = _styleResolver.ResolveBoxValues(
                "margin-top", "margin-right", "margin-bottom", "margin-left");

            _boxValues.MarginTop = margins.Top;
            _boxValues.MarginRight = margins.Right;
            _boxValues.MarginBottom = margins.Bottom;
            _boxValues.MarginLeft = margins.Left;

            // Calculate borders
            var borders = _styleResolver.ResolveBoxValues(
                "border-top-width", "border-right-width", "border-bottom-width", "border-left-width");

            _boxValues.BorderTop = borders.Top;
            _boxValues.BorderRight = borders.Right;
            _boxValues.BorderBottom = borders.Bottom;
            _boxValues.BorderLeft = borders.Left;

            // Calculate paddings
            var paddings = _styleResolver.ResolveBoxValues(
                "padding-top", "padding-right", "padding-bottom", "padding-left");

            _boxValues.PaddingTop = paddings.Top;
            _boxValues.PaddingRight = paddings.Right;
            _boxValues.PaddingBottom = paddings.Bottom;
            _boxValues.PaddingLeft = paddings.Left;

            // Calculate content dimensions
            _boxValues.ContentWidth = ComputeWidth();
            _boxValues.ContentHeight = ComputeHeight();
        }

        /// <summary>
        /// Computes the content width based on CSS properties.
        /// </summary>
        private float ComputeWidth()
        {
            // Check if width is specified
            bool isAuto = _styleResolver.IsAuto("width");
            string boxSizing = _style.GetPropertyValue("box-sizing") ?? "content-box";
            bool isBorderBox = boxSizing == "border-box";

            float horizontalInsets = _boxValues.PaddingLeft + _boxValues.PaddingRight +
                                     _boxValues.BorderLeft + _boxValues.BorderRight;

            if (isAuto)
            {
                // Auto width fills the available space
                float availableWidth = _styleResolver.GetContainerWidth();

                return isBorderBox
                    ? Math.Max(0, availableWidth - horizontalInsets)
                    : availableWidth;
            }

            float width = _styleResolver.ResolveLengthValue("width", 0);

            // Apply box-sizing
            if (isBorderBox)
            {
                width = Math.Max(0, width - horizontalInsets);
            }

            return width;
        }

        /// <summary>
        /// Computes the content height based on CSS properties.
        /// </summary>
        private float ComputeHeight()
        {
            // Check if height is specified
            bool isAuto = _styleResolver.IsAuto("height");
            string boxSizing = _style.GetPropertyValue("box-sizing") ?? "content-box";
            bool isBorderBox = boxSizing == "border-box";

            if (isAuto)
            {
                // Auto height will be determined by content
                return float.NaN;
            }

            float height = _styleResolver.ResolveLengthValue("height", float.NaN, RenderMode.Vertical);

            // Apply box-sizing
            if (!float.IsNaN(height) && isBorderBox)
            {
                float verticalInsets = _boxValues.PaddingTop + _boxValues.PaddingBottom +
                                       _boxValues.BorderTop + _boxValues.BorderBottom;

                height = Math.Max(0, height - verticalInsets);
            }

            return height;
        }

        /// <summary>
        /// Legacy adapter for backward compatibility.
        /// </summary>
        private class LegacyRenderDimensionsAdapter : AngleSharpRenderDimensionsAdapter
        {
            public LegacyRenderDimensionsAdapter(float availableWidth)
                : base(new LayoutContext(availableWidth, 0))
            {
            }
        }
    }
}