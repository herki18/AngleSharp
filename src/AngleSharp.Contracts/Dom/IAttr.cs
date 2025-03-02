namespace AngleSharp.Dom;

using System;
using Attributes;

[DomName("Attr")]
public interface IAttr : INode, IEquatable<IAttr>
{
    [DomName("localName")] String LocalName { get; }

    [DomName("name")] String Name { get; }

    [DomName("value")] String Value { get; set; }

    [DomName("namespaceURI")] String? NamespaceUri { get; }

    [DomName("prefix")] String? Prefix { get; }

    [DomName("ownerElement")] IElement? OwnerElement { get; }

    /// <summary>
    ///     Gets always true.
    /// </summary>
    [DomName("specified")]
    Boolean IsSpecified { get; }
}