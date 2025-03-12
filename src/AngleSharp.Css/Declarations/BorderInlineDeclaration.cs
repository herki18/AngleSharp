namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-inline CSS property.
    /// </summary>
    /// <remarks>
    /// The border-inline CSS property is a shorthand property for setting the individual logical inline border properties
    /// border-inline-start and border-inline-end in a single declaration.
    /// </remarks>
    static class BorderInlineDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderInline;

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
            PropertyNames.BorderInlineWidth,
            PropertyNames.BorderInlineStyle,
            PropertyNames.BorderInlineColor,
        };
    }
}