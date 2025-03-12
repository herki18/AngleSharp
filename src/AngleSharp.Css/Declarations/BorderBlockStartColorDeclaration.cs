namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-block-start-color CSS property.
    /// </summary>
    /// <remarks>
    /// The border-block-start-color CSS property defines the color of the logical block-start border of an element,
    /// which maps to a physical border color depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class BorderBlockStartColorDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderBlockStartColor;

        /// <summary>
        /// Gets the collection of shorthands that contain this property.
        /// </summary>
        public static String[] Shorthands = new[]
        {
            PropertyNames.BorderBlock,
            PropertyNames.BorderBlockStart,
            PropertyNames.BorderBlockColor,
        };

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = CurrentColorConverter;

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = InitialValues.BorderBlockStartColorDecl;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.None;
    }
}