namespace LayoutEngine.Platform.DOM.Abstractions;

using System;
using AngleSharp.Dom;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

public class ResizeObserverWrapper : IResizeObserver
{
    private readonly ResizeObserver _observer;

    public ResizeObserverWrapper(Action<ResizeObserverEntry[]> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        _observer = new ResizeObserver(callback);
    }

    public void Observe(IElement target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        _observer.Observe(target);
    }

    public void Disconnect()
    {
        _observer.Disconnect();
    }
}