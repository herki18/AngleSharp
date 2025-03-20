using System;

namespace LayoutEngine.Platform.DOM;

using Contracts.Platform.Dom;
using Contracts.Platform.Events;
using Infrastructure.EventAggregator.API.Aggregation;

/// <summary>
/// Tracks viewport dimensions and changes.
/// Publishes events when the viewport changes.
/// </summary>
public sealed class ViewportDetector : IViewportDetector, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private Size _currentViewport;
    private double _devicePixelRatio;
    private bool _isTracking;
    private bool _isDisposed;
    private ResizeObserver? _resizeObserver;
    private IWindow? _window;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewportDetector"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing viewport events.</param>
    public ViewportDetector(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    /// <summary>
    /// Gets the current viewport size.
    /// </summary>
    public Size ViewportSize => _currentViewport;

    /// <summary>
    /// Gets the current device pixel ratio.
    /// </summary>
    public double DevicePixelRatio => _devicePixelRatio;

    /// <summary>
    /// Starts tracking viewport changes.
    /// </summary>
    public void StartTracking()
    {
        ThrowIfDisposed();

        if (_isTracking)
            return;

        _isTracking = true;
        AttachResizeObserver();
        UpdateCurrentViewport();
        UpdateDevicePixelRatio();
    }

    /// <summary>
    /// Stops tracking viewport changes.
    /// </summary>
    public void StopTracking()
    {
        ThrowIfDisposed();

        if (!_isTracking)
            return;

        _isTracking = false;
        DetachResizeObserver();
    }

    /// <summary>
    /// Forces a check for viewport changes.
    /// </summary>
    /// <returns>True if the viewport changed, otherwise false.</returns>
    public bool CheckForChanges()
    {
        ThrowIfDisposed();

        var viewportChanged = false;

        // Check viewport size
        var newViewport = GetCurrentViewport();
        if (!newViewport.Equals(_currentViewport))
        {
            var oldViewport = _currentViewport;
            _currentViewport = newViewport;

            // Publish viewport changed event
            _eventAggregator.Publish(new ViewportChangedEvent(oldViewport, newViewport));
            viewportChanged = true;
        }

        // Check device pixel ratio
        var newDpr = GetDevicePixelRatio();
        if (Math.Abs(newDpr - _devicePixelRatio) > 0.001)
        {
            var oldDpr = _devicePixelRatio;
            _devicePixelRatio = newDpr;

            // Publish DPR changed event
            _eventAggregator.Publish(new DevicePixelRatioChangedEvent(oldDpr, newDpr));
            viewportChanged = true;
        }

        return viewportChanged;
    }

    /// <summary>
    /// Attaches the resize observer to the window.
    /// </summary>
    private void AttachResizeObserver()
    {
        _window = GetWindow();
        if (_window == null)
            return;

        _resizeObserver = new ResizeObserver(OnResize);
        _resizeObserver.Observe(_window.Document.DocumentElement);

        // Also attach to orientationchange and resize events for redundancy
        _window.AddEventListener("resize", OnWindowResize);
        _window.AddEventListener("orientationchange", OnWindowResize);
    }

    /// <summary>
    /// Detaches the resize observer from the window.
    /// </summary>
    private void DetachResizeObserver()
    {
        _resizeObserver?.Disconnect();
        _resizeObserver = null;

        if (_window != null)
        {
            _window.RemoveEventListener("resize", OnWindowResize);
            _window.RemoveEventListener("orientationchange", OnWindowResize);
            _window = null;
        }
    }

    /// <summary>
    /// Handles resize observer entries.
    /// </summary>
    private void OnResize(ResizeObserverEntry[] entries)
    {
        if (_isDisposed || !_isTracking)
            return;

        CheckForChanges();
    }

    /// <summary>
    /// Handles window resize and orientation change events.
    /// </summary>
    private void OnWindowResize(Event e)
    {
        if (_isDisposed || !_isTracking)
            return;

        CheckForChanges();
    }

    /// <summary>
    /// Updates the current viewport size.
    /// </summary>
    private void UpdateCurrentViewport()
    {
        _currentViewport = GetCurrentViewport();
    }

    /// <summary>
    /// Gets the current viewport size.
    /// </summary>
    private Size GetCurrentViewport()
    {
        var window = GetWindow();
        if (window == null)
            return new Size(0, 0);

        return new Size(window.InnerWidth, window.InnerHeight);
    }

    /// <summary>
    /// Updates the current device pixel ratio.
    /// </summary>
    private void UpdateDevicePixelRatio()
    {
        _devicePixelRatio = GetDevicePixelRatio();
    }

    /// <summary>
    /// Gets the current device pixel ratio.
    /// </summary>
    private double GetDevicePixelRatio()
    {
        var window = GetWindow();
        if (window == null)
            return 1.0;

        return window.DevicePixelRatio;
    }

    /// <summary>
    /// Gets the window.
    /// </summary>
    private IWindow? GetWindow()
    {
        // This would be implemented differently based on the actual DOM abstraction
        // For now, we'll just return null
        return null;
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ViewportDetector));
        }
    }

    /// <summary>
    /// Disposes the ViewportDetector and stops tracking.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        StopTracking();
    }
}