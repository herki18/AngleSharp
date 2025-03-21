namespace LayoutEngine.Platform.DOM.Abstractions;

using System;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Default implementation of ResizeObserver factory
/// </summary>
public class ResizeObserverFactory : IResizeObserverFactory
{
    /// <summary>
    /// Creates a new resize observer
    /// </summary>
    public IResizeObserver Create(Action<ResizeObserverEntry[]> callback)
    {
        return new ResizeObserverWrapper(callback);
    }
}