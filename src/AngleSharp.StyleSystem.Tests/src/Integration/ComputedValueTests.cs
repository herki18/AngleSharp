namespace AngleSharp.StyleSystem.Tests.Integration;

using AngleSharp.Css;
using AngleSharp.Css.Parser;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Models;
using Moq;

[TestFixture]
public class ComputedValueTests
{
    private IBrowsingContext _context;
    private StyleEngine _styleEngine;
    private IHtmlParser _parser;
    private IDocument _document;
    private ICssParser _cssParser;
    private Mock<IRenderDevice> _mockRenderDevice;
    private ValueCalculator _valueCalculator;

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

        // Initialize ValueCalculator for testing value computation
        _valueCalculator = new ValueCalculator(_context, _mockRenderDevice.Object);

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
    public void LineHeight_AsNumber_ShouldComputeToPixels()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "font-size: 16px; line-height: 1.5;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        // LineHeight as number 1.5 should be computed as 24px (1.5 * 16px)
        Assert.That(style.GetPropertyValue("line-height"), Is.EqualTo("24px"));
    }

    [Test]
    public void LineHeight_AsPixels_ShouldStayAsPixels()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "line-height: 24px;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.GetPropertyValue("line-height"), Is.EqualTo("24px"));
    }

    [Test]
    public void LineHeight_AsEm_ShouldComputeToPixels()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "font-size: 16px; line-height: 1.5em;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        // LineHeight as 1.5em should be computed as 24px (1.5 * 16px)
        Assert.That(style.GetPropertyValue("line-height"), Is.EqualTo("24px"));
    }

    [Test]
    public void MarginBottom_AsEm_ShouldComputeToPixels()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "font-size: 16px; margin-bottom: 1em;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        // 1em margin should be computed as 16px (1 * 16px font-size)
        Assert.That(style.GetPropertyValue("margin-bottom"), Is.EqualTo("16px"));
    }

    [Test]
    public void Rem_ShouldComputeBasedOnRootFontSize()
    {
        // Arrange
        var htmlElement = _document.DocumentElement;
        htmlElement!.SetAttribute("style", "font-size: 20px;");

        var element = _document.CreateElement("div");
        element.SetAttribute("style", "font-size: 1.5rem; margin: 2rem;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        // 1.5rem should be computed as 30px (1.5 * 20px root font-size)
        Assert.That(style.GetPropertyValue("font-size"), Is.EqualTo("30px"));
        // 2rem should be computed as 40px (2 * 20px root font-size)
        Assert.That(style.GetPropertyValue("margin-top"), Is.EqualTo("40px"));
        Assert.That(style.GetPropertyValue("margin-right"), Is.EqualTo("40px"));
        Assert.That(style.GetPropertyValue("margin-bottom"), Is.EqualTo("40px"));
        Assert.That(style.GetPropertyValue("margin-left"), Is.EqualTo("40px"));
    }

    [Test]
    public void FontWeight_Keywords_ShouldComputeToNumericValues()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        element1.SetAttribute("style", "font-weight: bold;");
        _document.Body!.AppendChild(element1);

        var element2 = _document.CreateElement("div");
        element2.SetAttribute("style", "font-weight: normal;");
        _document.Body!.AppendChild(element2);

        // Act
        var style1 = _styleEngine.ComputeElementStyle(element1);
        var style2 = _styleEngine.ComputeElementStyle(element2);

        // Assert
        // "bold" should be computed as "700"
        Assert.That(style1.GetPropertyValue("font-weight"), Is.EqualTo("700"));
        // "normal" should be computed as "400"
        Assert.That(style2.GetPropertyValue("font-weight"), Is.EqualTo("400"));
    }

    [Test]
    public void Color_Keywords_ShouldComputeToRGBA()
    {
        // Arrange
        var element1 = _document.CreateElement("div");
        element1.SetAttribute("style", "color: red;");
        _document.Body!.AppendChild(element1);

        var element2 = _document.CreateElement("div");
        element2.SetAttribute("style", "color: blue;");
        _document.Body!.AppendChild(element2);

        var element3 = _document.CreateElement("div");
        element3.SetAttribute("style", "color: #ff5500;");
        _document.Body!.AppendChild(element3);

        // Act
        var style1 = _styleEngine.ComputeElementStyle(element1);
        var style2 = _styleEngine.ComputeElementStyle(element2);
        var style3 = _styleEngine.ComputeElementStyle(element3);

        // Assert
        // Color keywords and hex values should be computed as rgba()
        Assert.That(style1.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(style2.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(style3.GetPropertyValue("color"), Is.EqualTo("rgba(255, 85, 0, 1)"));
    }

    [Test]
    public void Percentages_ShouldComputeBasedOnContainer()
    {
        // Arrange
        var parent = _document.CreateElement("div");
        parent.SetAttribute("style", "width: 200px; height: 400px;");
        _document.Body!.AppendChild(parent);

        var child = _document.CreateElement("div");
        child.SetAttribute("style", "width: 50%; height: 25%;");
        parent.AppendChild(child);

        // Act
        var styleParent = _styleEngine.ComputeElementStyle(parent);
        var styleChild = _styleEngine.ComputeElementStyle(child);

        // Assert
        // Parent has absolute dimensions
        Assert.That(styleParent.GetPropertyValue("width"), Is.EqualTo("200px"));
        Assert.That(styleParent.GetPropertyValue("height"), Is.EqualTo("400px"));

        // Child percentages should be computed based on parent dimensions
        Assert.That(styleChild.GetPropertyValue("width"), Is.EqualTo("100px")); // 50% of 200px
        Assert.That(styleChild.GetPropertyValue("height"), Is.EqualTo("100px")); // 25% of 400px
    }

    [Test]
    public void ViewportUnits_ShouldComputeBasedOnViewport()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "width: 50vw; height: 50vh; margin: 5vmin; padding: 5vmax;");
        _document.Body!.AppendChild(element);

        // Configure viewport dimensions (already set in Setup)
        // _mockRenderDevice.Setup(d => d.ViewPortWidth).Returns(1024);
        // _mockRenderDevice.Setup(d => d.ViewPortHeight).Returns(768);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        // Viewport units should be computed as pixels based on viewport size
        Assert.That(style.GetPropertyValue("width"), Is.EqualTo("512px")); // 50% of 1024px
        Assert.That(style.GetPropertyValue("height"), Is.EqualTo("384px")); // 50% of 768px

        // vmin is based on the smaller viewport dimension (768px)
        Assert.That(style.GetPropertyValue("margin-top"), Is.EqualTo("38.4px")); // 5% of 768px

        // vmax is based on the larger viewport dimension (1024px)
        Assert.That(style.GetPropertyValue("padding-top"), Is.EqualTo("51.2px")); // 5% of 1024px
    }

    [Test]
    public void LogicalProperties_ShouldComputeToPhysicalProperties()
    {
        // Arrange
        var element = _document.CreateElement("div");
        element.SetAttribute("style", "font-size: 16px; margin-block: 1em; padding-inline: 2em;");
        _document.Body!.AppendChild(element);

        // Act
        var style = _styleEngine.ComputeElementStyle(element);

        // Assert
        // Logical properties should be computed as physical properties based on writing mode
        // For horizontal top-to-bottom (default):
        // - block = top/bottom
        // - inline = left/right

        // margin-block expands to margin-top and margin-bottom
        Assert.That(style.GetPropertyValue("margin-top"), Is.EqualTo("16px")); // 1em = 16px
        Assert.That(style.GetPropertyValue("margin-bottom"), Is.EqualTo("16px")); // 1em = 16px

        // padding-inline expands to padding-left and padding-right
        Assert.That(style.GetPropertyValue("padding-left"), Is.EqualTo("32px")); // 2em = 32px
        Assert.That(style.GetPropertyValue("padding-right"), Is.EqualTo("32px")); // 2em = 32px
    }

    [Test]
    public void ValueCalculator_ComputesAbsoluteValues()
    {
        // Arrange - create some CSS values
        var emValue = new CssLengthValue(1.5, CssLengthValue.Unit.Em);
        var remValue = new CssLengthValue(2, CssLengthValue.Unit.Rem);
        var percentValue = new CssPercentageValue(50);
        var numberValue = new CssNumberValue(1.5);

        var element = _document.CreateElement("div");
        element.SetAttribute("style", "font-size: 16px;");
        _document.Body!.AppendChild(element);

        // Set root font size
        _document.DocumentElement!.SetAttribute("style", "font-size: 20px;");

        // Act
        var computedEm = _valueCalculator.Compute(emValue, element, "margin");
        var computedRem = _valueCalculator.Compute(remValue, element, "margin");
        var computedPercent = _valueCalculator.Compute(percentValue, element, "width");
        var computedNumber = _valueCalculator.Compute(numberValue, element, "line-height");

        // Assert
        Assert.That(computedEm, Is.Not.Null);
        Assert.That(computedEm, Is.InstanceOf<CssLengthValue>());
        var emLength = (CssLengthValue)computedEm;
        Assert.That(emLength.Type, Is.EqualTo(CssLengthValue.Unit.Px));
        Assert.That(emLength.Value, Is.EqualTo(24).Within(0.1)); // 1.5 * 16px

        Assert.That(computedRem, Is.Not.Null);
        Assert.That(computedRem, Is.InstanceOf<CssLengthValue>());
        var remLength = (CssLengthValue)computedRem;
        Assert.That(remLength.Type, Is.EqualTo(CssLengthValue.Unit.Px));
        Assert.That(remLength.Value, Is.EqualTo(40).Within(0.1)); // 2 * 20px

        Assert.That(computedPercent, Is.Not.Null);

        Assert.That(computedNumber, Is.Not.Null);
        Assert.That(computedNumber, Is.InstanceOf<CssLengthValue>());
        var lineHeightLength = (CssLengthValue)computedNumber;
        Assert.That(lineHeightLength.Type, Is.EqualTo(CssLengthValue.Unit.Px));
        Assert.That(lineHeightLength.Value, Is.EqualTo(24).Within(0.1)); // 1.5 * 16px
    }
}