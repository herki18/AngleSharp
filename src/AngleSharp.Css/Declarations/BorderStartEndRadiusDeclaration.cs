namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    static class BorderStartEndRadiusDeclaration
    {
        public static String Name = PropertyNames.BorderStartEndRadius;
        public static IValueConverter Converter = BorderRadiusLonghandConverter;
        public static ICssValue InitialValue = InitialValues.BorderRadiusDecl;
        public static PropertyFlags Flags = PropertyFlags.Animatable;
    }
}