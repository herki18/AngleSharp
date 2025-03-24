using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.Contracts.StyleSystem;
using LayoutEngine.Contracts.Platform.Lifecycle;

namespace LayoutEngine.Tests;

using Microsoft.Extensions.Logging;

public class LayoutEngineMainEndToEndTests : IDisposable
{
    private ServiceProvider _serviceProvider;
    private ILayoutEngineMain _layoutEngine;

    // Test HTML content
    private const string SimpleHtml = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Test Document</title>
                <style>
                    body { font-family: Arial; margin: 20px; }
                    .container { width: 800px; margin: 0 auto; }
                    h1 { color: #333; font-size: 24px; }
                    p { line-height: 1.5; color: #666; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <h1>Hello World</h1>
                    <p>This is a test paragraph for the layout engine.</p>
                </div>
            </body>
            </html>";

    public LayoutEngineMainEndToEndTests()
    {
        // Create a service collection and configure services using the extension method
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(configure => configure.AddConsole());

        // Add the LayoutEngine with configuration
        services.AddLayoutEngine(options => {
            options.DevicePixelRatio = 1.0f;
            options.MaxWorkerThreads = 2;
            options.StyleCacheSize = 1000;
            options.LayoutCacheSize = 500;
            options.ParallelStyleComputation = false; // Simplify testing
        });

        // Build the service provider
        _serviceProvider = services.BuildServiceProvider();

        // Resolve the layout engine
        _layoutEngine = _serviceProvider.GetRequiredService<ILayoutEngineMain>();
    }

    public void Dispose()
    {
        // Dispose the layout engine
        _layoutEngine.Dispose();

        // Dispose the service provider
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task EndToEnd_DocumentLoading_ShouldRenderCompletePage()
    {
        // Act
        var document = await _layoutEngine.OpenAsync(SimpleHtml);

        // Assert - Verify document was loaded
        Assert.NotNull(document);
        Assert.NotNull(document.Body);
        Assert.NotNull(document.Head);
        Assert.Equal("Test Document", document.Title);

        // Wait for layout processing to complete
        await _layoutEngine.ProcessFullDocumentAsync();

        // Verify basic document structure
        var container = document.QuerySelector(".container");
        Assert.NotNull(container);

        var h1 = document.QuerySelector("h1");
        Assert.NotNull(h1);
        Assert.Equal("Hello World", h1.TextContent.Trim());

        var paragraph = document.QuerySelector("p");
        Assert.NotNull(paragraph);
        Assert.Equal("This is a test paragraph for the layout engine.", paragraph.TextContent.Trim());

        // Verify style computation works
        var computedStyle = await _layoutEngine.GetComputedStyleAsync(h1);
        Assert.NotNull(computedStyle);
        Assert.True(computedStyle.HasProperty("color"));

        // Verify layout computation works
        var layoutBox = await _layoutEngine.GetLayoutBoxAsync(paragraph);
        Assert.NotNull(layoutBox);
        Assert.True(layoutBox.Width > 0);
        Assert.True(layoutBox.Height > 0);

        // Test viewport resizing
        _layoutEngine.SetViewportSize(1024, 768);
        await _layoutEngine.ProcessUpdatesAsync();

        // Verify element lookup by position
        var elementAtPoint = _layoutEngine.ElementFromPoint(
            layoutBox.X + layoutBox.Width / 2,
            layoutBox.Y + layoutBox.Height / 2
        );
        Assert.NotNull(elementAtPoint);

        // Test adding additional stylesheet
        var additionalCss = "body { background-color: #f0f0f0; }";
        var styleId = await _layoutEngine.AddStyleSheetAsync(additionalCss, StyleSheetOrigin.Author);
        Assert.False(string.IsNullOrEmpty(styleId));

        // Verify we can gracefully shut down
        await _layoutEngine.ShutdownAsync();
        Assert.Equal(DocumentLifecyclePhase.Disposed, _layoutEngine.CurrentPhase);
    }

    [Fact]
    public async Task EndToEnd_FileLoading_ShouldLoadAndRenderHtmlFile()
    {
        // Arrange - Create a temporary HTML file
        string tempHtmlPath = Path.GetTempFileName() + ".html";
        File.WriteAllText(tempHtmlPath, SimpleHtml);

        try
        {
            // Act
            var document = await _layoutEngine.OpenFileAsync(tempHtmlPath);

            // Assert
            Assert.NotNull(document);
            Assert.NotNull(document.Body);
            Assert.Equal("Test Document", document.Title);

            // Wait for layout processing
            await _layoutEngine.ProcessFullDocumentAsync();

            // Basic verification
            var h1 = document.QuerySelector("h1");
            Assert.NotNull(h1);
            Assert.Equal("Hello World", h1.TextContent.Trim());

            // Verify style and layout engines are working
            var computedStyle = await _layoutEngine.GetComputedStyleAsync(document.Body);
            Assert.NotNull(computedStyle);

            var layoutBox = await _layoutEngine.GetLayoutBoxAsync(document.Body);
            Assert.NotNull(layoutBox);
            Assert.True(layoutBox.Width > 0);
            Assert.True(layoutBox.Height > 0);
        }
        finally
        {
            // Clean up
            if (File.Exists(tempHtmlPath))
            {
                File.Delete(tempHtmlPath);
            }
        }
    }

    [Fact]
    public async Task EndToEnd_ComplexLayout_ShouldCalculateNestedLayout()
    {
        // Arrange
        string complexHtml = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Complex Layout Test</title>
                    <style>
                        body { margin: 0; padding: 0; font-family: Arial; }
                        .parent { display: block; width: 600px; margin: 20px auto; border: 1px solid #ccc; padding: 20px; }
                        .child { width: 45%; float: left; margin: 10px; padding: 15px; background-color: #f0f0f0; }
                        .footer { clear: both; padding: 10px; background-color: #333; color: white; }
                    </style>
                </head>
                <body>
                    <div class='parent'>
                        <div class='child'>
                            <h2>First Column</h2>
                            <p>This is the first column content.</p>
                        </div>
                        <div class='child'>
                            <h2>Second Column</h2>
                            <p>This is the second column content.</p>
                        </div>
                        <div class='footer'>Footer content here</div>
                    </div>
                </body>
                </html>";

        // Act
        var document = await _layoutEngine.OpenAsync(complexHtml);
        await _layoutEngine.ProcessFullDocumentAsync();

        // Assert
        var parent = document.QuerySelector(".parent");
        Assert.NotNull(parent);

        var children = document.QuerySelectorAll(".child");
        Assert.Equal(2, children.Length);

        var footer = document.QuerySelector(".footer");
        Assert.NotNull(footer);

        // Get layout boxes
        var parentBox = await _layoutEngine.GetLayoutBoxAsync(parent);
        var firstChildBox = await _layoutEngine.GetLayoutBoxAsync(children[0]);
        var secondChildBox = await _layoutEngine.GetLayoutBoxAsync(children[1]);
        var footerBox = await _layoutEngine.GetLayoutBoxAsync(footer);

        // Verify layout relationships
        Assert.True(parentBox.Width > 0);
        Assert.True(parentBox.Height > 0);

        // Children should be positioned inside parent
        Assert.True(firstChildBox.X >= parentBox.X);
        Assert.True(firstChildBox.Y >= parentBox.Y);

        // First and second child should be side by side (floated)
        Assert.True(Math.Abs(firstChildBox.Y - secondChildBox.Y) < 5); // Should be roughly at same Y position
        Assert.True(secondChildBox.X > firstChildBox.X); // Second child should be to the right

        // Footer should be below the children
        Assert.True(footerBox.Y > firstChildBox.Y + firstChildBox.Height);
        Assert.True(footerBox.Y > secondChildBox.Y + secondChildBox.Height);
    }
}