namespace AngleSharp.Dom;

using System;
using Attributes;

[DomName("Element")]
public interface IElement : INode, IParentNode, IChildNode, INonDocumentTypeChildNode
{
    /// <summary>
    ///     Gets the namespace URI of this element.
    ///     This one will not be resolved via its parent, but only yields the provided namespace, if any.
    /// </summary>
    String? GivenNamespaceUri { get; }

    [DomName("prefix")] String? Prefix { get; }

    [DomName("localName")] String LocalName { get; }

    [DomName("namespaceURI")] String? NamespaceUri { get; }

    [DomName("attributes")] INamedNodeMap Attributes { get; }

    [DomName("classList")] ITokenList ClassList { get; }

    [DomName("className")] String? ClassName { get; set; }

    [DomName("id")] String? Id { get; set; }

    /// <summary>
    ///     Gets or sets the inner HTML (excluding the current element)
    /// </summary>
    [DomName("innerHTML")]
    String InnerHtml { get; set; }

    /// <summary>
    ///     Gets or sets the outer HTML (including the current element)
    /// </summary>
    [DomName("outerHTML")]
    String OuterHtml { get; set; }

    [DomName("tagName")] String TagName { get; }

    [DomName("assignedSlot")] IElement? AssignedSlot { get; }

    [DomName("slot")] String? Slot { get; set; }

    [DomName("shadowRoot")] IShadowRoot? ShadowRoot { get; }

    Boolean IsFocused { get; }

    ISourceReference? SourceReference { get; }

    /// <summary>
    ///     Inserts new HTML elements at a position relative to the current element
    /// </summary>
    /// <param name="position">The relation to the current element</param>
    /// <param name="html">The HTML code to generate elements for</param>
    [DomName("insertAdjacentHTML")]
    void Insert(AdjacentPosition position, String html);

    [DomName("hasAttribute")]
    Boolean HasAttribute(String name);

    [DomName("hasAttributeNS")]
    Boolean HasAttribute(String? namespaceUri, String localName);

    /// <summary>
    ///     Returns null if the named attribute does not exist
    /// </summary>
    [DomName("getAttribute")]
    String? GetAttribute(String name);

    [DomName("getAttributeNS")]
    String? GetAttribute(String? namespaceUri, String localName);

    [DomName("setAttribute")]
    void SetAttribute(String name, String? value);

    [DomName("setAttributeNS")]
    void SetAttribute(String? namespaceUri, String name, String? value);

    [DomName("removeAttribute")]
    Boolean RemoveAttribute(String name);

    [DomName("removeAttributeNS")]
    Boolean RemoveAttribute(String? namespaceUri, String localName);

    /// <summary>
    ///     Returns a set of elements which have all the given class names (separated by whitespace)
    /// </summary>
    [DomName("getElementsByClassName")]
    IHtmlCollection<IElement> GetElementsByClassName(String classNames);

    /// <summary>
    ///     Returns elements with the given tag name. Use "*" to match all elements.
    /// </summary>
    [DomName("getElementsByTagName")]
    IHtmlCollection<IElement> GetElementsByTagName(String tagName);

    [DomName("getElementsByTagNameNS")]
    IHtmlCollection<IElement> GetElementsByTagNameNS(String? namespaceUri, String tagName);

    [DomName("matches")]
    Boolean Matches(String selectors);

    /// <summary>
    ///     Returns the closest ancestor (or self) matching the selectors, or null if none found
    /// </summary>
    [DomName("closest")]
    IElement? Closest(String selectors);

    /// <summary>
    ///     Creates a new shadow root if none exists
    /// </summary>
    [DomName("attachShadow")]
    [DomInitDict]
    IShadowRoot AttachShadow(ShadowRootMode mode = ShadowRootMode.Open);
}