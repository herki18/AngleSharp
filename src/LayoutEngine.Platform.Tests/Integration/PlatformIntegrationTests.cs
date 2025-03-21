using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Updates;

namespace LayoutEngine.Platform.Tests.Integration;

using Contracts.Platform.Dom.Abstractions;
using Unit.Helpers;

public class PlatformIntegrationTests : IDisposable
{
    private readonly PlatformTestEnvironment _testEnvironment;

    public PlatformIntegrationTests()
    {
        _testEnvironment = new PlatformTestEnvironment();
    }

    [Fact]
    public void DomMutation_ShouldTriggerStyleInvalidation()
    {
        // Arrange
        var eventAggregator = _testEnvironment.EventAggregator;
        var document = new TestDocument();
        var element = document.CreateElement("div");
        ((TestElement)document.DocumentElement).AppendChild(element);
        var domMutationTracker = _testEnvironment.DomMutationTracker;
        domMutationTracker.StartTracking(document);

        // Act
        element.SetAttribute("style", "width: 100px; height: 100px;");
        domMutationTracker.SignalAttributeChanged(element, "style", null, "width: 100px; height: 100px;");

        // Advance time to allow processing
        _testEnvironment.AdvanceTimeAndRunFrame(100);

        // Assert
        var attributeChangedEvents = eventAggregator.GetPublishedEvents<DomAttributeChangedEvent>();
        Assert.Single(attributeChangedEvents);
        Assert.Equal(element, attributeChangedEvents[0].Node);
        Assert.Equal("style", attributeChangedEvents[0].AttributeName);
    }

    [Fact]
    public void ViewportResize_ShouldTriggerViewportChangedEvent()
    {
        // Arrange
        var eventAggregator = _testEnvironment.EventAggregator;
        var viewportDetector = _testEnvironment.ViewportDetector;
        var windowProvider = (TestWindowProvider)_testEnvironment.GetService<IWindowProvider>();
        var window = windowProvider.TestWindow!;

        // Initialize
        viewportDetector.StartTracking();
        var initialWidth = window.InnerWidth;
        var initialHeight = window.InnerHeight;

        // Act
        window.SimulateResize(800, 600);
        viewportDetector.CheckForChanges();

        // Advance time to allow processing
        _testEnvironment.AdvanceTimeAndRunFrame(100);

        // Assert
        var viewportChangedEvents = eventAggregator.GetPublishedEvents<ViewportChangedEvent>();
        Assert.Single(viewportChangedEvents);
        Assert.Equal(initialWidth, viewportChangedEvents[0].OldSize.Width);
        Assert.Equal(initialHeight, viewportChangedEvents[0].OldSize.Height);
        Assert.Equal(800, viewportChangedEvents[0].NewSize.Width);
        Assert.Equal(600, viewportChangedEvents[0].NewSize.Height);
    }

    [Fact]
    public async Task ResourceLoading_ShouldTriggerEvents()
    {
        // Arrange
        var eventAggregator = _testEnvironment.EventAggregator;
        var resourceLoader = _testEnvironment.ResourceLoader;
        var resourceUrl = "https://example.com/image.png";

        // Act
        var resource = await resourceLoader.LoadResourceAsync(resourceUrl);

        // Advance time to allow processing
        _testEnvironment.AdvanceTimeAndRunFrame(100);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(resourceUrl, resource.Url);

        var loadingEvents = eventAggregator.GetPublishedEvents<ResourceLoadingEvent>();
        Assert.Single(loadingEvents);
        Assert.Equal(resourceUrl, loadingEvents[0].Url);

        var loadedEvents = eventAggregator.GetPublishedEvents<ResourceLoadedEvent>();
        Assert.Single(loadedEvents);
        Assert.Equal(resourceUrl, loadedEvents[0].Url);
        Assert.Same(resource, loadedEvents[0].Resource);
    }

    [Fact]
    public void ScheduleUpdates_ShouldTriggerEvents()
    {
        // Arrange
        var eventAggregator = _testEnvironment.EventAggregator;
        var frameScheduler = _testEnvironment.FrameScheduler;
        var updateScheduler = _testEnvironment.GetService<IUpdateScheduler>();
        var document = new TestDocument();
        var element = document.CreateElement("div");
        ((TestElement)document.DocumentElement).AppendChild(element);

        // Create test update
        var update = new TestVisualUpdate(
            Guid.NewGuid(),
            UpdateType.Style,
            element,
            new[] { "width", "height" });

        // Act
        updateScheduler.ScheduleUpdate(update);

        // Advance time to allow processing
        _testEnvironment.AdvanceTimeAndRunFrame(100);

        // Assert
        var styleEvents = eventAggregator.GetPublishedEvents<StyleInvalidatedEvent>();
        Assert.Single(styleEvents);
        Assert.Contains(element, styleEvents[0].Elements);

        var updateProcessedEvents = eventAggregator.GetPublishedEvents<UpdateProcessedEvent>();
        Assert.Single(updateProcessedEvents);
        Assert.Equal(update.Id, updateProcessedEvents[0].Update.Id);
    }

    [Fact]
    public void AnimationFrame_ShouldTriggerFrameEvents()
    {
        // Arrange
        var eventAggregator = _testEnvironment.EventAggregator;
        var frameScheduler = _testEnvironment.FrameScheduler;
        var executed = false;

        // Act
        frameScheduler.RequestAnimationFrame(_ => { executed = true; });

        // Advance time to run frame
        _testEnvironment.AdvanceTimeAndRunFrame(16);

        // Assert
        Assert.True(executed);

        var beginFrameEvents = eventAggregator.GetPublishedEvents<BeginFrameEvent>();
        Assert.Single(beginFrameEvents);

        var endFrameEvents = eventAggregator.GetPublishedEvents<EndFrameEvent>();
        Assert.Single(endFrameEvents);
    }

    [Fact]
    public void FullPlatformCycle_ShouldWorkCorrectly()
    {
        // This test verifies a full cycle of DOM changes → style invalidation → layout invalidation → render

        // Arrange
        var eventAggregator = _testEnvironment.EventAggregator;
        var domMutationTracker = _testEnvironment.DomMutationTracker;
        var document = new TestDocument();
        var element = document.CreateElement("div");
        ((TestElement)document.DocumentElement).AppendChild(element);

        domMutationTracker.StartTracking(document);

        // Act - simulate a style change that would trigger the whole pipeline
        element.SetAttribute("style", "width: 200px; height: 150px;");
        domMutationTracker.SignalAttributeChanged(element, "style", null, "width: 200px; height: 150px;");

        // Trigger style invalidation
        var styleInvalidation = new StyleInvalidatedEvent(new[] { element });
        eventAggregator.Publish(styleInvalidation);

        // Trigger layout invalidation
        var layoutInvalidation = new LayoutInvalidatedEvent(new[] { element });
        eventAggregator.Publish(layoutInvalidation);

        // Trigger render invalidation
        var renderInvalidation = new RenderInvalidatedEvent(null);
        eventAggregator.Publish(renderInvalidation);

        // Advance time to allow processing
        _testEnvironment.AdvanceTimeAndRunFrame(100);

        // Assert - verify all events were published in sequence
        var attributeChangedEvents = eventAggregator.GetPublishedEvents<DomAttributeChangedEvent>();
        Assert.Single(attributeChangedEvents);

        var styleEvents = eventAggregator.GetPublishedEvents<StyleInvalidatedEvent>();
        Assert.Single(styleEvents);

        var layoutEvents = eventAggregator.GetPublishedEvents<LayoutInvalidatedEvent>();
        Assert.Single(layoutEvents);

        var renderEvents = eventAggregator.GetPublishedEvents<RenderInvalidatedEvent>();
        Assert.Single(renderEvents);
    }

    public void Dispose()
    {
        _testEnvironment.Dispose();
    }
}

// Helper class for testing