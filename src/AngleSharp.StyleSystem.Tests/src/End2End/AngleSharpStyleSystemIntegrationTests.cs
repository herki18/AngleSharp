namespace AngleSharp.StyleSystem.Tests.End2End;

using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.StyleSystem;
using AngleSharp.StyleSystem.Services;
using AngleSharp.StyleSystem.DependencyInjection;
using Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class AngleSharpStyleSystemIntegrationTests
{
    private IBrowsingContext _context;
    private IDocument _document;
    private ServiceProvider _serviceProvider;
    [OneTimeSetUp]
    public async Task SetupFixture()
    {
        // Create AngleSharp configuration with CSS support
        var serviceCollection = new ServiceCollection();

        var config = Configuration.Default
            .WithCss()
            .WithRenderDevice()
            .WithDefaultLoader();

        _context = BrowsingContext.New(config);
        serviceCollection.AddAngleSharpServices(_context);

        serviceCollection.AddStyleSystem(options => {
            options.EnableOptimization = true;
            options.BatchSize = 200;
            options.UpdateStylesImmediately = true;
        });

        var list = serviceCollection.ToList();

        _serviceProvider = serviceCollection.BuildServiceProvider();

        _context.RegisterStyleSystemServices(_serviceProvider);
        // Not like this
        // _context.UseStyleSystem(options => {
        //     options.EnableOptimization = true;
        //     options.BatchSize = 200;
        //     options.UpdateStylesImmediately = true;
        // });



        // _context.RegisterStyleSystemServices();

        var styleSystemProvider = _serviceProvider.GetRequiredService<StyleSystemService>();
        Assert.That(styleSystemProvider, Is.Not.Null, "StyleSystem service should be available");
        Assert.That(styleSystemProvider.IsInitialized, Is.True, "StyleSystem should be initialized");



        // Verify StyleSystem is properly initialized using the extension method
        var styleSystem = _context.GetStyleSystem();
        Assert.That(styleSystem, Is.Not.Null, "StyleSystem service should be available");
        Assert.That(styleSystem.IsInitialized, Is.True, "StyleSystem should be initialized");

        // Load a test document
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

        // Load the document
        _document = await _context.OpenAsync(req => req.Content(html));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Dispose the service provider to clean up resources
        _serviceProvider?.Dispose();
    }

    [Test]
    public void ComputeStyles_WithStyleSystem_ShouldReturnCorrectComputedValues()
    {
        // Arrange - Get the test element
        var testElement = _document.GetElementById("test-element");
        Assert.That(testElement, Is.Not.Null, "Test element should exist");

        // The GetComputedStyle extension method uses StyleSystem behind the scenes
        var computedStyle = testElement.GetComputedStyle();

        // Assert - Verify style values using constraints
        Assert.That(computedStyle, Is.Not.Null, "Computed style should not be null");

        // Check display property
        Assert.That(computedStyle.Display, Is.EqualTo(DisplayMode.Block), "Display should be block");

        // Check dimensions
        Assert.That(computedStyle.GetPropertyValue("width"), Does.Contain("200px"), "Width should be 200px");
        Assert.That(computedStyle.GetPropertyValue("height"), Does.Contain("100px"), "Height should be 100px");

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
    public Task AngleSharpCssParser_ShouldIntegrateWithStyleSystem()
    {
        // Get a CSS parser from the context - still using GetService for built-in AngleSharp services
        var cssParser = _context.GetService<ICssParser>();
        Assert.That(cssParser, Is.Not.Null, "CSS Parser should be available");

        // Create a new style
        var style = cssParser.ParseDeclaration("background-color: yellow; color: blue;");
        Assert.That(style, Is.Not.Null, "Style should be parsed successfully");

        // Apply the style inline to a new div element
        var div = _document.CreateElement("div");
        div.SetAttribute("style", style.CssText);
        Assert.IsNotNull(_document.Body);
        _document.Body.AppendChild(div);

        // Get computed style using the extension method
        var computedStyle = div.GetComputedStyle();
        Assert.That(computedStyle, Is.Not.Null, "Computed style should not be null");

        // Check the applied styles
        string colorValue = computedStyle.GetPropertyValue("color");
        string bgColorValue = computedStyle.GetPropertyValue("background-color");

        Assert.That(colorValue, Does.Contain("rgb(0, 0, 255)").Or.Contain("rgba(0, 0, 255"),
            "Color should be blue");
        Assert.That(bgColorValue, Does.Contain("rgb(255, 255, 0)").Or.Contain("rgba(255, 255, 0"),
            "Background color should be yellow");
        return Task.CompletedTask;
    }

    [Test]
    public void StyleSystem_ShouldBeAccessibleThroughDifferentExtensionMethods()
    {
        // Test GetStyleSystemService with interface
        var styleEngine = _context.GetStyleSystemService<IStyleEngine>();
        Assert.That(styleEngine, Is.Not.Null, "Should be able to get style engine through GetStyleSystemService");

        // Test direct extension method on element
        var element = _document.GetElementById("test-element");
        Assert.IsNotNull(element);
        var computedStyle = element.GetComputedStyle();
        Assert.That(computedStyle, Is.Not.Null, "Should be able to get computed style directly from element");

        // Test context style operations
        _context.RecalculateStyles();
        var metrics = _context.GetStyleOptimizationMetrics();
        Assert.That(metrics, Is.Not.Null, "Should be able to get optimization metrics");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        // Dispose the context to clean up resources
        _context?.Dispose();
    }
}