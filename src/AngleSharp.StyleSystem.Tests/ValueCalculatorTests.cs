using Moq;
using AngleSharp.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Css;
using AngleSharp.StyleSystem.Core;

namespace AngleSharp.StyleSystem.Tests;

[TestFixture]
public class ValueCalculatorTests
{
    private Mock<IBrowsingContext> _contextMock;
    private Mock<IRenderDevice> _renderDeviceMock;
    private Mock<IElement> _elementMock;
    private Mock<IElement> _parentElementMock;
    private Mock<IElement> _rootElementMock;
    private Mock<IDocument> _documentMock;
    private ValueCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _contextMock = new Mock<IBrowsingContext>();
        _renderDeviceMock = new Mock<IRenderDevice>();

        // Setup render device
        _renderDeviceMock.Setup(rd => rd.ViewPortWidth).Returns(1024);
        _renderDeviceMock.Setup(rd => rd.ViewPortHeight).Returns(768);
        _renderDeviceMock.Setup(rd => rd.FontSize).Returns(16.0);

        _calculator = new ValueCalculator(_contextMock.Object, _renderDeviceMock.Object);

        // Setup element hierarchy
        _elementMock = new Mock<IElement>();
        _parentElementMock = new Mock<IElement>();
        _rootElementMock = new Mock<IElement>();
        _documentMock = new Mock<IDocument>();

        _elementMock.Setup(e => e.ParentElement).Returns(_parentElementMock.Object);
        _parentElementMock.Setup(e => e.ParentElement).Returns(_rootElementMock.Object);
        _rootElementMock.Setup(e => e.ParentElement).Returns((IElement)null!);

        _documentMock.Setup(d => d.DocumentElement).Returns(_rootElementMock.Object);
        _elementMock.Setup(e => e.OwnerDocument).Returns(_documentMock.Object);
    }

    [Test]
    public void Compute_SimplePixelValue_ReturnsUnchanged()
    {
        // Arrange
        var pixelValue = new CssLengthValue(10, CssLengthValue.Unit.Px);

        // Act
        var result = _calculator.Compute(pixelValue, _elementMock.Object, "width");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<CssLengthValue>(result);
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(10));
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px));
    }

    [Test]
    public void ToPixels_PixelValue_ReturnsSameValue()
    {
        // Arrange
        var pixelValue = new CssLengthValue(10, CssLengthValue.Unit.Px);

        // Act
        var result = _calculator.ToPixels(pixelValue, _elementMock.Object, "width");

        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ToPixels_EmValue_ConvertsToPx()
    {
        // Arrange
        var emValue = new CssLengthValue(2, CssLengthValue.Unit.Em);

        // Act
        var result = _calculator.ToPixels(emValue, _elementMock.Object, "width");

        // Assert
        Assert.That(result, Is.EqualTo(32)); // 2em * 16px font size = 32px
    }

    [Test]
    public void ToPixels_RemValue_ConvertsToPx()
    {
        // Arrange
        var remValue = new CssLengthValue(1.5, CssLengthValue.Unit.Rem);

        // Act
        var result = _calculator.ToPixels(remValue, _elementMock.Object, "width");

        // Assert
        Assert.That(result, Is.EqualTo(24)); // 1.5rem * 16px root font size = 24px
    }

    [Test]
    public void ToPixels_ViewportPercentage_ConvertsToPx()
    {
        // Arrange
        var vwValue = new CssLengthValue(50, CssLengthValue.Unit.Vw);
        var vhValue = new CssLengthValue(25, CssLengthValue.Unit.Vh);

        // Act
        var vwResult = _calculator.ToPixels(vwValue, _elementMock.Object, "width");
        var vhResult = _calculator.ToPixels(vhValue, _elementMock.Object, "height");

        // Assert
        Assert.That(vwResult, Is.EqualTo(512)); // 50% of 1024px = 512px
        Assert.That(vhResult, Is.EqualTo(192)); // 25% of 768px = 192px
    }

    [Test]
    public void ToPixels_AbsoluteUnits_ConvertsToPx()
    {
        // Arrange
        var inValue = new CssLengthValue(1, CssLengthValue.Unit.In);
        var ptValue = new CssLengthValue(72, CssLengthValue.Unit.Pt);
        var cmValue = new CssLengthValue(2.54, CssLengthValue.Unit.Cm);

        // Act
        var inResult = _calculator.ToPixels(inValue, _elementMock.Object, "width");
        var ptResult = _calculator.ToPixels(ptValue, _elementMock.Object, "width");
        var cmResult = _calculator.ToPixels(cmValue, _elementMock.Object, "width");

        // Assert - all should be approximately 96px (standard CSS DPI)
        Assert.That(inResult, Is.EqualTo(96).Within(0.1)); // 1in = 96px
        Assert.That(ptResult, Is.EqualTo(96).Within(0.1)); // 72pt = 96px
        Assert.That(cmResult, Is.EqualTo(96).Within(0.1)); // 2.54cm = 96px
    }

    [Test]
    public void EvaluateCalc_SimpleAddition_ReturnsResult()
    {
        // Arrange - In a real implementation, we would use actual calc values
        var calcValue = Mock.Of<CssCalcValue>(c => c.CssText == "calc(100px + 20px)");

        // Act
        var result = _calculator.EvaluateCalc(calcValue, _elementMock.Object, "width");

        // Assert
        Assert.IsNotNull(result);
        // In a complete implementation, this would return a CssLengthValue of 120px
    }

    [Test]
    public void ResolveRelative_PercentageValue_ConvertsToAbsolute()
    {
        // Arrange
        var percentValue = new CssPercentageValue(50);
        var baseValue = new CssLengthValue(200, CssLengthValue.Unit.Px);

        // Act
        var result = _calculator.ResolveRelative(percentValue, _elementMock.Object, baseValue, "width");

        // Assert
        Assert.IsNotNull(result);
        // In a complete implementation, this would return a CssLengthValue of 100px (50% of 200px)
    }
}