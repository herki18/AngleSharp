namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Converters;
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the inset-inline CSS property.
    /// </summary>
    /// <remarks>
    /// The inset-inline CSS property defines the logical inline start and end offsets of an element,
    /// which maps to physical offsets depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class InsetInlineDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.InsetInline;

        /// <summary>
        /// Gets the collection of shorthands that contain this property.
        /// </summary>
        public static String[] Shorthands = new[]
        {
            PropertyNames.Inset,
        };

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = new InsetInlineAggregator();

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = null;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Animatable | PropertyFlags.Shorthand;

        /// <summary>
        /// Gets the longhands of the property.
        /// </summary>
        public static String[] Longhands = new[]
        {
            PropertyNames.InsetInlineStart,
            PropertyNames.InsetInlineEnd,
        };

        /// <summary>
        /// Custom aggregator for inset-inline that handles flow-relative values correctly.
        /// </summary>
        sealed class InsetInlineAggregator : IValueAggregator, IValueConverter
        {
            private static readonly IValueConverter converter = AutoLengthOrPercentConverter.FlowRelative();

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