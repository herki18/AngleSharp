namespace LayoutEngine.Platform.Tests.Helpers;

using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Test resize observer for testing
/// </summary>
public class TestResizeObserver : IResizeObserver
{
    private readonly Action<ResizeObserverEntry[]> _callback;
    private IElement? _target;
    private bool _isConnected;

    public TestResizeObserver(Action<ResizeObserverEntry[]> callback)
    {
        _callback = callback;
    }

    public void Observe(IElement target)
    {
        _target = target;
        _isConnected = true;
    }

    public void Disconnect()
    {
        _isConnected = false;
        _target = null;
    }

    /// <summary>
    /// Simulates a resize for testing
    /// </summary>
    public void SimulateResize(Rectangle contentRect)
    {
        if (_isConnected && _target != null)
        {
            var entry = new ResizeObserverEntry(_target, contentRect);
            _callback(new[] { entry });
        }
    }

    /// <summary>
    /// Gets the target being observed
    /// </summary>
    public IElement? Target => _target;

    /// <summary>
    /// Gets whether the observer is connected
    /// </summary>
    public bool IsConnected => _isConnected;
}