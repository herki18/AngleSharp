namespace AngleSharp.StyleSystem.Tests.Integration;

[TestFixture]
public class ComputedValueTests : StyleSystemTestFixture
{
    [Test]
    public void LineHeight_AsNumber_ShouldComputeToPixels()
    {
        // Arrange
        var element = CreateTestElement("div");
        SetInlineStyle(element, "font-size: 16px; line-height: 1.5;");

        // Act
        var style = GetComputedStyle(element);

        // Assert - LineHeight as number 1.5 should compute to 24px (1.5 * 16px)
        AssertPropertyValue(style, "line-height", "24px");
    }

    [Test]
    public void LineHeight_AsPixels_ShouldStayAsPixels()
    {
        // Arrange
        var element = CreateTestElement("div");
        SetInlineStyle(element, "line-height: 24px;");

        // Act
        var style = GetComputedStyle(element);

        // Assert
        AssertPropertyValue(style, "line-height", "24px");
    }

    [Test]
    public void LineHeight_AsEm_ShouldComputeToPixels()
    {
        // Arrange
        var element = CreateTestElement("div");
        SetInlineStyle(element, "font-size: 16px; line-height: 1.5em;");

        // Act
        var style = GetComputedStyle(element);

        // Assert - LineHeight as 1.5em should compute to 24px (1.5 * 16px)
        AssertPropertyValue(style, "line-height", "24px");
    }

    [Test]
    public void MarginBottom_AsEm_ShouldComputeToPixels()
    {
        // Arrange
        var element = CreateTestElement("div");
        SetInlineStyle(element, "font-size: 16px; margin-bottom: 1em;");

        // Act
        var style = GetComputedStyle(element);

        // Assert - 1em margin should compute to 16px (1 * 16px font-size)
        AssertPropertyValue(style, "margin-bottom", "16px");
    }

    [Test]
    public void Rem_ShouldComputeBasedOnRootFontSize()
    {
        // Arrange - Set root font size
        var htmlElement = Document.DocumentElement;
        SetInlineStyle(htmlElement, "font-size: 20px;");

        // Create element with rem-based values
        var element = CreateTestElement("div");
        SetInlineStyle(element, "font-size: 1.5rem; margin: 2rem;");

        // Act
        var style = GetComputedStyle(element);

        // Assert
        // 1.5rem should compute to 30px (1.5 * 20px root font-size)
        AssertPropertyValue(style, "font-size", "30px");

        // 2rem should compute to 40px (2 * 20px root font-size) for all margin sides
        AssertPropertyValue(style, "margin-top", "40px");
        AssertPropertyValue(style, "margin-right", "40px");
        AssertPropertyValue(style, "margin-bottom", "40px");
        AssertPropertyValue(style, "margin-left", "40px");
    }

    [Test]
    public void FontWeight_Keywords_ShouldComputeToNumericValues()
    {
        // Arrange
        var element1 = CreateTestElement("div", "bold-element");
        SetInlineStyle(element1, "font-weight: bold;");

        var element2 = CreateTestElement("div", "normal-element");
        SetInlineStyle(element2, "font-weight: normal;");

        // Act
        var style1 = GetComputedStyle(element1);
        var style2 = GetComputedStyle(element2);

        // Assert
        // "bold" should compute to "700"
        AssertPropertyValue(style1, "font-weight", "700");

        // "normal" should compute to "400"
        AssertPropertyValue(style2, "font-weight", "400");
    }

    [Test]
    public void Color_Keywords_ShouldComputeToRGBA()
    {
        // Arrange
        var element1 = CreateTestElement("div", "red-element");
        SetInlineStyle(element1, "color: red;");

        var element2 = CreateTestElement("div", "blue-element");
        SetInlineStyle(element2, "color: blue;");

        var element3 = CreateTestElement("div", "hex-element");
        SetInlineStyle(element3, "color: #ff5500;");

        // Act
        var style1 = GetComputedStyle(element1);
        var style2 = GetComputedStyle(element2);
        var style3 = GetComputedStyle(element3);

        // Assert - Color keywords and hex values should compute to rgba()
        AssertPropertyValue(style1, "color", "rgba(255, 0, 0, 1)");
        AssertPropertyValue(style2, "color", "rgba(0, 0, 255, 1)");
        AssertPropertyValue(style3, "color", "rgba(255, 85, 0, 1)");
    }

    [Test]
    public void Percentages_ShouldComputeBasedOnContainer()
    {
        // Arrange
        var parent = CreateTestElement("div", "parent");
        SetInlineStyle(parent, "width: 200px; height: 400px;");

        var child = CreateTestElement("div", "child", parentElement: parent);
        SetInlineStyle(child, "width: 50%; height: 25%;");

        // Act
        var styleParent = GetComputedStyle(parent);
        var styleChild = GetComputedStyle(child);

        // Assert
        // Parent has absolute dimensions
        AssertPropertyValue(styleParent, "width", "200px");
        AssertPropertyValue(styleParent, "height", "400px");

        // Child percentages should compute based on parent dimensions
        AssertPropertyValue(styleChild, "width", "100px"); // 50% of 200px
        AssertPropertyValue(styleChild, "height", "100px"); // 25% of 400px
    }

    [Test]
    public void ViewportUnits_ShouldComputeBasedOnViewport()
    {
        // Arrange - Set viewport dimensions
        SetViewport(1024, 768);

        var element = CreateTestElement("div");
        SetInlineStyle(element, "width: 50vw; height: 50vh; margin: 5vmin; padding: 5vmax;");

        // Act
        var style = GetComputedStyle(element);

        // Assert
        // Viewport units should compute as pixels based on viewport size
        AssertPropertyValue(style, "width", "512px"); // 50% of 1024px
        AssertPropertyValue(style, "height", "384px"); // 50% of 768px

        // vmin is based on the smaller viewport dimension (768px)
        AssertPropertyValue(style, "margin-top", "38.4px"); // 5% of 768px

        // vmax is based on the larger viewport dimension (1024px)
        AssertPropertyValue(style, "padding-top", "51.2px"); // 5% of 1024px
    }

    [Test]
    public void LogicalProperties_ShouldComputeToPhysicalProperties()
    {
        // Arrange
        var element = CreateTestElement("div");
        SetInlineStyle(element, "font-size: 16px; margin-block: 1em; padding-inline: 2em;");

        // Act
        var style = GetComputedStyle(element);

        // Assert
        // Logical properties should compute as physical properties based on writing mode
        // For horizontal top-to-bottom (default):
        // - block = top/bottom
        // - inline = left/right

        // margin-block expands to margin-top and margin-bottom
        AssertPropertyValue(style, "margin-top", "16px"); // 1em = 16px
        AssertPropertyValue(style, "margin-bottom", "16px"); // 1em = 16px

        // padding-inline expands to padding-left and padding-right
        AssertPropertyValue(style, "padding-left", "32px"); // 2em = 32px
        AssertPropertyValue(style, "padding-right", "32px"); // 2em = 32px
    }

    [Test]
    public void InBlockSize_ShouldComputeToHeightInHorizontalMode()
    {
        // Arrange
        var element = CreateTestElement("div");
        SetInlineStyle(element, "block-size: 100px; inline-size: 200px;");

        // Act
        var style = GetComputedStyle(element);

        // Assert
        // In horizontal writing mode:
        // - block-size maps to height
        // - inline-size maps to width
        AssertPropertyValue(style, "height", "100px");
        AssertPropertyValue(style, "width", "200px");
    }

    [Test]
    public void BorderLogicalProperties_ShouldComputeToPhysicalProperties()
    {
        // Arrange
        var element = CreateTestElement("div");
        SetInlineStyle(element, "border-block-width: 5px; border-inline-width: 10px;");

        // Act
        var style = GetComputedStyle(element);

        // Assert
        // border-block-width expands to border-top-width and border-bottom-width
        AssertPropertyValue(style, "border-top-width", "5px");
        AssertPropertyValue(style, "border-bottom-width", "5px");

        // border-inline-width expands to border-left-width and border-right-width
        AssertPropertyValue(style, "border-left-width", "10px");
        AssertPropertyValue(style, "border-right-width", "10px");
    }

    [Test]
    public void Calc_ShouldComputeToAbsoluteValue()
    {
        // Arrange
        SetViewport(1000, 800);
        var element = CreateTestElement("div");
        SetInlineStyle(element, "width: calc(100px + 10%); font-size: 16px; margin-left: calc(1em + 10px);");

        // Act
        var style = GetComputedStyle(element);

        // Assert
        // calc(100px + 10%) with parent width of 1000px should be 200px
        AssertPropertyValue(style, "width", "200px");

        // calc(1em + 10px) with font-size of 16px should be 26px
        AssertPropertyValue(style, "margin-left", "26px");
    }

    [Test]
    public void WritingModeChangesLogicalPropertyMapping()
    {
        // Arrange
        var element = CreateTestElement("div");
        SetInlineStyle(element, @"
            writing-mode: vertical-rl;
            inline-size: 100px;
            block-size: 200px;
            margin-inline: 10px;
            margin-block: 20px;
        ");

        // Act
        var style = GetComputedStyle(element);

        // Assert
        // In vertical-rl writing mode:
        // - inline-size maps to height
        // - block-size maps to width
        AssertPropertyValue(style, "height", "100px");
        AssertPropertyValue(style, "width", "200px");

        // margin-inline maps to margin-top and margin-bottom
        AssertPropertyValue(style, "margin-top", "10px");
        AssertPropertyValue(style, "margin-bottom", "10px");

        // margin-block maps to margin-right and margin-left
        AssertPropertyValue(style, "margin-right", "20px");
        AssertPropertyValue(style, "margin-left", "20px");
    }
}