namespace LayoutEngine.Contracts.Platform.Dom.Abstractions;

using System;
using LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Factory interface for creating mutation observers
/// </summary>
public interface IMutationObserverFactory
{
    /// <summary>
    /// Creates a new mutation observer
    /// </summary>
    /// <param name="callback">Callback to execute when mutations occur</param>
    /// <returns>Mutation observer instance</returns>
    IMutationObserver Create(Action<MutationRecord[]> callback);
}