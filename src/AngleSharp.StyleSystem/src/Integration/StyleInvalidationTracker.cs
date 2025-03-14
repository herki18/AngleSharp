namespace AngleSharp.StyleSystem.Integration;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using AngleSharp.StyleSystem.Observers;

/// <summary>
/// Tracks which elements need style recalculation, using the observer pattern.
/// </summary>
public class StyleInvalidationTracker : IStyleInvalidationTracker
{
    private readonly Dictionary<IElement, InvalidationState> _invalidationStates = new();
    private readonly Dictionary<IElement, HashSet<IElement>> _dependencies = new();
    private readonly HashSet<IElement> _deviceDependentElements = new();
    private readonly List<IStyleInvalidationObserver> _observers = new();

    /// <summary>
    /// Adds an observer to receive invalidation notifications.
    /// </summary>
    /// <param name="observer">The observer to add.</param>
    public void AddObserver(IStyleInvalidationObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    /// <summary>
    /// Removes an observer from receiving invalidation notifications.
    /// </summary>
    /// <param name="observer">The observer to remove.</param>
    public void RemoveObserver(IStyleInvalidationObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        _observers.Remove(observer);
    }

    /// <inheritdoc />
    public void ProcessDomChanges(IEnumerable<DomChange> changes)
    {
        if (changes == null) return;

        var elementsToInvalidate = new Dictionary<IElement, HashSet<string>>();
        var subtreesToInvalidate = new HashSet<IElement>();

        foreach (var change in changes)
        {
            ProcessDomChange(change, elementsToInvalidate, subtreesToInvalidate);
        }

        ApplyInvalidations(elementsToInvalidate, subtreesToInvalidate);
    }

    /// <inheritdoc />
    public void InvalidateElement(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        MarkAsInvalid(element);

        if (_dependencies.TryGetValue(element, out var dependents))
        {
            foreach (var dependent in dependents)
            {
                MarkAsInvalid(dependent);
            }
        }

        // Notify observers
        foreach (var observer in _observers)
        {
            observer.OnElementInvalidated(element);
        }
    }

    /// <inheritdoc />
    public void InvalidateProperties(IElement element, IEnumerable<string> properties)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (properties == null)
            throw new ArgumentNullException(nameof(properties));

        InvalidateElement(element);

        // Notify observers with the specific properties
        var propertyList = properties.ToList();
        if (propertyList.Count > 0)
        {
            foreach (var observer in _observers)
            {
                observer.OnPropertiesInvalidated(element, propertyList);
            }
        }
    }

    /// <inheritdoc />
    public bool NeedsStyleRecalculation(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (_invalidationStates.TryGetValue(element, out var state))
        {
            return state == InvalidationState.Invalid;
        }

        return true;
    }

    /// <inheritdoc />
    public void TrackDependency(IElement dependent, IElement source)
    {
        if (dependent == null)
            throw new ArgumentNullException(nameof(dependent));
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (!_dependencies.TryGetValue(source, out var dependents))
        {
            dependents = new HashSet<IElement>();
            _dependencies[source] = dependents;
        }

        dependents.Add(dependent);
    }

    /// <inheritdoc />
    public IEnumerable<IElement> GetElementsToUpdate(IElement root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        var result = new List<IElement>();
        CollectInvalidElements(root, result);
        return result;
    }

    /// <inheritdoc />
    public void MarkAsUpToDate(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        _invalidationStates[element] = InvalidationState.Valid;
    }

    /// <inheritdoc />
    public bool IsUpToDate(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return _invalidationStates.TryGetValue(element, out var state) &&
               state == InvalidationState.Valid;
    }

    /// <inheritdoc />
    public void MarkAsDeviceDependent(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        _deviceDependentElements.Add(element);
    }

    /// <inheritdoc />
    public void InvalidateForDeviceChange()
    {
        foreach (var element in _deviceDependentElements)
        {
            MarkAsInvalid(element);
        }

        // Notify observers
        foreach (var observer in _observers)
        {
            observer.OnDeviceDependentElementsInvalidated();
        }
    }

    /// <summary>
    /// Invalidates a subtree of elements starting at the given root.
    /// </summary>
    /// <param name="rootElement">The root element of the subtree to invalidate.</param>
    public void InvalidateSubtree(IElement rootElement)
    {
        if (rootElement == null)
            throw new ArgumentNullException(nameof(rootElement));

        MarkAsInvalid(rootElement);
        InvalidateChildrenRecursively(rootElement);

        // Notify observers
        foreach (var observer in _observers)
        {
            observer.OnSubtreeInvalidated(rootElement);
        }
    }

    private void InvalidateChildrenRecursively(IElement element)
    {
        foreach (var child in element.Children)
        {
            MarkAsInvalid(child);
            InvalidateChildrenRecursively(child);
        }
    }

    private void ProcessDomChange(
        DomChange change,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate,
        HashSet<IElement> subtreesToInvalidate)
    {
        switch (change.Type)
        {
            case DomChangeType.StyleAttributeChanged:
                if (change.Target is IElement element)
                {
                    AddElementInvalidation(element, null, elementsToInvalidate);
                }
                break;
            case DomChangeType.ClassAttributeChanged:
                ProcessClassChange(change, elementsToInvalidate, subtreesToInvalidate);
                break;
            case DomChangeType.IdAttributeChanged:
                ProcessIdChange(change, elementsToInvalidate, subtreesToInvalidate);
                break;
            case DomChangeType.AttributeChanged:
                ProcessAttributeChange(change, elementsToInvalidate);
                break;
            case DomChangeType.NodeAdded:
                ProcessNodeAddition(change, elementsToInvalidate, subtreesToInvalidate);
                break;
            case DomChangeType.NodeRemoved:
                ProcessNodeRemoval(change, elementsToInvalidate, subtreesToInvalidate);
                break;
            case DomChangeType.TextChanged:
                ProcessTextChange(change, elementsToInvalidate);
                break;
            case DomChangeType.ElementStructureChanged:
                ProcessStructureChange(change, elementsToInvalidate);
                break;
            case DomChangeType.StylesheetChanged:
                if (change.Target is IElement targetElement && targetElement.OwnerDocument?.DocumentElement != null)
                {
                    subtreesToInvalidate.Add(targetElement.OwnerDocument.DocumentElement);
                }
                break;
        }
    }

    private void ApplyInvalidations(
        Dictionary<IElement, HashSet<string>> elementsToInvalidate,
        HashSet<IElement> subtreesToInvalidate)
    {
        // Apply containment optimizations first
        ApplyContainmentOptimizations(elementsToInvalidate, subtreesToInvalidate);

        // Process subtrees
        foreach (var root in subtreesToInvalidate)
        {
            InvalidateSubtree(root);
        }

        // Process individual elements
        foreach (var kvp in elementsToInvalidate)
        {
            var element = kvp.Key;
            var properties = kvp.Value;

            if (properties == null || properties.Count == 0)
            {
                InvalidateElement(element);
            }
            else
            {
                InvalidateProperties(element, properties);
            }
        }
    }

    private void ProcessClassChange(
        DomChange change,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate,
        HashSet<IElement> subtreesToInvalidate)
    {
        if (change.Target is IElement element)
        {
            AddElementInvalidation(element, null, elementsToInvalidate);

            // Classes can affect descendant selectors, so check siblings and their children
            if (DocumentUsesComplexSelectors(element.OwnerDocument))
            {
                if (element.ParentElement != null)
                {
                    AddElementInvalidation(element.ParentElement, null, elementsToInvalidate);

                    foreach (var sibling in element.ParentElement.Children)
                    {
                        if (sibling != element && sibling is IElement siblingElement)
                        {
                            AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                        }
                    }
                }
            }
        }
    }

    private void ProcessIdChange(
        DomChange change,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate,
        HashSet<IElement> subtreesToInvalidate)
    {
        if (change.Target is IElement element)
        {
            AddElementInvalidation(element, null, elementsToInvalidate);

            // ID changes can affect the entire document (through id selectors)
            if (!string.IsNullOrEmpty(change.OldValue) || !string.IsNullOrEmpty(change.NewValue))
            {
                if (element.OwnerDocument?.DocumentElement != null)
                {
                    subtreesToInvalidate.Add(element.OwnerDocument.DocumentElement);
                }
            }
        }
    }

    private void ProcessAttributeChange(
        DomChange change,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate)
    {
        if (change.Target is IElement element && IsStyleAffectingAttribute(change.AttributeName))
        {
            AddElementInvalidation(element, null, elementsToInvalidate);
        }
    }

    private void ProcessNodeAddition(
        DomChange change,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate,
        HashSet<IElement> subtreesToInvalidate)
    {
        if (change.Node is IElement addedElement)
        {
            AddElementInvalidation(addedElement, null, elementsToInvalidate);

            // Adding a node can affect the parent and siblings (for selectors like :first-child, etc.)
            if (change.Target is IElement parentElement)
            {
                AddElementInvalidation(parentElement, null, elementsToInvalidate);

                foreach (var sibling in parentElement.Children)
                {
                    if (sibling != addedElement && sibling is IElement siblingElement)
                    {
                        AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                    }
                }
            }

            // If adding a style element, invalidate the whole document
            if (addedElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) ||
                (addedElement.NodeName.Equals("LINK", StringComparison.OrdinalIgnoreCase) &&
                 addedElement.GetAttribute("rel")?.Contains("stylesheet") == true))
            {
                if (addedElement.OwnerDocument?.DocumentElement != null)
                {
                    subtreesToInvalidate.Add(addedElement.OwnerDocument.DocumentElement);
                }
            }
        }
    }

    private void ProcessNodeRemoval(
        DomChange change,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate,
        HashSet<IElement> subtreesToInvalidate)
    {
        if (change.Node is IElement removedElement)
        {
            // Removing a node can affect the parent and siblings (for selectors like :first-child, etc.)
            if (change.Target is IElement parentElement)
            {
                AddElementInvalidation(parentElement, null, elementsToInvalidate);

                foreach (var sibling in parentElement.Children)
                {
                    if (sibling is IElement siblingElement)
                    {
                        AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                    }
                }
            }

            // If removing a style element, invalidate the whole document
            if (removedElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) ||
                (removedElement.NodeName.Equals("LINK", StringComparison.OrdinalIgnoreCase) &&
                 removedElement.GetAttribute("rel")?.Contains("stylesheet") == true))
            {
                if (removedElement.OwnerDocument?.DocumentElement != null)
                {
                    subtreesToInvalidate.Add(removedElement.OwnerDocument.DocumentElement);
                }
            }
        }
    }

    private void ProcessTextChange(
        DomChange change,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate)
    {
        if (change.Target?.ParentElement is IElement parentElement)
        {
            AddElementInvalidation(parentElement, null, elementsToInvalidate);

            // If text inside a style element changes, invalidate the whole document
            if (parentElement.NodeName.Equals("STYLE", StringComparison.OrdinalIgnoreCase) &&
                parentElement.OwnerDocument?.DocumentElement != null)
            {
                // This should be handled by stylesheet manager, but just in case
                elementsToInvalidate.Clear();
                InvalidateElement(parentElement.OwnerDocument.DocumentElement);
            }
        }
    }

    private void ProcessStructureChange(
        DomChange change,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate)
    {
        if (change.Target is IElement element)
        {
            AddElementInvalidation(element, null, elementsToInvalidate);

            // Structure changes can affect siblings (for selectors like :first-child, etc.)
            if (element.ParentElement != null)
            {
                foreach (var sibling in element.ParentElement.Children)
                {
                    if (sibling != element && sibling is IElement siblingElement)
                    {
                        AddElementInvalidation(siblingElement, null, elementsToInvalidate);
                    }
                }
            }
        }
    }

    private void ApplyContainmentOptimizations(
        Dictionary<IElement, HashSet<string>> elementsToInvalidate,
        HashSet<IElement> subtreesToInvalidate)
    {
        var containmentBoundaries = new HashSet<IElement>();
        var elementsToRemove = new HashSet<IElement>();
        var subtreesToRemove = new HashSet<IElement>();

        // Find containment boundaries for elements and subtrees
        foreach (var element in elementsToInvalidate.Keys.Concat(subtreesToInvalidate))
        {
            var containmentBoundary = FindContainmentBoundary(element);
            if (containmentBoundary != null)
            {
                containmentBoundaries.Add(containmentBoundary);

                // If we're already invalidating the entire document, and found a containment boundary,
                // we can remove that whole-document invalidation and just use the boundary
                if (element.OwnerDocument?.DocumentElement != null &&
                    subtreesToInvalidate.Contains(element.OwnerDocument.DocumentElement))
                {
                    subtreesToRemove.Add(element.OwnerDocument.DocumentElement);
                }

                // Remove elements that are within a containment boundary
                if (elementsToInvalidate.ContainsKey(element) && element != containmentBoundary)
                {
                    elementsToRemove.Add(element);
                }
            }
        }

        // Remove elements that are covered by containment boundaries
        foreach (var element in elementsToRemove)
        {
            elementsToInvalidate.Remove(element);
        }

        // Remove subtrees that are covered by containment boundaries
        foreach (var element in subtreesToRemove)
        {
            subtreesToInvalidate.Remove(element);
        }

        // Add containment boundaries to subtrees to invalidate
        foreach (var boundary in containmentBoundaries)
        {
            subtreesToInvalidate.Add(boundary);
        }
    }

    private IElement? FindContainmentBoundary(IElement element)
    {
        var current = element;
        while (current.ParentElement != null)
        {
            current = current.ParentElement;
            if (HasStyleContainment(current))
            {
                return current;
            }
        }
        return null;
    }

    private bool HasStyleContainment(IElement element)
    {
        var styleAttribute = element.GetAttribute("style");
        if (string.IsNullOrEmpty(styleAttribute))
            return false;

        return styleAttribute.Contains("contain: style") ||
               styleAttribute.Contains("contain: layout style") ||
               styleAttribute.Contains("contain: strict") ||
               styleAttribute.Contains("contain: content");
    }

    private void AddElementInvalidation(
        IElement? element,
        HashSet<string>? properties,
        Dictionary<IElement, HashSet<string>> elementsToInvalidate)
    {
        if (element == null)
            return;

        if (!elementsToInvalidate.TryGetValue(element, out var existingProps))
        {
            existingProps = new HashSet<string>();
            elementsToInvalidate[element] = existingProps;
        }

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                existingProps.Add(prop);
            }
        }
    }

    private bool IsStyleAffectingAttribute(string? attributeName)
    {
        if (string.IsNullOrEmpty(attributeName))
            return false;

        var globalStyleAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "class", "style", "id", "lang", "dir", "hidden", "tabindex", "draggable", "contenteditable"
        };

        var elementSpecificStyleAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "disabled", "readonly", "checked", "required", "placeholder", "multiple",
            "colspan", "rowspan", "headers", "scope", "border",
            "width", "height", "align", "valign",
            "href", "target", "rel",
            "src", "alt", "title"
        };

        return globalStyleAttributes.Contains(attributeName) ||
               elementSpecificStyleAttributes.Contains(attributeName);
    }

    private bool DocumentUsesComplexSelectors(IDocument? document)
    {
        // For now, we assume all documents use complex selectors
        // In the future, this could analyze the stylesheets to determine this
        return true;
    }

    private void MarkAsInvalid(IElement element)
    {
        if (element == null)
            return;

        _invalidationStates[element] = InvalidationState.Invalid;
    }

    private void CollectInvalidElements(IElement element, List<IElement> result)
    {
        if (element == null)
            return;

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
}