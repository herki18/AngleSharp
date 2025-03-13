namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    static class OverflowBlockDeclaration
    {
        public static String Name = PropertyNames.OverflowBlock;
        public static IValueConverter Converter = OverflowExtendedModeConverter;
        public static ICssValue InitialValue = InitialValues.OverflowDecl;
        public static PropertyFlags Flags = PropertyFlags.None;
    }
}