namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the inset-block-end CSS property.
    /// </summary>
    /// <remarks>
    /// The inset-block-end CSS property defines the logical block end offset of an element,
    /// which maps to a physical offset depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class InsetBlockEndDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.InsetBlockEnd;

        /// <summary>
        /// Gets the collection of shorthands that contain this property.
        /// </summary>
        public static String[] Shorthands = new[]
        {
            PropertyNames.Inset,
            PropertyNames.InsetBlock,
        };

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = AutoLengthOrPercentConverter;

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = InitialValues.InsetBlockEndDecl;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Unitless | PropertyFlags.Animatable;
    }
}