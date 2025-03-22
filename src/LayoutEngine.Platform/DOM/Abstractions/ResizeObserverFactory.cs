namespace LayoutEngine.Platform.DOM.Abstractions;

using System;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

public class ResizeObserverFactory : IResizeObserverFactory
{
    public IMutationObserver Create(Action<MutationRecord[]> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        return new MutationObserverWrapper(callback);
    }

    public IResizeObserver Create(Action<ResizeObserverEntry[]> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        return new ResizeObserverWrapper(callback);
    }
}