using System;
namespace LayoutEngine.Platform.DOM;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;
using Contracts.Platform.Events;
using Infrastructure.EventAggregator.API.Aggregation;
using DOM.Abstractions;

public class ViewportDetector : IViewportDetector, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IResizeObserverFactory _resizeObserverFactory;
    private readonly IWindowProvider _windowProvider;
    private Size _currentViewport;
    private double _devicePixelRatio;
    private bool _isTracking;
    private bool _isDisposed;
    private IResizeObserver? _resizeObserver;
    private IWindow? _window;

    public ViewportDetector(
        IEventAggregator eventAggregator,
        IResizeObserverFactory? resizeObserverFactory = null,
        IWindowProvider? windowProvider = null)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _resizeObserverFactory = resizeObserverFactory ?? new ResizeObserverFactory();
        _windowProvider = windowProvider ?? new DefaultWindowProvider();
    }

    public Size ViewportSize => _currentViewport;
    public double DevicePixelRatio => _devicePixelRatio;

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

    public void StopTracking()
    {
        ThrowIfDisposed();
        if (!_isTracking)
            return;
        _isTracking = false;
        DetachResizeObserver();
    }

    public bool CheckForChanges()
    {
        ThrowIfDisposed();
        var viewportChanged = false;
        var newViewport = GetCurrentViewport();
        if (!newViewport.Equals(_currentViewport))
        {
            var oldViewport = _currentViewport;
            _currentViewport = newViewport;
            _eventAggregator.Publish(new ViewportChangedEvent(oldViewport, newViewport));
            viewportChanged = true;
        }
        var newDpr = GetDevicePixelRatio();
        if (Math.Abs(newDpr - _devicePixelRatio) > 0.001)
        {
            var oldDpr = _devicePixelRatio;
            _devicePixelRatio = newDpr;
            _eventAggregator.Publish(new DevicePixelRatioChangedEvent(oldDpr, newDpr));
            viewportChanged = true;
        }
        return viewportChanged;
    }

    // Changed from private to protected virtual for testability
    protected virtual void AttachResizeObserver()
    {
        _window = _windowProvider.GetWindow();
        if (_window == null)
            return;
        _resizeObserver = _resizeObserverFactory.Create(OnResize);
        _resizeObserver.Observe(_window.Document.DocumentElement);
        _window.AddEventListener("resize", OnWindowResize);
        _window.AddEventListener("orientationchange", OnWindowResize);
    }

    // Changed from private to protected virtual for testability
    protected virtual void DetachResizeObserver()
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

    // Changed from private to protected virtual for testability
    protected virtual void OnResize(ResizeObserverEntry[] entries)
    {
        if (_isDisposed || !_isTracking)
            return;
        CheckForChanges();
    }

    // Changed from private to protected virtual for testability
    protected virtual void OnWindowResize(Event e)
    {
        if (_isDisposed || !_isTracking)
            return;
        CheckForChanges();
    }

    // Changed from private to protected virtual for testability
    protected virtual void UpdateCurrentViewport()
    {
        _currentViewport = GetCurrentViewport();
    }

    // Changed from private to protected virtual for testability
    protected virtual Size GetCurrentViewport()
    {
        var window = _windowProvider.GetWindow();
        if (window == null)
            return new Size(0, 0);
        return new Size(window.InnerWidth, window.InnerHeight);
    }

    // Changed from private to protected virtual for testability
    protected virtual void UpdateDevicePixelRatio()
    {
        _devicePixelRatio = GetDevicePixelRatio();
    }

    // Changed from private to protected virtual for testability
    protected virtual double GetDevicePixelRatio()
    {
        var window = _windowProvider.GetWindow();
        if (window == null)
            return 1.0;
        return window.DevicePixelRatio;
    }

    // For testability - expose window for tests
    internal IWindow? CurrentWindow => _window;

    // For testability - expose tracking state
    internal bool IsTrackingInternal => _isTracking;

    protected virtual void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ViewportDetector));
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        // Call StopTracking before setting _isDisposed to true
        StopTracking();

        // Now mark as disposed
        _isDisposed = true;
    }
}