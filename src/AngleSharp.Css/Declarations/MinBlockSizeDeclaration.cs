namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the min-block-size CSS property.
    /// </summary>
    /// <remarks>
    /// The min-block-size CSS property defines the minimum horizontal or vertical size of an element's block,
    /// depending on its writing mode. It corresponds to either the min-width or the min-height property,
    /// depending on the value of writing-mode.
    /// </remarks>
    static class MinBlockSizeDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.MinBlockSize;

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = LengthOrPercentConverter;

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = InitialValues.MinBlockSizeDecl;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Animatable;
    }
}