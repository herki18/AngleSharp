namespace AngleSharp.StyleSystem.Tests.MaybeTests;

using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Models;
using Moq;

[TestFixture]
public class ComputedStyleTests
{
    private IBrowsingContext _context;
    private StyleEngine _styleEngine;
    private IHtmlParser _parser;
    private IDocument _document;
    private ICssParser _cssParser;
    private Mock<IRenderDevice> _mockRenderDevice;

    [SetUp]
    public void Setup()
    {
        var config = Configuration.Default.WithCss();
        _context = BrowsingContext.New(config);
        _parser = new HtmlParser();
        _document = _parser.ParseDocument("<html><head></head><body></body></html>");
        _cssParser = new CssParser();

        _mockRenderDevice = new Mock<IRenderDevice>();
        // Basic device properties
        _mockRenderDevice.Setup(d => d.ViewPortWidth).Returns(1024);
        _mockRenderDevice.Setup(d => d.ViewPortHeight).Returns(768);
        _mockRenderDevice.Setup(d => d.FontSize).Returns(16.0);

        // Additional required properties
        _mockRenderDevice.Setup(d => d.Category).Returns(DeviceCategory.Screen);
        _mockRenderDevice.Setup(d => d.IsInterlaced).Returns(false);
        _mockRenderDevice.Setup(d => d.IsScripting).Returns(true);
        _mockRenderDevice.Setup(d => d.IsGrid).Returns(false);
        _mockRenderDevice.Setup(d => d.DeviceWidth).Returns(1024);
        _mockRenderDevice.Setup(d => d.DeviceHeight).Returns(768);
        _mockRenderDevice.Setup(d => d.Resolution).Returns(96);
        _mockRenderDevice.Setup(d => d.Frequency).Returns(60);
        _mockRenderDevice.Setup(d => d.ColorBits).Returns(24);
        _mockRenderDevice.Setup(d => d.MonochromeBits).Returns(0);

        // Setup RenderWidth and RenderHeight as they might be used for calculations
        _mockRenderDevice.Setup(d => d.RenderWidth).Returns(1024.0);
        _mockRenderDevice.Setup(d => d.RenderHeight).Returns(768.0);

        _styleEngine = new StyleEngine(_context);
        _styleEngine.RenderDevice = _mockRenderDevice.Object;

        // Add a default stylesheet to ensure basic styling is available
        var defaultStyle = "body { margin: 0; font-family: sans-serif; }";
        var stylesheet = _cssParser.ParseStyleSheet(defaultStyle);
        _styleEngine.StylesheetManager.RegisterStylesheet(stylesheet, StylesheetOrigin.UserAgent);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _document?.Dispose();
        _styleEngine?.Dispose();
    }

    [Test]
    public void ComputedStyle_Construction_InitializesProperties()
    {
        // Arrange
        var element = _document.CreateElement("div");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style, Is.Not.Null);
        Assert.That(style.Box, Is.Not.Null);
        Assert.That(style.Text, Is.Not.Null);
    }

    [Test]
    public void ComputedStyle_GetPropertyValue_ReturnsCorrectValues()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "color: red; margin: 10px; font-size: 20px;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert - use string comparison for property values
        var colorValue = style.GetPropertyValue("color");
        Assert.That(colorValue, Is.EqualTo("rgba(255, 0, 0, 1)"));

        Assert.That(style.GetPropertyValue("margin-top"), Is.EqualTo("10px"));
        Assert.That(style.GetPropertyValue("margin-bottom"), Is.EqualTo("10px"));
        Assert.That(style.GetPropertyValue("margin-right"), Is.EqualTo("10px"));
        Assert.That(style.GetPropertyValue("margin-left"), Is.EqualTo("10px"));

        Assert.That(style.GetPropertyValue("font-size"), Is.EqualTo("20px"));
    }

    [Test]
    public void ComputedStyle_GetValue_ReturnsTypedValues()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "color: red; font-size: 20px;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);
        var color = style.GetValue<CssColorValue>("color");
        var fontSize = style.GetValue<CssLengthValue>("font-size");

        // Assert - no null checks needed for structs
        Assert.That(color.R, Is.EqualTo(255));
        Assert.That(color.G, Is.EqualTo(0));
        Assert.That(color.B, Is.EqualTo(0));

        Assert.That(fontSize.ToPixel(_mockRenderDevice.Object), Is.EqualTo(20).Within(0.1));
    }

    [Test]
    public void ComputedStyle_DisplayAndPosition_ReturnCorrectValues()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "display: flex; position: absolute;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.Display, Is.EqualTo(DisplayMode.Flex));
        Assert.That(style.Position, Is.EqualTo(PositionMode.Absolute));
    }

    [Test]
    public void ComputedStyle_OpacityAndZIndex_ReturnCorrectValues()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "opacity: 0.5; z-index: 100;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.Opacity, Is.EqualTo(0.5f).Within(0.01));
        Assert.That(style.ZIndex, Is.EqualTo(100));
    }

    [Test]
    public void ComputedStyle_BoxProperties_ReturnCorrectValues()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "width: 200px; height: 100px; margin: 10px; padding: 5px; border-width: 1px;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);
        var box = style.Box;

        // Assert - no null checks needed for value types
        var widthPx = box.Width.ToPixel(_mockRenderDevice.Object);
        var heightPx = box.Height.ToPixel(_mockRenderDevice.Object);
        Assert.That(widthPx, Is.EqualTo(200).Within(0.1));
        Assert.That(heightPx, Is.EqualTo(100).Within(0.1));

        // Check margin
        Assert.That(box.Margin.Top.ToPixel(_mockRenderDevice.Object), Is.EqualTo(10).Within(0.1));
        Assert.That(box.Margin.Right.ToPixel(_mockRenderDevice.Object), Is.EqualTo(10).Within(0.1));
        Assert.That(box.Margin.Bottom.ToPixel(_mockRenderDevice.Object), Is.EqualTo(10).Within(0.1));
        Assert.That(box.Margin.Left.ToPixel(_mockRenderDevice.Object), Is.EqualTo(10).Within(0.1));

        // Check padding
        Assert.That(box.Padding.Top.ToPixel(_mockRenderDevice.Object), Is.EqualTo(5).Within(0.1));
        Assert.That(box.Padding.Right.ToPixel(_mockRenderDevice.Object), Is.EqualTo(5).Within(0.1));
        Assert.That(box.Padding.Bottom.ToPixel(_mockRenderDevice.Object), Is.EqualTo(5).Within(0.1));
        Assert.That(box.Padding.Left.ToPixel(_mockRenderDevice.Object), Is.EqualTo(5).Within(0.1));

        // Check border
        Assert.That(box.Border.Top.ToPixel(_mockRenderDevice.Object), Is.EqualTo(1).Within(0.1));
        Assert.That(box.Border.Right.ToPixel(_mockRenderDevice.Object), Is.EqualTo(1).Within(0.1));
        Assert.That(box.Border.Bottom.ToPixel(_mockRenderDevice.Object), Is.EqualTo(1).Within(0.1));
        Assert.That(box.Border.Left.ToPixel(_mockRenderDevice.Object), Is.EqualTo(1).Within(0.1));
    }

    [Test]
    public void ComputedStyle_TextProperties_ReturnCorrectValues()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "color: blue; font-family: Arial; font-size: 16px; font-weight: bold; line-height: 1.5; text-align: center;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);
        var text = style.Text;

        // Assert - no null checks for structs
        Assert.That(text.Color.R, Is.EqualTo(0));
        Assert.That(text.Color.G, Is.EqualTo(0));
        Assert.That(text.Color.B, Is.EqualTo(255));

        Assert.That(text.FontFamily, Is.EqualTo("Arial"));

        // FontSize is on IComputedStyle, not on ITextProperties
        Assert.That(style.FontSize.ToPixel(_mockRenderDevice.Object), Is.EqualTo(16).Within(0.1));

        Assert.That(text.FontWeight, Is.EqualTo(700)); // "bold" is 700

        // LineHeight is a struct - no null check needed
        Assert.That(text.LineHeight.ToPixel(_mockRenderDevice.Object), Is.EqualTo(24).Within(0.1)); // 1.5 * 16px

        Assert.That(text.TextAlign, Is.EqualTo(TextAlign.Center));
    }

    [Test]
    public void ComputedStyle_WritingMode_ReturnsCorrectValues()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "direction: rtl; writing-mode: vertical-rl;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);
        var writingMode = style.WritingMode;

        // Assert - WritingMode is a struct, no null checks needed
        Assert.That(writingMode.IsRightToLeft, Is.True);
        Assert.That(writingMode.IsHorizontal, Is.False);
        Assert.That(writingMode.IsVertical, Is.True);
    }

    [Test]
    public void ComputedStyle_LogicalProperties_ReflectWritingMode()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "width: 100px; height: 200px; direction: rtl; writing-mode: horizontal-tb;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);
        var box = style.Box;

        // Assert - in horizontal RTL mode, inline-size is width and block-size is height
        // No null checks needed for structs
        Assert.That(box.InlineSize.ToPixel(_mockRenderDevice.Object), Is.EqualTo(100).Within(0.1));
        Assert.That(box.BlockSize.ToPixel(_mockRenderDevice.Object), Is.EqualTo(200).Within(0.1));
    }

    [Test]
    public void ComputedStyle_Inheritance_InheritsTextProperties()
    {
        // Arrange - create parent and child elements
        var parent = _document.CreateElement("div");
        parent.SetAttribute("style", "color: red; font-family: Arial; font-size: 16px;");
        _document.Body!.AppendChild(parent);

        var child = _document.CreateElement("span");
        parent.AppendChild(child);

        // Act - compute styles for both parent and child
        var parentStyle = _styleEngine.ComputeElementStyle(parent);
        var childStyle = _styleEngine.ComputeElementStyle(child);

        // Assert - child should inherit parent's text properties
        // No null checks for structs
        Assert.That(childStyle.Text.Color.R, Is.EqualTo(255));
        Assert.That(childStyle.Text.FontFamily, Is.EqualTo("Arial"));

        // FontSize is on ComputedStyle, not on Text
        Assert.That(childStyle.FontSize.ToPixel(_mockRenderDevice.Object), Is.EqualTo(16).Within(0.1));
    }

    [Test]
    public void ComputedStyle_NonInheritance_DoesNotInheritBoxProperties()
    {
        // Arrange - create parent with box properties and a child
        var parent = _document.CreateElement("div");
        parent.SetAttribute("style", "width: 100px; margin: 10px; display: flex;");
        _document.Body!.AppendChild(parent);

        var child = _document.CreateElement("span");
        parent.AppendChild(child);

        // Act
        var parentStyle = _styleEngine.ComputeElementStyle(parent);
        var childStyle = _styleEngine.ComputeElementStyle(child);

        // Assert - child should not inherit parent's box properties

        // Compare width px values (if parent is 100px, child should have a different value)
        var parentWidthPx = parentStyle.Box.Width.ToPixel(_mockRenderDevice.Object);
        var childWidthPx = childStyle.Box.Width.ToPixel(_mockRenderDevice.Object);
        Assert.That(childWidthPx, Is.Not.EqualTo(parentWidthPx));

        // Compare margin top (if parent is 10px, child should have a different value)
        var parentMarginTopPx = parentStyle.Box.Margin.Top.ToPixel(_mockRenderDevice.Object);
        var childMarginTopPx = childStyle.Box.Margin.Top.ToPixel(_mockRenderDevice.Object);
        Assert.That(childMarginTopPx, Is.Not.EqualTo(parentMarginTopPx));

        // Compare display mode
        Assert.That(childStyle.Display, Is.Not.EqualTo(parentStyle.Display));
    }
}