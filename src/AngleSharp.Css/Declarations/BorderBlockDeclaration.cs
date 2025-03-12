namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-block CSS property.
    /// </summary>
    /// <remarks>
    /// The border-block CSS property is a shorthand property for setting the individual logical block border properties
    /// border-block-start and border-block-end in a single declaration.
    /// </remarks>
    static class BorderBlockDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderBlock;

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = WithBorderSide(
            InitialValues.BorderBlockStartWidthDecl,
            InitialValues.BorderBlockStartStyleDecl,
            InitialValues.BorderBlockStartColorDecl);

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
            PropertyNames.BorderBlockWidth,
            PropertyNames.BorderBlockStyle,
            PropertyNames.BorderBlockColor,
        };
    }
}