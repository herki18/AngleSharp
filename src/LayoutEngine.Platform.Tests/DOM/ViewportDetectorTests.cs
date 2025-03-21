using LayoutEngine.Platform.DOM;
using LayoutEngine.Platform.Tests.Helpers;
using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Contracts.Platform.Events;

namespace LayoutEngine.Platform.Tests.DOM;

public class ViewportDetectorTests : IDisposable
{
    private readonly TestEventAggregator _eventAggregator;
    private readonly TestResizeObserverFactory _resizeObserverFactory;
    private readonly TestWindowProvider _windowProvider;
    private readonly ViewportDetector _viewportDetector;
    private readonly TestWindow _window;

    public ViewportDetectorTests()
    {
        _eventAggregator = new TestEventAggregator();
        _resizeObserverFactory = new TestResizeObserverFactory();
        _windowProvider = new TestWindowProvider();
        _window = new TestWindow();
        _windowProvider.SetWindow(_window);
        _viewportDetector = new ViewportDetector(_eventAggregator, _resizeObserverFactory, _windowProvider);
    }

    [Fact]
    public void StartTracking_ShouldInitializeCorrectly()
    {
        // Act
        _viewportDetector.StartTracking();

        // Assert
        Assert.True(_viewportDetector.IsTrackingInternal);
        Assert.NotNull(_viewportDetector.CurrentWindow);
        Assert.Equal(_window, _viewportDetector.CurrentWindow);
    }

    [Fact]
    public void StopTracking_ShouldCleanupCorrectly()
    {
        // Arrange
        _viewportDetector.StartTracking();

        // Act
        _viewportDetector.StopTracking();

        // Assert
        Assert.False(_viewportDetector.IsTrackingInternal);
        Assert.Null(_viewportDetector.CurrentWindow);
    }

    [Fact]
    public void ViewportSize_ShouldMatchWindowSize()
    {
        // Arrange
        _window.InnerWidth = 1024;
        _window.InnerHeight = 768;
        _viewportDetector.StartTracking();

        // Act
        var viewportSize = _viewportDetector.ViewportSize;

        // Assert
        Assert.Equal(1024, viewportSize.Width);
        Assert.Equal(768, viewportSize.Height);
    }

    [Fact]
    public void DevicePixelRatio_ShouldMatchWindowDevicePixelRatio()
    {
        // Arrange
        _window.DevicePixelRatio = 2.0;
        _viewportDetector.StartTracking();

        // Act
        var dpr = _viewportDetector.DevicePixelRatio;

        // Assert
        Assert.Equal(2.0, dpr);
    }

    [Fact]
    public void CheckForChanges_WhenViewportChanged_ShouldReturnTrue()
    {
        // Arrange
        _window.InnerWidth = 1024;
        _window.InnerHeight = 768;
        _viewportDetector.StartTracking();

        // Change window size
        _window.InnerWidth = 800;
        _window.InnerHeight = 600;

        // Act
        bool result = _viewportDetector.CheckForChanges();

        // Assert
        Assert.True(result);
        Assert.Equal(800, _viewportDetector.ViewportSize.Width);
        Assert.Equal(600, _viewportDetector.ViewportSize.Height);

        // Verify event was published
        var publishedEvents = _eventAggregator.GetPublishedEvents<ViewportChangedEvent>();
        Assert.Single(publishedEvents);
        var viewportEvent = publishedEvents[0];
        Assert.Equal(1024, viewportEvent.OldSize.Width);
        Assert.Equal(768, viewportEvent.OldSize.Height);
        Assert.Equal(800, viewportEvent.NewSize.Width);
        Assert.Equal(600, viewportEvent.NewSize.Height);
    }

    [Fact]
    public void CheckForChanges_WhenDevicePixelRatioChanged_ShouldReturnTrue()
    {
        // Arrange
        _window.DevicePixelRatio = 1.0;
        _viewportDetector.StartTracking();

        // Change device pixel ratio
        _window.DevicePixelRatio = 2.0;

        // Act
        bool result = _viewportDetector.CheckForChanges();

        // Assert
        Assert.True(result);
        Assert.Equal(2.0, _viewportDetector.DevicePixelRatio);

        // Verify event was published
        var publishedEvents = _eventAggregator.GetPublishedEvents<DevicePixelRatioChangedEvent>();
        Assert.Single(publishedEvents);
        var dprEvent = publishedEvents[0];
        Assert.Equal(1.0, dprEvent.OldDevicePixelRatio);
        Assert.Equal(2.0, dprEvent.NewDevicePixelRatio);
    }

    [Fact]
    public void CheckForChanges_WhenNoChanges_ShouldReturnFalse()
    {
        // Arrange
        _viewportDetector.StartTracking();

        // Act
        bool result = _viewportDetector.CheckForChanges();

        // Assert
        Assert.False(result);
        Assert.Empty(_eventAggregator.GetPublishedEvents<ViewportChangedEvent>());
        Assert.Empty(_eventAggregator.GetPublishedEvents<DevicePixelRatioChangedEvent>());
    }

    [Fact]
    public void OnResize_ShouldTriggerCheckForChanges()
    {
        // Arrange
        _viewportDetector.StartTracking();

        // Act - Simulate window resize event
        _window.SimulateResize(800, 600);

        // Assert
        Assert.Equal(800, _viewportDetector.ViewportSize.Width);
        Assert.Equal(600, _viewportDetector.ViewportSize.Height);

        // Verify event was published
        var publishedEvents = _eventAggregator.GetPublishedEvents<ViewportChangedEvent>();
        Assert.Single(publishedEvents);
    }

    public void Dispose()
    {
        _viewportDetector.Dispose();
    }
}