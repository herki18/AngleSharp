namespace AngleSharp.Text
{
    using System;

    /// <summary>
    /// Represents a position in the source code.
    /// </summary>
    public interface ITextPosition : IEquatable<ITextPosition>, IComparable<ITextPosition>
    {
        /// <summary>
        /// Gets the line within the document.
        /// The line is 1-based, so the first line has value 1.
        /// </summary>
        Int32 Line { get; }

        /// <summary>
        /// Gets the column within the document.
        /// The column is 1-based, so the first column has value 1.
        /// </summary>
        Int32 Column { get; }

        /// <summary>
        /// Gets the position within the source.
        /// The position is 1-based, so the first character is at position 1.
        /// </summary>
        Int32 Position { get; }

        /// <summary>
        /// Gets the index within the source.
        /// The index is 0-based, so the first character is at index 0.
        /// </summary>
        Int32 Index { get; }

        /// <summary>
        /// Returns a new text position that includes the given offset.
        /// </summary>
        /// <param name="columns">The number of columns to shift.</param>
        /// <returns>The new text position.</returns>
        ITextPosition Shift(Int32 columns);

        /// <summary>
        /// Returns a new text position that is after the given character.
        /// </summary>
        /// <param name="chr">The character to analyze.</param>
        /// <returns>The new text position.</returns>
        ITextPosition After(Char chr);

        /// <summary>
        /// Returns a new text position that is after the given string.
        /// </summary>
        /// <param name="str">The string to analyze.</param>
        /// <returns>The new text position.</returns>
        ITextPosition After(String str);
    }
}