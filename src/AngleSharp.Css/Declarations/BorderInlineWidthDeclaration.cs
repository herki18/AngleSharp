namespace AngleSharp.Css.Declarations
{
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    /// <summary>
    /// Represents the border-inline-width CSS property.
    /// </summary>
    /// <remarks>
    /// The border-inline-width CSS property defines the width of the logical inline borders of an element,
    /// which maps to a physical border width depending on the element's writing mode, directionality, and text orientation.
    /// </remarks>
    static class BorderInlineWidthDeclaration
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public static String Name = PropertyNames.BorderInlineWidth;

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
        public static IValueConverter Converter = AggregatePeriodic(LineWidthConverter);

        /// <summary>
        /// Gets the initial value of the property.
        /// </summary>
        public static ICssValue InitialValue = null;

        /// <summary>
        /// Gets the flags of the property.
        /// </summary>
        public static PropertyFlags Flags = PropertyFlags.Unitless | PropertyFlags.Animatable | PropertyFlags.Shorthand;

        /// <summary>
        /// Gets the longhands of the property.
        /// </summary>
        public static String[] Longhands = new[]
        {
            PropertyNames.BorderInlineStartWidth,
            PropertyNames.BorderInlineEndWidth,
        };
    }
}