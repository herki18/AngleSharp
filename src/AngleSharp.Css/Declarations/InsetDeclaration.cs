namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Converters;
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the inset CSS property.
    /// </summary>
    /// <remarks>
    /// The inset CSS property is a shorthand that corresponds to the top, right, bottom, and/or left properties.
    /// It has the same multi-value syntax of the margin shorthand.
    /// </remarks>
    static class InsetDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.Inset;

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = new InsetAggregator();

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
            PropertyNames.Top,
            PropertyNames.Right,
            PropertyNames.Bottom,
            PropertyNames.Left,
        };

        sealed class InsetAggregator : IValueAggregator, IValueConverter
        {
            private static readonly IValueConverter converter = AutoLengthOrPercentConverter.Periodic();

            public ICssValue Convert(StringSource source) => converter.Convert(source);

            public ICssValue Merge(ICssValue[] values)
            {
                var top = values[0];
                var right = values[1];
                var bottom = values[2];
                var left = values[3];

                if (top != null && right != null && bottom != null && left != null)
                {
                    return new CssPeriodicValue(new[] { top, right, bottom, left });
                }

                return null;
            }

            public ICssValue[] Split(ICssValue value)
            {
                if (value is CssPeriodicValue period)
                {
                    return new[] { period.Top, period.Right, period.Bottom, period.Left };
                }

                return null;
            }
        }
    }
}