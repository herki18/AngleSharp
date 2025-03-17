namespace AngleSharp.Common;

using System;

/// <summary>
///     Represents a string and equivalent memory representation of this string.
///     Prevents multiple allocations of string by caching it.
/// </summary>
public interface IStringOrMemory
{
    ReadOnlyMemory<Char> Memory { get; }
    Int32 Length { get; }
    Char this[Int32 i] { get; }
    Boolean IsNullOrEmpty { get; }
    IStringOrMemory Replace(Char target, Char replacement);
    String ToString();
}