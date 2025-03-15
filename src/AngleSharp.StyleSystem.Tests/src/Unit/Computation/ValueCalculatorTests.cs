// namespace AngleSharp.StyleSystem.Tests.Unit.Computation;
//
// using AngleSharp.Css;
// using AngleSharp.Css.Dom;
// using AngleSharp.Css.Parser;
// using AngleSharp.Css.Values;
// using AngleSharp.Dom;
// using AngleSharp.StyleSystem.Computation;
// using Moq;
//
// [TestFixture]
// public class ValueCalculatorTests
// {
//     private Mock<IBrowsingContext> _contextMock;
//     private Mock<IRenderDevice> _renderDeviceMock;
//     private Mock<IElement> _elementMock;
//     private Mock<IElement> _parentElementMock;
//     private Mock<IElement> _rootElementMock;
//     private Mock<IDocument> _documentMock;
//     private Mock<IDeclarationFactory> _declarationFactoryMock;
//     private ValueCalculator _calculator;
//
//     [SetUp]
//     public void Setup()
//     {
//         _contextMock = new Mock<IBrowsingContext>();
//         _renderDeviceMock = new Mock<IRenderDevice>();
//         _declarationFactoryMock = new Mock<IDeclarationFactory>();
//
//         // Setup render device
//         _renderDeviceMock.Setup(rd => rd.ViewPortWidth).Returns(1024);
//         _renderDeviceMock.Setup(rd => rd.ViewPortHeight).Returns(768);
//         _renderDeviceMock.Setup(rd => rd.FontSize).Returns(16.0);
//         _renderDeviceMock.Setup(rd => rd.Resolution).Returns(96);
//
//         // Setup context with declaration factory
//         _contextMock.Setup(c => c.GetServices<IDeclarationFactory>())
//             .Returns(new List<IDeclarationFactory> { _declarationFactoryMock.Object });
//
//         _calculator = new ValueCalculator(_contextMock.Object, _renderDeviceMock.Object);
//
//         // Setup element hierarchy
//         _elementMock = new Mock<IElement>();
//         _parentElementMock = new Mock<IElement>();
//         _rootElementMock = new Mock<IElement>();
//         _documentMock = new Mock<IDocument>();
//
//         _elementMock.Setup(e => e.ParentElement).Returns(_parentElementMock.Object);
//         _parentElementMock.Setup(e => e.ParentElement).Returns(_rootElementMock.Object);
//         _rootElementMock.Setup(e => e.ParentElement).Returns((IElement)null!);
//
//         _documentMock.Setup(d => d.DocumentElement).Returns(_rootElementMock.Object);
//         _elementMock.Setup(e => e.OwnerDocument).Returns(_documentMock.Object);
//         _parentElementMock.Setup(e => e.OwnerDocument).Returns(_documentMock.Object);
//         _rootElementMock.Setup(e => e.OwnerDocument).Returns(_documentMock.Object);
//
//         // Setup style attributes for font size
//         SetupFontSizeStyleAttribute(_elementMock, "16px");
//         SetupFontSizeStyleAttribute(_parentElementMock, "16px");
//         SetupFontSizeStyleAttribute(_rootElementMock, "16px");
//     }
//
//     private void SetupFontSizeStyleAttribute(Mock<IElement> elementMock, string fontSize)
//     {
//         elementMock.Setup(e => e.GetAttribute("style")).Returns($"font-size: {fontSize};");
//     }
//
//     #region Basic Computation Tests
//
//     [Test]
//     public void Compute_Null_ReturnsDefaultValue()
//     {
//         // Arrange
//         ICssValue? nullValue = null;
//
//         // Setup default value for width
//         var initialWidthValue = CssLengthValue.Auto;
//         var widthDeclaration = new DeclarationInfo(
//             "width",
//             Mock.Of<IValueConverter>(),
//             initialValue: initialWidthValue
//         );
//         _declarationFactoryMock.Setup(f => f.Create("width")).Returns(widthDeclaration);
//
//         // Act
//         var result = _calculator.Compute(nullValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.EqualTo(initialWidthValue), "Should return the initial value");
//     }
//
//     [Test]
//     public void Compute_SimplePixelValue_ReturnsUnchanged()
//     {
//         // Arrange
//         var pixelValue = new CssLengthValue(10, CssLengthValue.Unit.Px);
//
//         // Act
//         var result = _calculator.Compute(pixelValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(10), "Value should be unchanged");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Unit should be unchanged");
//     }
//
//     [Test]
//     public void Compute_ColorValue_ReturnsUnchanged()
//     {
//         // Arrange
//         var colorValue = CssColorValue.Red;
//
//         // Act
//         var result = _calculator.Compute(colorValue, _elementMock.Object, "color");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.EqualTo(colorValue), "Color value should be unchanged");
//     }
//
//     [Test]
//     public void Compute_WithNullElement_ThrowsException()
//     {
//         // Arrange
//         var pixelValue = new CssLengthValue(10, CssLengthValue.Unit.Px);
//
//         // Act & Assert
//         Assert.Throws<NullReferenceException>(() => _calculator.Compute(pixelValue, null!, "width"));
//     }
//
//     #endregion
//
//     #region Length Unit Conversion Tests
//
//     [Test]
//     public void ToPixels_PixelValue_ReturnsSameValue()
//     {
//         // Arrange
//         var pixelValue = new CssLengthValue(10, CssLengthValue.Unit.Px);
//
//         // Act
//         var result = _calculator.ToPixels(pixelValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.EqualTo(10), "Pixel values should remain unchanged");
//     }
//
//     [Test]
//     public void ToPixels_EmValue_ConvertsToPx()
//     {
//         // Arrange
//         var emValue = new CssLengthValue(2, CssLengthValue.Unit.Em);
//
//         // Act
//         var result = _calculator.ToPixels(emValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.EqualTo(32), "2em * 16px font size should equal 32px");
//     }
//
//     [Test]
//     public void ToPixels_RemValue_ConvertsToPx()
//     {
//         // Arrange
//         var remValue = new CssLengthValue(1.5, CssLengthValue.Unit.Rem);
//
//         // Act
//         var result = _calculator.ToPixels(remValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.EqualTo(24), "1.5rem * 16px root font size should equal 24px");
//     }
//
//     [Test]
//     public void ToPixels_ViewportUnits_ConvertsToPx()
//     {
//         // Arrange
//         var vwValue = new CssLengthValue(50, CssLengthValue.Unit.Vw);
//         var vhValue = new CssLengthValue(25, CssLengthValue.Unit.Vh);
//         var vminValue = new CssLengthValue(10, CssLengthValue.Unit.Vmin);
//         var vmaxValue = new CssLengthValue(10, CssLengthValue.Unit.Vmax);
//
//         // Act
//         var vwResult = _calculator.ToPixels(vwValue, _elementMock.Object, "width");
//         var vhResult = _calculator.ToPixels(vhValue, _elementMock.Object, "height");
//         var vminResult = _calculator.ToPixels(vminValue, _elementMock.Object, "width");
//         var vmaxResult = _calculator.ToPixels(vmaxValue, _elementMock.Object, "height");
//
//         // Assert
//         Assert.That(vwResult, Is.EqualTo(512), "50% of 1024px viewport width should be 512px");
//         Assert.That(vhResult, Is.EqualTo(192), "25% of 768px viewport height should be 192px");
//         Assert.That(vminResult, Is.EqualTo(76.8), "10% of 768px min dimension should be 76.8px");
//         Assert.That(vmaxResult, Is.EqualTo(102.4), "10% of 1024px max dimension should be 102.4px");
//     }
//
//     [Test]
//     public void ToPixels_AbsoluteUnits_ConvertsToPx()
//     {
//         // Arrange
//         var inValue = new CssLengthValue(1, CssLengthValue.Unit.In);
//         var ptValue = new CssLengthValue(72, CssLengthValue.Unit.Pt);
//         var cmValue = new CssLengthValue(2.54, CssLengthValue.Unit.Cm);
//         var mmValue = new CssLengthValue(25.4, CssLengthValue.Unit.Mm);
//         var pcValue = new CssLengthValue(6, CssLengthValue.Unit.Pc);
//
//         // Act
//         var inResult = _calculator.ToPixels(inValue, _elementMock.Object, "width");
//         var ptResult = _calculator.ToPixels(ptValue, _elementMock.Object, "width");
//         var cmResult = _calculator.ToPixels(cmValue, _elementMock.Object, "width");
//         var mmResult = _calculator.ToPixels(mmValue, _elementMock.Object, "width");
//         var pcResult = _calculator.ToPixels(pcValue, _elementMock.Object, "width");
//
//         // Assert - all should be approximately 96px (standard CSS DPI)
//         Assert.That(inResult, Is.EqualTo(96).Within(0.1), "1in should equal 96px");
//         Assert.That(ptResult, Is.EqualTo(96).Within(0.1), "72pt should equal 96px");
//         Assert.That(cmResult, Is.EqualTo(96).Within(0.1), "2.54cm should equal 96px");
//         Assert.That(mmResult, Is.EqualTo(96).Within(0.1), "25.4mm should equal 96px");
//         Assert.That(pcResult, Is.EqualTo(96).Within(0.1), "6pc should equal 96px");
//     }
//
//     [Test]
//     public void ToPixels_RelativeUnits_ConvertsToPx()
//     {
//         // Arrange
//         var exValue = new CssLengthValue(2, CssLengthValue.Unit.Ex);
//         var chValue = new CssLengthValue(2, CssLengthValue.Unit.Ch);
//
//         // Act
//         var exResult = _calculator.ToPixels(exValue, _elementMock.Object, "width");
//         var chResult = _calculator.ToPixels(chValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(exResult, Is.EqualTo(16), "2ex * 16px * 0.5 should equal 16px");
//         Assert.That(chResult, Is.EqualTo(16), "2ch * 16px * 0.5 should equal 16px");
//     }
//
//     [Test]
//     public void ToPixels_SpecialValues_HandlesCorrectly()
//     {
//         // Arrange
//         var autoValue = CssLengthValue.Auto;
//         var zeroValue = CssLengthValue.Zero;
//
//         // Act
//         var autoResult = _calculator.ToPixels(autoValue, _elementMock.Object, "width");
//         var zeroResult = _calculator.ToPixels(zeroValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(autoResult, Is.EqualTo(0), "'auto' should convert to 0 for pixel calculation");
//         Assert.That(zeroResult, Is.EqualTo(0), "Zero should remain 0");
//     }
//
//     #endregion
//
//     #region Calc Expression Tests
//
//     [Test]
//     public void EvaluateCalc_SimpleAddition_ReturnsCorrectResult()
//     {
//         // Arrange - create a mock calc value with addition
//         var calcValue = new CssCalcValue(
//             new CssCalcAddExpression(
//                 new CssLengthValue(100, CssLengthValue.Unit.Px),
//                 new CssLengthValue(20, CssLengthValue.Unit.Px)));
//
//         // Act
//         var result = _calculator.EvaluateCalc(calcValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(120), "100px + 20px should equal 120px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void EvaluateCalc_SimpleSubtraction_ReturnsCorrectResult()
//     {
//         // Arrange - create a mock calc value with subtraction
//         var calcValue = new CssCalcValue(
//             new CssCalcSubExpression(
//                 new CssLengthValue(100, CssLengthValue.Unit.Px),
//                 new CssLengthValue(20, CssLengthValue.Unit.Px)));
//
//         // Act
//         var result = _calculator.EvaluateCalc(calcValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(80), "100px - 20px should equal 80px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void EvaluateCalc_Multiplication_ReturnsCorrectResult()
//     {
//         // Arrange - create a mock calc value with multiplication
//         var calcValue = new CssCalcValue(
//             new CssCalcMulExpression(
//                 new CssNumberValue(2),
//                 new CssLengthValue(50, CssLengthValue.Unit.Px)));
//
//         // Act
//         var result = _calculator.EvaluateCalc(calcValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(100), "2 * 50px should equal 100px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void EvaluateCalc_Division_ReturnsCorrectResult()
//     {
//         // Arrange - create a mock calc value with division
//         var calcValue = new CssCalcValue(
//             new CssCalcDivExpression(
//                 new CssLengthValue(100, CssLengthValue.Unit.Px),
//                 new CssNumberValue(2)));
//
//         // Act
//         var result = _calculator.EvaluateCalc(calcValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(50), "100px / 2 should equal 50px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void EvaluateCalc_MixedUnits_ReturnsCorrectResult()
//     {
//         // Arrange - create a mock calc value with mixed units
//         var calcValue = new CssCalcValue(
//             new CssCalcAddExpression(
//                 new CssLengthValue(50, CssLengthValue.Unit.Px),
//                 new CssLengthValue(1, CssLengthValue.Unit.Em)));
//
//         // Act
//         var result = _calculator.EvaluateCalc(calcValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(66), "50px + 1em (16px) should equal 66px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void EvaluateCalc_WithPercentage_ReturnsCorrectResult()
//     {
//         // Arrange
//         var calcValue = new CssCalcValue(
//             new CssCalcAddExpression(
//                 new CssLengthValue(50, CssLengthValue.Unit.Px),
//                 new CssPercentageValue(10)));
//
//         // Setup parent element width
//         _parentElementMock.Setup(e => e.GetAttribute("style")).Returns("width: 200px;");
//
//         // Create a properly mocked ICssStyleDeclaration
//         var styleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the width property
//         styleDeclaration.Setup(d => d.GetPropertyValue("width")).Returns("200px");
//
//         // Create a property for the width
//         var widthProperty = new Mock<ICssProperty>();
//         widthProperty.Setup(p => p.Name).Returns("width");
//         widthProperty.Setup(p => p.Value).Returns("200px");
//         widthProperty.Setup(p => p.RawValue).Returns(new CssLengthValue(200, CssLengthValue.Unit.Px));
//
//         // Setup enumerator to return the property
//         var propertyList = new List<ICssProperty> { widthProperty.Object };
//         styleDeclaration.Setup(s => s.GetEnumerator()).Returns(propertyList.GetEnumerator());
//
//         // Mock CSS parser to return the style declaration
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration(It.IsAny<string>())).Returns(styleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Act
//         var result = _calculator.EvaluateCalc(calcValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(70), "50px + 10% of 200px (20px) should equal 70px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void EvaluateCalc_DivisionByZero_HandlesGracefully()
//     {
//         // Arrange - create a mock calc value with division by zero
//         var calcValue = new CssCalcValue(
//             new CssCalcDivExpression(
//                 new CssLengthValue(100, CssLengthValue.Unit.Px),
//                 new CssNumberValue(0)));
//
//         // Act
//         var result = _calculator.EvaluateCalc(calcValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(0), "Division by zero should be handled gracefully");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     #endregion
//
//     #region CSS Global Keywords Tests
//
//     [Test]
//     public void Compute_InheritKeyword_InheritsFromParent()
//     {
//         // Arrange
//         var inheritValue = new CssIdentifierValue("inherit");
//         var parentColorValue = CssColorValue.Red;
//
//         // Setup parent element to have a color
//         _parentElementMock.Setup(e => e.GetAttribute("style")).Returns("color: red;");
//
//         // Create a properly mocked ICssStyleDeclaration for the parent
//         var parentStyleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the color property
//         parentStyleDeclaration.Setup(d => d.GetPropertyValue("color")).Returns("red");
//
//         // Create a property for the color
//         var colorProperty = new Mock<ICssProperty>();
//         colorProperty.Setup(p => p.Name).Returns("color");
//         colorProperty.Setup(p => p.Value).Returns("red");
//         colorProperty.Setup(p => p.RawValue).Returns(parentColorValue);
//
//         // Setup enumerator to return the property
//         var propertyList = new List<ICssProperty> { colorProperty.Object };
//         parentStyleDeclaration
//             .As<IEnumerable<ICssProperty>>()
//             .Setup(s => s.GetEnumerator())
//             .Returns(() => propertyList.GetEnumerator());
//
//         // Setup the parent style declaration to be returned for "color" property
//         parentStyleDeclaration.Setup(s => s.GetProperty("color")).Returns(colorProperty.Object);
//
//         // Mock CSS parser to return the style declaration
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration(It.IsAny<string>())).Returns(parentStyleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Act
//         var result = _calculator.Compute(inheritValue, _elementMock.Object, "color");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssColorValue>(), "Result should be a color value");
//
//         // Compare the color values properly
//         var resultColor = (CssColorValue)result;
//         Assert.That(resultColor.R, Is.EqualTo(parentColorValue.R), "Red component should match");
//         Assert.That(resultColor.G, Is.EqualTo(parentColorValue.G), "Green component should match");
//         Assert.That(resultColor.B, Is.EqualTo(parentColorValue.B), "Blue component should match");
//         Assert.That(resultColor.A, Is.EqualTo(parentColorValue.A), "Alpha component should match");
//     }
//
//     [Test]
//     public void Compute_InitialKeyword_ReturnsInitialValue()
//     {
//         // Arrange
//         var initialValue = new CssIdentifierValue("initial");
//         var initialColorValue = CssColorValue.Black; // Default color is black
//
//         // Setup declaration factory to return initial values
//         var colorDeclaration = new DeclarationInfo(
//             "color",
//             Mock.Of<IValueConverter>(),
//             initialValue: initialColorValue
//         );
//         _declarationFactoryMock.Setup(f => f.Create("color")).Returns(colorDeclaration);
//
//         // Act
//         var result = _calculator.Compute(initialValue, _elementMock.Object, "color");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssColorValue>(), "Result should be a color value");
//         Assert.That(result, Is.EqualTo(initialColorValue), "Result should equal the initial color value");
//     }
//
//     [Test]
//     public void Compute_UnsetKeyword_InheritsForInheritedProperties()
//     {
//         // Arrange
//         var unsetValue = new CssIdentifierValue("unset");
//         var parentColorValue = CssColorValue.Red;
//
//         // Setup parent element to have a color
//         _parentElementMock.Setup(e => e.GetAttribute("style")).Returns("color: red;");
//
//         // Create a properly mocked ICssStyleDeclaration for the parent style
//         var parentStyleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the color property value
//         parentStyleDeclaration.Setup(d => d.GetPropertyValue("color")).Returns("red");
//
//         // Create a property for the color
//         var colorProperty = new Mock<ICssProperty>();
//         colorProperty.Setup(p => p.Name).Returns("color");
//         colorProperty.Setup(p => p.Value).Returns("red");
//         colorProperty.Setup(p => p.RawValue).Returns(parentColorValue);
//
//         // Setup property access and enumeration
//         var propertyList = new List<ICssProperty> { colorProperty.Object };
//         parentStyleDeclaration.Setup(s => s.GetProperty("color")).Returns(colorProperty.Object);
//
//         // Use a lambda to get a fresh enumerator each time
//         parentStyleDeclaration.As<IEnumerable<ICssProperty>>()
//             .Setup(s => s.GetEnumerator())
//             .Returns(() => propertyList.GetEnumerator());
//
//         // Mock CSS parser to return the style declaration with specific format
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration("color: red"))
//             .Returns(parentStyleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Setup declaration factory to indicate color is inheritable
//         var colorInfo = new DeclarationInfo(
//             "color",
//             Mock.Of<IValueConverter>(),
//             PropertyFlags.Inherited,
//             CssColorValue.Black
//         );
//         _declarationFactoryMock.Setup(f => f.Create("color")).Returns(colorInfo);
//
//         // Act
//         var result = _calculator.Compute(unsetValue, _elementMock.Object, "color");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssColorValue>(), "Result should be a color value");
//
//         var colorResult = result as CssColorValue? ?? default;
//         Assert.That(colorResult.R, Is.EqualTo(parentColorValue.R), "Red component should match");
//         Assert.That(colorResult.G, Is.EqualTo(parentColorValue.G), "Green component should match");
//         Assert.That(colorResult.B, Is.EqualTo(parentColorValue.B), "Blue component should match");
//         Assert.That(colorResult.A, Is.EqualTo(parentColorValue.A), "Alpha component should match");
//     }
//
//     [Test]
//     public void Compute_UnsetKeyword_UsesInitialForNonInheritedProperties()
//     {
//         // Arrange
//         var unsetValue = new CssIdentifierValue("unset");
//         var initialDisplayValue = new CssIdentifierValue("inline");
//
//         // Setup declaration factory to indicate display is NOT inheritable
//         var displayInfo = new DeclarationInfo(
//             "display",
//             Mock.Of<IValueConverter>(),
//             PropertyFlags.None,
//             initialDisplayValue
//         );
//         _declarationFactoryMock.Setup(f => f.Create("display")).Returns(displayInfo);
//
//         // Act
//         var result = _calculator.Compute(unsetValue, _elementMock.Object, "display");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.EqualTo(initialDisplayValue), "Result should equal the initial display value");
//     }
//
//     #endregion
//
//     #region Percentage Handling Tests
//
//     [Test]
//     public void Compute_PercentageForWidth_ConvertsToPx()
//     {
//         // Arrange
//         var percentValue = new CssPercentageValue(50);
//
//         // Setup parent element width
//         _parentElementMock.Setup(e => e.GetAttribute("style")).Returns("width: 200px;");
//
//         // Create a properly mocked ICssStyleDeclaration
//         var styleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the width property
//         styleDeclaration.Setup(d => d.GetPropertyValue("width")).Returns("200px");
//
//         // Create a property for the width
//         var widthProperty = new Mock<ICssProperty>();
//         widthProperty.Setup(p => p.Name).Returns("width");
//         widthProperty.Setup(p => p.Value).Returns("200px");
//         widthProperty.Setup(p => p.RawValue).Returns(new CssLengthValue(200, CssLengthValue.Unit.Px));
//
//         // Setup enumerator to return the property
//         var propertyList = new List<ICssProperty> { widthProperty.Object };
//         styleDeclaration.Setup(s => s.GetEnumerator()).Returns(propertyList.GetEnumerator());
//
//         // Mock CSS parser to return the style declaration
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration(It.IsAny<string>())).Returns(styleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Act
//         var result = _calculator.Compute(percentValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(100), "50% of 200px should equal 100px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void Compute_PercentageForFontSize_ConvertsToPx()
//     {
//         // Arrange
//         var percentValue = new CssPercentageValue(150);
//
//         // Setup parent element font size
//         _parentElementMock.Setup(e => e.GetAttribute("style")).Returns("font-size: 16px;");
//
//         // Create a properly mocked ICssStyleDeclaration
//         var styleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the font-size property getter
//         styleDeclaration.Setup(d => d.GetPropertyValue("font-size")).Returns("16px");
//
//         // Create a property for the font-size
//         var fontSizeProperty = new Mock<ICssProperty>();
//         fontSizeProperty.Setup(p => p.Name).Returns("font-size");
//         fontSizeProperty.Setup(p => p.Value).Returns("16px");
//         fontSizeProperty.Setup(p => p.RawValue).Returns(new CssLengthValue(16, CssLengthValue.Unit.Px));
//
//         // Setup enumerator to return the property
//         var propertyList = new List<ICssProperty> { fontSizeProperty.Object };
//         styleDeclaration.Setup(s => s.GetEnumerator()).Returns(propertyList.GetEnumerator());
//
//         // Mock CSS parser to return the style declaration
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration(It.IsAny<string>())).Returns(styleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Act
//         var result = _calculator.Compute(percentValue, _elementMock.Object, "font-size");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(24), "150% of 16px should equal 24px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void Compute_PercentageForMargin_ConvertsToPx()
//     {
//         // Arrange
//         var percentValue = new CssPercentageValue(25);
//
//         // Setup parent element width
//         _parentElementMock.Setup(e => e.GetAttribute("style")).Returns("width: 400px;");
//
//         // Create a properly mocked ICssStyleDeclaration
//         var styleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the width property
//         styleDeclaration.Setup(d => d.GetPropertyValue("width")).Returns("400px");
//
//         // Create a property for the width
//         var widthProperty = new Mock<ICssProperty>();
//         widthProperty.Setup(p => p.Name).Returns("width");
//         widthProperty.Setup(p => p.Value).Returns("400px");
//         widthProperty.Setup(p => p.RawValue).Returns(new CssLengthValue(400, CssLengthValue.Unit.Px));
//
//         // Setup enumerator to return the property
//         var propertyList = new List<ICssProperty> { widthProperty.Object };
//         styleDeclaration.Setup(s => s.GetEnumerator()).Returns(propertyList.GetEnumerator());
//
//         // Mock CSS parser to return the style declaration
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration(It.IsAny<string>())).Returns(styleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Act
//         var result = _calculator.Compute(percentValue, _elementMock.Object, "margin-left");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(100), "25% of 400px should equal 100px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void Compute_PercentageForNonDimensionalProperty_KeepsAsPercentage()
//     {
//         // Arrange
//         var percentValue = new CssPercentageValue(75);
//
//         // Act
//         var result = _calculator.Compute(percentValue, _elementMock.Object, "opacity");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssPercentageValue>(), "Result should remain a percentage value");
//         var resultPercent = (CssPercentageValue)result;
//         Assert.That(resultPercent.Value, Is.EqualTo(75), "Percentage should remain unchanged");
//     }
//
//     #endregion
//
//     #region Context-Dependent Tests
//
//     [Test]
//     public void Compute_FontSizeWithEm_RelativeToParentFontSize()
//     {
//         // Arrange
//         var emValue = new CssLengthValue(1.5, CssLengthValue.Unit.Em);
//
//         // Setup parent element font size to be different
//         _parentElementMock.Setup(e => e.GetAttribute("style")).Returns("font-size: 20px;");
//
//         // Create a properly mocked ICssStyleDeclaration
//         var styleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the font-size property
//         styleDeclaration.Setup(d => d.GetPropertyValue("font-size")).Returns("20px");
//
//         // Create a property for the font-size
//         var fontSizeProperty = new Mock<ICssProperty>();
//         fontSizeProperty.Setup(p => p.Name).Returns("font-size");
//         fontSizeProperty.Setup(p => p.Value).Returns("20px");
//         fontSizeProperty.Setup(p => p.RawValue).Returns(new CssLengthValue(20, CssLengthValue.Unit.Px));
//
//         // Setup enumerator to return the property
//         var propertyList = new List<ICssProperty> { fontSizeProperty.Object };
//         styleDeclaration.Setup(s => s.GetEnumerator()).Returns(propertyList.GetEnumerator());
//
//         // Mock CSS parser to return the style declaration
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration(It.IsAny<string>())).Returns(styleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Act
//         var result = _calculator.Compute(emValue, _elementMock.Object, "font-size");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(30), "1.5em * 20px should equal 30px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void Compute_FontSizeWithRem_RelativeToRootFontSize()
//     {
//         // Arrange
//         var remValue = new CssLengthValue(2, CssLengthValue.Unit.Rem);
//
//         // Setup root element font size to be different
//         _rootElementMock.Setup(e => e.GetAttribute("style")).Returns("font-size: 18px;");
//
//         // Create a properly mocked ICssStyleDeclaration
//         var styleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the font-size property value
//         styleDeclaration.Setup(d => d.GetPropertyValue("font-size")).Returns("18px");
//
//         // Create a property for the font-size
//         var fontSizeProperty = new Mock<ICssProperty>();
//         fontSizeProperty.Setup(p => p.Name).Returns("font-size");
//         fontSizeProperty.Setup(p => p.Value).Returns("18px");
//         fontSizeProperty.Setup(p => p.RawValue).Returns(new CssLengthValue(18, CssLengthValue.Unit.Px));
//
//         // Setup enumerator to return the property
//         var propertyList = new List<ICssProperty> { fontSizeProperty.Object };
//         styleDeclaration.Setup(s => s.GetEnumerator()).Returns(propertyList.GetEnumerator());
//
//         // Mock CSS parser to return the style declaration
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration(It.IsAny<string>())).Returns(styleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Act
//         var result = _calculator.Compute(remValue, _elementMock.Object, "font-size");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(36), "2rem * 18px should equal 36px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void Compute_EmInNonFontSizeProperty_RelativeToElementFontSize()
//     {
//         // Arrange
//         var emValue = new CssLengthValue(2, CssLengthValue.Unit.Em);
//
//         // Setup element font size
//         _elementMock.Setup(e => e.GetAttribute("style")).Returns("font-size: 14px;");
//
//         // Create a properly mocked ICssStyleDeclaration
//         var styleDeclaration = new Mock<ICssStyleDeclaration>();
//
//         // Setup the font-size property value
//         styleDeclaration.Setup(d => d.GetPropertyValue("font-size")).Returns("14px");
//
//         // Create a property for the font-size
//         var fontSizeProperty = new Mock<ICssProperty>();
//         fontSizeProperty.Setup(p => p.Name).Returns("font-size");
//         fontSizeProperty.Setup(p => p.Value).Returns("14px");
//         fontSizeProperty.Setup(p => p.RawValue).Returns(new CssLengthValue(14, CssLengthValue.Unit.Px));
//
//         // Setup enumerator to return the property
//         var propertyList = new List<ICssProperty> { fontSizeProperty.Object };
//         styleDeclaration.Setup(s => s.GetEnumerator()).Returns(propertyList.GetEnumerator());
//
//         // Mock CSS parser to return the style declaration
//         var parserMock = new Mock<ICssParser>();
//         parserMock.Setup(p => p.ParseDeclaration(It.IsAny<string>())).Returns(styleDeclaration.Object);
//         _contextMock.Setup(c => c.GetService<ICssParser>()).Returns(parserMock.Object);
//
//         // Act
//         var result = _calculator.Compute(emValue, _elementMock.Object, "margin-top");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(28), "2em * 14px should equal 28px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     #endregion
//
//     #region Edge Cases and Special Values
//
//     [Test]
//     public void Compute_AutoValue_HandlesCorrectly()
//     {
//         // Arrange
//         var autoValue = CssLengthValue.Auto;
//
//         // Act
//         var result = _calculator.Compute(autoValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Equals(CssLengthValue.Auto), Is.True, "Auto value should remain auto");
//     }
//
//     [Test]
//     public void Compute_NoneValue_HandlesCorrectly()
//     {
//         // Arrange
//         var noneValue = new CssIdentifierValue("none");
//
//         // Setup initial border width (0px)
//         var initialBorderWidth = CssLengthValue.Zero;
//         var borderWidthInfo = new DeclarationInfo(
//             "border-width",
//             Mock.Of<IValueConverter>(),
//             initialValue: initialBorderWidth
//         );
//         _declarationFactoryMock.Setup(f => f.Create("border-width")).Returns(borderWidthInfo);
//
//         // Act
//         var result = _calculator.Compute(noneValue, _elementMock.Object, "border-width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         // In most contexts, 'none' for border-width should compute to 0px
//     }
//
//     [Test]
//     public void Compute_NegativeLength_HandlesCorrectly()
//     {
//         // Arrange
//         var negativeValue = new CssLengthValue(-10, CssLengthValue.Unit.Px);
//
//         // Act
//         var result = _calculator.Compute(negativeValue, _elementMock.Object, "margin-top");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(-10), "Negative values should be preserved");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Unit should remain unchanged");
//     }
//
//     #endregion
//
//     #region Relative Value Tests
//
//     [Test]
//     public void ResolveRelative_PercentageToLength_ReturnsAbsoluteLength()
//     {
//         // Arrange
//         var percentValue = new CssPercentageValue(50);
//         var baseValue = new CssLengthValue(200, CssLengthValue.Unit.Px);
//
//         // Act
//         var result = _calculator.ResolveRelative(percentValue, _elementMock.Object, baseValue, "width");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssLengthValue>(), "Result should be a length value");
//         var resultLength = (CssLengthValue)result;
//         Assert.That(resultLength.Value, Is.EqualTo(100), "50% of 200px should equal 100px");
//         Assert.That(resultLength.Type, Is.EqualTo(CssLengthValue.Unit.Px), "Result unit should be px");
//     }
//
//     [Test]
//     public void ResolveRelative_FontWeightKeyword_ReturnsNumericWeight()
//     {
//         // Arrange
//         var lighterValue = new CssIdentifierValue("lighter");
//         var boldBaseValue = new CssIntegerValue(700); // Parent has bold
//
//         // Act
//         var result = _calculator.ResolveRelative(lighterValue, _elementMock.Object, boldBaseValue, "font-weight");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssIntegerValue>(), "Result should be an integer value");
//         var resultWeight = (CssIntegerValue)result;
//         Assert.That(resultWeight.IntValue, Is.EqualTo(400), "'lighter' relative to 700 should be 400");
//     }
//
//     [Test]
//     public void ResolveRelative_BolderKeyword_ReturnsNumericWeight()
//     {
//         // Arrange
//         var bolderValue = new CssIdentifierValue("bolder");
//         var normalBaseValue = new CssIntegerValue(400); // Parent has normal
//
//         // Act
//         var result = _calculator.ResolveRelative(bolderValue, _elementMock.Object, normalBaseValue, "font-weight");
//
//         // Assert
//         Assert.That(result, Is.Not.Null, "Result should not be null");
//         Assert.That(result, Is.InstanceOf<CssIntegerValue>(), "Result should be an integer value");
//         var resultWeight = (CssIntegerValue)result;
//         Assert.That(resultWeight.IntValue, Is.EqualTo(700), "'bolder' relative to 400 should be 700");
//     }
//
//     #endregion
//
//     #region Caching and Optimization Tests
//
//     [Test]
//     public void Compute_SameInputsMultipleTimes_CachesResult()
//     {
//         // Arrange
//         var emValue = new CssLengthValue(1.5, CssLengthValue.Unit.Em);
//
//         // Act
//         var result1 = _calculator.Compute(emValue, _elementMock.Object, "width");
//         var result2 = _calculator.Compute(emValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result1, Is.Not.Null, "First result should not be null");
//         Assert.That(result2, Is.Not.Null, "Second result should not be null");
//         Assert.That(result2, Is.SameAs(result1), "Results should be the same object instance due to caching");
//     }
//
//     [Test]
//     public void ClearCache_RemovesAllCachedResults()
//     {
//         // Arrange
//         var emValue = new CssLengthValue(1.5, CssLengthValue.Unit.Em);
//         var result1 = _calculator.Compute(emValue, _elementMock.Object, "width");
//
//         // Act
//         _calculator.ClearCache();
//         var result2 = _calculator.Compute(emValue, _elementMock.Object, "width");
//
//         // Assert
//         Assert.That(result1, Is.Not.Null, "First result should not be null");
//         Assert.That(result2, Is.Not.Null, "Second result should not be null");
//         Assert.That(result2, Is.Not.SameAs(result1), "Results should be different object instances after clearing cache");
//     }
//
//     #endregion
// }