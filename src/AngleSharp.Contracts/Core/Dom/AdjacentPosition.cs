namespace AngleSharp.Dom;

using System;
using Attributes;

public enum AdjacentPosition : Byte
{
    [DomName("beforebegin")] BeforeBegin,
    [DomName("afterbegin")] AfterBegin,
    [DomName("beforeend")] BeforeEnd,
    [DomName("afterend")] AfterEnd
}