namespace LayoutEngine.Platform.DOM.Abstractions;

using System;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Wrapper for ResizeObserver that implements IResizeObserver
/// </summary>
public class ResizeObserverWrapper : IResizeObserver
{
    private readonly ResizeObserver _observer;

    /// <summary>
    /// Creates a new resize observer wrapper
    /// </summary>
    /// <param name="callback">Callback to execute when resize events occur</param>
    public ResizeObserverWrapper(Action<ResizeObserverEntry[]> callback)
    {
        _observer = new ResizeObserver(callback);
    }

    /// <summary>
    /// Observes resize events on a target element
    /// </summary>
    public void Observe(IElement target)
    {
        _observer.Observe(target);
    }

    /// <summary>
    /// Stops observing resize events
    /// </summary>
    public void Disconnect()
    {
        _observer.Disconnect();
    }
}