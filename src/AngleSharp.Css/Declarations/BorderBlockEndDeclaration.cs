namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using Converters;
    using static ValueConverters;

    static class BorderBlockEndDeclaration
    {
        public static String Name = PropertyNames.BorderBlockEnd;
        public static String[] Shorthands = new[]
        {
            PropertyNames.BorderBlock,
        };
        public static IValueConverter Converter = new BorderBlockEndAggregator();
        public static ICssValue InitialValue = null;
        public static PropertyFlags Flags = PropertyFlags.Animatable | PropertyFlags.Shorthand;
        public static String[] Longhands = new[]
        {
            PropertyNames.BorderBlockEndWidth,
            PropertyNames.BorderBlockEndStyle,
            PropertyNames.BorderBlockEndColor,
        };

        sealed class BorderBlockEndAggregator : IValueAggregator, IValueConverter
        {
            private static readonly IValueConverter converter = WithAny(
                LineWidthConverter.Option(InitialValues.BorderBlockEndWidthDecl),
                LineStyleConverter.Option(InitialValues.BorderBlockEndStyleDecl),
                CurrentColorConverter.Option(InitialValues.BorderBlockEndColorDecl));

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