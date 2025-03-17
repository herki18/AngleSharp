namespace AngleSharp.Dom;

using System;
using Attributes;

[DomName("NodeFilter")]
public enum FilterResult : Byte
{
    [DomName("FILTER_ACCEPT")] Accept = 1,
    [DomName("FILTER_REJECT")] Reject = 2,
    [DomName("FILTER_SKIP")] Skip = 3
}