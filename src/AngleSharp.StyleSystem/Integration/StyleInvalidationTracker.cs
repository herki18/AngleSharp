namespace AngleSharp.StyleSystem.Integration;

using System.Collections.Generic;
using AngleSharp.Dom;
using Interfaces;

public class StyleInvalidationTracker : IStyleInvalidationTracker
{
    private readonly Dictionary<IElement, InvalidationState> _invalidationStates = new Dictionary<IElement, InvalidationState>();
    private readonly Dictionary<IElement, HashSet<IElement>> _dependencies = new Dictionary<IElement, HashSet<IElement>>();
    private HashSet<IElement> _deviceDependentElements = new HashSet<IElement>();

    public void InvalidateElement(IElement element)
    {
        MarkAsInvalid(element);
        if (_dependencies.TryGetValue(element, out var dependents))
        {
            foreach (var dependent in dependents)
            {
                MarkAsInvalid(dependent);
            }
        }
        InvalidateSubtree(element);
    }

    public void InvalidateProperties(IElement element, IEnumerable<string> properties)
    {
        InvalidateElement(element);
    }

    public bool NeedsStyleRecalculation(IElement element)
    {
        if (_invalidationStates.TryGetValue(element, out var state))
        {
            return state == InvalidationState.Invalid;
        }
        return true;
    }

    public void TrackDependency(IElement dependent, IElement source)
    {
        if (!_dependencies.TryGetValue(source, out var dependents))
        {
            dependents = new HashSet<IElement>();
            _dependencies[source] = dependents;
        }
        dependents.Add(dependent);
    }

    public IEnumerable<IElement> GetElementsToUpdate(IElement root)
    {
        var result = new List<IElement>();
        CollectInvalidElements(root, result);
        return result;
    }

    public void MarkAsUpToDate(IElement element)
    {
        _invalidationStates[element] = InvalidationState.Valid;
    }

    public bool IsUpToDate(IElement element)
    {
        return _invalidationStates.TryGetValue(element, out var state) &&
               state == InvalidationState.Valid;
    }

    private void InvalidateSubtree(IElement element)
    {
        foreach (var child in element.Children)
        {
            MarkAsInvalid(child);
            InvalidateSubtree(child);
        }
    }

    private void MarkAsInvalid(IElement element)
    {
        _invalidationStates[element] = InvalidationState.Invalid;
    }

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

    public void MarkAsDeviceDependent(IElement element)
    {
        _deviceDependentElements.Add(element);
    }

    public void InvalidateForDeviceChange()
    {
        foreach (var element in _deviceDependentElements)
        {
            MarkAsInvalid(element);
        }
    }
}