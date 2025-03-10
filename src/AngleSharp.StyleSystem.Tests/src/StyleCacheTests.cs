using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Moq;

namespace AngleSharp.StyleSystem.Tests;

using Interfaces;
using Models;
using Storage;

[TestFixture]
public class StyleCacheTests
{
    private StyleCache _styleCache;
    private IHtmlParser _parser;
    private IDocument _document;
    private Mock<IComputedStyle> _mockStyle;

    [SetUp]
    public void Setup()
    {
        _styleCache = new StyleCache();
        _parser = new HtmlParser();
        // Create a proper document structure instead of an empty document
        _document = _parser.ParseDocument("<html><head></head><body></body></html>");
        _mockStyle = new Mock<IComputedStyle>();
    }

    [TearDown]
    public void TearDown()
    {
        _document?.Dispose();
    }

    [Test]
    public void TryGetValue_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);
        var key = new StyleCacheKey(element, null);

        // Act
        var result = _styleCache.TryGetValue(key, out var style);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(style, Is.Null);
    }

    [Test]
    public void Store_StoresStyleForKey()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);
        var key = new StyleCacheKey(element, null);

        // Act
        _styleCache.Store(key, _mockStyle.Object);
        var result = _styleCache.TryGetValue(key, out var style);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(style, Is.SameAs(_mockStyle.Object));
    }

    [Test]
    public void Remove_RemovesStyleForKey()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);
        var key = new StyleCacheKey(element, null);
        _styleCache.Store(key, _mockStyle.Object);

        // Verify it's stored
        var preCheckResult = _styleCache.TryGetValue(key, out _);
        Assert.That(preCheckResult, Is.True, "Style should be stored initially");

        // Act
        _styleCache.Remove(key);
        var result = _styleCache.TryGetValue(key, out var style);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(style, Is.Null);
    }

    [Test]
    public void Clear_RemovesAllStyles()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        var element2 = _document.CreateElement("span");
        _document.Body!.AppendChild(element1);
        _document.Body!.AppendChild(element2);

        var key1 = new StyleCacheKey(element1, null);
        var key2 = new StyleCacheKey(element2, null);

        _styleCache.Store(key1, _mockStyle.Object);
        _styleCache.Store(key2, _mockStyle.Object);

        // Verify they're stored
        var preCheck1 = _styleCache.TryGetValue(key1, out _);
        var preCheck2 = _styleCache.TryGetValue(key2, out _);
        Assert.That(preCheck1, Is.True, "Style 1 should be stored initially");
        Assert.That(preCheck2, Is.True, "Style 2 should be stored initially");

        // Act
        _styleCache.Clear();

        var result1 = _styleCache.TryGetValue(key1, out _);
        var result2 = _styleCache.TryGetValue(key2, out _);

        // Assert
        Assert.That(result1, Is.False, "Style 1 should be removed");
        Assert.That(result2, Is.False, "Style 2 should be removed");
    }

    [Test]
    public void Store_ReplacesExistingStyle()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);
        var key = new StyleCacheKey(element, null);
        var mockStyle1 = new Mock<IComputedStyle>();
        var mockStyle2 = new Mock<IComputedStyle>();

        _styleCache.Store(key, mockStyle1.Object);

        // Act
        _styleCache.Store(key, mockStyle2.Object);
        var result = _styleCache.TryGetValue(key, out var style);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(style, Is.SameAs(mockStyle2.Object));
        Assert.That(style, Is.Not.SameAs(mockStyle1.Object));
    }

    [Test]
    public void StyleCacheKey_EqualityWorks()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        var element2 = _document.CreateElement("div");
        _document.Body!.AppendChild(element1);
        _document.Body!.AppendChild(element2);

        var key1 = new StyleCacheKey(element1, null);
        var key2 = new StyleCacheKey(element1, null); // Same element, no pseudo-element
        var key3 = new StyleCacheKey(element1, "hover"); // Same element, with pseudo-element
        var key4 = new StyleCacheKey(element2, null); // Different element

        // Act & Assert
        Assert.That(key1, Is.EqualTo(key2), "Keys with same element and no pseudo-element should be equal");
        Assert.That(key1, Is.Not.EqualTo(key3), "Keys with same element but different pseudo-elements should not be equal");
        Assert.That(key1, Is.Not.EqualTo(key4), "Keys with different elements should not be equal");

        // Hash codes should match for equal objects
        Assert.That(key1.GetHashCode(), Is.EqualTo(key2.GetHashCode()));
    }

    [Test]
    public void TryGetValue_WithPseudoElement_RespectsKey()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);
        var keyNormal = new StyleCacheKey(element, null);
        var keyHover = new StyleCacheKey(element, "hover");

        var mockNormalStyle = new Mock<IComputedStyle>();
        var mockHoverStyle = new Mock<IComputedStyle>();

        _styleCache.Store(keyNormal, mockNormalStyle.Object);
        _styleCache.Store(keyHover, mockHoverStyle.Object);

        // Act
        var resultNormal = _styleCache.TryGetValue(keyNormal, out var styleNormal);
        var resultHover = _styleCache.TryGetValue(keyHover, out var styleHover);

        // Assert
        Assert.That(resultNormal, Is.True);
        Assert.That(resultHover, Is.True);
        Assert.That(styleNormal, Is.SameAs(mockNormalStyle.Object));
        Assert.That(styleHover, Is.SameAs(mockHoverStyle.Object));
    }
}