namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-inline-start CSS property.
    /// </summary>
    /// <remarks>
    /// The border-inline-start CSS property defines the width, style, and color of the logical inline-start border of an element,
    /// which maps to a physical border depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class BorderInlineStartDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderInlineStart;

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
            InitialValues.BorderInlineStartWidthDecl,
            InitialValues.BorderInlineStartStyleDecl,
            InitialValues.BorderInlineStartColorDecl);

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
            PropertyNames.BorderInlineStartWidth,
            PropertyNames.BorderInlineStartStyle,
            PropertyNames.BorderInlineStartColor,
        };
    }
}