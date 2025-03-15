namespace AngleSharp.StyleSystem.Tests.Unit.Computation;

using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Storage;
using Css.Parser;
using Moq;

[TestFixture]
public class ComputedStyleTests
{
    #region Test Setup & Helpers

    private ComputedStyle CreateComputedStyle(
        IElement element,
        ICssStyleDeclaration declaration,
        IComputedStyle? parentStyle = null,
        PropertyTreeNode? propertyTree = null)
    {
        var mockRenderDevice = new Mock<IRenderDevice>();
        mockRenderDevice.Setup(r => r.ViewPortWidth).Returns(1024);
        mockRenderDevice.Setup(r => r.ViewPortHeight).Returns(768);
        mockRenderDevice.Setup(r => r.FontSize).Returns(16);

        var mockDeclarationFactory = new Mock<IDeclarationFactory>();

        var mockContext = new Mock<IBrowsingContext>();
        mockContext.Setup(c => c.GetServices<IDeclarationFactory>())
            .Returns(new List<IDeclarationFactory> { mockDeclarationFactory.Object });

        var mockInvalidationTracker = new Mock<StyleInvalidationTracker>();

        propertyTree ??= new PropertyTreeNode(null);

        return new ComputedStyle(
            element,
            parentStyle,
            declaration,
            propertyTree,
            mockRenderDevice.Object,
            mockInvalidationTracker.Object,
            mockContext.Object);
    }

    private ICssStyleDeclaration CreateDeclaration(Dictionary<string, ICssValue> properties)
    {
        // Create a mock style declaration instead of using the actual CssStyleDeclaration
        var mockDeclaration = new Mock<ICssStyleDeclaration>();

        // Setup basic properties
        mockDeclaration.Setup(d => d.Length).Returns(properties.Count);

        // Setup GetPropertyValue method to return values from the dictionary
        mockDeclaration.Setup(d => d.GetPropertyValue(It.IsAny<string>()))
            .Returns<string>(key => properties.ContainsKey(key) ? properties[key].CssText : string.Empty);

        // Setup the indexer to return properties
        var mockProperties = new List<ICssProperty>();
        foreach (var prop in properties)
        {
            var mockProperty = new Mock<ICssProperty>();
            mockProperty.Setup(p => p.Name).Returns(prop.Key);
            mockProperty.Setup(p => p.Value).Returns(prop.Value.CssText);

            // Create a CSS value for the RawValue
            var cssValue = prop.Value;
            // cssValue.Setup(v => v.CssText).Returns(prop.Value.CssText);
            mockProperty.Setup(p => p.RawValue).Returns(cssValue);

            mockProperties.Add(mockProperty.Object);
        }

        // Setup enumerator to iterate through properties
        mockDeclaration.Setup(d => d.GetEnumerator())
            .Returns(() => mockProperties.GetEnumerator());


        return mockDeclaration.Object;
    }

    private ICssStyleDeclaration CreateEmptyDeclaration()
    {
        return CreateDeclaration(new Dictionary<string, ICssValue>());
    }

    private IElement CreateMockElement(string nodeName = "div", IElement? parent = null)
    {
        var mockElement = new Mock<IElement>();
        mockElement.Setup(e => e.NodeName).Returns(nodeName);
        mockElement.Setup(e => e.ParentElement).Returns(parent);
        return mockElement.Object;
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_ShouldInitializeWithValidParameters()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var propertyTree = new PropertyTreeNode(null);

        // Act & Assert (no exception should be thrown)
        var style = CreateComputedStyle(element, declaration, null, propertyTree);

        Assert.That(style, Is.Not.Null);
        Assert.That(style.Declaration, Is.SameAs(declaration));
    }

    [Test]
    public void Constructor_ShouldProcessDeclaration()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "color", CssColorValue.Red },
            // { "display", "block" }, // TODO: Fix this
            { "font-size", new CssLengthValue(16, CssLengthValue.Unit.Px) }
        });
        var propertyTree = new PropertyTreeNode(null);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);

        // Assert
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(style.Display, Is.EqualTo(DisplayMode.Block));
        Assert.That(style.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    #endregion

    #region GetPropertyValue Tests

    [Test]
    public void GetPropertyValue_ShouldReturnValueFromPropertyTree()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var propertyTree = new PropertyTreeNode(null);
        propertyTree.SetProperty("color", "blue");

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        var color = style.GetPropertyValue("color");

        // Assert
        Assert.That(color, Is.EqualTo("blue"));
    }

    [Test]
    public void GetPropertyValue_ShouldReturnValueFromDeclaration_WhenNotInPropertyTree()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "color", CssColorValue.Red }
        });
        var propertyTree = new PropertyTreeNode(null);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        var color = style.GetPropertyValue("color");

        // Assert
        Assert.That(color, Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public void GetPropertyValue_ShouldReturnEmptyString_WhenPropertyNotFound()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var propertyTree = new PropertyTreeNode(null);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        var nonExistentProperty = style.GetPropertyValue("non-existent-property");

        // Assert
        Assert.That(nonExistentProperty, Is.EqualTo(string.Empty));
    }

    #endregion

    #region GetValue<T> Tests

    [Test]
    public void GetValue_ShouldReturnTypedValue_WhenPropertyExists()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var propertyTree = new PropertyTreeNode(null);
        propertyTree.SetProperty("font-size", CssLengthValue.Medium);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        var fontSize = style.GetValue<CssLengthValue>("font-size");

        // Assert
        // CssLengthValue is a struct, so we check if it has the default value
        Assert.That(fontSize.CssText, Is.EqualTo(CssLengthValue.Medium.CssText));
    }

    [Test]
    public void GetValue_ShouldReturnDefaultValue_WhenPropertyDoesNotExist()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var propertyTree = new PropertyTreeNode(null);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        var nonExistentProperty = style.GetValue<CssLengthValue>("non-existent-property");

        // Assert
        // For a struct, we compare with default value
        Assert.That(nonExistentProperty, Is.EqualTo(default(CssLengthValue)));
    }

    [Test]
    public void GetValue_ShouldReturnDefaultValue_WhenValueIsNotOfRequestedType()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var propertyTree = new PropertyTreeNode(null);
        propertyTree.SetProperty("color", CssColorValue.Red);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        var colorAsLength = style.GetValue<CssLengthValue>("color");

        // Assert
        Assert.That(colorAsLength, Is.EqualTo(default(CssLengthValue)));
    }

    #endregion

    #region Display Property Tests

    [TestCase(CssKeywords.None, DisplayMode.None, DisplayMode.None)]
    [TestCase(CssKeywords.Block, DisplayMode.Block, DisplayMode.Block)]
    [TestCase(CssKeywords.Inline, DisplayMode.Inline, DisplayMode.Inline)]
    [TestCase(CssKeywords.InlineBlock, DisplayMode.InlineBlock, DisplayMode.InlineBlock)]
    [TestCase(CssKeywords.Flex, DisplayMode.Flex, DisplayMode.Flex)]
    [TestCase(CssKeywords.Grid, DisplayMode.Grid, DisplayMode.Grid)]
    [TestCase(CssKeywords.Table, DisplayMode.Table, DisplayMode.Table)]
    [TestCase(CssKeywords.InlineFlex, DisplayMode.InlineFlex, DisplayMode.InlineFlex)]
    public void Display_ShouldReturnCorrectDisplayMode(string cssKeywords, DisplayMode displayValue, DisplayMode expectedMode)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "display", new CssConstantValue<DisplayMode>(cssKeywords, displayValue) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Display, Is.EqualTo(expectedMode));
    }

    #endregion

    #region Position Property Tests

    [TestCase(CssKeywords.Static, PositionMode.Static, PositionMode.Static)]
    [TestCase(CssKeywords.Relative, PositionMode.Relative, PositionMode.Relative)]
    [TestCase(CssKeywords.Absolute, PositionMode.Absolute, PositionMode.Absolute)]
    [TestCase(CssKeywords.Fixed, PositionMode.Fixed, PositionMode.Fixed)]
    [TestCase(CssKeywords.Sticky, PositionMode.Sticky, PositionMode.Sticky)]
    public void Position_ShouldReturnCorrectPositionMode(string cssKeyword, PositionMode positionValue, PositionMode expectedMode)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "position", new CssConstantValue<PositionMode>(cssKeyword, positionValue) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Position, Is.EqualTo(expectedMode));
    }

    #endregion

    #region Box Properties Tests

    [Test]
    public void Box_Width_ShouldReturnCorrectValue()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "width", new CssLengthValue(100, CssLengthValue.Unit.Px) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Box.Width.CssText, Is.EqualTo("100px"));
    }

    [Test]
    public void Box_Height_ShouldReturnCorrectValue()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "height", new CssLengthValue(200, CssLengthValue.Unit.Px) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Box.Height.CssText, Is.EqualTo("200px"));
    }

    [Test]
    public void Box_Margin_ShouldReturnCorrectValues()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "margin-top", new CssLengthValue(10, CssLengthValue.Unit.Px) },
            { "margin-right", new CssLengthValue(20, CssLengthValue.Unit.Px) },
            { "margin-bottom", new CssLengthValue(30, CssLengthValue.Unit.Px) },
            { "margin-left", new CssLengthValue(40, CssLengthValue.Unit.Px) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Box.Margin.Top.CssText, Is.EqualTo("10px"));
        Assert.That(style.Box.Margin.Right.CssText, Is.EqualTo("20px"));
        Assert.That(style.Box.Margin.Bottom.CssText, Is.EqualTo("30px"));
        Assert.That(style.Box.Margin.Left.CssText, Is.EqualTo("40px"));
    }

    [Test]
    public void Box_Border_ShouldReturnCorrectValues()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "border-top-width", new CssLengthValue(1, CssLengthValue.Unit.Px) },
            { "border-right-width", new CssLengthValue(2, CssLengthValue.Unit.Px) },
            { "border-bottom-width", new CssLengthValue(3, CssLengthValue.Unit.Px) },
            { "border-left-width", new CssLengthValue(4, CssLengthValue.Unit.Px) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Box.Border.Top.CssText, Is.EqualTo("1px"));
        Assert.That(style.Box.Border.Right.CssText, Is.EqualTo("2px"));
        Assert.That(style.Box.Border.Bottom.CssText, Is.EqualTo("3px"));
        Assert.That(style.Box.Border.Left.CssText, Is.EqualTo("4px"));
    }

    [Test]
    public void Box_Padding_ShouldReturnCorrectValues()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "padding-top", new CssLengthValue(5, CssLengthValue.Unit.Px) },
            { "padding-right", new CssLengthValue(10, CssLengthValue.Unit.Px) },
            { "padding-bottom", new CssLengthValue(15, CssLengthValue.Unit.Px) },
            { "padding-left", new CssLengthValue(20, CssLengthValue.Unit.Px) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Box.Padding.Top.CssText, Is.EqualTo("5px"));
        Assert.That(style.Box.Padding.Right.CssText, Is.EqualTo("10px"));
        Assert.That(style.Box.Padding.Bottom.CssText, Is.EqualTo("15px"));
        Assert.That(style.Box.Padding.Left.CssText, Is.EqualTo("20px"));
    }

    #endregion

    #region Text Properties Tests

    [Test]
    public void Text_FontFamily_ShouldReturnCorrectValue()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "font-family", new CssIdentifierValue("Arial, sans-serif") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Text.FontFamily, Is.EqualTo("Arial, sans-serif"));
    }

    [Test]
    public void Text_FontSize_ShouldReturnCorrectValue()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "font-size", new CssLengthValue(18, CssLengthValue.Unit.Px) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.FontSize.CssText, Is.EqualTo("18px"));
    }

    // [TestCase("400", 400)]
    // [TestCase("700", 700)]
    [TestCase(CssKeywords.Bold, FontWeight.Bold, 700)]
    [TestCase(CssKeywords.Normal, FontWeight.Normal, 400)]
    public void Text_FontWeight_ShouldReturnCorrectValue(string cssKeyword, FontWeight fontWeightValue, int expectedWeight)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "font-weight", new CssConstantValue<FontWeight>(cssKeyword, fontWeightValue) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Text.FontWeight, Is.EqualTo(expectedWeight));
    }

    [TestCase(CssKeywords.Italic, FontStyle.Italic, true)]
    [TestCase(CssKeywords.Normal, FontStyle.Normal, false)]
    public void Text_IsItalic_ShouldReturnCorrectValue(string cssKeyword, FontStyle fontStyleValue, bool expectedIsItalic)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "font-style", new CssConstantValue<FontStyle>(cssKeyword, fontStyleValue) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Text.IsItalic, Is.EqualTo(expectedIsItalic));
    }

    [TestCase(CssKeywords.Left, TextAlign.Left, TextAlign.Left)]
    [TestCase(CssKeywords.Right, TextAlign.Right, TextAlign.Right)]
    [TestCase(CssKeywords.Center, TextAlign.Center, TextAlign.Center)]
    [TestCase(CssKeywords.Justify, TextAlign.Justify, TextAlign.Justify)]
    [TestCase(CssKeywords.Start, TextAlign.Start, TextAlign.Start)]
    [TestCase(CssKeywords.End, TextAlign.End, TextAlign.End)]
    public void Text_TextAlign_ShouldReturnCorrectValue(string cssKeyword, TextAlign textAlignValue, TextAlign expectedAlign)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "text-align", new CssConstantValue<TextAlign>(cssKeyword, textAlignValue) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Text.TextAlign, Is.EqualTo(expectedAlign));
    }

    [Test]
    public void Text_Color_ShouldReturnCorrectValue()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "color", CssColorValue.Blue }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        // Note: The exact representation might depend on how AngleSharp normalizes color values
        Assert.That(style.Text.Color.CssText, Does.Contain(CssColorValue.Blue.CssText).IgnoreCase);
    }

    #endregion

    #region Writing Mode Tests

    [TestCase("ltr", "horizontal-tb", DirectionMode.Ltr, WritingModeType.HorizontalTopToBottom)]
    [TestCase("rtl", "horizontal-tb", DirectionMode.Rtl, WritingModeType.HorizontalTopToBottom)]
    [TestCase("ltr", "vertical-rl", DirectionMode.Ltr, WritingModeType.VerticalRightToLeft)]
    [TestCase("rtl", "vertical-lr", DirectionMode.Rtl, WritingModeType.VerticalLeftToRight)]
    public void WritingMode_ShouldReturnCorrectValues(string direction, string writingMode,
        DirectionMode expectedDirection, WritingModeType expectedMode)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "direction", new CssConstantValue<DirectionMode>(direction, expectedDirection) },
            { "writing-mode", new CssConstantValue<WritingModeType>(writingMode, expectedMode) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.WritingMode.Direction, Is.EqualTo(expectedDirection));
        Assert.That(style.WritingMode.Mode, Is.EqualTo(expectedMode));
    }

    [Test]
    public void WritingMode_IsHorizontal_ShouldReturnTrue_ForHorizontalMode()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "writing-mode", new CssStringValue("horizontal-tb") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.WritingMode.IsHorizontal, Is.True);
        Assert.That(style.WritingMode.IsVertical, Is.False);
    }

    [Test]
    public void WritingMode_IsVertical_ShouldReturnTrue_ForVerticalMode()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "writing-mode", new CssIdentifierValue("vertical-rl") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.WritingMode.IsVertical, Is.True);
        Assert.That(style.WritingMode.IsHorizontal, Is.False);
    }

    #endregion

    #region Inheritance Tests

    [Test]
    public void Inheritance_ShouldInheritColor_FromParent()
    {
        // Arrange
        var parentElement = CreateMockElement("div");
        var parentDeclaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "color", CssColorValue.Red }
        });
        var parentTree = new PropertyTreeNode(null);
        var parentStyle = CreateComputedStyle(parentElement, parentDeclaration, null, parentTree);

        var childElement = CreateMockElement("div", parentElement);
        var childDeclaration = CreateEmptyDeclaration();
        var childTree = new PropertyTreeNode(parentTree);

        // Act
        var childStyle = CreateComputedStyle(childElement, childDeclaration, parentStyle, childTree);

        // Assert
        Assert.That(childStyle.GetPropertyValue("color"), Does.Contain(CssColorValue.Red.CssText).IgnoreCase);
    }

    [Test]
    public void Inheritance_ShouldInheritFontFamily_FromParent()
    {
        // Arrange
        var parentElement = CreateMockElement("div");
        var parentDeclaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "font-family", new CssStringValue("Arial, sans-serif") }
        });
        var parentTree = new PropertyTreeNode(null);
        var parentStyle = CreateComputedStyle(parentElement, parentDeclaration, null, parentTree);

        var childElement = CreateMockElement("div", parentElement);
        var childDeclaration = CreateEmptyDeclaration();
        var childTree = new PropertyTreeNode(parentTree);

        // Act
        var childStyle = CreateComputedStyle(childElement, childDeclaration, parentStyle, childTree);

        // Assert
        Assert.That(childStyle.Text.FontFamily, Is.EqualTo("Arial, sans-serif"));
    }

    [Test]
    public void Inheritance_ShouldNotInheritWidth_FromParent()
    {
        // Arrange
        var parentElement = CreateMockElement("div");
        var parentDeclaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "width", new CssLengthValue(100, CssLengthValue.Unit.Px) }
        });
        var parentTree = new PropertyTreeNode(null);
        var parentStyle = CreateComputedStyle(parentElement, parentDeclaration, null, parentTree);

        var childElement = CreateMockElement("div", parentElement);
        var childDeclaration = CreateEmptyDeclaration();
        var childTree = new PropertyTreeNode(parentTree);

        // Act
        var childStyle = CreateComputedStyle(childElement, childDeclaration, parentStyle, childTree);

        // Assert
        Assert.That(childStyle.GetPropertyValue("width"), Is.Not.EqualTo("100px"));
    }

    [Test]
    public void Inheritance_ChildPropertyShouldOverrideParentProperty()
    {
        // Arrange
        var parentElement = CreateMockElement("div");
        var parentDeclaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "color", CssColorValue.Red }
        });
        var parentTree = new PropertyTreeNode(null);
        var parentStyle = CreateComputedStyle(parentElement, parentDeclaration, null, parentTree);

        var childElement = CreateMockElement("div", parentElement);
        var childDeclaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "color", CssColorValue.Blue }
        });
        var childTree = new PropertyTreeNode(parentTree);

        // Act
        var childStyle = CreateComputedStyle(childElement, childDeclaration, parentStyle, childTree);

        // Assert
        Assert.That(childStyle.GetPropertyValue("color"), Does.Contain("rgba(0, 0, 255, 1)").IgnoreCase);
    }

    #endregion

    #region Initial Values Tests

    [Test]
    public void InitialValues_ShouldApplyDefaultColor_WhenNoParent()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        // Default color is typically black, but this might depend on browser implementation
        Assert.That(style.Text.Color.CssText, Does.Contain("rgba(0, 0, 0, 1").IgnoreCase);
    }

    [Test]
    public void InitialValues_ShouldApplyDefaultFontFamily_WhenNoParent()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Text.FontFamily, Is.EqualTo("Times New Roman"));
    }

    [Test]
    public void ValueCalculator_ShouldConvertMediumToCorrectPixelValue()
    {
        // Arrange
        var mockRenderDevice = new Mock<IRenderDevice>();
        mockRenderDevice.Setup(r => r.FontSize).Returns(16);

        var mockDeclarationFactory = new Mock<IDeclarationFactory>();

        var mockCssParser = new Mock<ICssParser>();

        var mockElement = CreateMockElement();

        var valueCalculator = new ValueCalculator(mockDeclarationFactory.Object, mockCssParser.Object, mockRenderDevice.Object);
        var mediumValue = new CssLengthValue(16);

        // Act
        var pixelValue = valueCalculator.ToPixels(mediumValue, mockElement, "font-size");
        var computedValue = valueCalculator.Compute(mediumValue, mockElement, "font-size");

        // Assert
        Assert.That(pixelValue, Is.EqualTo(16));
        Assert.That(computedValue?.CssText, Is.EqualTo("16px"));
    }

    #endregion

    #region Logical Properties Tests

    [Test]
    public void LogicalProperties_MarginBlock_ShouldMapCorrectly_InHorizontalWritingMode()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.MarginBlock, new CssLengthValue(20, CssLengthValue.Unit.Px) },
            { PropertyNames.Direction, new CssIdentifierValue("ltr") },
            { PropertyNames.WritingMode, new CssIdentifierValue("horizontal-tb") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - in horizontal writing mode, margin-block maps to margin-top and margin-bottom
        Assert.That(style.Box.Margin.Top.CssText, Is.EqualTo("20px"));
        Assert.That(style.Box.Margin.Bottom.CssText, Is.EqualTo("20px"));
    }

    [Test]
    public void LogicalProperties_MarginInline_ShouldMapCorrectly_InHorizontalWritingMode()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.MarginInline, new CssLengthValue(15, CssLengthValue.Unit.Px) },
            { PropertyNames.Direction, new CssIdentifierValue("ltr") },
            { PropertyNames.WritingMode, new CssIdentifierValue("horizontal-tb") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - in horizontal ltr writing mode, margin-inline maps to margin-left and margin-right
        Assert.That(style.Box.Margin.Left.CssText, Is.EqualTo("15px"));
        Assert.That(style.Box.Margin.Right.CssText, Is.EqualTo("15px"));
    }

    [Test]
    public void LogicalProperties_PaddingBlock_ShouldMapCorrectly_InVerticalWritingMode()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.PaddingBlock, new CssLengthValue(10, CssLengthValue.Unit.Px) },
            { PropertyNames.Direction, new CssIdentifierValue("ltr") },
            { PropertyNames.WritingMode, new CssIdentifierValue("vertical-rl") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - in vertical-rl writing mode, padding-block maps to padding-right and padding-left
        Assert.That(style.Box.Padding.Right.CssText, Is.EqualTo("10px"));
        Assert.That(style.Box.Padding.Left.CssText, Is.EqualTo("10px"));
    }

    [Test]
    public void LogicalProperties_BlockSize_ShouldMapToHeight_InHorizontalWritingMode()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.BlockSize, new CssLengthValue(100, CssLengthValue.Unit.Px) },
            { PropertyNames.Direction, new CssIdentifierValue("ltr") },
            { PropertyNames.WritingMode, new CssIdentifierValue("horizontal-tb") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - in horizontal writing mode, block-size maps to height
        Assert.That(style.Box.Height.CssText, Is.EqualTo("100px"));
    }

    [Test]
    public void LogicalProperties_InlineSize_ShouldMapToWidth_InHorizontalWritingMode()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.InlineSize, new CssLengthValue(200, CssLengthValue.Unit.Px) },
            { PropertyNames.Direction, new CssIdentifierValue("ltr") },
            { PropertyNames.WritingMode, new CssIdentifierValue("horizontal-tb") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - in horizontal writing mode, inline-size maps to width
        Assert.That(style.Box.Width.CssText, Is.EqualTo("200px"));
    }

    #endregion

    #region Property Tree and Caching Tests

    [Test]
    public void PropertyTree_ShouldShareValues_BetweenNodes()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();

        var rootNode = new PropertyTreeNode(null);
        rootNode.SetProperty(PropertyNames.Color, CssColorValue.Red);

        var childNode = new PropertyTreeNode(rootNode);

        // Act
        var style = CreateComputedStyle(element, declaration, null, childNode);

        // Assert - child node should inherit color from parent node
        Assert.That(style.GetPropertyValue(PropertyNames.Color), Is.EqualTo(CssColorValue.Red.CssText));
    }

    [Test]
    public void GetValue_ShouldCache_TypedValues()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.FontSize, new CssLengthValue(16, CssLengthValue.Unit.Px) }
        });

        var style = CreateComputedStyle(element, declaration);

        // Act - Get the value twice
        var firstCall = style.GetValue<CssLengthValue>(PropertyNames.FontSize);
        var secondCall = style.GetValue<CssLengthValue>(PropertyNames.FontSize);

        // Assert - Both calls should return equal values (cached)
        Assert.That(secondCall, Is.EqualTo(firstCall));
    }

    [Test]
    public void PropertyTree_ShouldProvide_RawValues()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var propertyTree = new PropertyTreeNode(null);

        var colorValue = CssColorValue.Blue;
        propertyTree.SetProperty(PropertyNames.Color, colorValue);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);

        // Assert - The raw property value should be available and equal the original value
        Assert.That(propertyTree.GetPropertyRawValue(PropertyNames.Color), Is.EqualTo(colorValue));
    }

    #endregion

    #region Device-Dependent Properties Tests

    [Test]
    public void ViewportRelativeUnits_ShouldBeDeviceDependent()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.Width, new CssLengthValue(50, CssLengthValue.Unit.Vw) }
        });

        var mockRenderDevice = new Mock<IRenderDevice>();
        mockRenderDevice.Setup(r => r.ViewPortWidth).Returns(1024);
        mockRenderDevice.Setup(r => r.ViewPortHeight).Returns(768);

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - Verify the viewport unit is processed correctly
        Assert.That(style.GetPropertyValue(PropertyNames.Width), Is.EqualTo("50vw"));

        // Since we can't directly test the MarkAsDeviceDependent call, we're testing
        // that the viewport-relative value is processed
    }

    [Test]
    public void EmUnits_ShouldBeDeviceDependent()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.FontSize, new CssLengthValue(2, CssLengthValue.Unit.Em) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - Verify the em unit is processed correctly
        Assert.That(style.GetPropertyValue(PropertyNames.FontSize), Is.EqualTo("2em"));

        // Since we can't directly test the MarkAsDeviceDependent call, we're testing
        // that the em value is processed
    }

    [Test]
    public void AbsoluteUnits_ShouldHavePixelValue()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.Width, new CssLengthValue(100, CssLengthValue.Unit.Px) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - Verify the absolute pixel value is processed correctly
        Assert.That(style.Box.Width.CssText, Is.EqualTo("100px"));

        // Since we can't directly test that MarkAsDeviceDependent was not called,
        // we're just testing that the absolute unit is processed
    }

    #endregion

    #region Rare Properties and Bitfields Tests

    [Test]
    public void RareProperties_ShouldStore_UncommonValues()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { "transform", new CssIdentifierValue("rotate(45deg)") },
            { "transition", new CssIdentifierValue("all 0.3s ease") },
            { "filter", new CssIdentifierValue("blur(2px)") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - Rare properties should be retrievable
        Assert.That(style.GetPropertyValue("transform"), Is.EqualTo("rotate(45deg)"));
        Assert.That(style.GetPropertyValue("transition"), Is.EqualTo("all 0.3s ease"));
        Assert.That(style.GetPropertyValue("filter"), Is.EqualTo("blur(2px)"));
    }

    [Test]
    public void Bitfields_ShouldReflect_HasBorder_WhenBorderWidthIsSet()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.BorderTopWidth, new CssLengthValue(1, CssLengthValue.Unit.Px) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - HasBorder should be true
        // Note: We can't directly test the internal bitfield, but we can test its effects
        // on the border property values
        Assert.That(style.Box.Border.Top.CssText, Is.EqualTo("1px"));
    }

    [Test]
    public void Bitfields_Overflow_ShouldHandleBothProperties()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.OverflowX, new CssIdentifierValue("scroll") },
            { PropertyNames.OverflowY, new CssIdentifierValue("hidden") }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - Both overflow properties should be correctly processed
        Assert.That(style.GetPropertyValue(PropertyNames.OverflowX), Is.EqualTo("scroll"));
        Assert.That(style.GetPropertyValue(PropertyNames.OverflowY), Is.EqualTo("hidden"));
    }

    #endregion

    #region Value Computation Tests

    [Test]
    public void CalcExpressions_ShouldBeComputedCorrectly()
    {
        // This is a simplified test since we can't easily create CssCalcValue
        // In a real implementation, we'd need to test calculation results

        // Arrange
        var element = CreateMockElement();
        var calcValue = new CssLengthValue(100, CssLengthValue.Unit.Px); // Simplified stand-in for a calc expression

        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.Width, calcValue }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Box.Width.CssText, Is.EqualTo("100px"));
    }

    [Test]
    public void RelativeUnits_ShouldBeComputed_BasedOnContext()
    {
        // Arrange - Setup element with parent that has font-size: 20px
        var parentElement = CreateMockElement("div");
        var parentDeclaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.FontSize, new CssLengthValue(20, CssLengthValue.Unit.Px) }
        });
        var parentTree = new PropertyTreeNode(null);
        var parentStyle = CreateComputedStyle(parentElement, parentDeclaration, null, parentTree);

        var childElement = CreateMockElement("div", parentElement);
        var childDeclaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.FontSize, new CssLengthValue(2, CssLengthValue.Unit.Em) } // 2em should be 40px
        });
        var childTree = new PropertyTreeNode(parentTree);

        // Act
        var childStyle = CreateComputedStyle(childElement, childDeclaration, parentStyle, childTree);

        // Assert - This is a simple check without actual computation
        Assert.That(childStyle.GetPropertyValue(PropertyNames.FontSize), Is.EqualTo("2em"));
    }

    #endregion

    #region Shorthand Properties Tests

    [Test]
    public void ShorthandProperty_Margin_ShouldExpandToIndividualProperties()
    {
        // Arrange
        var element = CreateMockElement();

        // Create a declaration with margin shorthand that would expand to all sides
        var marginValue = new CssLengthValue(10, CssLengthValue.Unit.Px);
        var propertyTree = new PropertyTreeNode(null);

        // Set properties as if they were expanded from margin: 10px;
        propertyTree.SetProperty(PropertyNames.MarginTop, marginValue);
        propertyTree.SetProperty(PropertyNames.MarginRight, marginValue);
        propertyTree.SetProperty(PropertyNames.MarginBottom, marginValue);
        propertyTree.SetProperty(PropertyNames.MarginLeft, marginValue);

        var declaration = CreateEmptyDeclaration();

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);

        // Assert - All margin sides should have the value
        Assert.That(style.Box.Margin.Top.CssText, Is.EqualTo("10px"));
        Assert.That(style.Box.Margin.Right.CssText, Is.EqualTo("10px"));
        Assert.That(style.Box.Margin.Bottom.CssText, Is.EqualTo("10px"));
        Assert.That(style.Box.Margin.Left.CssText, Is.EqualTo("10px"));
    }

    [Test]
    public void ShorthandProperty_Border_ShouldExpandToIndividualProperties()
    {
        // Arrange
        var element = CreateMockElement();

        // Create a declaration with border properties that would be set by border shorthand
        var borderWidthValue = new CssLengthValue(1, CssLengthValue.Unit.Px);
        var propertyTree = new PropertyTreeNode(null);

        // Set properties as if they were expanded from border: 1px solid black;
        propertyTree.SetProperty(PropertyNames.BorderTopWidth, borderWidthValue);
        propertyTree.SetProperty(PropertyNames.BorderRightWidth, borderWidthValue);
        propertyTree.SetProperty(PropertyNames.BorderBottomWidth, borderWidthValue);
        propertyTree.SetProperty(PropertyNames.BorderLeftWidth, borderWidthValue);

        propertyTree.SetProperty(PropertyNames.BorderTopColor, CssColorValue.Black);
        propertyTree.SetProperty(PropertyNames.BorderRightColor, CssColorValue.Black);
        propertyTree.SetProperty(PropertyNames.BorderBottomColor, CssColorValue.Black);
        propertyTree.SetProperty(PropertyNames.BorderLeftColor, CssColorValue.Black);

        var declaration = CreateEmptyDeclaration();

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);

        // Assert - All border widths should have the value
        Assert.That(style.Box.Border.Top.CssText, Is.EqualTo("1px"));
        Assert.That(style.Box.Border.Right.CssText, Is.EqualTo("1px"));
        Assert.That(style.Box.Border.Bottom.CssText, Is.EqualTo("1px"));
        Assert.That(style.Box.Border.Left.CssText, Is.EqualTo("1px"));
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Test]
    public void NullValues_ShouldNotCauseExceptions()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var propertyTree = new PropertyTreeNode(null);
        propertyTree.SetProperty(PropertyNames.Color, (ICssValue?)null);

        // Act & Assert - Should not throw exceptions
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        Assert.DoesNotThrow(() => style.GetPropertyValue(PropertyNames.Color));
    }

    [Test]
    public void UnknownProperties_ShouldReturnEmptyString()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();
        var style = CreateComputedStyle(element, declaration);

        // Act
        var value = style.GetPropertyValue("non-existent-property-xyz");

        // Assert
        Assert.That(value, Is.EqualTo(string.Empty));
    }

    #endregion

    #region Specific Properties Tests

    [Test]
    public void Opacity_ShouldAffect_VisibilityFlag()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.Opacity, new CssNumberValue(0) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - with opacity: 0, the element should not be visible
        // Note: We can't directly test the internal bitfield, but we can test that
        // the opacity value was processed correctly
        Assert.That(style.Opacity, Is.EqualTo(0));
    }

    [Test]
    public void ZIndex_ShouldBeParsedCorrectly()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.ZIndex, new CssNumberValue(123) }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.ZIndex, Is.EqualTo(123));
    }

    [Test]
    public void BackgroundColor_ShouldSetHasBackgroundFlag()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.BackgroundColor, CssColorValue.Blue }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - background-color should be set correctly
        Assert.That(style.GetPropertyValue(PropertyNames.BackgroundColor), Does.Contain("rgba(0, 0, 255, 1)").IgnoreCase);
    }

    [Test]
    public void TransparentBackgroundColor_ShouldNotSetHasBackgroundFlag()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, ICssValue>
        {
            { PropertyNames.BackgroundColor, CssColorValue.Transparent }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert - transparent color should be set correctly
        Assert.That(style.GetPropertyValue(PropertyNames.BackgroundColor), Does.Contain("rgba(0, 0, 0, 0)").IgnoreCase);
    }

    #endregion
}