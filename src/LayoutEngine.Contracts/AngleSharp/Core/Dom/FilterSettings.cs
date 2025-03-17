namespace AngleSharp.Dom;

using System;
using Attributes;

[Flags]
[DomName("NodeFilter")]
public enum FilterSettings : UInt64
{
    [DomName("SHOW_ALL")] All = 0xffffffff,
    [DomName("SHOW_ELEMENT")] Element = 0x1,

    [DomName("SHOW_ATTRIBUTE")] [DomHistorical]
    Attribute = 0x2,
    [DomName("SHOW_TEXT")] Text = 0x4,

    [DomName("SHOW_CDATA_SECTION")] [DomHistorical]
    CharacterData = 0x8,

    [DomName("SHOW_ENTITY_REFERENCE")] [DomHistorical]
    EntityReference = 0x10,

    [DomName("SHOW_ENTITY")] [DomHistorical]
    Entity = 0x20,

    [DomName("SHOW_PROCESSING_INSTRUCTION")]
    ProcessingInstruction = 0x40,
    [DomName("SHOW_COMMENT")] Comment = 0x80,
    [DomName("SHOW_DOCUMENT")] Document = 0x100,
    [DomName("SHOW_DOCUMENT_TYPE")] DocumentType = 0x200,
    [DomName("SHOW_DOCUMENT_FRAGMENT")] DocumentFragment = 0x400,

    [DomName("SHOW_NOTATION")] [DomHistorical]
    Notation = 0x800
}