namespace AngleSharp.Dom;

using Text;

/// <summary>
///     Represents a reference from an element to its original source code.
/// </summary>
public interface ISourceReference
{
    /// <summary>
    ///     Gets the position in the original source code.
    /// </summary>
    ITextPosition Position { get; }
}