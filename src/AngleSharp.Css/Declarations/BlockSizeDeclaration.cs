namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the block-size CSS property.
    /// </summary>
    /// <remarks>
    /// The block-size CSS property defines the horizontal or vertical size of an element's block,
    /// depending on its writing mode. It corresponds to either the width or the height property,
    /// depending on the value of writing-mode.
    /// </remarks>
    static class BlockSizeDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BlockSize;

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = AutoLengthOrPercentConverter;

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = InitialValues.BlockSizeDecl;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Unitless | PropertyFlags.Animatable;
    }
}