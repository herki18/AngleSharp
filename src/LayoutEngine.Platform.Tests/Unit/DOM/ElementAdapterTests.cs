namespace LayoutEngine.Platform.Tests.Unit.DOM;

using System;
using System.Linq;
using AutoFixture;
using Helpers;
using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Platform.DOM;
using Xunit;

public class ElementAdapterTests
{
    private readonly Fixture _fixture;
    private readonly ElementAdapter _elementAdapter;
    private readonly TestDocument _document;
    private readonly TestElement _element;

    public ElementAdapterTests()
    {
        _fixture = new Fixture();
        _elementAdapter = new ElementAdapter();
        _document = new TestDocument();
        _element = _document.CreateElement("div");
        ((TestElement)_document.DocumentElement).AppendChild(_element);
    }

    [Fact]
    public void GetId_ShouldReturnElementId()
    {
        // Arrange
        string expectedId = "test-id";
        _element.SetAttribute("id", expectedId);

        // Act
        string id = _elementAdapter.GetId(_element);

        // Assert
        Assert.Equal(expectedId, id);
    }

    [Fact]
    public void GetId_WithNullElement_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _elementAdapter.GetId(null!));
    }

    [Fact]
    public void GetTagName_ShouldReturnLowercaseTagName()
    {
        // Arrange - TestElement always has uppercase NodeName
        var upperCaseElement = _document.CreateElement("DIV");

        // Act
        string tagName = _elementAdapter.GetTagName(upperCaseElement);

        // Assert
        Assert.Equal("div", tagName);
    }

    [Fact]
    public void GetTagName_WithNullElement_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _elementAdapter.GetTagName(null!));
    }

    [Fact]
    public void GetAttribute_ShouldReturnAttributeValue()
    {
        // Arrange
        string attributeName = "data-test";
        string expectedValue = "test-value";
        _element.SetAttribute(attributeName, expectedValue);

        // Act
        string? value = _elementAdapter.GetAttribute(_element, attributeName);

        // Assert
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void GetAttribute_WithNonExistentAttribute_ShouldReturnNull()
    {
        // Act
        string? value = _elementAdapter.GetAttribute(_element, "non-existent");

        // Assert
        Assert.Null(value);
    }

    [Theory]
    [InlineData(null, "attribute")]
    [InlineData("element", null)]
    [InlineData("element", "")]
    public void GetAttribute_WithInvalidParameters_ShouldThrow(string elementId, string attributeName)
    {
        // Arrange
        IElement? element = elementId != null ? _element : null;
        string? attribute = attributeName;

        // Act & Assert
        if (element == null)
        {
            Assert.Throws<ArgumentNullException>(() =>
                _elementAdapter.GetAttribute(element!, attribute!));
        }
        else if (string.IsNullOrEmpty(attribute))
        {
            Assert.Throws<ArgumentException>(() =>
                _elementAdapter.GetAttribute(element, attribute!));
        }
    }

    [Fact]
    public void SetAttribute_ShouldUpdateAttributeValue()
    {
        // Arrange
        string attributeName = "data-test";
        string expectedValue = "test-value";

        // Act
        _elementAdapter.SetAttribute(_element, attributeName, expectedValue);

        // Assert
        Assert.Equal(expectedValue, _element.GetAttribute(attributeName));
    }

    [Fact]
    public void SetAttribute_WithNullValue_ShouldRemoveAttribute()
    {
        // Arrange
        string attributeName = "data-test";
        _element.SetAttribute(attributeName, "initial-value");

        // Act
        _elementAdapter.SetAttribute(_element, attributeName, null);

        // Assert
        Assert.Null(_element.GetAttribute(attributeName));
    }

    [Theory]
    [InlineData(null, "attribute", "value")]
    [InlineData("element", null, "value")]
    [InlineData("element", "", "value")]
    public void SetAttribute_WithInvalidParameters_ShouldThrow(string elementId, string attributeName, string attributeValue)
    {
        // Arrange
        IElement? element = elementId != null ? _element : null;
        string? attribute = attributeName;

        // Act & Assert
        if (element == null)
        {
            Assert.Throws<ArgumentNullException>(() =>
                _elementAdapter.SetAttribute(element!, attribute!, attributeValue));
        }
        else if (string.IsNullOrEmpty(attribute))
        {
            Assert.Throws<ArgumentException>(() =>
                _elementAdapter.SetAttribute(element, attribute!, attributeValue));
        }
    }

    [Fact]
    public void GetChildren_ShouldReturnOnlyElementChildren()
    {
        // Arrange
        var child1 = _document.CreateElement("span");
        var child2 = _document.CreateElement("div");
        _element.AppendChild(child1);
        _element.AppendChild(child2);

        // Act
        var children = _elementAdapter.GetChildren(_element);

        // Assert
        Assert.Equal(2, children.Count);
        Assert.Contains(child1, children);
        Assert.Contains(child2, children);
    }

    [Fact]
    public void GetChildren_WithNoChildren_ShouldReturnEmptyList()
    {
        // Act
        var children = _elementAdapter.GetChildren(_element);

        // Assert
        Assert.Empty(children);
    }

    [Fact]
    public void GetChildren_WithNullElement_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _elementAdapter.GetChildren(null!));
    }

    [Fact]
    public void GetParent_ShouldReturnParentElement()
    {
        // Arrange
        var childElement = _document.CreateElement("span");
        _element.AppendChild(childElement);

        // Act
        var parent = _elementAdapter.GetParent(childElement);

        // Assert
        Assert.Equal(_element, parent);
    }

    [Fact]
    public void GetParent_WithNoParent_ShouldReturnNull()
    {
        // Act
        var parent = _elementAdapter.GetParent(_document.DocumentElement);

        // Assert
        Assert.Null(parent);
    }

    [Fact]
    public void GetParent_WithNullElement_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _elementAdapter.GetParent(null!));
    }

    [Fact]
    public void HasAttribute_ShouldReturnCorrectValue()
    {
        // Arrange
        string attributeName = "data-test";
        _element.SetAttribute(attributeName, "test-value");

        // Act
        bool hasAttribute = _elementAdapter.HasAttribute(_element, attributeName);
        bool missingAttribute = _elementAdapter.HasAttribute(_element, "non-existent");

        // Assert
        Assert.True(hasAttribute);
        Assert.False(missingAttribute);
    }

    [Fact]
    public void RemoveAttribute_ShouldRemoveAttribute()
    {
        // Arrange
        string attributeName = "data-test";
        _element.SetAttribute(attributeName, "test-value");

        // Act
        _elementAdapter.RemoveAttribute(_element, attributeName);

        // Assert
        Assert.Null(_element.GetAttribute(attributeName));
    }

    [Fact]
    public void GetElementById_ShouldReturnMatchingElement()
    {
        // Arrange
        string id = "test-id";
        var idElement = _document.CreateElement("span");
        idElement.SetAttribute("id", id);
        ((TestElement)_document.DocumentElement).AppendChild(idElement);

        // Act
        var foundElement = _elementAdapter.GetElementById(_document, id);

        // Assert
        Assert.Equal(idElement, foundElement);
    }

    [Fact]
    public void GetElementById_WithNonExistentId_ShouldReturnNull()
    {
        // Act
        var foundElement = _elementAdapter.GetElementById(_document, "non-existent");

        // Assert
        Assert.Null(foundElement);
    }

    [Fact]
    public void GetElementsByTagName_ShouldReturnMatchingElements()
    {
        // Arrange
        var span1 = _document.CreateElement("span");
        var span2 = _document.CreateElement("span");
        var div = _document.CreateElement("div");

        _element.AppendChild(span1);
        _element.AppendChild(div);
        div.AppendChild(span2);

        // Act
        var spans = _elementAdapter.GetElementsByTagName(_element, "span");
        var divs = _elementAdapter.GetElementsByTagName(_element, "div");
        var all = _elementAdapter.GetElementsByTagName(_element, "*");

        // Assert
        // In standard browser behavior:
        // - spans should have 2 elements: span1 and span2 (descendants only)
        Assert.Equal(2, spans.Count);

        // - divs should have 1 element: the child div (not including _element itself)
        Assert.Single(divs);

        // - all should have 3 elements: span1, div, and span2 (all descendants, not including _element)
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void GetElementsByTagName_WithCaseDifference_ShouldBeInsensitive()
    {
        // Arrange
        var span = _document.CreateElement("span");
        _element.AppendChild(span);

        // Act
        var spans1 = _elementAdapter.GetElementsByTagName(_element, "span");
        var spans2 = _elementAdapter.GetElementsByTagName(_element, "SPAN");

        // Assert
        Assert.Equal(spans1.Count, spans2.Count);
        Assert.Equal(spans1.First(), spans2.First());
    }
}