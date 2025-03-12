namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-inline-end-width CSS property.
    /// </summary>
    /// <remarks>
    /// The border-inline-end-width CSS property defines the width of the logical inline-end border of an element,
    /// which maps to a physical border width depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class BorderInlineEndWidthDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderInlineEndWidth;

        /// <summary>
        /// Gets the collection of shorthands that contain this property.
        /// </summary>
        public static String[] Shorthands = new[]
        {
            PropertyNames.BorderInline,
            PropertyNames.BorderInlineEnd,
            PropertyNames.BorderInlineWidth,
        };

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = LineWidthConverter;

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = InitialValues.BorderInlineEndWidthDecl;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Unitless | PropertyFlags.Animatable;
    }
}