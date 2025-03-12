namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the max-inline-size CSS property.
    /// </summary>
    /// <remarks>
    /// The max-inline-size CSS property defines the maximum horizontal or vertical size of an element's block,
    /// depending on its writing mode. It corresponds to either the max-width or the max-height property,
    /// depending on the value of writing-mode.
    /// </remarks>
    static class MaxInlineSizeDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.MaxInlineSize;

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = OptionalLengthOrPercentConverter;

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = InitialValues.MaxInlineSizeDecl;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Animatable;
    }
}