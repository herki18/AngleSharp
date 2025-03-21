namespace LayoutEngine.Platform.DOM.Abstractions;

using System;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;

/// <summary>
/// Default implementation of MutationObserver factory
/// </summary>
public class MutationObserverFactory : IMutationObserverFactory
{
    /// <summary>
    /// Creates a new mutation observer
    /// </summary>
    public IMutationObserver Create(Action<MutationRecord[]> callback)
    {
        return new MutationObserverWrapper(callback);
    }
}