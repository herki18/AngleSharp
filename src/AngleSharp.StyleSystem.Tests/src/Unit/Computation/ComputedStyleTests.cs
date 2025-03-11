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

    private ICssStyleDeclaration CreateDeclaration(Dictionary<string, string> properties)
    {
        // Create a mock style declaration instead of using the actual CssStyleDeclaration
        var mockDeclaration = new Mock<ICssStyleDeclaration>();

        // Setup basic properties
        mockDeclaration.Setup(d => d.Length).Returns(properties.Count);

        // Setup GetPropertyValue method to return values from the dictionary
        mockDeclaration.Setup(d => d.GetPropertyValue(It.IsAny<string>()))
            .Returns<string>(key => properties.ContainsKey(key) ? properties[key] : string.Empty);

        // Setup the indexer to return properties
        var mockProperties = new List<ICssProperty>();
        foreach (var prop in properties)
        {
            var mockProperty = new Mock<ICssProperty>();
            mockProperty.Setup(p => p.Name).Returns(prop.Key);
            mockProperty.Setup(p => p.Value).Returns(prop.Value);

            // Create a CSS value for the RawValue
            var cssValue = new Mock<ICssValue>();
            cssValue.Setup(v => v.CssText).Returns(prop.Value);
            mockProperty.Setup(p => p.RawValue).Returns(cssValue.Object);

            mockProperties.Add(mockProperty.Object);
        }

        // Setup enumerator to iterate through properties
        mockDeclaration.Setup(d => d.GetEnumerator())
            .Returns(() => mockProperties.GetEnumerator());

        return mockDeclaration.Object;
    }

    private ICssStyleDeclaration CreateEmptyDeclaration()
    {
        return CreateDeclaration(new Dictionary<string, string>());
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "color", "red" },
            { "display", "block" },
            { "font-size", "16px" }
        });
        var propertyTree = new PropertyTreeNode(null);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);

        // Assert
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("red"));
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "color", "red" }
        });
        var propertyTree = new PropertyTreeNode(null);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        var color = style.GetPropertyValue("color");

        // Assert
        Assert.That(color, Is.EqualTo("red"));
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

    [TestCase("none", DisplayMode.None)]
    [TestCase("block", DisplayMode.Block)]
    [TestCase("inline", DisplayMode.Inline)]
    [TestCase("inline-block", DisplayMode.InlineBlock)]
    [TestCase("flex", DisplayMode.Flex)]
    [TestCase("grid", DisplayMode.Grid)]
    [TestCase("table", DisplayMode.Table)]
    [TestCase("inline-flex", DisplayMode.InlineFlex)]
    public void Display_ShouldReturnCorrectDisplayMode(string displayValue, DisplayMode expectedMode)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "display", displayValue }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Display, Is.EqualTo(expectedMode));
    }

    #endregion

    #region Position Property Tests

    [TestCase("static", PositionMode.Static)]
    [TestCase("relative", PositionMode.Relative)]
    [TestCase("absolute", PositionMode.Absolute)]
    [TestCase("fixed", PositionMode.Fixed)]
    [TestCase("sticky", PositionMode.Sticky)]
    public void Position_ShouldReturnCorrectPositionMode(string positionValue, PositionMode expectedMode)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "position", positionValue }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "width", "100px" }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "height", "200px" }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "margin-top", "10px" },
            { "margin-right", "20px" },
            { "margin-bottom", "30px" },
            { "margin-left", "40px" }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "border-top-width", "1px" },
            { "border-right-width", "2px" },
            { "border-bottom-width", "3px" },
            { "border-left-width", "4px" }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "padding-top", "5px" },
            { "padding-right", "10px" },
            { "padding-bottom", "15px" },
            { "padding-left", "20px" }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "font-family", "Arial, sans-serif" }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "font-size", "18px" }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.FontSize.CssText, Is.EqualTo("18px"));
    }

    [TestCase("400", 400)]
    [TestCase("700", 700)]
    [TestCase("bold", 700)]
    [TestCase("normal", 400)]
    public void Text_FontWeight_ShouldReturnCorrectValue(string fontWeightValue, int expectedWeight)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "font-weight", fontWeightValue }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Text.FontWeight, Is.EqualTo(expectedWeight));
    }

    [TestCase("italic", true)]
    [TestCase("normal", false)]
    public void Text_IsItalic_ShouldReturnCorrectValue(string fontStyleValue, bool expectedIsItalic)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "font-style", fontStyleValue }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Text.IsItalic, Is.EqualTo(expectedIsItalic));
    }

    [TestCase("left", TextAlign.Left)]
    [TestCase("right", TextAlign.Right)]
    [TestCase("center", TextAlign.Center)]
    [TestCase("justify", TextAlign.Justify)]
    [TestCase("start", TextAlign.Start)]
    [TestCase("end", TextAlign.End)]
    public void Text_TextAlign_ShouldReturnCorrectValue(string textAlignValue, TextAlign expectedAlign)
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "text-align", textAlignValue }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "color", "blue" }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        // Note: The exact representation might depend on how AngleSharp normalizes color values
        Assert.That(style.Text.Color.CssText, Does.Contain("blue").IgnoreCase);
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "direction", direction },
            { "writing-mode", writingMode }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "writing-mode", "horizontal-tb" }
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
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "writing-mode", "vertical-rl" }
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
        var parentDeclaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "color", "red" }
        });
        var parentTree = new PropertyTreeNode(null);
        var parentStyle = CreateComputedStyle(parentElement, parentDeclaration, null, parentTree);

        var childElement = CreateMockElement("div", parentElement);
        var childDeclaration = CreateEmptyDeclaration();
        var childTree = new PropertyTreeNode(parentTree);

        // Act
        var childStyle = CreateComputedStyle(childElement, childDeclaration, parentStyle, childTree);

        // Assert
        Assert.That(childStyle.GetPropertyValue("color"), Does.Contain("red").IgnoreCase);
    }

    [Test]
    public void Inheritance_ShouldInheritFontFamily_FromParent()
    {
        // Arrange
        var parentElement = CreateMockElement("div");
        var parentDeclaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "font-family", "Arial, sans-serif" }
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
        var parentDeclaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "width", "100px" }
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
        var parentDeclaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "color", "red" }
        });
        var parentTree = new PropertyTreeNode(null);
        var parentStyle = CreateComputedStyle(parentElement, parentDeclaration, null, parentTree);

        var childElement = CreateMockElement("div", parentElement);
        var childDeclaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "color", "blue" }
        });
        var childTree = new PropertyTreeNode(parentTree);

        // Act
        var childStyle = CreateComputedStyle(childElement, childDeclaration, parentStyle, childTree);

        // Assert
        Assert.That(childStyle.GetPropertyValue("color"), Does.Contain("blue").IgnoreCase);
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
        Assert.That(style.Text.Color.CssText, Does.Contain("rgb(0, 0, 0").IgnoreCase);
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
        Assert.That(style.Text.FontFamily, Is.EqualTo("sans-serif"));
    }

    [Test]
    public void InitialValues_ShouldApplyDefaultFontSize_WhenNoParent()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateEmptyDeclaration();

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        // Default font size is typically 16px (medium)
        Assert.That(style.FontSize.CssText, Is.EqualTo("medium"));
    }

    #endregion

    #region Integration Tests

    [Test]
    public void Integration_ComplexStyle_ShouldProcessAllProperties()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "display", "flex" },
            { "position", "relative" },
            { "color", "red" },
            { "background-color", "blue" },
            { "font-size", "18px" },
            { "font-weight", "bold" },
            { "margin", "10px" },
            { "padding", "5px" },
            { "border-width", "1px" },
            { "width", "200px" },
            { "height", "100px" },
            { "z-index", "5" },
            { "opacity", "0.8" }
        });

        // Act
        var style = CreateComputedStyle(element, declaration);

        // Assert
        Assert.That(style.Display, Is.EqualTo(DisplayMode.Flex));
        Assert.That(style.Position, Is.EqualTo(PositionMode.Relative));
        Assert.That(style.Text.Color.CssText, Does.Contain("red").IgnoreCase);
        Assert.That(style.FontSize.CssText, Is.EqualTo("18px"));
        Assert.That(style.Text.FontWeight, Is.EqualTo(700)); // bold == 700
        Assert.That(style.Box.Width.CssText, Is.EqualTo("200px"));
        Assert.That(style.Box.Height.CssText, Is.EqualTo("100px"));
        Assert.That(style.Box.Margin.Top.CssText, Is.EqualTo("10px"));
        Assert.That(style.Box.Padding.Top.CssText, Is.EqualTo("5px"));
        Assert.That(style.ZIndex, Is.EqualTo(5));
        Assert.That(style.Opacity, Is.EqualTo(0.8f));
    }

    [Test]
    public void Integration_PropertyTree_ShouldStoreAndRetrieveValues()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "color", "red" },
            { "font-size", "20px" }
        });
        var propertyTree = new PropertyTreeNode(null);

        // Act
        var style = CreateComputedStyle(element, declaration, null, propertyTree);
        var color = style.GetPropertyValue("color");
        var fontSize = style.GetPropertyValue("font-size");

        // Assert
        Assert.That(color, Does.Contain("red").IgnoreCase);
        Assert.That(fontSize, Is.EqualTo("20px"));
    }

    [Test]
    public void Integration_DeviceDependentValues_ShouldMarkElementAsDeviceDependent()
    {
        // Arrange
        var element = CreateMockElement();
        var declaration = CreateDeclaration(new Dictionary<string, string>
        {
            { "width", "50%" },
            { "font-size", "2em" },
            { "height", "50vh" }
        });

        var mockInvalidationTracker = new Mock<StyleInvalidationTracker>();
        var mockRenderDevice = new Mock<IRenderDevice>();
        mockRenderDevice.Setup(r => r.ViewPortWidth).Returns(1024);
        mockRenderDevice.Setup(r => r.ViewPortHeight).Returns(768);
        mockRenderDevice.Setup(r => r.FontSize).Returns(16);

        var mockContext = new Mock<IBrowsingContext>();
        var propertyTree = new PropertyTreeNode(null);

        // Act
        var style = new ComputedStyle(
            element,
            null,
            declaration,
            propertyTree,
            mockRenderDevice.Object,
            mockInvalidationTracker.Object,
            mockContext.Object);

        // Assert
        mockInvalidationTracker.Verify(t => t.MarkAsDeviceDependent(element), Times.AtLeastOnce);
    }

    #endregion
}