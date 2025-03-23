namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Dom;
using IDocument = Contracts.Platform.Dom.IDocument;
using IElement = Contracts.Platform.Dom.IElement;

/// <summary>
/// Test document for testing
/// </summary>
public class TestDocument : IDocument
{
    private readonly TestElement _documentElement;
    private readonly Dictionary<string, TestElement> _elementsById = new Dictionary<string, TestElement>();

    public TestDocument()
    {
        _documentElement = new TestElement(this, "html");
    }

    public NodeType NodeType => NodeType.Document;
    public string NodeName => "#document";
    public IDomNode? ParentNode => null;
    public IElement DocumentElement => _documentElement;

    public IElement? GetElementById(string id)
    {
        if (_elementsById.TryGetValue(id, out var element))
        {
            return element;
        }

        return null;
    }

    /// <summary>
    /// Registers an element by ID
    /// </summary>
    public void RegisterElementById(string id, TestElement element)
    {
        _elementsById[id] = element;
    }

    /// <summary>
    /// Creates a new element
    /// </summary>
    public TestElement CreateElement(string tagName)
    {
        return new TestElement(this, tagName);
    }
}