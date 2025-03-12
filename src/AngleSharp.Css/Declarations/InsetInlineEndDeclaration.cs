namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the inset-inline-end CSS property.
    /// </summary>
    /// <remarks>
    /// The inset-inline-end CSS property defines the logical inline end offset of an element,
    /// which maps to a physical offset depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class InsetInlineEndDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.InsetInlineEnd;

        /// <summary>
        /// Gets the collection of shorthands that contain this property.
        /// </summary>
        public static String[] Shorthands = new[]
        {
            PropertyNames.Inset,
            PropertyNames.InsetInline,
        };

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = AutoLengthOrPercentConverter;

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = InitialValues.InsetInlineEndDecl;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Unitless | PropertyFlags.Animatable;
    }
}