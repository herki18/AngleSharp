namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using Converters;
    using static ValueConverters;

    static class BorderInlineDeclaration
    {
        public static String Name = PropertyNames.BorderInline;
        public static IValueConverter Converter = new BorderInlineAggregator();
        public static ICssValue InitialValue = null;
        public static PropertyFlags Flags = PropertyFlags.Animatable | PropertyFlags.Shorthand;
        public static String[] Longhands = new[]
        {
            PropertyNames.BorderInlineWidth,
            PropertyNames.BorderInlineStyle,
            PropertyNames.BorderInlineColor,
        };

        sealed class BorderInlineAggregator : IValueAggregator, IValueConverter
        {
            private static readonly IValueConverter converter = WithAny(
                LineWidthConverter.Option(InitialValues.BorderInlineStartWidthDecl),
                LineStyleConverter.Option(InitialValues.BorderInlineStartStyleDecl),
                CurrentColorConverter.Option(InitialValues.BorderInlineStartColorDecl));

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
                    var borderInlineWidth = width != null ?
                        new CssFlowRelativeValue(new[] { width, width }) : null;
                    var borderInlineStyle = style != null ?
                        new CssFlowRelativeValue(new[] { style, style }) : null;
                    var borderInlineColor = color != null ?
                        new CssFlowRelativeValue(new[] { color, color }) : null;

                    return new[] { borderInlineWidth, borderInlineStyle, borderInlineColor };
                }

                return null;
            }
        }
    }
}