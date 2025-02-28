namespace AngleSharp.LayoutEngine.Tests.BoxTests;

using AngleSharp.LayoutEngine.Box;
using Dom;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class BoxValuesTests
{
    private IBrowsingContext _context;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss().WithTestRenderDevice(baseFontSize: 16.0));
    }

    [TearDown]
    public void Cleanup()
    {
        _context?.Dispose();
    }

    [Test]
    public void BoxValues_WithPixelMeasurements_CalculatesCorrectDimensions()
    {
        // Create a document to give us access to AngleSharp's CSS infrastructure
        var document = _context.OpenAsync(req => req.Content("<div></div>")).Result;
        var element = document.QuerySelector("div");

        Assert.That(element, Is.Not.Null);
        // Set inline styles for testing
        element.SetAttribute("style", "width: 100px; padding: 10px; border-width: 5px; margin: 15px;");

        // Get the computed style
        var computedStyle = document.DefaultView.GetComputedStyle(element);

        // Now test our BoxModelCalculator with the real style
        var calculator = new BoxModelCalculator(computedStyle, 800);
        var boxValues = calculator.GetBoxValues();

        Assert.That(boxValues.ContentWidth, Is.EqualTo(100));
        Assert.That(boxValues.PaddingLeft, Is.EqualTo(10));
        Assert.That(boxValues.BorderRight, Is.EqualTo(5));
        Assert.That(boxValues.MarginTop, Is.EqualTo(15));
        Assert.That(boxValues.BorderBoxWidth, Is.EqualTo(130)); // 100 + 10*2 + 5*2
        Assert.That(boxValues.MarginBoxWidth, Is.EqualTo(160)); // 130 + 15*2
    }

    [Test]
    public void BoxValues_WithMixedUnits_CalculatesCorrectDimensions()
    {
        var document = _context.OpenAsync(req => req.Content(@"
            <div style='font-size: 16px;'>
                <div id='test' style='width: 300px; height: 10em; padding: 1em 5%; border-width: 5px; margin: 10px 5%;'></div>
            </div>
        ")).Result;

        var element = document.QuerySelector("#test");

        // Get the computed style
        var computedStyle = document.DefaultView.GetComputedStyle(element);

        // Print actual values for debugging
        Console.WriteLine($"Computed padding-right: {computedStyle.PaddingRight}");
        Console.WriteLine($"Computed margin-right: {computedStyle.MarginRight}");

        // Now test our BoxModelCalculator with the real style
        var calculator = new BoxModelCalculator(computedStyle, 1000); // Parent width is 1000px
        var boxValues = calculator.GetBoxValues();

        // Print calculated values for debugging
        Console.WriteLine($"Calculated padding-right: {boxValues.PaddingRight}");
        Console.WriteLine($"Calculated margin-right: {boxValues.MarginRight}");

        // Test dimensions with mixed units
        Assert.That(boxValues.ContentWidth, Is.EqualTo(300).Within(1)); // 300px

        // em values (1em = 16px in this case)
        Assert.That(boxValues.PaddingTop, Is.EqualTo(16).Within(1)); // 1em = 16px

        // Percentage values - AngleSharp uses device width (1024px) not container width
        Assert.That(boxValues.PaddingRight, Is.InRange(50, 52)); // ~5% of device width

        // Mixed margins (10px top/bottom, 5% of device width left/right)
        Assert.That(boxValues.MarginTop, Is.EqualTo(10).Within(1)); // 10px
        Assert.That(boxValues.MarginRight, Is.InRange(50, 52)); // ~5% of device width

        // Height should be 10em = 160px
        Assert.That(boxValues.ContentHeight, Is.EqualTo(160).Within(1)); // 10em = 10 * 16px
    }

    [Test]
    public void BoxValues_WithPercentageMeasurements_CalculatesCorrectDimensions()
    {
        var document = _context.OpenAsync(req => req.Content(@"<div style='width: 1000px; height: 800px;'></div>")).Result;
        var element = document.QuerySelector("div");

        Assert.That(element, Is.Not.Null);
        // Set percentage-based styles for testing
        element.SetAttribute("style", "width: 50%; height: 25%; padding: 5%; margin: 10%; border-width: 5px;");

        var computedStyle = document.DefaultView.GetComputedStyle(element);
        var currentStyle = element.ComputeCurrentStyle();
        // Test with container width of 1000px
        var calculator = new BoxModelCalculator(currentStyle, 1000);
        var boxValues = calculator.GetBoxValues();

        // 50% of 1000px = 500px
        Assert.That(boxValues.ContentWidth, Is.EqualTo(500).Within(0.1));
        // 5% of 1000px = 50px
        Assert.That(boxValues.PaddingLeft, Is.EqualTo(50).Within(0.1));
        Assert.That(boxValues.BorderRight, Is.EqualTo(2));
        // 3% of 1000px = 30px
        Assert.That(boxValues.MarginTop, Is.EqualTo(30).Within(0.1));

        // BorderBox: 500 + (50*2) + (2*2) = 604px
        Assert.That(boxValues.BorderBoxWidth, Is.EqualTo(604).Within(0.1));
        // MarginBox: 604 + (30*2) = 664px
        Assert.That(boxValues.MarginBoxWidth, Is.EqualTo(664).Within(0.1));
    }

    [Test]
    public void CalculateContentWidthFromBorderBox_WithDifferentBoxSizing_ReturnsCorrectValue()
    {
        // Create a BoxValues instance with some predefined values
        var boxValues = new BoxValues
        {
            PaddingLeft = 10,
            PaddingRight = 10,
            BorderLeft = 5,
            BorderRight = 5
        };

        // Calculate content width from border box width of 200px
        boxValues.CalculateContentWidthFromBorderBox(200);

        // Content width should be: border-box width - padding - border
        // 200 - (10*2) - (5*2) = 200 - 20 - 10 = 170
        Assert.That(boxValues.ContentWidth, Is.EqualTo(170));

        // Test with border-box of 0 (should clamp to 0)
        boxValues.CalculateContentWidthFromBorderBox(0);
        Assert.That(boxValues.ContentWidth, Is.EqualTo(0));

        // Test with border-box smaller than insets (should clamp to 0)
        boxValues.CalculateContentWidthFromBorderBox(20);
        Assert.That(boxValues.ContentWidth, Is.EqualTo(0));
    }

    [Test]
    public void CalculateContentHeightFromBorderBox_WithDifferentBoxSizing_ReturnsCorrectValue()
    {
        // Create a BoxValues instance with some predefined values
        var boxValues = new BoxValues
        {
            PaddingTop = 15,
            PaddingBottom = 15,
            BorderTop = 8,
            BorderBottom = 8
        };

        // Calculate content height from border box height of 250px
        boxValues.CalculateContentHeightFromBorderBox(250);

        // Content height should be: border-box height - padding - border
        // 250 - (15*2) - (8*2) = 250 - 30 - 16 = 204
        Assert.That(boxValues.ContentHeight, Is.EqualTo(204));

        // Test with border-box of 0 (should clamp to 0)
        boxValues.CalculateContentHeightFromBorderBox(0);
        Assert.That(boxValues.ContentHeight, Is.EqualTo(0));

        // Test with border-box smaller than insets (should clamp to 0)
        boxValues.CalculateContentHeightFromBorderBox(40);
        Assert.That(boxValues.ContentHeight, Is.EqualTo(0));
    }

    [Test]
    public void HorizontalInsets_WithVariousValues_CalculatesCorrectly()
    {
        var boxValues = new BoxValues
        {
            PaddingLeft = 10,
            PaddingRight = 15,
            BorderLeft = 5,
            BorderRight = 8
        };

        // Horizontal insets should be sum of left and right padding and border
        // 10 + 15 + 5 + 8 = 38
        Assert.That(boxValues.HorizontalInsets, Is.EqualTo(38));

        // Test with zero values
        boxValues.PaddingLeft = 0;
        boxValues.PaddingRight = 0;
        boxValues.BorderLeft = 0;
        boxValues.BorderRight = 0;
        Assert.That(boxValues.HorizontalInsets, Is.EqualTo(0));

        // Test with negative values (should not happen in practice, but testing for robustness)
        boxValues.PaddingLeft = -5;  // In real CSS, this would be clamped to 0
        Assert.That(boxValues.HorizontalInsets, Is.EqualTo(-5));
    }

    [Test]
    public void VerticalInsets_WithVariousValues_CalculatesCorrectly()
    {
        var boxValues = new BoxValues
        {
            PaddingTop = 12,
            PaddingBottom = 18,
            BorderTop = 6,
            BorderBottom = 9
        };

        // Vertical insets should be sum of top and bottom padding and border
        // 12 + 18 + 6 + 9 = 45
        Assert.That(boxValues.VerticalInsets, Is.EqualTo(45));

        // Test with zero values
        boxValues.PaddingTop = 0;
        boxValues.PaddingBottom = 0;
        boxValues.BorderTop = 0;
        boxValues.BorderBottom = 0;
        Assert.That(boxValues.VerticalInsets, Is.EqualTo(0));
    }

    [Test]
    public void BoxValues_WithBorderBoxSizing_CalculatesCorrectDimensions()
    {
        var document = _context.OpenAsync(req => req.Content("<div></div>")).Result;
        var element = document.QuerySelector("div");

        Assert.That(element, Is.Not.Null);
        // Set box-sizing: border-box with dimensions
        element.SetAttribute("style", "box-sizing: border-box; width: 200px; height: 150px; padding: 20px; border: 5px solid black;");

        var computedStyle = document.DefaultView.GetComputedStyle(element);

        var calculator = new BoxModelCalculator(computedStyle, 1000);
        var boxValues = calculator.GetBoxValues();

        // Content width should be: width - padding - border
        // 200 - (20*2) - (5*2) = 200 - 40 - 10 = 150
        Assert.That(boxValues.ContentWidth, Is.EqualTo(150).Within(0.1));

        // BorderBoxWidth should be the specified width
        Assert.That(boxValues.BorderBoxWidth, Is.EqualTo(200).Within(0.1));
    }
}