namespace AngleSharp.Dom;

using System;
using Attributes;

/// <summary>
///     The CharacterData abstract interface represents a Node object that
///     contains characters.
/// </summary>
[DomName("CharacterData")]
public interface ICharacterData : INode, IChildNode, INonDocumentTypeChildNode
{
    [DomName("data")] String Data { get; set; }

    [DomName("length")] Int32 Length { get; }

    /// <summary>
    ///     Returns a string containing the part of Data of the specified
    ///     length and starting at the specified offset.
    /// </summary>
    [DomName("substringData")]
    String Substring(Int32 offset, Int32 count);

    /// <summary>
    ///     Appends the given value to the Data string.
    /// </summary>
    [DomName("appendData")]
    void Append(String value);

    /// <summary>
    ///     Inserts the specified characters, at the specified offset,
    ///     in the Data text.
    /// </summary>
    [DomName("insertData")]
    void Insert(Int32 offset, String value);

    /// <summary>
    ///     Removes the specified amount of characters, starting at
    ///     the specified offset, from the Data.
    /// </summary>
    [DomName("deleteData")]
    void Delete(Int32 offset, Int32 count);

    /// <summary>
    ///     Replaces the specified amount of characters, starting at the
    ///     specified offset, with the specified value.
    /// </summary>
    [DomName("replaceData")]
    void Replace(Int32 offset, Int32 count, String value);
}