namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using Converters;
    using static ValueConverters;

    static class BorderBlockDeclaration
    {
        public static String Name = PropertyNames.BorderBlock;
        public static IValueConverter Converter = new BorderBlockAggregator();
        public static ICssValue InitialValue = null;
        public static PropertyFlags Flags = PropertyFlags.Animatable | PropertyFlags.Shorthand;
        public static String[] Longhands = new[]
        {
            PropertyNames.BorderBlockWidth,
            PropertyNames.BorderBlockStyle,
            PropertyNames.BorderBlockColor,
        };

        sealed class BorderBlockAggregator : IValueAggregator, IValueConverter
        {
            private static readonly IValueConverter converter = WithAny(
                LineWidthConverter.Option(InitialValues.BorderBlockStartWidthDecl),
                LineStyleConverter.Option(InitialValues.BorderBlockStartStyleDecl),
                CurrentColorConverter.Option(InitialValues.BorderBlockStartColorDecl));

            public ICssValue Convert(StringSource source) => converter.Convert(source);

            public ICssValue Merge(ICssValue[] values)
            {
                var width = values[0];
                var style = values[1];
                var color = values[2];

                if (width != null || style != null || color != null)
                {
                    return new CssTupleValue(new[] { width, style, color });
                }

                return null;
            }

            public ICssValue[] Split(ICssValue value)
            {
                if (value is CssTupleValue tuple)
                {
                    var width = tuple.Items[0];
                    var style = tuple.Items[1];
                    var color = tuple.Items[2];

                    // Create flow-relative values for each middle-level shorthand
                    var borderBlockWidth = width != null ?
                        new CssFlowRelativeValue(new[] { width, width }) : null;
                    var borderBlockStyle = style != null ?
                        new CssFlowRelativeValue(new[] { style, style }) : null;
                    var borderBlockColor = color != null ?
                        new CssFlowRelativeValue(new[] { color, color }) : null;

                    return new[] { borderBlockWidth, borderBlockStyle, borderBlockColor };
                }

                return null;
            }
        }
    }
}