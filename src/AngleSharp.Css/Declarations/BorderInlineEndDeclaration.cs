namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-inline-end CSS property.
    /// </summary>
    /// <remarks>
    /// The border-inline-end CSS property defines the width, style, and color of the logical inline-end border of an element,
    /// which maps to a physical border depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class BorderInlineEndDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderInlineEnd;

        /// <summary>
        /// Gets the collection of shorthands that contain this property.
        /// </summary>
        public static String[] Shorthands = new[]
        {
            PropertyNames.BorderInline,
        };

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = WithBorderSide(
            InitialValues.BorderInlineEndWidthDecl,
            InitialValues.BorderInlineEndStyleDecl,
            InitialValues.BorderInlineEndColorDecl);

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = null;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Animatable | PropertyFlags.Shorthand;

        /// <summary>
        /// Gets the longhands of the property.
        /// </summary>
        public static String[] Longhands = new[]
        {
            PropertyNames.BorderInlineEndWidth,
            PropertyNames.BorderInlineEndStyle,
            PropertyNames.BorderInlineEndColor,
        };
    }
}