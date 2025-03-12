namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-inline-start-color CSS property.
    /// </summary>
    /// <remarks>
    /// The border-inline-start-color CSS property defines the color of the logical inline-start border of an element,
    /// which maps to a physical border color depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class BorderInlineStartColorDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderInlineStartColor;

        /// <summary>
        /// Gets the collection of shorthands that contain this property.
        /// </summary>
        public static String[] Shorthands = new[]
        {
            PropertyNames.BorderInline,
            PropertyNames.BorderInlineStart,
            PropertyNames.BorderInlineColor,
        };

        /// <summary>
        /// Gets the converter for the property.
        /// </summary>
        public static IValueConverter Converter = CurrentColorConverter;

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = InitialValues.BorderInlineStartColorDecl;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.None;
    }
}