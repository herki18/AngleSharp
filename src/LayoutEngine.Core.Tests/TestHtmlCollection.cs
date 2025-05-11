using AngleSharp.Dom;

namespace LayoutEngine.Core.Tests;

public class TestHtmlCollection : IHtmlCollection<IElement>
{
    private readonly List<IElement> _elements;

    public TestHtmlCollection(IEnumerable<IElement> elements)
    {
        _elements = new List<IElement>(elements);
    }

    public int Length => _elements.Count;

    public IElement this[int index] => _elements[index];

    public IElement? this[string id] => _elements.FirstOrDefault(e => e.Id == id || e.GetAttribute("name") == id);

    public IEnumerator<IElement> GetEnumerator() => _elements.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}