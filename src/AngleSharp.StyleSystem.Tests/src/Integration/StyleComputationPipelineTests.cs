namespace AngleSharp.StyleSystem.Tests.Integration;

[TestFixture]
public class StyleComputationPipelineTests : StyleSystemTestFixture
{
    [Test]
    public async Task BasicStyleComputation_ShouldProduceCorrectComputedStyle()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
            "<div class='test'>Text</div>",
            ".test { color: red; font-size: 16px; }");

        var element = document.QuerySelector(".test");
        Assert.IsNotNull(element, "Test element should exist");

        // Act
        var computedStyle = GetComputedStyle(element);

        // Assert
        Assert.IsNotNull(computedStyle, "Computed style should not be null");
        AssertPropertyValue(computedStyle, "color", "rgba(255, 0, 0, 1)");
        AssertPropertyValue(computedStyle, "font-size", "16px");
    }

    [Test]
    public async Task StyleCascade_SpecificityRules_ShouldBeAppliedCorrectly()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
            "<div id='test' class='test-class' style='color: green;'>Text</div>",
            @"
                div { color: black; font-size: 12px; }
                .test-class { color: blue; font-size: 14px; }
                #test { color: red; }
                ");

        var element = document.QuerySelector("#test");
        Assert.IsNotNull(element, "Test element should exist");

        // Act
        var computedStyle = GetComputedStyle(element);

        // Assert - inline style should override id selector
        AssertPropertyValue(computedStyle, "color", "rgba(0, 128, 0, 1)"); // green
        AssertPropertyValue(computedStyle, "font-size", "14px");
    }

    [Test]
    public async Task Important_Rules_ShouldOverrideCascade()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
            "<div id='test' class='test-class' style='color: green;'>Text</div>",
            @"
                div { color: black; font-size: 12px; }
                .test-class { color: blue !important; font-size: 14px; }
                #test { color: red; }
                ");

        var element = document.QuerySelector("#test");
        Assert.IsNotNull(element, "Test element should exist");

        // Act
        var computedStyle = GetComputedStyle(element);

        // Assert - !important should override inline style
        AssertPropertyValue(computedStyle, "color", "rgba(0, 0, 255, 1)"); // blue
        AssertPropertyValue(computedStyle, "font-size", "14px");
    }

    [Test]
    public async Task PropertyInheritance_ShouldWorkCorrectly()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var parentStyle = GetComputedStyle(parentElement);
        var childStyle = GetComputedStyle(childElement);

        // Assert
        AssertPropertyValue(parentStyle, "color", "rgba(0, 0, 255, 1)");
        AssertPropertyValue(parentStyle, "font-family", "Arial");

        // Child should inherit color and font-family
        AssertInheritedFromParent(childStyle, parentStyle, "color");
        AssertInheritedFromParent(childStyle, parentStyle, "font-family");
        AssertPropertyValue(childStyle, "font-size", "16px");
    }

    [Test]
    public async Task ExplicitInheritKeyword_ShouldForceInheritance()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var parentStyle = GetComputedStyle(parentElement);
        var childStyle = GetComputedStyle(childElement);

        // Assert
        AssertPropertyValue(parentStyle, "font-size", "20px");
        AssertPropertyValue(parentStyle, "background-color", "rgba(238, 238, 238, 1)");

        // Child has explicit color but inherits font-size and background-color
        AssertPropertyValue(childStyle, "color", "rgba(255, 0, 0, 1)");
        AssertPropertyValue(childStyle, "font-size", "20px");
        AssertPropertyValue(childStyle, "background-color", "rgba(238, 238, 238, 1)");
    }

    [Test]
    public async Task InitialKeyword_ShouldResetToInitialValues()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
            @"<div id='parent'>
                     <div id='child'>Text</div>
                   </div>",
            @"
                #parent { color: blue; }
                #child { color: initial; font-weight: initial; }
                ");

        var childElement = document.GetElementById("child");

        // Act
        var childStyle = GetComputedStyle(childElement);

        // Assert - color should be reset to black (initial)
        AssertPropertyValue(childStyle, "color", "rgba(0, 0, 0, 1)");
        AssertPropertyValue(childStyle, "font-weight", "400");
    }

    [Test]
    public async Task UnsetKeyword_ShouldInheritOrUseInitialValue()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
            @"<div id='parent'>
                     <div id='child'>Text</div>
                   </div>",
            @"
                #parent { color: blue; display: flex; }
                #child { color: unset; display: unset; }
                ");

        var childElement = document.GetElementById("child");

        // Act
        var childStyle = GetComputedStyle(childElement);

        // Assert
        // color inherits because it's an inherited property
        AssertPropertyValue(childStyle, "color", "rgba(0, 0, 255, 1)");
        // display uses initial value (block) because it's not inherited
        AssertPropertyValue(childStyle, "display", "block");
    }

    [Test]
    public async Task LengthUnits_ShouldBeComputedCorrectly()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var computedStyle = GetComputedStyle(element);

        // Assert
        AssertPropertyValue(computedStyle, "width", "200px");
        AssertPropertyValue(computedStyle, "height", "160px"); // 10em = 10 * 16px = 160px
        AssertPropertyValue(computedStyle, "padding-top", "16px"); // 1rem = 1 * 16px = 16px
        AssertPropertyValue(computedStyle, "margin-top", "32px"); // 2rem = 2 * 16px = 32px
    }

    [Test]
    public async Task CssVariables_ShouldBeResolved()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var parentStyle = GetComputedStyle(parentElement);
        var childStyle = GetComputedStyle(childElement);

        // Assert
        AssertPropertyValue(parentStyle, "color", "rgba(0, 0, 255, 1)"); // --main-color: blue
        AssertPropertyValue(parentStyle, "padding-top", "16px"); // --main-padding: 16px

        AssertPropertyValue(childStyle, "color", "rgba(255, 0, 0, 1)"); // --parent-color: red
        AssertPropertyValue(childStyle, "margin-top", "16px"); // --main-padding: 16px
    }

    [Test]
    public async Task NestedCssVariables_ShouldBeResolved()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var computedStyle = GetComputedStyle(element);

        // Assert - should resolve through multiple variable references
        AssertPropertyValue(computedStyle, "padding-top", "16px");
    }

    [Test]
    public async Task CalcExpressions_ShouldBeComputed()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var computedStyle = GetComputedStyle(element);

        // Assert
        AssertPropertyValue(computedStyle, "width", "150px");
        AssertPropertyValue(computedStyle, "height", "200px");
        AssertPropertyValue(computedStyle, "margin-top", "15px");
    }

    [Test]
    public async Task LogicalProperties_ShouldMapToPhysicalProperties()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var ltrStyle = GetComputedStyle(ltrElement);
        var rtlStyle = GetComputedStyle(rtlElement);

        // Assert
        // In LTR, inline-start = left, inline-end = right, block-start = top
        AssertPropertyValue(ltrStyle, "margin-left", "10px");
        AssertPropertyValue(ltrStyle, "padding-right", "20px");
        AssertPropertyValue(ltrStyle, "border-top-width", "2px");

        // In RTL, inline-start = right, inline-end = left, block-start = top
        AssertPropertyValue(rtlStyle, "margin-right", "10px");
        AssertPropertyValue(rtlStyle, "padding-left", "20px");
        AssertPropertyValue(rtlStyle, "border-top-width", "2px");
    }

    [Test]
    public async Task ShorthandProperties_ShouldExpandCorrectly()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var computedStyle = GetComputedStyle(element);

        // Assert
        // Four-value shorthand expands to top, right, bottom, left
        AssertPropertyValue(computedStyle, "margin-top", "10px");
        AssertPropertyValue(computedStyle, "margin-right", "20px");
        AssertPropertyValue(computedStyle, "margin-bottom", "30px");
        AssertPropertyValue(computedStyle, "margin-left", "40px");

        // Two-value shorthand expands to top/bottom, left/right
        AssertPropertyValue(computedStyle, "padding-top", "5px");
        AssertPropertyValue(computedStyle, "padding-right", "15px");
        AssertPropertyValue(computedStyle, "padding-bottom", "5px");
        AssertPropertyValue(computedStyle, "padding-left", "15px");

        // One-value shorthand expands to all four sides
        AssertPropertyValue(computedStyle, "border-top-width", "1px");
        AssertPropertyValue(computedStyle, "border-right-width", "1px");
        AssertPropertyValue(computedStyle, "border-bottom-width", "1px");
        AssertPropertyValue(computedStyle, "border-left-width", "1px");
    }

    [Test]
    public async Task ComplexSelectors_ShouldMatchCorrectly()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var firstItemStyle = GetComputedStyle(items[0]);
        var secondItemStyle = GetComputedStyle(items[1]);
        var thirdItemStyle = GetComputedStyle(items[2]);
        var lastItemStyle = GetComputedStyle(items[3]);

        // Assert
        AssertPropertyValue(firstItemStyle, "color", "rgba(255, 0, 0, 1)");
        AssertPropertyValue(firstItemStyle, "font-weight", "700");

        AssertPropertyValue(secondItemStyle, "color", "rgba(0, 0, 0, 1)");
        AssertPropertyValue(secondItemStyle, "background-color", "rgba(240, 240, 240, 1)");

        AssertPropertyValue(thirdItemStyle, "color", "rgba(0, 0, 0, 1)");
        AssertPropertyValue(thirdItemStyle, "margin-top", "10px");

        AssertPropertyValue(lastItemStyle, "color", "rgba(0, 0, 255, 1)");
        AssertPropertyValue(lastItemStyle, "background-color", "rgba(240, 240, 240, 1)");
    }

    [Test]
    public async Task MediaQueries_ShouldApplyCorrectStyles()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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

        // Act - Test with different viewport widths
        // First set to 800px width - should match min-width: 600px
        SetViewport(800, 600);
        var wideStyle = GetComputedStyle(element);

        // Then set to 300px width - should match max-width: 400px
        SetViewport(300, 600);
        var narrowStyle = GetComputedStyle(element);

        // Finally set to 500px - should match neither media query
        SetViewport(500, 600);
        var mediumStyle = GetComputedStyle(element);

        // Assert
        AssertPropertyValue(wideStyle, "color", "rgba(0, 0, 255, 1)"); // blue from min-width: 600px
        AssertPropertyValue(narrowStyle, "color", "rgba(255, 0, 0, 1)"); // red from max-width: 400px
        AssertPropertyValue(mediumStyle, "color", "rgba(0, 0, 0, 1)"); // black (base style)
    }

    [Test]
    public async Task PseudoElements_ShouldComputeCorrectly()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
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
        var beforeStyle = GetComputedPseudoElementStyle(element, "::before");
        var afterStyle = GetComputedPseudoElementStyle(element, "::after");

        // Assert
        Assert.IsNotNull(beforeStyle);
        Assert.IsNotNull(afterStyle);
        AssertPropertyValue(beforeStyle, "content", "'Before'");
        AssertPropertyValue(beforeStyle, "color", "rgba(255, 0, 0, 1)");

        AssertPropertyValue(afterStyle, "content", "'After'");
        AssertPropertyValue(afterStyle, "color", "rgba(0, 0, 255, 1)");
    }

    [Test]
    public async Task BoxModel_ShouldHaveCorrectDimensions()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
            @"<div id='box'>Box Model Test</div>",
            @"
                #box {
                    width: 200px;
                    height: 100px;
                    margin: 10px 20px 30px 40px;
                    padding: 5px 15px 25px 35px;
                    border-width: 1px 2px 3px 4px;
                    border-style: solid;
                }
                ");

        var element = document.GetElementById("box");

        // Act
        var computedStyle = GetComputedStyle(element);

        // Assert
        AssertBoxModel(computedStyle,
            "200px", "100px",
            "10px", "20px", "30px", "40px");

        AssertPadding(computedStyle,
            "5px", "15px", "25px", "35px");

        AssertBorders(computedStyle,
            "1px", "2px", "3px", "4px");
    }

    [Test]
    public async Task TypographyProperties_ShouldBeComputedCorrectly()
    {
        // Arrange
        var document = await CreateDocumentWithCssAsync(
            @"<div id='text'>Typography Test</div>",
            @"
                #text {
                    font-family: 'Arial', sans-serif;
                    font-size: 18px;
                    font-weight: 700;
                    line-height: 1.5;
                    color: #336699;
                    text-align: center;
                }
                ");

        var element = document.GetElementById("text");

        // Act
        var computedStyle = GetComputedStyle(element);

        // Assert
        AssertTypography(computedStyle,
            "18px", "'Arial', sans-serif", "700",
            "rgba(51, 102, 153, 1)", "center");

        // Check the line height specifically
        var lineHeight = computedStyle.GetPropertyValue("line-height");
        Assert.That(lineHeight, Is.EqualTo("27px")); // 18px * 1.5 = 27px
    }
}