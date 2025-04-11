using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.Contracts.StyleSystem;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Aggregation;

namespace LayoutEngine.Tests;

using Microsoft.Extensions.Logging;
using Contracts.LayoutSystem;

public class LayoutEngineMainEndToEndTests : IDisposable
{
    private ServiceProvider _serviceProvider;
    private ILayoutEngineMain _layoutEngine;
    private IEventAggregator _eventAggregator;

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
        services.AddLayoutEngine(options =>
        {
            options.DevicePixelRatio = 1.0f;
            options.MaxWorkerThreads = 2;
            options.StyleCacheSize = 1000;
            options.LayoutCacheSize = 500;
            options.ParallelStyleComputation = false; // Simplify testing
        });

        // Build the service provider
        _serviceProvider = services.BuildServiceProvider();

        // Resolve the layout engine and event aggregator
        _layoutEngine = _serviceProvider.GetRequiredService<ILayoutEngineMain>();
        _eventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
    }

    public void Dispose()
    {
        // Dispose the layout engine
        _layoutEngine.Dispose();

        // Dispose the service provider
        _serviceProvider.Dispose();
    }

    [Fact]
    public void GetComputedStyle_ForH1_ShouldReturnMockedH1Styles()
    {
        // Arrange
        var document = _layoutEngine.Open(SimpleHtml);
        _layoutEngine.ProcessFullDocument();

        var h1Element = document.QuerySelector("h1");
        Assert.NotNull(h1Element); // Ensure element exists

        // Act
        var computedStyle = _layoutEngine.GetComputedStyle(h1Element);

        // Assert
        Assert.NotNull(computedStyle);

        // --- Assert specific values from the MOCK ComputedStyle for H1 ---
        // Values defined in LayoutEngine.StyleSystem.ComputedStyle constructor
        Assert.True(computedStyle.HasProperty("font-weight"), "Mock should provide font-weight for h1");
        Assert.Equal("bold", computedStyle.GetValue("font-weight"));

        Assert.True(computedStyle.HasProperty("font-size"), "Mock should provide font-size for h1");
        Assert.Equal("32px", computedStyle.GetValue("font-size")); // Mock specific value for h1

        Assert.True(computedStyle.HasProperty("margin-bottom"), "Mock should provide margin-bottom for h1");
        Assert.Equal("16px", computedStyle.GetValue("margin-bottom"));

        Assert.True(computedStyle.HasProperty("display"), "Mock should provide display");
        Assert.Equal("block", computedStyle.GetValue("display")); // Default mock value

        // Optional: Assert the type if you want to be very specific
        Assert.IsType<LayoutEngine.StyleSystem.ComputedStyle>(computedStyle);
    }

    [Fact]
    public void GetComputedStyle_ForParagraph_ShouldReturnMockedParagraphStyles()
    {
        // Arrange
        var document = _layoutEngine.Open(SimpleHtml);
        _layoutEngine.ProcessFullDocument();

        var pElement = document.QuerySelector("p");
        Assert.NotNull(pElement);

        // Act
        var computedStyle = _layoutEngine.GetComputedStyle(pElement);

        // Assert
        Assert.NotNull(computedStyle);

        // --- Assert specific values from the MOCK ComputedStyle for P ---
        Assert.True(computedStyle.HasProperty("margin-bottom"), "Mock should provide margin-bottom for p");
        Assert.Equal("16px", computedStyle.GetValue("margin-bottom")); // Mock specific value for p

        Assert.True(computedStyle.HasProperty("font-size"), "Mock should provide font-size");
        Assert.Equal("16px", computedStyle.GetValue("font-size")); // Default mock value

        Assert.True(computedStyle.HasProperty("color"), "Mock should provide color");
        Assert.Equal("rgba(0, 0, 0, 1)", computedStyle.GetValue("color")); // Default mock value
    }

    [Fact]
    public void GetComputedStyle_ForAnchor_ShouldReturnMockedAnchorStyles()
    {
        // Arrange
        var document = _layoutEngine.Open(SimpleHtml);
        _layoutEngine.ProcessFullDocument();

        var aElement = document.QuerySelector("a");
        Assert.NotNull(aElement);

        // Act
        var computedStyle = _layoutEngine.GetComputedStyle(aElement);

        // Assert
        Assert.NotNull(computedStyle);

        // --- Assert specific values from the MOCK ComputedStyle for A ---
        Assert.True(computedStyle.HasProperty("color"), "Mock should provide color for a");
        Assert.Equal("rgba(0, 0, 255, 1)", computedStyle.GetValue("color")); // Mock specific value for a

        Assert.True(computedStyle.HasProperty("text-decoration"), "Mock should provide text-decoration for a");
        Assert.Equal("underline", computedStyle.GetValue("text-decoration")); // Mock specific value for a
    }

    [Fact]
    public void GetComputedStyle_ForSpan_ShouldReturnMockedSpanStyles()
    {
        // Arrange
        var document = _layoutEngine.Open(SimpleHtml);
        _layoutEngine.ProcessFullDocument();

        var spanElement = document.QuerySelector("span");
        Assert.NotNull(spanElement);

        // Act
        var computedStyle = _layoutEngine.GetComputedStyle(spanElement);

        // Assert
        Assert.NotNull(computedStyle);

        // --- Assert specific values from the MOCK ComputedStyle for SPAN ---
        Assert.True(computedStyle.HasProperty("display"), "Mock should provide display for span");
        Assert.Equal("inline", computedStyle.GetValue("display")); // Mock specific value for span
    }

    [Fact]
    public void EndToEnd_DocumentLoading_ShouldRenderCompletePage()
    {
        // Act
        var document = _layoutEngine.Open(SimpleHtml);

        // Assert - Verify document was loaded
        Assert.NotNull(document);
        Assert.NotNull(document.Body);
        Assert.NotNull(document.Head);
        Assert.Equal("Test Document", document.Title);

        // Process full document
        _layoutEngine.ProcessFullDocument();

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
        var computedStyle = _layoutEngine.GetComputedStyle(h1);
        Assert.NotNull(computedStyle);
        Assert.True(computedStyle.HasProperty("color"));

        // Verify layout computation works
        var layoutBox = _layoutEngine.GetLayoutBox(paragraph);
        Assert.NotNull(layoutBox);
        Assert.True(layoutBox.Width > 0);
        Assert.True(layoutBox.Height > 0);

        // Test viewport resizing
        _layoutEngine.SetViewportSize(1024, 768);
        _layoutEngine.ProcessUpdates();

        // Verify element lookup by position
        var elementAtPoint = _layoutEngine.ElementFromPoint(
            layoutBox.X + layoutBox.Width / 2,
            layoutBox.Y + layoutBox.Height / 2
        );
        Assert.NotNull(elementAtPoint);

        // Test adding additional stylesheet
        var additionalCss = "body { background-color: #f0f0f0; }";
        var styleId = _layoutEngine.AddStyleSheet(additionalCss, StyleSheetOrigin.Author);
        Assert.False(string.IsNullOrEmpty(styleId));

        // Update processing for style changes
        _layoutEngine.ProcessUpdates();

        // Verify we can gracefully shut down
        _layoutEngine.Shutdown();
        Assert.Equal(DocumentLifecyclePhase.Disposed, _layoutEngine.CurrentPhase);
    }

    [Fact]
    public void EndToEnd_ComplexLayout_ShouldCalculateNestedLayout()
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
                    <div id='parent' class='parent'>
                        <div id='first-child' class='child'>
                            <h2>First Column</h2>
                            <p>This is the first column content.</p>
                        </div>
                        <div id='second-child' class='child'>
                            <h2>Second Column</h2>
                            <p>This is the second column content.</p>
                        </div>
                        <div id='third-child' class='footer'>Footer content here</div>
                    </div>
                </body>
                </html>";

        // Act
        var document = _layoutEngine.Open(complexHtml);
        _layoutEngine.ProcessFullDocument();

        // Assert
        var parent = document.QuerySelector(".parent");
        Assert.NotNull(parent);

        var children = document.QuerySelectorAll(".child");
        Assert.Equal(2, children.Length);

        var footer = document.QuerySelector(".footer");
        Assert.NotNull(footer);

        // Get layout boxes
        var parentBox = _layoutEngine.GetLayoutBox(parent);
        var firstChildBox = _layoutEngine.GetLayoutBox(children[0]);
        var secondChildBox = _layoutEngine.GetLayoutBox(children[1]);
        var footerBox = _layoutEngine.GetLayoutBox(footer);

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

    [Fact]
    public void EndToEnd_StyleChange_ShouldUpdateComputedStyles()
    {
        // Load the document
        var document = _layoutEngine.Open(SimpleHtml);
        _layoutEngine.ProcessFullDocument();

        // Get the paragraph element
        var paragraph = document.QuerySelector("p");
        Assert.NotNull(paragraph);

        // Get the initial style and layout
        var initialStyle = _layoutEngine.GetComputedStyle(paragraph);
        var initialLayout = _layoutEngine.GetLayoutBox(paragraph);

        // Track update events
        var styleUpdatedCount = 0;
        var layoutUpdatedCount = 0;

        var styleSubscription = _eventAggregator.Subscribe<StyleComputedEvent>(e =>
        {
            if (e.Elements.Contains(paragraph))
                styleUpdatedCount++;
        });

        var layoutSubscription = _eventAggregator.Subscribe<LayoutUpdatedEvent>(e =>
        {
            if (e.UpdatedElements.Contains(paragraph))
                layoutUpdatedCount++;
        });

        try
        {
            // Make a style change that should trigger layout
            paragraph.SetAttribute("style", "font-size: 24px; margin: 30px;");

            // Trigger updates - since our system is now synchronous, this should process immediately
            _layoutEngine.ProcessUpdates();

            // Get the updated style and layout
            var updatedStyle = _layoutEngine.GetComputedStyle(paragraph);
            var updatedLayout = _layoutEngine.GetLayoutBox(paragraph);

            // Verify style changed
            Assert.NotEqual(initialStyle.GetValue("font-size"), updatedStyle.GetValue("font-size"));

            // Verify layout changed
            Assert.NotEqual(initialLayout.Height, updatedLayout.Height);

            // Verify events were triggered
            Assert.True(styleUpdatedCount > 0, "Style update event should have been triggered");
            Assert.True(layoutUpdatedCount > 0, "Layout update event should have been triggered");
        }
        finally
        {
            _eventAggregator.Unsubscribe(styleSubscription);
            _eventAggregator.Unsubscribe(layoutSubscription);
        }
    }
}