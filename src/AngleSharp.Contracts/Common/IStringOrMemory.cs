namespace AngleSharp.Common;

using System;

/// <summary>
///     Represents a string and equivalent memory representation of this string.
///     Prevents multiple allocations of string by caching it.
/// </summary>
public interface IStringOrMemory
{
    /// <summary>
    ///     Returns memory representation of string
    /// </summary>
    ReadOnlyMemory<Char> Memory { get; }

    /// <summary>
    ///     Length of string
    /// </summary>
    Int32 Length { get; }

    /// <summary>
    ///     Returns character at specified index
    /// </summary>
    Char this[Int32 i] { get; }

    /// <summary>
    ///     Checks if string is null or empty
    /// </summary>
    Boolean IsNullOrEmpty { get; }

    /// <summary>
    ///     Replace all occurrences of target character with replacement character
    /// </summary>
    IStringOrMemory Replace(Char target, Char replacement);

    /// <summary>
    ///     Returns the string representation
    /// </summary>
    String ToString();
}