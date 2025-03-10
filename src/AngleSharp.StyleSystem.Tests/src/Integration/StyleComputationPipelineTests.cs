using AngleSharp.Css;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.Integration;

namespace AngleSharp.StyleSystem.Tests.Integration;

[TestFixture]
public class StyleComputationPipelineTests
{
    private IBrowsingContext _context;
    private StyleEngine _styleEngine;
    private IHtmlParser _htmlParser;

    [SetUp]
    public void Setup()
    {
        // Create a browsing context with CSS and style system enabled
        var config = Configuration.Default
            .WithCss()
            .WithDefaultLoader();

        _context = BrowsingContext.New(config);

        // Initialize the style engine
        _styleEngine = new StyleEngine(_context);
        _htmlParser = _context.GetService<IHtmlParser>()!;
    }

    [TearDown]
    public void Cleanup()
    {
        _styleEngine?.Dispose();
        _context?.Dispose();
    }

    [Test]
    public async Task BasicStyleComputation_ShouldProduceCorrectComputedStyle()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            "<div class='test'>Text</div>",
            ".test { color: red; font-size: 16px; }");

        var element = document.QuerySelector(".test");
        Assert.IsNotNull(element, "Test element should exist");

        // Act
        var computedStyle = _styleEngine.ComputeElementStyle(element);

        // Assert
        Assert.IsNotNull(computedStyle, "Computed style should not be null");
        Assert.That(computedStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(computedStyle.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    [Test]
    public async Task StyleCascade_SpecificityRules_ShouldBeAppliedCorrectly()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            "<div id='test' class='test-class' style='color: green;'>Text</div>",
            @"
                div { color: black; font-size: 12px; }
                .test-class { color: blue; font-size: 14px; }
                #test { color: red; }
                ");

        var element = document.QuerySelector("#test");
        Assert.IsNotNull(element, "Test element should exist");

        // Act
        var computedStyle = _styleEngine.ComputeElementStyle(element);

        // Assert - inline style should override id selector
        Assert.That(computedStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 128, 0, 1)"));
        // Id selector should override class selector for font-size
        Assert.That(computedStyle.GetPropertyValue("font-size"), Is.EqualTo("14px"));
    }

    [Test]
    public async Task Important_Rules_ShouldOverrideCascade()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            "<div id='test' class='test-class' style='color: green;'>Text</div>",
            @"
                div { color: black; font-size: 12px; }
                .test-class { color: blue !important; font-size: 14px; }
                #test { color: red; }
                ");

        var element = document.QuerySelector("#test");
        Assert.IsNotNull(element, "Test element should exist");

        // Act
        var computedStyle = _styleEngine.ComputeElementStyle(element);

        // Assert - !important should override inline style
        Assert.That(computedStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(computedStyle.GetPropertyValue("font-size"), Is.EqualTo("14px"));
    }

    [Test]
    public async Task PropertyInheritance_ShouldWorkCorrectly()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='parent'>
                     <div id='child'>Text</div>
                   </div>",
            @"
                #parent { color: blue; font-family: Arial; }
                #child { font-size: 16px; }
                ");

        var parentElement = document.GetElementById("parent");
        var childElement = document.GetElementById("child");

        Assert.IsNotNull(parentElement, "Parent element should exist");
        Assert.IsNotNull(childElement, "Child element should exist");

        // Act
        var parentStyle = _styleEngine.ComputeElementStyle(parentElement);
        var childStyle = _styleEngine.ComputeElementStyle(childElement);

        // Assert
        Assert.That(parentStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(parentStyle.GetPropertyValue("font-family"), Is.EqualTo("Arial"));

        // Child should inherit color and font-family
        Assert.That(childStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(childStyle.GetPropertyValue("font-family"), Is.EqualTo("Arial"));
        Assert.That(childStyle.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    [Test]
    public async Task ExplicitInheritKeyword_ShouldForceInheritance()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='parent'>
                     <div id='child'>Text</div>
                   </div>",
            @"
                #parent { color: blue; font-size: 20px; background-color: #eee; }
                #child { color: red; font-size: inherit; background-color: inherit; }
                ");

        var parentElement = document.GetElementById("parent");
        var childElement = document.GetElementById("child");

        // Act
        var parentStyle = _styleEngine.ComputeElementStyle(parentElement!);
        var childStyle = _styleEngine.ComputeElementStyle(childElement!);

        // Assert
        Assert.That(parentStyle.GetPropertyValue("font-size"), Is.EqualTo("20px"));
        Assert.That(parentStyle.GetPropertyValue("background-color"), Is.EqualTo("rgba(238, 238, 238, 1)"));

        // Child has explicit color but inherits font-size and background-color
        Assert.That(childStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(childStyle.GetPropertyValue("font-size"), Is.EqualTo("20px"));
        Assert.That(childStyle.GetPropertyValue("background-color"), Is.EqualTo("rgba(238, 238, 238, 1)"));
    }

    [Test]
    public async Task InitialKeyword_ShouldResetToInitialValues()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='parent'>
                     <div id='child'>Text</div>
                   </div>",
            @"
                #parent { color: blue; }
                #child { color: initial; font-weight: initial; }
                ");

        var childElement = document.GetElementById("child");

        // Act
        var childStyle = _styleEngine.ComputeElementStyle(childElement!);

        // Assert - color should be reset to black (initial)
        Assert.That(childStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 0, 1)"));
        Assert.That(childStyle.GetPropertyValue("font-weight"), Is.EqualTo("400"));
    }

    [Test]
    public async Task UnsetKeyword_ShouldInheritOrUseInitialValue()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='parent'>
                     <div id='child'>Text</div>
                   </div>",
            @"
                #parent { color: blue; display: flex; }
                #child { color: unset; display: unset; }
                ");

        var childElement = document.GetElementById("child");

        // Act
        var childStyle = _styleEngine.ComputeElementStyle(childElement!);

        // Assert
        // color inherits because it's an inherited property
        Assert.That(childStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        // display uses initial value (block) because it's not inherited
        Assert.That(childStyle.GetPropertyValue("display").ToLowerInvariant(), Is.EqualTo("block"));
    }

    [Test]
    public async Task LengthUnits_ShouldBeComputedCorrectly()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='test'>Text</div>",
            @"
                html { font-size: 16px; }
                #test {
                    width: 200px;
                    height: 10em;
                    padding: 1rem;
                    margin: 2rem;
                }
                ");

        var element = document.GetElementById("test");

        // Act
        var computedStyle = _styleEngine.ComputeElementStyle(element!);

        // Assert
        Assert.That(computedStyle.GetPropertyValue("width"), Is.EqualTo("200px"));
        Assert.That(computedStyle.GetPropertyValue("height"), Is.EqualTo("160px")); // 10em = 10 * 16px = 160px
        Assert.That(computedStyle.GetPropertyValue("padding-top"), Is.EqualTo("16px")); // 1rem = 1 * 16px = 16px
        Assert.That(computedStyle.GetPropertyValue("margin-top"), Is.EqualTo("32px")); // 2rem = 2 * 16px = 32px
    }

    [Test]
    public async Task CssVariables_ShouldBeResolved()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='parent'>
                     <div id='child'>Text</div>
                   </div>",
            @"
                :root {
                    --main-color: blue;
                    --main-padding: 16px;
                }
                #parent {
                    --parent-color: red;
                    color: var(--main-color);
                    padding: var(--main-padding);
                }
                #child {
                    color: var(--parent-color);
                    margin: var(--main-padding);
                }
                ");

        var parentElement = document.GetElementById("parent");
        var childElement = document.GetElementById("child");

        // Act
        var parentStyle = _styleEngine.ComputeElementStyle(parentElement!);
        var childStyle = _styleEngine.ComputeElementStyle(childElement!);

        // Assert
        Assert.That(parentStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // --main-color: blue
        Assert.That(parentStyle.GetPropertyValue("padding-top"), Is.EqualTo("16px")); // --main-padding: 16px

        Assert.That(childStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")); // --parent-color: red
        Assert.That(childStyle.GetPropertyValue("margin-top"), Is.EqualTo("16px")); // --main-padding: 16px
    }

    [Test]
    public async Task NestedCssVariables_ShouldBeResolved()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='test'>Text</div>",
            @"
                :root {
                    --base-size: 16px;
                    --main-padding: var(--base-size);
                    --box-padding: var(--main-padding);
                }
                #test {
                    padding: var(--box-padding);
                }
                ");

        var element = document.GetElementById("test");

        // Act
        var computedStyle = _styleEngine.ComputeElementStyle(element!);

        // Assert - should resolve through multiple variable references
        Assert.That(computedStyle.GetPropertyValue("padding-top"), Is.EqualTo("16px"));
    }

    [Test]
    public async Task CalcExpressions_ShouldBeComputed()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='test'>Text</div>",
            @"
                #test {
                    width: calc(100px + 50px);
                    height: calc(100px * 2);
                    margin-top: calc(20px - 5px);
                }
                ");

        var element = document.GetElementById("test");

        // Act
        var computedStyle = _styleEngine.ComputeElementStyle(element!);

        // Assert
        Assert.That(computedStyle.GetPropertyValue("width"), Is.EqualTo("150px"));
        Assert.That(computedStyle.GetPropertyValue("height"), Is.EqualTo("200px"));
        Assert.That(computedStyle.GetPropertyValue("margin-top"), Is.EqualTo("15px"));
    }

    [Test]
    public async Task LogicalProperties_ShouldMapToPhysicalProperties()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='test-ltr' dir='ltr'>LTR Text</div>
                  <div id='test-rtl' dir='rtl'>RTL Text</div>",
            @"
                div {
                    margin-inline-start: 10px;
                    padding-inline-end: 20px;
                    border-block-start-width: 2px;
                }
                ");

        var ltrElement = document.GetElementById("test-ltr");
        var rtlElement = document.GetElementById("test-rtl");

        // Act
        var ltrStyle = _styleEngine.ComputeElementStyle(ltrElement!);
        var rtlStyle = _styleEngine.ComputeElementStyle(rtlElement!);

        // Assert
        // In LTR, inline-start = left, inline-end = right, block-start = top
        Assert.That(ltrStyle.GetPropertyValue("margin-left"), Is.EqualTo("10px"));
        Assert.That(ltrStyle.GetPropertyValue("padding-right"), Is.EqualTo("20px"));
        Assert.That(ltrStyle.GetPropertyValue("border-top-width"), Is.EqualTo("2px"));

        // In RTL, inline-start = right, inline-end = left, block-start = top
        Assert.That(rtlStyle.GetPropertyValue("margin-right"), Is.EqualTo("10px"));
        Assert.That(rtlStyle.GetPropertyValue("padding-left"), Is.EqualTo("20px"));
        Assert.That(rtlStyle.GetPropertyValue("border-top-width"), Is.EqualTo("2px"));
    }

    [Test]
    public async Task ShorthandProperties_ShouldExpandCorrectly()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='test'>Text</div>",
            @"
                #test {
                    margin: 10px 20px 30px 40px;
                    padding: 5px 15px;
                    border-width: 1px;
                }
                ");

        var element = document.GetElementById("test");

        // Act
        var computedStyle = _styleEngine.ComputeElementStyle(element!);

        // Assert
        // Four-value shorthand expands to top, right, bottom, left
        Assert.That(computedStyle.GetPropertyValue("margin-top"), Is.EqualTo("10px"));
        Assert.That(computedStyle.GetPropertyValue("margin-right"), Is.EqualTo("20px"));
        Assert.That(computedStyle.GetPropertyValue("margin-bottom"), Is.EqualTo("30px"));
        Assert.That(computedStyle.GetPropertyValue("margin-left"), Is.EqualTo("40px"));

        // Two-value shorthand expands to top/bottom, left/right
        Assert.That(computedStyle.GetPropertyValue("padding-top"), Is.EqualTo("5px"));
        Assert.That(computedStyle.GetPropertyValue("padding-right"), Is.EqualTo("15px"));
        Assert.That(computedStyle.GetPropertyValue("padding-bottom"), Is.EqualTo("5px"));
        Assert.That(computedStyle.GetPropertyValue("padding-left"), Is.EqualTo("15px"));

        // One-value shorthand expands to all four sides
        Assert.That(computedStyle.GetPropertyValue("border-top-width"), Is.EqualTo("1px"));
        Assert.That(computedStyle.GetPropertyValue("border-right-width"), Is.EqualTo("1px"));
        Assert.That(computedStyle.GetPropertyValue("border-bottom-width"), Is.EqualTo("1px"));
        Assert.That(computedStyle.GetPropertyValue("border-left-width"), Is.EqualTo("1px"));
    }

    [Test]
    public async Task ComplexSelectors_ShouldMatchCorrectly()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='parent'>
                     <div class='item first'>Item 1</div>
                     <div class='item'>Item 2</div>
                     <div class='item'>Item 3</div>
                     <div class='item last'>Item 4</div>
                   </div>",
            @"
                .item { color: black; }
                .item.first { color: red; }
                .item:first-child { font-weight: bold; }
                .item:nth-child(even) { background-color: #f0f0f0; }
                .item:nth-child(3) { margin-top: 10px; }
                .item.last { color: blue; }
                ");

        var items = document.QuerySelectorAll(".item").ToArray();

        // Act
        var firstItemStyle = _styleEngine.ComputeElementStyle(items[0]);
        var secondItemStyle = _styleEngine.ComputeElementStyle(items[1]);
        var thirdItemStyle = _styleEngine.ComputeElementStyle(items[2]);
        var lastItemStyle = _styleEngine.ComputeElementStyle(items[3]);

        // Assert
        Assert.That(firstItemStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(firstItemStyle.GetPropertyValue("font-weight"), Is.EqualTo("700"));

        Assert.That(secondItemStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 0, 1)"));
        Assert.That(secondItemStyle.GetPropertyValue("background-color"), Is.EqualTo("rgba(240, 240, 240, 1)"));

        Assert.That(thirdItemStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 0, 1)"));
        Assert.That(thirdItemStyle.GetPropertyValue("margin-top"), Is.EqualTo("10px"));

        Assert.That(lastItemStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
        Assert.That(lastItemStyle.GetPropertyValue("background-color"), Is.EqualTo("rgba(240, 240, 240, 1)"));
    }

    [Test]
    public async Task MediaQueries_ShouldApplyCorrectStyles()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='test'>Text</div>",
            @"
                #test { color: black; }

                @media screen and (min-width: 600px) {
                    #test { color: blue; }
                }

                @media screen and (max-width: 400px) {
                    #test { color: red; }
                }
                ");

        var element = document.GetElementById("test");
        var defaultRenderDevice = _context.GetService<IRenderDevice>();

        // Act - Test with different viewport widths

        // First set to 800px width - should match min-width: 600px
        defaultRenderDevice!.SetViewport(800, 600);
        var wideStyle = _styleEngine.ComputeElementStyle(element!);

        // Then set to 300px width - should match max-width: 400px
        defaultRenderDevice.SetViewport(300, 600);
        var narrowStyle = _styleEngine.ComputeElementStyle(element!);

        // Finally set to 500px - should match neither media query
        defaultRenderDevice.SetViewport(500, 600);
        var mediumStyle = _styleEngine.ComputeElementStyle(element!);

        // Assert
        Assert.That(wideStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)")); // blue from min-width: 600px
        Assert.That(narrowStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)")); // red from max-width: 400px
        Assert.That(mediumStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 0, 1)")); // black (base style)
    }

    [Test]
    public async Task PseudoElements_ShouldComputeCorrectly()
    {
        // Arrange
        var document = await ParseDocumentWithStylesAsync(
            @"<div id='test'>Text</div>",
            @"
                #test::before {
                    content: 'Before';
                    color: red;
                }

                #test::after {
                    content: 'After';
                    color: blue;
                }
                ");

        var element = document.GetElementById("test");

        // Act
        var beforeStyle = _styleEngine.ComputeElementStyle(element!, "before");
        var afterStyle = _styleEngine.ComputeElementStyle(element!, "after");

        // Assert
        Assert.IsNotNull(beforeStyle);
        Assert.IsNotNull(afterStyle);
        Assert.That(beforeStyle.GetPropertyValue("content"), Is.EqualTo("'Before'"));
        Assert.That(beforeStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));

        Assert.That(afterStyle.GetPropertyValue("content"), Is.EqualTo("'After'"));
        Assert.That(afterStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
    }

    /// <summary>
    /// Sets up a document with the given HTML and CSS
    /// </summary>
    private async Task<IDocument> ParseDocumentWithStylesAsync(string html, string css)
    {
        var document = await _htmlParser.ParseDocumentAsync(html);

        // Add style element
        var styleElement = document.CreateElement("style");
        styleElement.TextContent = css;
        document.Head!.AppendChild(styleElement);

        // Setup style engine with document
        _styleEngine.StylesheetManager.AttachToDocument(document);

        return document;
    }
}