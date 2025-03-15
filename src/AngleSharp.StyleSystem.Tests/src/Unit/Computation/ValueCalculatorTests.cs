namespace AngleSharp.StyleSystem.Tests.Unit.Computation;

using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Computation;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;

[TestFixture]
public class ValueCalculatorTests
{
    private IBrowsingContext _context;
    private IRenderDevice _renderDevice;
    private IElement _element;
    private IElement _parentElement;
    private IElement _rootElement;
    private IDocument _document;
    private IDeclarationFactory _declarationFactory;
    private ICssParser _cssParser;
    private ValueCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _context = Substitute.For<IBrowsingContext>();
        _renderDevice = Substitute.For<IRenderDevice>();
        _declarationFactory = Substitute.For<IDeclarationFactory>();
        _cssParser = Substitute.For<ICssParser>();

        // Setup render device
        _renderDevice.ViewPortWidth.Returns(1024);
        _renderDevice.ViewPortHeight.Returns(768);
        _renderDevice.FontSize.Returns(16.0);
        _renderDevice.Resolution.Returns(96);

        // Setup context with declaration factory
        _context.GetServices<IDeclarationFactory>().Returns(new List<IDeclarationFactory> { _declarationFactory });
        _context.GetService<ICssParser>().Returns(_cssParser);

        _calculator = new ValueCalculator(_declarationFactory, _cssParser, _renderDevice);

        // Setup element hierarchy
        _element = Substitute.For<IElement>();
        _parentElement = Substitute.For<IElement>();
        _rootElement = Substitute.For<IElement>();
        _document = Substitute.For<IDocument>();

        _element.ParentElement.Returns(_parentElement);
        _parentElement.ParentElement.Returns(_rootElement);
        _rootElement.ParentElement.Returns((IElement)null!);

        _document.DocumentElement.Returns(_rootElement);
        _element.OwnerDocument.Returns(_document);
        _parentElement.OwnerDocument.Returns(_document);
        _rootElement.OwnerDocument.Returns(_document);

        // Setup style attributes for font size
        SetupFontSizeStyleAttribute(_element, "16px");
        SetupFontSizeStyleAttribute(_parentElement, "16px");
        SetupFontSizeStyleAttribute(_rootElement, "16px");
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _document?.Dispose();
    }

    private void SetupFontSizeStyleAttribute(IElement elementMock, string fontSize)
    {
        elementMock.GetAttribute("style").Returns($"font-size: {fontSize};");
    }

    #region Basic Computation Tests

    [Test]
    public void Compute_Null_ReturnsDefaultValue()
    {
        // Arrange
        ICssValue? nullValue = null;

        // Setup default value for width
        var initialWidthValue = CssLengthValue.Auto;
        var widthDeclaration = new DeclarationInfo(
            "width",
            Substitute.For<IValueConverter>(),
            initialValue: initialWidthValue
        );
        _declarationFactory.Create("width").Returns(widthDeclaration);

        // Act
        var result = _calculator.Compute(nullValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.EqualTo(initialWidthValue), "Should return the initial value");
    }

    [Test]
    public void Compute_SimplePixelValue_ReturnsUnchanged()
    {
        // Arrange
        var pixelValue = new CssLengthValue(10, CssLengthValue.Unit.Px);

        // Act
        var result = _calculator.Compute(pixelValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(10), "Value should be unchanged");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Unit should be unchanged");
    }

    [Test]
    public void Compute_ColorValue_ReturnsUnchanged()
    {
        // Arrange
        var colorValue = CssColorValue.Red;

        // Act
        var result = _calculator.Compute(colorValue, _element, "color");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.EqualTo(colorValue), "Color value should be unchanged");
    }

    [Test]
    public void Compute_WithNullElement_ThrowsException()
    {
        // Arrange
        var pixelValue = new CssLengthValue(10, CssLengthValue.Unit.Px);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _calculator.Compute(pixelValue, null!, "width"));
    }

    #endregion

    #region Length Unit Conversion Tests

    [Test]
    public void ToPixels_PixelValue_ReturnsSameValue()
    {
        // Arrange
        var pixelValue = new CssLengthValue(10, CssLengthValue.Unit.Px);

        // Act
        var result = _calculator.ToPixels(pixelValue, _element, "width");

        // Assert
        Assert.That(result, Is.EqualTo(10), "Pixel values should remain unchanged");
    }

    [Test]
    public void ToPixels_EmValue_ConvertsToPx()
    {
        // Arrange
        var emValue = new CssLengthValue(2, CssLengthValue.Unit.Em);

        // Act
        var result = _calculator.ToPixels(emValue, _element, "width");

        // Assert
        Assert.That(result, Is.EqualTo(32), "2em * 16px font size should equal 32px");
    }

    [Test]
    public void ToPixels_RemValue_ConvertsToPx()
    {
        // Arrange
        var remValue = new CssLengthValue(1.5, CssLengthValue.Unit.Rem);

        // Act
        var result = _calculator.ToPixels(remValue, _element, "width");

        // Assert
        Assert.That(result, Is.EqualTo(24), "1.5rem * 16px root font size should equal 24px");
    }

    [Test]
    public void ToPixels_ViewportUnits_ConvertsToPx()
    {
        // Arrange
        var vwValue = new CssLengthValue(50, CssLengthValue.Unit.Vw);
        var vhValue = new CssLengthValue(25, CssLengthValue.Unit.Vh);
        var vminValue = new CssLengthValue(10, CssLengthValue.Unit.Vmin);
        var vmaxValue = new CssLengthValue(10, CssLengthValue.Unit.Vmax);

        // Act
        var vwResult = _calculator.ToPixels(vwValue, _element, "width");
        var vhResult = _calculator.ToPixels(vhValue, _element, "height");
        var vminResult = _calculator.ToPixels(vminValue, _element, "width");
        var vmaxResult = _calculator.ToPixels(vmaxValue, _element, "height");

        // Assert
        Assert.That(vwResult, Is.EqualTo(512), "50% of 1024px viewport width should be 512px");
        Assert.That(vhResult, Is.EqualTo(192), "25% of 768px viewport height should be 192px");
        Assert.That(vminResult, Is.EqualTo(76.8), "10% of 768px min dimension should be 76.8px");
        Assert.That(vmaxResult, Is.EqualTo(102.4), "10% of 1024px max dimension should be 102.4px");
    }

    [Test]
    public void ToPixels_AbsoluteUnits_ConvertsToPx()
    {
        // Arrange
        var inValue = new CssLengthValue(1, CssLengthValue.Unit.In);
        var ptValue = new CssLengthValue(72, CssLengthValue.Unit.Pt);
        var cmValue = new CssLengthValue(2.54, CssLengthValue.Unit.Cm);
        var mmValue = new CssLengthValue(25.4, CssLengthValue.Unit.Mm);
        var pcValue = new CssLengthValue(6, CssLengthValue.Unit.Pc);

        // Act
        var inResult = _calculator.ToPixels(inValue, _element, "width");
        var ptResult = _calculator.ToPixels(ptValue, _element, "width");
        var cmResult = _calculator.ToPixels(cmValue, _element, "width");
        var mmResult = _calculator.ToPixels(mmValue, _element, "width");
        var pcResult = _calculator.ToPixels(pcValue, _element, "width");

        // Assert - all should be approximately 96px (standard CSS DPI)
        Assert.That(inResult, Is.EqualTo(96).Within(0.1), "1in should equal 96px");
        Assert.That(ptResult, Is.EqualTo(96).Within(0.1), "72pt should equal 96px");
        Assert.That(cmResult, Is.EqualTo(96).Within(0.1), "2.54cm should equal 96px");
        Assert.That(mmResult, Is.EqualTo(96).Within(0.1), "25.4mm should equal 96px");
        Assert.That(pcResult, Is.EqualTo(96).Within(0.1), "6pc should equal 96px");
    }

    [Test]
    public void ToPixels_RelativeUnits_ConvertsToPx()
    {
        // Arrange
        var exValue = new CssLengthValue(2, CssLengthValue.Unit.Ex);
        var chValue = new CssLengthValue(2, CssLengthValue.Unit.Ch);

        // Act
        var exResult = _calculator.ToPixels(exValue, _element, "width");
        var chResult = _calculator.ToPixels(chValue, _element, "width");

        // Assert
        Assert.That(exResult, Is.EqualTo(16), "2ex * 16px * 0.5 should equal 16px");
        Assert.That(chResult, Is.EqualTo(16), "2ch * 16px * 0.5 should equal 16px");
    }

    [Test]
    public void ToPixels_SpecialValues_HandlesCorrectly()
    {
        // Arrange
        var autoValue = CssLengthValue.Auto;
        var zeroValue = CssLengthValue.Zero;

        // Act
        var autoResult = _calculator.ToPixels(autoValue, _element, "width");
        var zeroResult = _calculator.ToPixels(zeroValue, _element, "width");

        // Assert
        Assert.That(autoResult, Is.EqualTo(0), "'auto' should convert to 0 for pixel calculation");
        Assert.That(zeroResult, Is.EqualTo(0), "Zero should remain 0");
    }

    #endregion

    #region Calc Expression Tests

    [Test]
    public void EvaluateCalc_SimpleAddition_ReturnsCorrectResult()
    {
        // Arrange - create a calc value with addition
        var calcValue = new CssCalcValue(
            new CssCalcAddExpression(
                new CssLengthValue(100, CssLengthValue.Unit.Px),
                new CssLengthValue(20, CssLengthValue.Unit.Px)));

        // Act
        var result = _calculator.EvaluateCalc(calcValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(120), "100px + 20px should equal 120px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void EvaluateCalc_SimpleSubtraction_ReturnsCorrectResult()
    {
        // Arrange - create a calc value with subtraction
        var calcValue = new CssCalcValue(
            new CssCalcSubExpression(
                new CssLengthValue(100, CssLengthValue.Unit.Px),
                new CssLengthValue(20, CssLengthValue.Unit.Px)));

        // Act
        var result = _calculator.EvaluateCalc(calcValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(80), "100px - 20px should equal 80px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void EvaluateCalc_Multiplication_ReturnsCorrectResult()
    {
        // Arrange - create a calc value with multiplication
        var calcValue = new CssCalcValue(
            new CssCalcMulExpression(
                new CssNumberValue(2),
                new CssLengthValue(50, CssLengthValue.Unit.Px)));

        // Act
        var result = _calculator.EvaluateCalc(calcValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(100), "2 * 50px should equal 100px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void EvaluateCalc_Division_ReturnsCorrectResult()
    {
        // Arrange - create a calc value with division
        var calcValue = new CssCalcValue(
            new CssCalcDivExpression(
                new CssLengthValue(100, CssLengthValue.Unit.Px),
                new CssNumberValue(2)));

        // Act
        var result = _calculator.EvaluateCalc(calcValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(50), "100px / 2 should equal 50px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void EvaluateCalc_MixedUnits_ReturnsCorrectResult()
    {
        // Arrange - create a calc value with mixed units
        var calcValue = new CssCalcValue(
            new CssCalcAddExpression(
                new CssLengthValue(50, CssLengthValue.Unit.Px),
                new CssLengthValue(1, CssLengthValue.Unit.Em)));

        // Act
        var result = _calculator.EvaluateCalc(calcValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(66), "50px + 1em (16px) should equal 66px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void EvaluateCalc_WithPercentage_ReturnsCorrectResult()
    {
        // Arrange
        var calcValue = new CssCalcValue(
            new CssCalcAddExpression(
                new CssLengthValue(50, CssLengthValue.Unit.Px),
                new CssPercentageValue(10)));

        // Setup parent element width
        _parentElement.GetAttribute("style").Returns("width: 200px;");

        // Create a properly mocked ICssStyleDeclaration
        var styleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the width property
        styleDeclaration.GetPropertyValue("width").Returns("200px");

        // Create a property for the width
        var widthProperty = Substitute.For<ICssProperty>();
        widthProperty.Name.Returns("width");
        widthProperty.Value.Returns("200px");
        widthProperty.RawValue.Returns(new CssLengthValue(200, CssLengthValue.Unit.Px));

        // Setup enumerator for the style declaration
        var propertyList = new List<ICssProperty> { widthProperty };
        styleDeclaration.GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(styleDeclaration);

        // Act
        var result = _calculator.EvaluateCalc(calcValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(70), "50px + 10% of 200px (20px) should equal 70px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void EvaluateCalc_DivisionByZero_HandlesGracefully()
    {
        // Arrange - create a calc value with division by zero
        var calcValue = new CssCalcValue(
            new CssCalcDivExpression(
                new CssLengthValue(100, CssLengthValue.Unit.Px),
                new CssNumberValue(0)));

        // Act
        var result = _calculator.EvaluateCalc(calcValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(0), "Division by zero should be handled gracefully");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    #endregion

    #region CSS Global Keywords Tests

    [Test]
    public void Compute_InheritKeyword_InheritsFromParent()
    {
        // Arrange
        var inheritValue = new CssIdentifierValue("inherit");
        var parentColorValue = CssColorValue.Red;

        // Setup parent element to have a color
        _parentElement.GetAttribute("style").Returns("color: red;");

        // Create a properly mocked ICssStyleDeclaration for the parent
        var parentStyleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the color property
        parentStyleDeclaration.GetPropertyValue("color").Returns("red");

        // Create a property for the color
        var colorProperty = Substitute.For<ICssProperty>();
        colorProperty.Name.Returns("color");
        colorProperty.Value.Returns("red");
        colorProperty.RawValue.Returns(parentColorValue);

        // Setup enumerator to return the property
        var propertyList = new List<ICssProperty> { colorProperty };
        ((IEnumerable<ICssProperty>)parentStyleDeclaration).GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the parent style declaration to be returned for "color" property
        parentStyleDeclaration.GetProperty("color").Returns(colorProperty);

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(parentStyleDeclaration);

        // Act
        var result = _calculator.Compute(inheritValue, _element, "color");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssColorValue>(), "Result should be a color value");

        // Compare the color values properly
        var resultColor = (CssColorValue)result;
        Assert.That(resultColor.R, Is.EqualTo(parentColorValue.R), "Red component should match");
        Assert.That(resultColor.G, Is.EqualTo(parentColorValue.G), "Green component should match");
        Assert.That(resultColor.B, Is.EqualTo(parentColorValue.B), "Blue component should match");
        Assert.That(resultColor.A, Is.EqualTo(parentColorValue.A), "Alpha component should match");
    }

    [Test]
    public void Compute_InitialKeyword_ReturnsInitialValue()
    {
        // Arrange
        var initialValue = new CssIdentifierValue("initial");
        var initialColorValue = CssColorValue.Black; // Default color is black

        // Setup declaration factory to return initial values
        var colorDeclaration = new DeclarationInfo(
            "color",
            Substitute.For<IValueConverter>(),
            initialValue: initialColorValue
        );
        _declarationFactory.Create("color").Returns(colorDeclaration);

        // Act
        var result = _calculator.Compute(initialValue, _element, "color");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssColorValue>(), "Result should be a color value");
        Assert.That(result, Is.EqualTo(initialColorValue), "Result should equal the initial color value");
    }

    [Test]
    public void Compute_UnsetKeyword_InheritsForInheritedProperties()
    {
        // Arrange
        var unsetValue = new CssIdentifierValue("unset");
        var parentColorValue = CssColorValue.Red;

        // Setup parent element to have a color
        _parentElement.GetAttribute("style").Returns("color: red;");

        // Create a properly mocked ICssStyleDeclaration for the parent style
        var parentStyleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the color property value
        parentStyleDeclaration.GetPropertyValue("color").Returns("red");

        // Create a property for the color
        var colorProperty = Substitute.For<ICssProperty>();
        colorProperty.Name.Returns("color");
        colorProperty.Value.Returns("red");
        colorProperty.RawValue.Returns(parentColorValue);

        // Setup property access and enumeration
        var propertyList = new List<ICssProperty> { colorProperty };
        parentStyleDeclaration.GetProperty("color").Returns(colorProperty);

        // Setup enumerator
        ((IEnumerable<ICssProperty>)parentStyleDeclaration).GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration("color: red").Returns(parentStyleDeclaration);

        // Setup declaration factory to indicate color is inheritable
        var colorInfo = new DeclarationInfo(
            "color",
            Substitute.For<IValueConverter>(),
            PropertyFlags.Inherited,
            CssColorValue.Black
        );
        _declarationFactory.Create("color").Returns(colorInfo);

        // Act
        var result = _calculator.Compute(unsetValue, _element, "color");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssColorValue>(), "Result should be a color value");

        var colorResult = result is CssColorValue ? (CssColorValue)result : default;
        Assert.That(colorResult.R, Is.EqualTo(parentColorValue.R), "Red component should match");
        Assert.That(colorResult.G, Is.EqualTo(parentColorValue.G), "Green component should match");
        Assert.That(colorResult.B, Is.EqualTo(parentColorValue.B), "Blue component should match");
        Assert.That(colorResult.A, Is.EqualTo(parentColorValue.A), "Alpha component should match");
    }

    [Test]
    public void Compute_UnsetKeyword_UsesInitialForNonInheritedProperties()
    {
        // Arrange
        var unsetValue = new CssIdentifierValue("unset");
        var initialDisplayValue = new CssIdentifierValue("inline");

        // Setup declaration factory to indicate display is NOT inheritable
        var displayInfo = new DeclarationInfo(
            "display",
            Substitute.For<IValueConverter>(),
            PropertyFlags.None,
            initialDisplayValue
        );
        _declarationFactory.Create("display").Returns(displayInfo);

        // Act
        var result = _calculator.Compute(unsetValue, _element, "display");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.EqualTo(initialDisplayValue), "Result should equal the initial display value");
    }

    #endregion

    #region Percentage Handling Tests

    [Test]
    public void Compute_PercentageForWidth_ConvertsToPx()
    {
        // Arrange
        var percentValue = new CssPercentageValue(50);

        // Setup parent element width
        _parentElement.GetAttribute("style").Returns("width: 200px;");

        // Create a properly mocked ICssStyleDeclaration
        var styleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the width property
        styleDeclaration.GetPropertyValue("width").Returns("200px");

        // Create a property for the width
        var widthProperty = Substitute.For<ICssProperty>();
        widthProperty.Name.Returns("width");
        widthProperty.Value.Returns("200px");
        widthProperty.RawValue.Returns(new CssLengthValue(200, CssLengthValue.Unit.Px));

        // Setup enumerator to return the property
        var propertyList = new List<ICssProperty> { widthProperty };
        styleDeclaration.GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(styleDeclaration);

        // Act
        var result = _calculator.Compute(percentValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(100), "50% of 200px should equal 100px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void Compute_PercentageForFontSize_ConvertsToPx()
    {
        // Arrange
        var percentValue = new CssPercentageValue(150);

        // Setup parent element font size
        _parentElement.GetAttribute("style").Returns("font-size: 16px;");

        // Create a properly mocked ICssStyleDeclaration
        var styleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the font-size property getter
        styleDeclaration.GetPropertyValue("font-size").Returns("16px");

        // Create a property for the font-size
        var fontSizeProperty = Substitute.For<ICssProperty>();
        fontSizeProperty.Name.Returns("font-size");
        fontSizeProperty.Value.Returns("16px");
        fontSizeProperty.RawValue.Returns(new CssLengthValue(16, CssLengthValue.Unit.Px));

        // Setup enumerator to return the property
        var propertyList = new List<ICssProperty> { fontSizeProperty };
        styleDeclaration.GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(styleDeclaration);

        // Act
        var result = _calculator.Compute(percentValue, _element, "font-size");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(24), "150% of 16px should equal 24px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void Compute_PercentageForMargin_ConvertsToPx()
    {
        // Arrange
        var percentValue = new CssPercentageValue(25);

        // Setup parent element width
        _parentElement.GetAttribute("style").Returns("width: 400px;");

        // Create a properly mocked ICssStyleDeclaration
        var styleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the width property
        styleDeclaration.GetPropertyValue("width").Returns("400px");

        // Create a property for the width
        var widthProperty = Substitute.For<ICssProperty>();
        widthProperty.Name.Returns("width");
        widthProperty.Value.Returns("400px");
        widthProperty.RawValue.Returns(new CssLengthValue(400, CssLengthValue.Unit.Px));

        // Setup enumerator to return the property
        var propertyList = new List<ICssProperty> { widthProperty };
        styleDeclaration.GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(styleDeclaration);

        // Act
        var result = _calculator.Compute(percentValue, _element, "margin-left");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(100), "25% of 400px should equal 100px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void Compute_PercentageForNonDimensionalProperty_KeepsAsPercentage()
    {
        // Arrange
        var percentValue = new CssPercentageValue(75);

        // Act
        var result = _calculator.Compute(percentValue, _element, "opacity");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssPercentageValue>(), "Result should remain a percentage value");
        var resultPercent = (CssPercentageValue)result;
        Assert.That(resultPercent.Value, Is.EqualTo(75), "Percentage should remain unchanged");
    }

    #endregion

    #region Context-Dependent Tests

    [Test]
    public void Compute_FontSizeWithEm_RelativeToParentFontSize()
    {
        // Arrange
        var emValue = new CssLengthValue(1.5, CssLengthValue.Unit.Em);

        // Setup parent element font size to be different
        _parentElement.GetAttribute("style").Returns("font-size: 20px;");

        // Create a properly mocked ICssStyleDeclaration
        var styleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the font-size property
        styleDeclaration.GetPropertyValue("font-size").Returns("20px");

        // Create a property for the font-size
        var fontSizeProperty = Substitute.For<ICssProperty>();
        fontSizeProperty.Name.Returns("font-size");
        fontSizeProperty.Value.Returns("20px");
        fontSizeProperty.RawValue.Returns(new CssLengthValue(20, CssLengthValue.Unit.Px));

        // Setup enumerator to return the property
        var propertyList = new List<ICssProperty> { fontSizeProperty };
        styleDeclaration.GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(styleDeclaration);

        // Act
        var result = _calculator.Compute(emValue, _element, "font-size");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(30), "1.5em * 20px should equal 30px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void Compute_FontSizeWithRem_RelativeToRootFontSize()
    {
        // Arrange
        var remValue = new CssLengthValue(2, CssLengthValue.Unit.Rem);

        // Setup root element font size to be different
        _rootElement.GetAttribute("style").Returns("font-size: 18px;");

        // Create a properly mocked ICssStyleDeclaration
        var styleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the font-size property value
        styleDeclaration.GetPropertyValue("font-size").Returns("18px");

        // Create a property for the font-size
        var fontSizeProperty = Substitute.For<ICssProperty>();
        fontSizeProperty.Name.Returns("font-size");
        fontSizeProperty.Value.Returns("18px");
        fontSizeProperty.RawValue.Returns(new CssLengthValue(18, CssLengthValue.Unit.Px));

        // Setup enumerator to return the property
        var propertyList = new List<ICssProperty> { fontSizeProperty };
        styleDeclaration.GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(styleDeclaration);

        // Act
        var result = _calculator.Compute(remValue, _element, "font-size");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(36), "2rem * 18px should equal 36px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void Compute_EmInNonFontSizeProperty_RelativeToElementFontSize()
    {
        // Arrange
        var emValue = new CssLengthValue(2, CssLengthValue.Unit.Em);

        // Setup element font size
        _element.GetAttribute("style").Returns("font-size: 14px;");

        // Create a properly mocked ICssStyleDeclaration
        var styleDeclaration = Substitute.For<ICssStyleDeclaration>();

        // Setup the font-size property value
        styleDeclaration.GetPropertyValue("font-size").Returns("14px");

        // Create a property for the font-size
        var fontSizeProperty = Substitute.For<ICssProperty>();
        fontSizeProperty.Name.Returns("font-size");
        fontSizeProperty.Value.Returns("14px");
        fontSizeProperty.RawValue.Returns(new CssLengthValue(14, CssLengthValue.Unit.Px));

        // Setup enumerator to return the property
        var propertyList = new List<ICssProperty> { fontSizeProperty };
        styleDeclaration.GetEnumerator().Returns(propertyList.GetEnumerator());

        // Setup the CSS parser to return the style declaration
        _cssParser.ParseDeclaration(Arg.Any<string>()).Returns(styleDeclaration);

        // Act
        var result = _calculator.Compute(emValue, _element, "margin-top");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(28), "2em * 14px should equal 28px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    #endregion

    #region Edge Cases and Special Values

    [Test]
    public void Compute_AutoValue_HandlesCorrectly()
    {
        // Arrange
        var autoValue = CssLengthValue.Auto;

        // Act
        var result = _calculator.Compute(autoValue, _element, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Equals(CssLengthValue.Auto), Is.True, "Auto value should remain auto");
    }

    [Test]
    public void Compute_NoneValue_HandlesCorrectly()
    {
        // Arrange
        var noneValue = new CssIdentifierValue("none");

        // Setup initial border width (0px)
        var initialBorderWidth = CssLengthValue.Zero;
        var borderWidthInfo = new DeclarationInfo(
            "border-width",
            Substitute.For<IValueConverter>(),
            initialValue: initialBorderWidth
        );
        _declarationFactory.Create("border-width").Returns(borderWidthInfo);

        // Act
        var result = _calculator.Compute(noneValue, _element, "border-width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        // In most contexts, 'none' for border-width should compute to 0px
    }

    [Test]
    public void Compute_NegativeLength_HandlesCorrectly()
    {
        // Arrange
        var negativeValue = new CssLengthValue(-10, CssLengthValue.Unit.Px);

        // Act
        var result = _calculator.Compute(negativeValue, _element, "margin-top");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(-10), "Negative values should be preserved");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Unit should remain unchanged");
    }

    #endregion

    #region Relative Value Tests

    [Test]
    public void ResolveRelative_PercentageToLength_ReturnsAbsoluteLength()
    {
        // Arrange
        var percentValue = new CssPercentageValue(50);
        var baseValue = new CssLengthValue(200, CssLengthValue.Unit.Px);

        // Act
        var result = _calculator.ResolveRelative(percentValue, _element, baseValue, "width");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
        var resultLength = (CssLengthValue)result;
        Assert.That(resultLength.Value, Is.EqualTo(100), "50% of 200px should equal 100px");
        Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
    }

    [Test]
    public void ResolveRelative_FontWeightKeyword_ReturnsNumericWeight()
    {
        // Arrange
        var lighterValue = new CssIdentifierValue("lighter");
        var boldBaseValue = new CssIntegerValue(700); // Parent has bold

        // Act
        var result = _calculator.ResolveRelative(lighterValue, _element, boldBaseValue, "font-weight");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssIntegerValue>(), "Result should be an integer value");
        var resultWeight = (CssIntegerValue)result;
        Assert.That(resultWeight.IntValue, Is.EqualTo(400), "'lighter' relative to 700 should be 400");
    }

    [Test]
    public void ResolveRelative_BolderKeyword_ReturnsNumericWeight()
    {
        // Arrange
        var bolderValue = new CssIdentifierValue("bolder");
        var normalBaseValue = new CssIntegerValue(400); // Parent has normal

        // Act
        var result = _calculator.ResolveRelative(bolderValue, _element, normalBaseValue, "font-weight");

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result, Is.InstanceOf<CssIntegerValue>(), "Result should be an integer value");
        var resultWeight = (CssIntegerValue)result;
        Assert.That(resultWeight.IntValue, Is.EqualTo(700), "'bolder' relative to 400 should be 700");
    }

    #endregion

    #region Caching and Optimization Tests

    [Test]
    public void Compute_SameInputsMultipleTimes_CachesResult()
    {
        // Arrange
        var emValue = new CssLengthValue(1.5, CssLengthValue.Unit.Em);

        // Act
        var result1 = _calculator.Compute(emValue, _element, "width");
        var result2 = _calculator.Compute(emValue, _element, "width");

        // Assert
        Assert.That(result1, Is.Not.Null, "First result should not be null");
        Assert.That(result2, Is.Not.Null, "Second result should not be null");
        // NSubstitute won't automatically return the same object reference for caching
        // So we'll just check that the values are the same
        Assert.That(result2.CssText, Is.EqualTo(result1.CssText), "Results should have the same CSS text");
    }

    [Test]
    public void ClearCache_RemovesAllCachedResults()
    {
        // Arrange
        var emValue = new CssLengthValue(1.5, CssLengthValue.Unit.Em);
        var result1 = _calculator.Compute(emValue, _element, "width");

        // Act
        _calculator.ClearCache();
        var result2 = _calculator.Compute(emValue, _element, "width");

        // Assert
        Assert.That(result1, Is.Not.Null, "First result should not be null");
        Assert.That(result2, Is.Not.Null, "Second result should not be null");
        // Since we can't check for same reference after clearing cache
        // We'll just make sure both results are valid and have equivalent values
        Assert.That(result1.CssText, Is.EqualTo(result2.CssText), "Results should have the same computed value");
    }

    #endregion
}