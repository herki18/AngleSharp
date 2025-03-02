namespace AngleSharp.Text;

using System;

/// <summary>
/// Position in the source code
/// </summary>
public interface ITextPosition : IEquatable<ITextPosition>, IComparable<ITextPosition>
{
    /// <summary>
    /// Line number (1-based)
    /// </summary>
    Int32 Line { get; }

    /// <summary>
    /// Column number (1-based)
    /// </summary>
    Int32 Column { get; }

    /// <summary>
    /// Position in source (1-based)
    /// </summary>
    Int32 Position { get; }

    /// <summary>
    /// Index in source (0-based)
    /// </summary>
    Int32 Index { get; }

    /// <summary>
    /// Creates new position shifted by given columns
    /// </summary>
    ITextPosition Shift(Int32 columns);

    /// <summary>
    /// Creates new position after given character
    /// </summary>
    ITextPosition After(Char chr);

    /// <summary>
    /// Creates new position after given string
    /// </summary>
    ITextPosition After(String str);
}