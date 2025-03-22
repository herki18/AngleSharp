namespace LayoutEngine.Platform.DOM.Abstractions;

using System;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

public class MutationObserverWrapper : IMutationObserver
{
    private readonly MutationObserver _observer;

    public MutationObserverWrapper(Action<MutationRecord[]> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        _observer = new MutationObserver(callback);
    }

    public void Observe(IDomNode target, MutationObserverInit options)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _observer.Observe(target, options);
    }

    public void Disconnect()
    {
        _observer.Disconnect();
    }
}