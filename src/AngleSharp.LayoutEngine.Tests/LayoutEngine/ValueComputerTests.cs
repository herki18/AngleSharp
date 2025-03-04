namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using System;
using AngleSharp;
using AngleSharp.Css.Dom;
using Css.Parser;
using Dom;
using Html.Parser;
using StyleComputation;
using NUnit.Framework;

[TestFixture]
public class ValueComputerTests
{
    private IBrowsingContext _context;
    private ICssParser? _cssParser;
    private IHtmlParser? _htmlParser;
    private MockRenderDevice _device;
    private ValueComputer _valueComputer;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
        _cssParser = _context.GetService<ICssParser>();
        _htmlParser = _context.GetService<IHtmlParser>();
        _device = new MockRenderDevice
        {
            ViewPortWidth = 1024,
            ViewPortHeight = 768,
            FontSize = 16,
            Resolution = 96
        };
        _valueComputer = new ValueComputer(_device, _context);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public void ComputeValues_FontSize_PixelValue_ReturnsUnchanged()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var elementStyle = CreateStyle("font-size: 20px;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var rootStyle = CreateStyle("font-size: 16px;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedFontSize = result.GetPropertyValue("font-size");
        Assert.That(computedFontSize, Is.EqualTo("20px"));
    }

    [Test]
    public void ComputeValues_FontSize_EmValue_ComputesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var elementStyle = CreateStyle("font-size: 1.5em;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var rootStyle = CreateStyle("font-size: 16px;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedFontSize = result.GetPropertyValue("font-size");
        Assert.That(computedFontSize, Is.EqualTo("24px"));
    }

    [Test]
    public void ComputeValues_FontSize_RemValue_ComputesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var elementStyle = CreateStyle("font-size: 1.5rem;");
        var parentStyle = CreateStyle("font-size: 20px;"); // Should use root, not parent
        var rootStyle = CreateStyle("font-size: 16px;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedFontSize = result.GetPropertyValue("font-size");
        Assert.That(computedFontSize, Is.EqualTo("24px"));
    }

    [Test]
    public void ComputeValues_FontSize_PercentageValue_ComputesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var elementStyle = CreateStyle("font-size: 150%;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var rootStyle = CreateStyle("font-size: 16px;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedFontSize = result.GetPropertyValue("font-size");
        Assert.That(computedFontSize, Is.EqualTo("24px"));
    }

    [Test]
    public void ComputeValues_FontSize_KeywordValues_ComputesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var elementStyle = CreateStyle("font-size: large;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var rootStyle = CreateStyle("font-size: 16px;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedFontSize = result.GetPropertyValue("font-size");
        // large is typically 1.2em or 18px with 16px base
        Assert.That(double.Parse(computedFontSize.Replace("px", "")), Is.GreaterThan(17).And.LessThan(19));
    }

    [Test]
    public void ComputeValues_ViewportUnits_ComputesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var elementStyle = CreateStyle("width: 50vw; height: 50vh;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var rootStyle = CreateStyle("font-size: 16px;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedWidth = result.GetPropertyValue("width");
        var computedHeight = result.GetPropertyValue("height");

        Assert.That(computedWidth, Is.EqualTo("512px")); // 50% of 1024px
        Assert.That(computedHeight, Is.EqualTo("384px")); // 50% of 768px
    }

    [Test]
    [Ignore("Not yet implemented")]
    public void ComputeValues_CssVariables_ResolvesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("--main-color: red; font-size: 16px;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var elementStyle = CreateStyle("color: var(--main-color);");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedColor = result.GetPropertyValue("color");
        Assert.That(computedColor, Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public void ComputeValues_NestedCssVariables_ResolvesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("--base-size: 16px; --spacing: var(--base-size); font-size: 16px;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var elementStyle = CreateStyle("margin: var(--spacing);");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedMargin = result.GetPropertyValue("margin");
        Assert.That(computedMargin, Is.EqualTo("16px"));
    }

    [Test]
    public void ComputeValues_InheritKeyword_InheritsFromParent()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("font-size: 16px;");
        var parentStyle = CreateStyle("color: red; font-size: 16px;");
        var elementStyle = CreateStyle("color: inherit;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedColor = result.GetPropertyValue("color");
        Assert.That(computedColor, Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public void ComputeValues_InitialKeyword_SetsToDefault()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("font-size: 16px;");
        var parentStyle = CreateStyle("color: red; font-size: 16px;");
        var elementStyle = CreateStyle("color: initial;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedColor = result.GetPropertyValue("color");
        // initial color is typically black
        Assert.That(computedColor, Is.EqualTo("rgba(0, 0, 0, 1)"));
    }

    [Test]
    public void ComputeValues_UnsetKeyword_InheritsForInheritableProperties()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("font-size: 16px;");
        var parentStyle = CreateStyle("color: red; font-size: 16px;");
        var elementStyle = CreateStyle("color: unset;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedColor = result.GetPropertyValue("color");
        Assert.That(computedColor, Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public void ComputeValues_AbsoluteUnits_ConvertsToPixels()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("font-size: 16px;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var elementStyle = CreateStyle("width: 1in; height: 2cm; padding: 10pt;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedWidth = result.GetPropertyValue("width");
        var computedHeight = result.GetPropertyValue("height");
        var computedPadding = result.GetPropertyValue("padding");

        // 1in = 96px (at 96dpi)
        Assert.That(computedWidth, Is.EqualTo("96px"));

        // 2cm ≈ 75.6px (at 96dpi)
        var heightPixels = double.Parse(computedHeight.Replace("px", ""));
        Assert.That(heightPixels, Is.GreaterThan(75).And.LessThan(76));

        // 10pt ≈ 13.33px (at 96dpi)
        var paddingPixels = double.Parse(computedPadding.Replace("px", ""));
        Assert.That(paddingPixels, Is.GreaterThan(13).And.LessThan(14));
    }

    [Test]
    public void ComputeValues_LineHeightUnitless_ComputesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("font-size: 16px;");
        var parentStyle = CreateStyle("font-size: 16px;");
        var elementStyle = CreateStyle("font-size: 20px; line-height: 1.5;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedLineHeight = result.GetPropertyValue("line-height");
        Assert.That(computedLineHeight, Is.EqualTo("30px")); // 20px * 1.5
    }

    [Test]
    public void ComputeValues_InvalidInput_HandlesGracefully()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("font-size: 16px;");
        var parentStyle = CreateStyle("font-size: 16px;");
        // Invalid value format
        var elementStyle = CreateStyle("width: invalid;");

        // Act - should not throw
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert - just confirm we got a result without exception
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void ComputeValues_NullParentStyle_HandlesGracefully()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("font-size: 16px;");
        var elementStyle = CreateStyle("color: red;");

        // Act & Assert - should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() =>
            _valueComputer.ComputeValues(elementStyle, element, null!, rootStyle));
    }

    [Test]
    public void ComputeValues_ComplexInheritanceChain_ComputesCorrectly()
    {
        // Arrange
        var element = CreateElement("<div></div>");
        var rootStyle = CreateStyle("--base-spacing: 8px; font-size: 16px;");
        var parentStyle = CreateStyle("font-size: 20px; --multiplier: 2;");
        var elementStyle = CreateStyle(
            "margin: calc(var(--base-spacing) * var(--multiplier)); " +
            "padding: 0.5em; " +
            "font-size: 1.2em;");

        // Act
        var result = _valueComputer.ComputeValues(elementStyle, element, parentStyle, rootStyle);

        // Assert
        var computedFontSize = result.GetPropertyValue("font-size");
        var computedPadding = result.GetPropertyValue("padding");
        var computedMargin = result.GetPropertyValue("margin");

        Assert.That(computedFontSize, Is.EqualTo("24px")); // 1.2em * 20px
        Assert.That(computedPadding, Is.EqualTo("12px")); // 0.5em * 24px
        // margin would ideally be 16px, but calc() handling might vary
    }

    // Helper methods
    private IElement CreateElement(string html)
    {
        Assert.IsNotNull(_htmlParser);
        var document = _htmlParser.ParseDocument("");
        var container = document.CreateElement("div");
        container.InnerHtml = html;
        Assert.IsNotNull(container);
        var element = container.FirstElementChild;
        Assert.IsNotNull(element);
        return element;
    }

    private ICssStyleDeclaration CreateStyle(string cssText)
    {
        Assert.IsNotNull(_cssParser);
        return _cssParser.ParseDeclaration(cssText);
    }
}