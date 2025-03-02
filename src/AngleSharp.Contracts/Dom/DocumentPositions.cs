namespace AngleSharp.Dom;

using System;
using Attributes;

[Flags]
[DomName("Document")]
public enum DocumentPositions : Byte
{
    Same = 0,

    [DomName("DOCUMENT_POSITION_DISCONNECTED")]
    Disconnected = 0x01,

    [DomName("DOCUMENT_POSITION_PRECEDING")]
    Preceding = 0x02,

    [DomName("DOCUMENT_POSITION_FOLLOWING")]
    Following = 0x04,

    [DomName("DOCUMENT_POSITION_CONTAINS")]
    Contains = 0x08,

    [DomName("DOCUMENT_POSITION_CONTAINED_BY")]
    ContainedBy = 0x10,

    [DomName("DOCUMENT_POSITION_IMPLEMENTATION_SPECIFIC")]
    ImplementationSpecific = 0x20
}