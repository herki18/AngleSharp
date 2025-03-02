namespace AngleSharp.Css.Dom;

using System;
using System.Collections.Generic;
using Attributes;

[DomName("MediaList")]
public interface IMediaList : IEnumerable<ICssMedium>, IStyleFormattable
{
    /// <summary>
    ///     Gets or sets the parsable textual representation of the media list.
    ///     This is a comma-separated list of media.
    /// </summary>
    [DomName("mediaText")]
    String MediaText { get; set; }
    [DomName("length")]
    Int32 Length { get; }
    [DomAccessor(Accessors.Getter)]
    [DomName("item")]
    String this[Int32 index] { get; }
    [DomName("appendMedium")]
    void Add(String medium);
    [DomName("removeMedium")]
    void Remove(String medium);
}