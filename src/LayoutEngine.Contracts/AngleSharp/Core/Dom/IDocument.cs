namespace AngleSharp.Dom;

using System;
using System.Net;
using Attributes;
using Events;
using Html.Dom;
using Text;

[DomName("Document")]
public interface IDocument : INode, IParentNode, IGlobalEventHandlers, IDocumentStyle, INonElementParentNode, IDisposable
{
    [DomName("all")] IHtmlAllCollection All { get; }

    [DomName("anchors")] IHtmlCollection<IHtmlAnchorElement> Anchors { get; }

    [DomName("implementation")] IImplementation Implementation { get; }

    [DomName("designMode")] String DesignMode { get; set; }

    [DomName("dir")] String? Direction { get; set; }

    [DomName("documentURI")] String DocumentUri { get; }

    [DomName("characterSet")] String CharacterSet { get; }

    /// <summary>
    ///     Gets a value to indicate whether the document is rendered in Quirks mode (BackComp) or Strict mode (CSS1Compat).
    /// </summary>
    [DomName("compatMode")]
    String CompatMode { get; }

    [DomName("URL")] String Url { get; }

    [DomName("contentType")] String ContentType { get; }

    [DomName("doctype")] IDocumentType Doctype { get; }

    [DomName("documentElement")] IElement DocumentElement { get; }

    [DomName("lastModified")] String? LastModified { get; }

    [DomLenientThis]
    [DomName("readyState")]
    DocumentReadyState ReadyState { get; }

    [DomName("location")]
    [DomPutForwards("href")]
    ILocation Location { get; }

    [DomName("forms")] IHtmlCollection<IHtmlFormElement> Forms { get; }

    [DomName("images")] IHtmlCollection<IHtmlImageElement> Images { get; }

    [DomName("scripts")] IHtmlCollection<IHtmlScriptElement> Scripts { get; }

    [DomName("embeds")]
    [DomName("plugins")]
    IHtmlCollection<IHtmlEmbedElement> Plugins { get; }

    [DomName("commands")] IHtmlCollection<IElement> Commands { get; }

    /// <summary>
    ///     Gets a collection of all area and anchor elements in a document with a value for the href attribute.
    /// </summary>
    [DomName("links")]
    IHtmlCollection<IElement> Links { get; }

    [DomName("title")] String? Title { get; set; }

    [DomName("head")] IHtmlHeadElement? Head { get; }

    [DomName("body")] IHtmlElement? Body { get; set; }

    [DomName("cookie")] String Cookie { get; set; }

    [DomName("origin")] String? Origin { get; }

    [DomName("domain")] String Domain { get; set; }

    [DomName("referrer")] String? Referrer { get; }

    /// <summary>
    ///     Gets the currently focused element, that is, the element that will get keystroke events if the user types any.
    /// </summary>
    [DomName("activeElement")]
    IElement? ActiveElement { get; }

    [DomName("currentScript")] IHtmlScriptElement? CurrentScript { get; }

    [DomName("defaultView")] IWindow? DefaultView { get; }

    IBrowsingContext Context { get; }

    IDocument? ImportAncestor { get; }

    ITextSource Source { get; }

    HttpStatusCode StatusCode { get; }

    IEntityProvider Entities { get; }

    [DomName("open")]
    IDocument Open(String type = "text/html", String? replace = null);

    [DomName("close")]
    void Close();

    [DomName("write")]
    void Write(String content);

    [DomName("writeln")]
    void WriteLine(String content);

    [DomName("load")]
    void Load(String url);

    [DomName("getElementsByName")]
    IHtmlCollection<IElement> GetElementsByName(String name);

    [DomName("getElementsByClassName")]
    IHtmlCollection<IElement> GetElementsByClassName(String classNames);

    [DomName("getElementsByTagName")]
    IHtmlCollection<IElement> GetElementsByTagName(String tagName);

    [DomName("getElementsByTagNameNS")]
    IHtmlCollection<IElement> GetElementsByTagName(String? namespaceUri, String tagName);

    [DomName("createEvent")]
    IEvent CreateEvent(String type);

    [DomName("createRange")]
    IRange CreateRange();

    [DomName("createComment")]
    IComment CreateComment(String data);

    [DomName("createDocumentFragment")]
    IDocumentFragment CreateDocumentFragment();

    [DomName("createElement")]
    IElement CreateElement(String name);

    [DomName("createElementNS")]
    IElement CreateElement(String? namespaceUri, String name);

    /// <seealso href="https://dom.spec.whatwg.org/#dom-document-createelementns" />
    [DomName("createElementNS")]
    IElement CreateElement(String? namespaceUri, String name, ElementCreationOptions? options);

    [DomName("createElement")]
    IElement CreateElement(String name, ElementCreationOptions? options);

    [DomName("createAttribute")]
    IAttr CreateAttribute(String name);

    [DomName("createAttributeNS")]
    IAttr CreateAttribute(String? namespaceUri, String name);

    [DomName("createProcessingInstruction")]
    IProcessingInstruction CreateProcessingInstruction(String target, String data);

    [DomName("createTextNode")]
    IText CreateTextNode(String data);

    [DomName("createNodeIterator")]
    INodeIterator CreateNodeIterator(INode root, FilterSettings settings = FilterSettings.All, NodeFilter? filter = null);

    [DomName("createTreeWalker")]
    ITreeWalker CreateTreeWalker(INode root, FilterSettings settings = FilterSettings.All, NodeFilter? filter = null);

    [DomName("importNode")]
    INode Import(INode externalNode, Boolean deep = true);

    [DomName("adoptNode")]
    INode Adopt(INode externalNode);

    [DomName("onreadystatechange")] event DomEventHandler ReadyStateChanged;

    [DomName("hasFocus")]
    Boolean HasFocus();

    [DomName("execCommand")]
    Boolean ExecuteCommand(String commandId, Boolean showUserInterface = false, String value = "");

    [DomName("queryCommandEnabled")]
    Boolean IsCommandEnabled(String commandId);

    [DomName("queryCommandIndeterm")]
    Boolean IsCommandIndeterminate(String commandId);

    [DomName("queryCommandState")]
    Boolean IsCommandExecuted(String commandId);

    [DomName("queryCommandSupported")]
    Boolean IsCommandSupported(String commandId);

    [DomName("queryCommandValue")]
    String? GetCommandValue(String commandId);

    Boolean AddImportUrl(Uri uri);

    Boolean HasImported(Uri uri);
}