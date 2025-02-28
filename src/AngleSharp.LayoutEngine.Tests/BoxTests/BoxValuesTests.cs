namespace AngleSharp.LayoutEngine.Tests.BoxTests;

using Box;
using Dom;

public class BoxValuesTests
{
    [Test]
    public void BoxValues_WithPixelMeasurements_CalculatesCorrectDimensions()
    {
        // Create a document to give us access to AngleSharp's CSS infrastructure
        var context = BrowsingContext.New(Configuration.Default.WithCss());
        var document = context.OpenAsync(req => req.Content("<div></div>")).Result;
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
}