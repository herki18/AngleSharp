namespace AngleSharp.Dom;

using System;
using Attributes;

public enum DocumentReadyState : Byte
{
    [DomName("loading")] Loading,
    [DomName("interactive")] Interactive,
    [DomName("complete")] Complete
}