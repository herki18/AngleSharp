namespace AngleSharp.StyleSystem.Core;

using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;

/// <summary>
/// Tracks style invalidation and dependencies.
/// </summary>
public class StyleInvalidationTracker : IStyleInvalidationTracker
{
    private readonly Dictionary<IElement, InvalidationState> _invalidationStates = new Dictionary<IElement, InvalidationState>();
    private readonly Dictionary<IElement, HashSet<IElement>> _dependencies = new Dictionary<IElement, HashSet<IElement>>();
    private HashSet<IElement> _deviceDependentElements = new HashSet<IElement>();

    /// <summary>
    /// Marks an element as needing style recalculation.
    /// </summary>
    public void InvalidateElement(IElement element)
    {
        MarkAsInvalid(element);

        // Also invalidate dependent elements
        if (_dependencies.TryGetValue(element, out var dependents))
        {
            foreach (var dependent in dependents)
            {
                MarkAsInvalid(dependent);
            }
        }

        // Invalidate children for inheritance
        InvalidateSubtree(element);
    }

    /// <summary>
    /// Marks specific properties as needing recalculation.
    /// </summary>
    public void InvalidateProperties(IElement element, IEnumerable<string> properties)
    {
        // For simplicity, we'll just invalidate the entire element
        // In a real implementation, we'd track property-level invalidation
        InvalidateElement(element);
    }

    /// <summary>
    /// Determines if an element needs style recalculation.
    /// </summary>
    public bool NeedsStyleRecalculation(IElement element)
    {
        if (_invalidationStates.TryGetValue(element, out var state))
        {
            return state == InvalidationState.Invalid;
        }

        // If not tracked, assume it needs calculation
        return true;
    }

    /// <summary>
    /// Tracks a style dependency between elements.
    /// </summary>
    public void TrackDependency(IElement dependent, IElement source)
    {
        if (!_dependencies.TryGetValue(source, out var dependents))
        {
            dependents = new HashSet<IElement>();
            _dependencies[source] = dependents;
        }

        dependents.Add(dependent);
    }

    /// <summary>
    /// Gets all elements that need to be updated in a subtree.
    /// </summary>
    public IEnumerable<IElement> GetElementsToUpdate(IElement root)
    {
        var result = new List<IElement>();
        CollectInvalidElements(root, result);
        return result;
    }

    /// <summary>
    /// Marks an element as up-to-date after style calculation.
    /// </summary>
    public void MarkAsUpToDate(IElement element)
    {
        _invalidationStates[element] = InvalidationState.Valid;
    }

    /// <summary>
    /// Recursively invalidates a subtree.
    /// </summary>
    private void InvalidateSubtree(IElement element)
    {
        foreach (var child in element.Children)
        {
            MarkAsInvalid(child);
            InvalidateSubtree(child);
        }
    }

    /// <summary>
    /// Marks an element as invalid.
    /// </summary>
    private void MarkAsInvalid(IElement element)
    {
        _invalidationStates[element] = InvalidationState.Invalid;
    }

    /// <summary>
    /// Recursively collects invalid elements.
    /// </summary>
    private void CollectInvalidElements(IElement element, List<IElement> result)
    {
        if (NeedsStyleRecalculation(element))
        {
            result.Add(element);
        }

        foreach (var child in element.Children)
        {
            CollectInvalidElements(child, result);
        }
    }

    private enum InvalidationState
    {
        Valid,
        Invalid
    }

    /// <summary>
    /// Marks an element as dependent on render device properties.
    /// </summary>
    /// <param name="element">The element with device-dependent styles.</param>
    public void MarkAsDeviceDependent(IElement element)
    {
        _deviceDependentElements.Add(element);
    }

    /// <summary>
    /// Invalidates all elements with device-dependent styles.
    /// </summary>
    public void InvalidateForDeviceChange()
    {
        foreach (var element in _deviceDependentElements)
        {
            MarkAsInvalid(element);
        }
    }
}