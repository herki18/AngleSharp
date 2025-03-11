namespace AngleSharp.StyleSystem.Models
{
    /// <summary>
    /// Represents the type of DOM change that occurred.
    /// </summary>
    public enum DomChangeType
    {
        /// <summary>
        /// An attribute was changed.
        /// </summary>
        AttributeChanged,

        /// <summary>
        /// The style attribute was changed.
        /// </summary>
        StyleAttributeChanged,

        /// <summary>
        /// The class attribute was changed.
        /// </summary>
        ClassAttributeChanged,

        /// <summary>
        /// The id attribute was changed.
        /// </summary>
        IdAttributeChanged,

        /// <summary>
        /// A node was added to the DOM.
        /// </summary>
        NodeAdded,

        /// <summary>
        /// A node was removed from the DOM.
        /// </summary>
        NodeRemoved,

        /// <summary>
        /// Text content was changed.
        /// </summary>
        TextChanged,

        /// <summary>
        /// Element structure was changed (children added/removed).
        /// </summary>
        ElementStructureChanged,

        /// <summary>
        /// A stylesheet was added, removed, or changed.
        /// </summary>
        StylesheetChanged
    }
}