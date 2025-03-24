namespace AngleSharp.Dom;

using AngleSharp.Browser;
using System;
using System.Collections.Generic;

/// <summary>
/// Interface for coupling mutation events to mutation observers and the event loop.
/// </summary>
public interface IMutationHost
{
    /// <summary>
    /// Gets the collection of registered mutation observers.
    /// </summary>
    IEnumerable<IMutationObserver> Observers { get; }

    /// <summary>
    /// Registers a mutation observer.
    /// </summary>
    /// <param name="observer">The observer to register.</param>
    void Register(IMutationObserver observer);

    /// <summary>
    /// Unregisters a mutation observer.
    /// </summary>
    /// <param name="observer">The observer to unregister.</param>
    void Unregister(IMutationObserver observer);

    /// <summary>
    /// Schedules a callback for all registered observers.
    /// </summary>
    void ScheduleCallback();
}