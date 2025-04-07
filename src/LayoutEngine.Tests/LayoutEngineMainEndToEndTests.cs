using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.Contracts.StyleSystem;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Aggregation;

namespace LayoutEngine.Tests;

using Microsoft.Extensions.Logging;
using System.Threading;
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
        services.AddLayoutEngine(options => {
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

    /// <summary>
    /// Waits for update processing to complete via the event system.
    /// </summary>
    private async Task WaitForUpdatesAsync(UpdateType updateType, TimeSpan? timeout = null)
    {
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(3);
        var tcs = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource(timeoutValue);

        // Set up cancellation
        cts.Token.Register(() => tcs.TrySetResult(false));

        // Subscribe to the update processed event
        var subscription = _eventAggregator.Subscribe<UpdateProcessedEvent>(e =>
        {
            if (e.Update.Type == updateType || e.Update.Type == UpdateType.Full)
            {
                tcs.TrySetResult(true);
            }
        });

        try
        {
            // Wait for either the event or timeout
            var result = await tcs.Task;
            if (!result)
            {
                // If we timed out, we should also wait a bit to allow for any final processing
                await Task.Delay(50);
            }
        }
        finally
        {
            _eventAggregator.Unsubscribe(subscription);
        }
    }

    /// <summary>
    /// Waits for a specific lifecycle phase change.
    /// </summary>
    private async Task WaitForPhaseAsync(DocumentLifecyclePhase phase, PhaseChangeType changeType, TimeSpan? timeout = null)
    {
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(3);
        var tcs = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource(timeoutValue);

        // Set up cancellation
        cts.Token.Register(() => tcs.TrySetResult(false));

        // Subscribe to the phase changed event
        var subscription = _eventAggregator.Subscribe<PhaseChangedEvent>(e =>
        {
            if (e.Phase == phase && e.ChangeType == changeType)
            {
                tcs.TrySetResult(true);
            }
        });

        try
        {
            // Wait for either the event or timeout
            await tcs.Task;
        }
        finally
        {
            _eventAggregator.Unsubscribe(subscription);
        }
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

        // Wait for the update to be processed
        await WaitForUpdatesAsync(UpdateType.Full);

        // Wait for render ready phase
        if (_layoutEngine.CurrentPhase != DocumentLifecyclePhase.RenderReady)
        {
            await WaitForPhaseAsync(DocumentLifecyclePhase.RenderReady, PhaseChangeType.Enter);
        }

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
        await WaitForUpdatesAsync(UpdateType.Layout);

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

        // Wait for style update to be processed
        await WaitForUpdatesAsync(UpdateType.Style);

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
            await WaitForUpdatesAsync(UpdateType.Full);

            // Wait for render ready phase
            if (_layoutEngine.CurrentPhase != DocumentLifecyclePhase.RenderReady)
            {
                await WaitForPhaseAsync(DocumentLifecyclePhase.RenderReady, PhaseChangeType.Enter);
            }

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
        var document = await _layoutEngine.OpenAsync(complexHtml);
        await _layoutEngine.ProcessFullDocumentAsync();
        await WaitForUpdatesAsync(UpdateType.Full);

        // Wait for layout phase to complete
        if (_layoutEngine.CurrentPhase != DocumentLifecyclePhase.LayoutClean &&
            _layoutEngine.CurrentPhase != DocumentLifecyclePhase.RenderReady)
        {
            await WaitForPhaseAsync(DocumentLifecyclePhase.LayoutClean, PhaseChangeType.Enter);
        }

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

    [Fact]
    public async Task EndToEnd_IncreaseEvents_ShouldDetectAndTriggerOtherUpdates()
    {
        // Load the document
        var document = await _layoutEngine.OpenAsync(SimpleHtml);
        await _layoutEngine.ProcessFullDocumentAsync();
        await WaitForUpdatesAsync(UpdateType.Full);

        // Get the paragraph element
        var paragraph = document.QuerySelector("p");
        Assert.NotNull(paragraph);

        // Get the initial style and layout
        var initialStyle = await _layoutEngine.GetComputedStyleAsync(paragraph);
        var initialLayout = await _layoutEngine.GetLayoutBoxAsync(paragraph);

        // Track update events
        var styleUpdatedCount = 0;
        var layoutUpdatedCount = 0;

        var styleSubscription = _eventAggregator.Subscribe<StyleComputedEvent>(e => {
            if (e.Elements.Contains(paragraph))
                styleUpdatedCount++;
        });

        var layoutSubscription = _eventAggregator.Subscribe<LayoutUpdatedEvent>(e => {
            if (e.UpdatedElements.Contains(paragraph))
                layoutUpdatedCount++;
        });

        try
        {
            // Make a style change that should trigger layout
            paragraph.SetAttribute("style", "font-size: 24px; margin: 30px;");

            // Trigger updates
            await _layoutEngine.ProcessUpdatesAsync();
            await WaitForUpdatesAsync(UpdateType.Layout);

            // Get the updated style and layout
            var updatedStyle = await _layoutEngine.GetComputedStyleAsync(paragraph);
            var updatedLayout = await _layoutEngine.GetLayoutBoxAsync(paragraph);

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