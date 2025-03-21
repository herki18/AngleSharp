namespace LayoutEngine.Platform.DOM.Abstractions;

using System;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Wrapper for MutationObserver that implements IMutationObserver
/// </summary>
public class MutationObserverWrapper : IMutationObserver
{
    private readonly MutationObserver _observer;

    /// <summary>
    /// Creates a new mutation observer wrapper
    /// </summary>
    /// <param name="callback">Callback to execute when mutations occur</param>
    public MutationObserverWrapper(Action<MutationRecord[]> callback)
    {
        _observer = new MutationObserver(callback);
    }

    /// <summary>
    /// Observes mutations on a target node
    /// </summary>
    public void Observe(IDomNode target, MutationObserverInit options)
    {
        _observer.Observe(target, options);
    }

    /// <summary>
    /// Stops observing mutations
    /// </summary>
    public void Disconnect()
    {
        _observer.Disconnect();
    }
}