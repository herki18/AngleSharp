namespace AngleSharp.StyleSystem.Tests.End2End;

using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Html.Parser;
using AngleSharp.StyleSystem.DependencyInjection;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Services;
using Microsoft.Extensions.DependencyInjection;

[TestFixture]
public class AngleSharpStyleSystemIntegrationTests
{
    private IServiceProvider _serviceProvider;
    private IBrowsingContext _context;
    private IStyleEngine _styleEngine;

    [OneTimeSetUp]
    public void SetupFixture()
    {
        // Create service collection
        var services = new ServiceCollection();

        // Create AngleSharp context with configuration
        var config = Configuration.Default
            .WithCss()
            .WithDefaultLoader();

        var context = BrowsingContext.New(config);

        // Register AngleSharp services
        services.AddAngleSharpServices(context);

        // Register StyleSystem services
        services.AddStyleSystem(options => {
            options.EnableOptimization = true;
            options.LoadUserAgentStylesheets = true;
            options.UpdateStylesImmediately = true;
        });

        // Build the service provider
        _serviceProvider = services.BuildServiceProvider();

        // Get the browsing context
        _context = _serviceProvider.GetRequiredService<IBrowsingContext>();

        // Initialize StyleSystem
        var styleSystemService = _serviceProvider.GetRequiredService<StyleSystemService>();
        styleSystemService.Initialize(_context);

        // Get the style engine
        _styleEngine = _serviceProvider.GetRequiredService<IStyleEngine>();
    }

    [Test]
    public async Task ComputeStyles_WithDependencyInjection_ShouldReturnCorrectComputedValues()
    {
        // Arrange - Create a simple HTML document with CSS
        string html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { font-family: Arial, sans-serif; }
                        .test-div {
                            display: block;
                            width: 200px;
                            height: 100px;
                            color: red;
                            margin: 20px;
                            padding: 10px;
                            border: 1px solid black;
                        }
                    </style>
                </head>
                <body>
                    <div id='test-element' class='test-div'>Test Element</div>
                </body>
                </html>";

        // Act - Parse and compute styles
        var document = await _context.OpenAsync(req => req.Content(html));
        var testElement = document.GetElementById("test-element");

        Assert.That(testElement, Is.Not.Null, "Test element should exist");

        // Trigger style computation
        _styleEngine.UpdateStyles(document.DocumentElement);
        var computedStyle = _styleEngine.ComputeElementStyle(testElement);

        // Assert - Verify style values using constraints
        Assert.That(computedStyle, Is.Not.Null, "Computed style should not be null");

        // Check display property
        Assert.That(computedStyle.Display, Is.EqualTo(DisplayMode.Block), "Display should be block");

        // Check dimensions
        Assert.That(computedStyle.GetPropertyValue("width"), Contains.Substring("200px"), "Width should be 200px");
        Assert.That(computedStyle.GetPropertyValue("height"), Contains.Substring("100px"), "Height should be 100px");

        // Check color
        Assert.That(computedStyle.GetPropertyValue("color"), Does.Contain("rgb(255, 0, 0)")
                .Or.Contain("rgba(255, 0, 0"),
            "Color should be red");

        // Check box properties
        Assert.That(computedStyle.Box.Margin.Top.ToPixel(null), Is.EqualTo(20).Within(0.01),
            "Top margin should be 20px");
        Assert.That(computedStyle.Box.Padding.Top.ToPixel(null), Is.EqualTo(10).Within(0.01),
            "Top padding should be 10px");
        Assert.That(computedStyle.Box.Border.Top.ToPixel(null), Is.EqualTo(1).Within(0.01),
            "Top border should be 1px");

        // Check text properties
        Assert.That(computedStyle.Text.FontFamily, Does.Contain("Arial"),
            "Font family should include Arial");
    }

    [Test]
    public async Task StyleSystemServiceDI_ShouldWorkWithAngleSharpTypes()
    {
        // Arrange - Get services via DI
        var cssParser = _serviceProvider.GetService<ICssParser>();
        var htmlParser = _serviceProvider.GetService<IHtmlParser>();
        var cssStyleEngine = _serviceProvider.GetService<IStyleEngine>();

        // Assert service availability
        Assert.That(cssParser, Is.Not.Null, "CSS Parser should be available");
        Assert.That(htmlParser, Is.Not.Null, "HTML Parser should be available");
        Assert.That(cssStyleEngine, Is.Not.Null, "Style Engine should be available");

        // Act - Use services together
        string html = "<div style='color: blue;'>Test</div>";
        string css = "div { background-color: yellow; }";

        var fragment = await htmlParser.ParseDocumentAsync(html);
        var stylesheet = cssParser.ParseStyleSheet(css);

        var styleSheetManager = _serviceProvider.GetRequiredService<IStyleSheetManager>();
        styleSheetManager.RegisterStylesheet(stylesheet, Models.StylesheetOrigin.Author);

        var divElement = fragment.QuerySelector("div");

        // Make sure styles are computed
        if (divElement != null)
        {
            cssStyleEngine.ComputeElementStyle(divElement);
        }
        else
        {
            Assert.Fail("Div element not found in the parsed document");
        }

        // Assert style computation worked correctly
        Assert.IsNotNull(divElement);
        var style = divElement.GetComputedStyle();
        Assert.That(style, Is.Not.Null, "Computed style should not be null");

        // Verify color and background-color properties
        string colorValue = style.GetPropertyValue("color");
        string bgColorValue = style.GetPropertyValue("background-color");

        Assert.That(colorValue, Does.Contain("rgb(0, 0, 255)").Or.Contain("rgba(0, 0, 255"),
            "Color should be blue");
        Assert.That(bgColorValue, Does.Contain("rgb(255, 255, 0)").Or.Contain("rgba(255, 255, 0"),
            "Background color should be yellow");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        // Dispose the context to clean up resources
        _context?.Dispose();

        // Dispose the service provider if it implements IDisposable
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // For .NET Core 3.0+ ServiceProvider
        if (_serviceProvider is ServiceProvider serviceProvider)
        {
            serviceProvider.Dispose();
        }
    }
}