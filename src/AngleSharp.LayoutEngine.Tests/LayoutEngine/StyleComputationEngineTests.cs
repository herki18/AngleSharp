namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using Css;
using Dom;
using StyleComputation;

[TestFixture]
public class StyleComputationEngineIntegrationTests
{
    private IBrowsingContext _context;
    private IRenderDevice _device;

    [SetUp]
    public void Setup()
    {
        _context = BrowsingContext.New(Configuration.Default.WithCss());
        _device = new MockRenderDevice();
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task ComputeElementStyle_BasicStyling_ComputesCorrectly()
    {
        // Arrange
        var html = @"
        <html>
        <head>
            <style>
                div { color: red; }
                .test { font-size: 16px; }
                #myDiv { margin: 10px; }
            </style>
        </head>
        <body>
            <div id='myDiv' class='test'>Test</div>
        </body>
        </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("myDiv") as IElement;

        // Create engine with the document already loaded
        var engine = new StyleComputationEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = engine.ComputeElementStyle(element);

        // Assert
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
        Assert.That(style.GetPropertyValue("font-size"), Is.EqualTo("16px"));
        Assert.That(style.GetPropertyValue("margin"), Is.EqualTo("10px"));
    }

    [Test]
    public async Task ComputeElementStyle_CascadeWorks_HighestSpecificityWins()
    {
        // Arrange
        var html = @"
        <html>
        <head>
            <style>
                div { color: black; }
                .test { color: blue; }
                #myDiv { color: red; }
            </style>
        </head>
        <body>
            <div id='myDiv' class='test'>Test</div>
        </body>
        </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("myDiv") as IElement;

        // Create engine with the document already loaded
        var engine = new StyleComputationEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = engine.ComputeElementStyle(element);

        // Assert - ID selector should win
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public async Task ComputeElementStyle_InlineStylesWork()
    {
        // Arrange
        var html = @"
        <html>
        <head>
            <style>
                div { color: red; }
            </style>
        </head>
        <body>
            <div id='myDiv' style='color: green;'>Test</div>
        </body>
        </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("myDiv") as IElement;

        // Create engine with the document already loaded
        var engine = new StyleComputationEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = engine.ComputeElementStyle(element);

        // Assert - Inline styles should win
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 128, 0, 1)"));
    }

    [Test]
    public async Task ComputeElementStyle_ImportantFlagWorks()
    {
        // Arrange
        var html = @"
        <html>
        <head>
            <style>
                div { color: red !important; }
            </style>
        </head>
        <body>
            <div id='myDiv' style='color: green;'>Test</div>
        </body>
        </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("myDiv") as IElement;

        // Create engine with the document already loaded
        var engine = new StyleComputationEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = engine.ComputeElementStyle(element);

        // Assert - !important should override inline
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));
    }

    [Test]
    public async Task ComputeElementStyle_MediaQueriesFilterRules()
    {
        // Arrange
        var html = @"
        <html>
        <head>
            <style>
                div { color: black; }
                @media screen and (min-width: 800px) {
                    div { color: red; }
                }
                @media screen and (max-width: 600px) {
                    div { color: blue; }
                }
            </style>
        </head>
        <body>
            <div id='myDiv'>Test</div>
        </body>
        </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("myDiv") as IElement;

        // Create a device with width 1000px
        var device = new MockRenderDevice { ViewPortWidth = 1000, ViewPortHeight = 800 };
        var engine = new StyleComputationEngine(device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var style = engine.ComputeElementStyle(element);

        // Assert - min-width: 800px rule should apply
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));

        // Now test with a smaller device
        device.ViewPortWidth = 500;
        engine = new StyleComputationEngine(device, _context, document);
        style = engine.ComputeElementStyle(element);

        // Assert - max-width: 600px rule should apply
        Assert.That(style.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
    }

    [Test]
    public async Task ComputeElementStyle_PseudoElementsStylingWorks()
    {
        // Arrange
        var html = @"
        <html>
        <head>
            <style>
                div::before { content: 'prefix'; color: red; }
                div::after { content: 'suffix'; color: blue; }
            </style>
        </head>
        <body>
            <div id='myDiv'>Test</div>
        </body>
        </html>";

        var document = await _context.OpenAsync(req => req.Content(html));
        var element = document.GetElementById("myDiv") as IElement;

        // Create engine with the document already loaded
        var engine = new StyleComputationEngine(_device, _context, document);

        // Act
        Assert.IsNotNull(element);
        var beforeStyle = engine.ComputeElementStyle(element, null, "::before");
        var afterStyle = engine.ComputeElementStyle(element, null, "::after");

        // Assert
        Assert.That(beforeStyle.GetPropertyValue("content"), Is.EqualTo("\"prefix\""));
        Assert.That(beforeStyle.GetPropertyValue("color"), Is.EqualTo("rgba(255, 0, 0, 1)"));

        Assert.That(afterStyle.GetPropertyValue("content"), Is.EqualTo("\"suffix\""));
        Assert.That(afterStyle.GetPropertyValue("color"), Is.EqualTo("rgba(0, 0, 255, 1)"));
    }
}