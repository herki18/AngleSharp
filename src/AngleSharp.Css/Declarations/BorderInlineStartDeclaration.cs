namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using Converters;
    using static ValueConverters;

    static class BorderInlineStartDeclaration
    {
        public static String Name = PropertyNames.BorderInlineStart;
        public static String[] Shorthands = new[]
        {
            PropertyNames.BorderInline,
        };
        public static IValueConverter Converter = new BorderInlineStartAggregator();
        public static ICssValue InitialValue = null;
        public static PropertyFlags Flags = PropertyFlags.Animatable | PropertyFlags.Shorthand;
        public static String[] Longhands = new[]
        {
            PropertyNames.BorderInlineStartWidth,
            PropertyNames.BorderInlineStartStyle,
            PropertyNames.BorderInlineStartColor,
        };

        sealed class BorderInlineStartAggregator : IValueAggregator, IValueConverter
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
                    return new[]
                    {
                        tuple.Items[0],
                        tuple.Items[1],
                        tuple.Items[2]
                    };
                }

                return null;
            }
        }
    }
}