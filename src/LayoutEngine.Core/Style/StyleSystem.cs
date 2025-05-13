namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Events;
using Infrastructure.EventAggregator.API.Aggregation;

// Implementation of the style system
public class StyleSystem : IStyleSystem
{
    private readonly Dictionary<IElement, IComputedStyle> _computedStyles = new();
    private readonly HashSet<IElement> _elementsNeedingStyleRecalc = new();
    private readonly IEventAggregator _eventAggregator;

    public StyleSystem(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    // Computes style for a specific element
    public IComputedStyle ComputeStyle(IElement element)
    {
        // Check if parent needs style calculation
        IComputedStyle? parentStyle = null;
        if (element.Parent is IElement parentElement)
        {
            if (NeedsStyleRecalc(parentElement))
            {
                ComputeStyle(parentElement);
            }

            _computedStyles.TryGetValue(parentElement, out parentStyle);
        }

        // Create the computed style
        var computedStyle = new ComputedStyle(element, parentStyle);

        // Store in cache and remove from dirty list
        _computedStyles[element] = computedStyle;
        _elementsNeedingStyleRecalc.Remove(element);

        return computedStyle;
    }

    // Computes styles for the entire document
    public void ComputeDocumentStyles(IDocument document)
    {
        // Process elements in document order (depth-first)
        if (document.DocumentElement != null)
        {
            ComputeStylesRecursive(document.DocumentElement);
        }

        // Notify that style computation is complete
        var elements = _computedStyles.Keys.ToList();
        _eventAggregator.Publish(new StyleComputedEvent(elements, _computedStyles));
    }

    // Helper to compute styles recursively
    private void ComputeStylesRecursive(IElement element)
    {
        ComputeStyle(element);

        foreach (var child in element.Children.OfType<IElement>())
        {
            ComputeStylesRecursive(child);
        }
    }

    // TODO: Needs to return null if the element is not foun
    public IComputedStyle GetComputedStyle(IElement element)
    {
        _computedStyles.TryGetValue(element, out var style);
        return style;
    }

    public bool NeedsStyleRecalc(IElement element)
    {
        return _elementsNeedingStyleRecalc.Contains(element);
    }

    public void InvalidateStyle(IElement element, bool recursive = true)
    {
        var affectedElements = new List<IElement>();

        // Add the element to the dirty list
        _elementsNeedingStyleRecalc.Add(element);
        affectedElements.Add(element);

        if (recursive)
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                InvalidateStyleRecursive(child, affectedElements);
            }
        }

        // Publish style invalidation event
        _eventAggregator.Publish(new StyleInvalidatedEvent(affectedElements));
    }

    private void InvalidateStyleRecursive(IElement element, List<IElement> affectedElements)
    {
        _elementsNeedingStyleRecalc.Add(element);
        affectedElements.Add(element);

        foreach (var child in element.Children.OfType<IElement>())
        {
            InvalidateStyleRecursive(child, affectedElements);
        }
    }

    public void ClearStyles()
    {
        _computedStyles.Clear();
    }
}