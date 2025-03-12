namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-block-start CSS property.
    /// </summary>
    /// <remarks>
    /// The border-block-start CSS property defines the width, style, and color of the logical block-start border of an element,
    /// which maps to a physical border depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class BorderBlockStartDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderBlockStart;

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
            PropertyNames.BorderBlockStartWidth,
            PropertyNames.BorderBlockStartStyle,
            PropertyNames.BorderBlockStartColor,
        };
    }
}