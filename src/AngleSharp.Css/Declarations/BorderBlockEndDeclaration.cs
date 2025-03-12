namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-block-end CSS property.
    /// </summary>
    /// <remarks>
    /// The border-block-end CSS property defines the width, style, and color of the logical block-end border of an element,
    /// which maps to a physical border depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class BorderBlockEndDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderBlockEnd;

        /// <summary>
        /// Gets the collection of shorthands that contain this property.
        /// </summary>
        public static String[] Shorthands = new[]
        {
            PropertyNames.BorderBlock,
        };

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = WithBorderSide(
            InitialValues.BorderBlockEndWidthDecl,
            InitialValues.BorderBlockEndStyleDecl,
            InitialValues.BorderBlockEndColorDecl);

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
            PropertyNames.BorderBlockEndWidth,
            PropertyNames.BorderBlockEndStyle,
            PropertyNames.BorderBlockEndColor,
        };
    }
}