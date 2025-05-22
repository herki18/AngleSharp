namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Events;
using Infrastructure.EventAggregator.API.Aggregation;
using Microsoft.Extensions.Logging;

// Implementation of the style system
public class StyleSystem : IStyleSystem
{
    private readonly Dictionary<IElement, IComputedStyle> _computedStyles = new();
    // REMOVED: private readonly HashSet<IElement> _elementsNeedingStyleRecalc = new();
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleSystem> _logger;

    public StyleSystem(IEventAggregator eventAggregator, ILogger<StyleSystem> logger)
    {
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

    // Computes style for a specific element
    public IComputedStyle ComputeStyle(IElement element)
    {
        if (MockLayoutData.UseMockData)
        {
            var style = new ComputedStyle(element, null);
            style.SetProperty("background-color", "blue");
            style.SetProperty("width", "300px");
            style.SetProperty("height", "150px");
            style.SetProperty("color", "white");
            style.SetProperty("font-size", "24");
            return style;
        }

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

        // Store in cache
        _computedStyles[element] = computedStyle;
        // REMOVED: _elementsNeedingStyleRecalc.Remove(element);

        return computedStyle;
    }

    // MODIFIED: ComputeDocumentStyles to use node flags
    public void ComputeDocumentStyles(IDocument document)
    {
        _logger.LogDebug("[StyleSystem] Computing document styles");

        if (document.DocumentElement != null)
        {
            ComputeStylesRecursive(document.DocumentElement);
        }

        var elements = _computedStyles.Keys.ToList();
        _eventAggregator.Publish(new StyleComputedEvent(elements, _computedStyles));
    }

    // NEW: Recursive method that respects node invalidation flags
    private void ComputeStylesRecursive(IElement element)
    {
        if (element.NeedsStyleRecalc())
        {
            var oldStyle = GetComputedStyle(element);
            var newStyle = ComputeStyle(element);

            // Check if style changes affect layout
            if (oldStyle != null && StyleChangeAffectsLayout(oldStyle, newStyle))
            {
                element.SetNeedsLayout();
            }

            // Paint is always affected by style changes
            element.SetNeedsPaintInvalidation();

            // Clear the style flag since we just computed it
            element.ClearNeedsStyleRecalc();
        }

        // Process children if they need style recalc
        if (element.ChildNeedsStyleRecalc())
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                ComputeStylesRecursive(child);
            }
        }
    }

    // TODO: Needs to return null if the element is not foun
    public IComputedStyle GetComputedStyle(IElement element)
    {
        _computedStyles.TryGetValue(element, out var style);
        return style;
    }

    // MODIFIED: NeedsStyleRecalc to use node flags
    public bool NeedsStyleRecalc(IElement element)
    {
        return element.NeedsStyleRecalc(); // Use node flag instead of internal collection
    }

    // MODIFIED: InvalidateStyle to use node flags
    public void InvalidateStyle(IElement element, bool recursive = true)
    {
        element.SetNeedsStyleRecalc(); // Set node flag instead of internal collection

        if (recursive)
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                InvalidateStyle(child, true);
            }
        }
    }

    // NEW: Helper method to determine if style changes affect layout
    private bool StyleChangeAffectsLayout(IComputedStyle oldStyle, IComputedStyle newStyle)
    {
        var layoutProps = new[] { "width", "height", "margin", "padding", "border", "display", "position", "flex", "grid" };

        return layoutProps.Any(prop =>
            oldStyle.GetValue(prop) != newStyle.GetValue(prop));
    }

    public void ClearStyles()
    {
        _computedStyles.Clear();
    }
}
