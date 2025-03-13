namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Converters;
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the padding-inline CSS property.
    /// </summary>
    /// <remarks>
    /// The padding-inline CSS property defines the logical inline start and end padding of an element,
    /// which maps to physical padding depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class PaddingInlineDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.PaddingInline;

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = new PaddingInlineAggregator();

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = null;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Shorthand;

        /// <summary>
        /// Gets the longhands of the property.
        /// </summary>
        public static String[] Longhands = new[]
        {
            PropertyNames.PaddingInlineStart,
            PropertyNames.PaddingInlineEnd,
        };

        /// <summary>
        /// Custom aggregator for padding-inline that handles flow-relative values correctly.
        /// </summary>
        sealed class PaddingInlineAggregator : IValueAggregator, IValueConverter
        {
            private static readonly IValueConverter converter = LengthOrPercentConverter.FlowRelative();

            public ICssValue Convert(StringSource source) => converter.Convert(source);

            public ICssValue Merge(ICssValue[] values)
            {
                var start = values[0];
                var end = values[1];

                if (start != null && end != null)
                {
                    return new CssFlowRelativeValue(new[] { start, end });
                }

                return null;
            }

            public ICssValue[] Split(ICssValue value)
            {
                if (value is CssFlowRelativeValue flowRelative)
                {
                    return new[] { flowRelative.Start, flowRelative.End };
                }

                return null;
            }
        }
    }
}