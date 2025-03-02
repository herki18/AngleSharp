namespace AngleSharp.Dom;

using Attributes;

public enum AdjacentPosition : System.Byte
{
    [DomName("beforebegin")]
    BeforeBegin,
    [DomName("afterbegin")]
    AfterBegin,
    [DomName("beforeend")]
    BeforeEnd,
    [DomName("afterend")]
    AfterEnd
}