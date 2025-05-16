namespace LayoutEngine.Core;

using System;
using AngleSharp.Dom;

/// <summary>
/// Factory for creating AngleSharp MutationObserver instances.
/// </summary>
public class MutationObserverFactory : IMutationObserverFactory
{
    /// <summary>
    /// Creates a new AngleSharp MutationObserver.
    /// </summary>
    /// <param name="callback">The callback to invoke when mutations occur, passing both the mutation records and the observer itself.</param>
    /// <returns>A new MutationObserver instance.</returns>
    public IMutationObserver Create(Action<IMutationRecord[], IMutationObserver> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        // Create and return an AngleSharp MutationObserver with the native callback
        return new MutationObserver((records, observer) =>
        {
            callback(records, observer);
        });
    }
}