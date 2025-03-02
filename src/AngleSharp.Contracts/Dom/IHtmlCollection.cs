namespace AngleSharp.Dom;

using System;
using System.Collections.Generic;
using Attributes;

/// <summary>
///     HTMLCollection is an interface representing a generic collection
///     (array) of elements (in document order) and offers methods and
///     properties for selecting from the list.
/// </summary>
[DomName("HTMLCollection")]
public interface IHtmlCollection<T> : IEnumerable<T>
    where T : IElement
{
    [DomName("length")]
    Int32 Length { get; }

    [DomName("item")]
    [DomAccessor(Accessors.Getter)]
    T this[Int32 index] { get; }

    /// <summary>
    ///     Gets the specific node whose ID or, as a fallback, name matches the
    ///     string specified by name. Matching by name is only done as a last
    ///     resort, only in HTML, and only if the referenced element supports
    ///     the name attribute.
    /// </summary>
    [DomName("namedItem")]
    [DomAccessor(Accessors.Getter)]
    T? this[String id] { get; }
}