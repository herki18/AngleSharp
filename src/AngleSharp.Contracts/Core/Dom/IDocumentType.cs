namespace AngleSharp.Dom;

using System;
using Attributes;

[DomName("DocumentType")]
public interface IDocumentType : INode, IChildNode
{
    [DomName("name")]
    String Name { get; }

    [DomName("publicId")]
    String PublicIdentifier { get; }

    [DomName("systemId")]
    String SystemIdentifier { get; }
}