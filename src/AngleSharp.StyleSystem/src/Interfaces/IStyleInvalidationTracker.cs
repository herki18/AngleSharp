namespace AngleSharp.StyleSystem.Interfaces;

using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Observers;

/// <summary>
/// Tracks which elements need style recalculation.
/// </summary>
public interface IStyleInvalidationTracker
{
    /// <summary>
    /// Adds an observer to receive invalidation notifications.
    /// </summary>
    /// <param name="observer">The observer to add.</param>
    void AddObserver(IStyleInvalidationObserver observer);

    /// <summary>
    /// Removes an observer from receiving invalidation notifications.
    /// </summary>
    /// <param name="observer">The observer to remove.</param>
    void RemoveObserver(IStyleInvalidationObserver observer);

    /// <summary>
    /// Marks an element as needing style recalculation.
    /// </summary>
    /// <param name="element">The element to invalidate.</param>
    void InvalidateElement(IElement element);

    /// <summary>
    /// Marks specific properties as needing recalculation.
    /// </summary>
    /// <param name="element">The element with properties to invalidate.</param>
    /// <param name="properties">The names of the properties to invalidate.</param>
    void InvalidateProperties(IElement element, IEnumerable<string> properties);

    /// <summary>
    /// Determines if an element needs style recalculation.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>True if the element needs recalculation; otherwise, false.</returns>
    bool NeedsStyleRecalculation(IElement element);

    /// <summary>
    /// Tracks a style dependency between elements.
    /// </summary>
    /// <param name="dependent">The element that depends on the source.</param>
    /// <param name="source">The element that affects the dependent.</param>
    void TrackDependency(IElement dependent, IElement source);

    /// <summary>
    /// Processes DOM changes and determines style invalidation.
    /// </summary>
    /// <param name="changes">The DOM changes to process.</param>
    void ProcessDomChanges(IEnumerable<DomChange> changes);

    /// <summary>
    /// Gets all elements that need style recalculation in a given subtree.
    /// </summary>
    /// <param name="root">The root element of the subtree.</param>
    /// <returns>The elements needing recalculation.</returns>
    IEnumerable<IElement> GetElementsToUpdate(IElement root);

    /// <summary>
    /// Marks an element as having up-to-date styles.
    /// </summary>
    /// <param name="element">The element to mark as up-to-date.</param>
    void MarkAsUpToDate(IElement element);

    /// <summary>
    /// Determines if an element's styles are up-to-date.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>True if the element's styles are up-to-date; otherwise, false.</returns>
    bool IsUpToDate(IElement element);

    /// <summary>
    /// Marks an element as dependent on device characteristics.
    /// </summary>
    /// <param name="element">The element to mark.</param>
    void MarkAsDeviceDependent(IElement element);

    /// <summary>
    /// Invalidates all device-dependent elements.
    /// </summary>
    void InvalidateForDeviceChange();

    /// <summary>
    /// Invalidates a subtree of elements starting at the given root.
    /// </summary>
    /// <param name="rootElement">The root element of the subtree to invalidate.</param>
    void InvalidateSubtree(IElement rootElement);
}