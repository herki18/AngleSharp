using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.StyleSystem.Core;
using Moq;

namespace AngleSharp.StyleSystem.Tests;

using Integration;
using Models;

[TestFixture]
public class StyleEngineTests
{
    private IBrowsingContext _context;
    private StyleEngine _styleEngine;
    private IHtmlDocument _document;
    private IHtmlParser _parser;
    private Mock<IRenderDevice> _mockRenderDevice;

    [SetUp]
    public void Setup()
    {
        // Setup browsing context with necessary services
        var config = Configuration.Default
            .WithCss()
            .WithDefaultLoader();

        _context = BrowsingContext.New(config);
        _mockRenderDevice = new Mock<IRenderDevice>();
        _mockRenderDevice.Setup(d => d.ViewPortWidth).Returns(1024);
        _mockRenderDevice.Setup(d => d.ViewPortHeight).Returns(768);
        _mockRenderDevice.Setup(d => d.FontSize).Returns(16.0);

        // Create style engine with the context
        _styleEngine = new StyleEngine(_context);
        _styleEngine.RenderDevice = _mockRenderDevice.Object;

        // Setup HTML parser
        _parser = new HtmlParser();
        _document = _parser.ParseDocument("");

        Assert.IsNotNull(_context);
        Assert.IsNotNull(_styleEngine);
        Assert.IsNotNull(_document);
    }

    [TearDown]
    public void Teardown()
    {
        _context?.Dispose();
        _document?.Dispose();
        _styleEngine?.Dispose();
    }

    [Test]
    public void StyleEngine_Construction_ShouldNotBeNull()
    {
        Assert.That(_styleEngine, Is.Not.Null);
    }

    [Test]
    public void StyleEngine_RenderDevice_ShouldBeSet()
    {
        Assert.That(_styleEngine.RenderDevice, Is.EqualTo(_mockRenderDevice.Object));
    }

    [Test]
    public void StyleEngine_StyleFactory_ShouldNotBeNull()
    {
        Assert.That(_styleEngine.StyleFactory, Is.Not.Null);
    }

    [Test]
    public void StyleEngine_InvalidationTracker_ShouldNotBeNull()
    {
        Assert.That(_styleEngine.InvalidationTracker, Is.Not.Null);
    }

    [Test]
    public void StyleEngine_StylesheetManager_ShouldNotBeNull()
    {
        Assert.That(_styleEngine.StylesheetManager, Is.Not.Null);
    }

    [Test]
    public void ComputeElementStyle_WithNoStyles_ShouldReturnDefaultStyle()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style, Is.Not.Null);
        Assert.That(style.Display, Is.EqualTo(DisplayMode.Block));
        Assert.That(style.Position, Is.EqualTo(PositionMode.Static));
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 0, 1)"));
    }

    [Test]
    public void ComputeElementStyle_WithInlineStyle_ShouldApplyInlineStyles()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "color: red; font-size: 20px;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style, Is.Not.Null);
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(style.FontSize.ToPixel(_mockRenderDevice.Object), Is.EqualTo(20).Within(0.1));
    }

    [Test]
    public void ComputeElementStyle_WithParentStyle_ShouldInheritProperties()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        parent.SetAttribute("style", "color: blue; font-family: Arial;");
        _document.Body!.AppendChild(parent);

        var child = _document.CreateElement("span");
        parent.AppendChild(child);

        // Act
        var parentStyle = _styleEngine.ComputeElementStyle(parent);
        var childStyle = _styleEngine.ComputeElementStyle(child);

        // Assert
        Assert.That(childStyle, Is.Not.Null);
        Assert.That(childStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(childStyle.Text.FontFamily, Is.EqualTo("Arial"));
    }

    [Test]
    public void ComputeElementStyle_CachesSameElementStyle()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act
        var style1 = _styleEngine.ComputeElementStyle(element);
        var style2 = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style2, Is.SameAs(style1), "Style cache should return the same object");
    }

    [Test]
    public void UpdateStyles_InvalidatesAndRecomputesStyles()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);
        var style1 = _styleEngine.ComputeElementStyle(element);

        // Act
        // Invalidate the element
        _styleEngine.InvalidationTracker.InvalidateElement(element);
        // Update styles
        _styleEngine.UpdateStyles(element);
        var style2 = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style2, Is.Not.SameAs(style1), "Style should be recomputed");
    }

    [Test]
    public void NotifyViewportChanged_InvalidatesDeviceDependentStyles()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "width: 50%; height: 50vh;");  // Device-dependent styles
        _document.Body!.AppendChild(element);
        var style1 = _styleEngine.ComputeElementStyle(element);

        // Verify initial setup - cache should be in place
        var initialCheck = _styleEngine.ComputeElementStyle(element);
        Assert.That(initialCheck, Is.SameAs(style1), "Style should be cached initially");

        // Act
        // Change viewport size and notify style engine
        _mockRenderDevice.Setup(d => d.ViewPortWidth).Returns(800);
        _mockRenderDevice.Setup(d => d.ViewPortHeight).Returns(600);
        _styleEngine.NotifyViewportChanged(800, 600);

        // Get style again - should be different due to invalidation
        var style2 = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style2, Is.Not.SameAs(style1), "Style should be recomputed after viewport change");
    }

    [Test]
    public void StyleEngine_WithStylesheet_AppliesStyles()
    {
        // Arrange
        var css = "div { color: green; }";
        var cssParser = new CssParser();
        var stylesheet = cssParser.ParseStyleSheet(css);

        _styleEngine.StylesheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.Author);

        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 128, 0, 1)"));
    }
}