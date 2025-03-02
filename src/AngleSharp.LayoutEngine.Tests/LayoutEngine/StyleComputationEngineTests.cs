namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using Dom;
using StyleComputation;

[TestFixture]
public class StyleComputationEngineTests
{
    private IBrowsingContext _context;
    private StyleComputationEngine _engine;

    [SetUp]
    public void Setup()
    {
        // Create a new browsing context
        _context = BrowsingContext.New(Configuration.Default.WithCss());

        // Initialize the StyleComputationEngine
        _engine = new StyleComputationEngine();
    }

    [Test]
    public async Task ComputeElementStyle_BasicElement_ReturnsCorrectStyles()
    {
        // Arrange
        var html = @"
        <html>
        <head>
            <style>
                div { color: red; font-size: 16px; }
            </style>
        </head>
        <body>
            <div id='testDiv'>Test</div>
        </body>
        </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("testDiv") as IElement;

        Assert.IsNotNull(element);

        // Act
        var computedStyle = _engine.ComputeElementStyle(element);

        // Assert
        Assert.IsNotNull(computedStyle);
        Assert.That(computedStyle.GetPropertyValue("color"), Is.EqualTo("red"));
        Assert.That(computedStyle.GetPropertyValue("font-size"), Is.EqualTo("16px"));
    }

    [TearDown]
    public void Teardown()
    {
        _context.Dispose();
    }

    // Test methods will go here
}