namespace LayoutEngine.Contracts.Platform.Dom;

using System;
using AngleSharp.Dom;

/// <summary>
/// Represents a resize observer.
/// </summary>
public class ResizeObserver
{
    private readonly Action<ResizeObserverEntry[]> _callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizeObserver"/> class.
    /// </summary>
    /// <param name="callback">The callback to invoke when resize occurs.</param>
    public ResizeObserver(Action<ResizeObserverEntry[]> callback)
    {
        _callback = callback;
    }

    /// <summary>
    /// Starts observing resize on the specified target.
    /// </summary>
    /// <param name="target">The target to observe.</param>
    public void Observe(IElement target)
    {
        // This would be implemented by platform-specific code
    }

    /// <summary>
    /// Stops observing resize.
    /// </summary>
    public void Disconnect()
    {
        // This would be implemented by platform-specific code
    }
}