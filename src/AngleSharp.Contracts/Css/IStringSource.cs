namespace AngleSharp.Text
{
    using System;

    /// <summary>
    /// Represents a string abstraction for micro parsers.
    /// </summary>
    public interface IStringSource
    {
        /// <summary>
        /// Gets the current character.
        /// </summary>
        Char Current { get; }

        /// <summary>
        /// Gets if the content has been fully scanned.
        /// </summary>
        Boolean IsDone { get; }

        /// <summary>
        /// Gets the current index.
        /// </summary>
        Int32 Index { get; }

        /// <summary>
        /// Gets the underlying content.
        /// </summary>
        String Content { get; }

        /// <summary>
        /// Advances by one character and returns the character.
        /// </summary>
        /// <returns>The next character.</returns>
        Char Next();

        /// <summary>
        /// Goes back by one character and returns the character.
        /// </summary>
        /// <returns>The previous character.</returns>
        Char Back();
    }
}