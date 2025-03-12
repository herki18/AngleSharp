namespace AngleSharp.Css
{
    using Dom;

    /// <summary>
    /// Provides a contract for useful information regarding a CSS declaration.
    /// </summary>
    public interface IDeclarationInfo
    {
        /// <summary>
        /// Gets the declaration name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the initial value of the declaration, if any.
        /// </summary>
        ICssValue InitialValue { get; }

        /// <summary>
        /// Gets the associated value converter.
        /// </summary>
        IValueConverter Converter { get; }

        /// <summary>
        /// Gets the value aggregator, if any.
        /// </summary>
        IValueAggregator Aggregator { get; }

        /// <summary>
        /// Gets the flags of the declaration.
        /// </summary>
        PropertyFlags Flags { get; }

        /// <summary>
        /// Gets the names of related shorthand declarations.
        /// </summary>
        string[] Shorthands { get; }

        /// <summary>
        /// Gets the names of related required longhand declarations.
        /// </summary>
        string[] Longhands { get; }
    }
}