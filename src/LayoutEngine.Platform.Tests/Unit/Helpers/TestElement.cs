namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Dom;
using IElement = Contracts.Platform.Dom.IElement;

/// <summary>
/// Test element for testing
/// </summary>
public class TestElement : IElement
{
    private readonly TestDocument _document;
    private readonly List<IDomNode> _childNodes = new List<IDomNode>();
    private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();
    private TestElement? _parentElement;

    public TestElement(TestDocument document, string tagName)
    {
        _document = document;
        TagName = tagName;
    }

    public NodeType NodeType => NodeType.Element;
    public string NodeName => TagName.ToUpperInvariant();
    public IDomNode? ParentNode => _parentElement;
    public string Id => GetAttribute("id") ?? string.Empty;
    public string TagName { get; }
    public IElement? ParentElement => _parentElement;

    public IDomNode[] ChildNodes => _childNodes.ToArray();

    public string? GetAttribute(string name)
    {
        if (_attributes.TryGetValue(name.ToLowerInvariant(), out var value))
        {
            return value;
        }

        return null;
    }

    public void SetAttribute(string name, string? value)
    {
        var lowerName = name.ToLowerInvariant();

        if (value == null)
        {
            _attributes.Remove(lowerName);
        }
        else
        {
            _attributes[lowerName] = value;

            if (lowerName == "id")
            {
                _document.RegisterElementById(value, this);
            }
        }
    }

    public bool HasAttribute(string name)
    {
        return _attributes.ContainsKey(name.ToLowerInvariant());
    }

    public void RemoveAttribute(string name)
    {
        _attributes.Remove(name.ToLowerInvariant());
    }

    public IElement[] GetElementsByTagName(string tagName)
    {
        var elements = new List<IElement>();
        var lowerTagName = tagName.ToLowerInvariant();

        // For DIRECT children ONLY:
        // If their tag matches or if using wildcard, add them
        foreach (var child in _childNodes)
        {
            if (child is TestElement childElement)
            {
                if (lowerTagName == "*" || childElement.TagName.ToLowerInvariant() == lowerTagName)
                {
                    elements.Add(childElement);
                }

                // Also add any matching elements from this child's descendants
                elements.AddRange(childElement.GetElementsByTagName(tagName));
            }
        }

        return elements.ToArray();
    }

    /// <summary>
    /// Adds a child node
    /// </summary>
    public void AppendChild(IDomNode node)
    {
        if (node is TestElement element)
        {
            element._parentElement = this;
        }

        _childNodes.Add(node);
    }

    /// <summary>
    /// Removes a child node
    /// </summary>
    public void RemoveChild(IDomNode node)
    {
        if (node is TestElement element)
        {
            element._parentElement = null;
        }

        _childNodes.Remove(node);
    }
}