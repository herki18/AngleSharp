namespace LayoutEngine.Core.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Events;
using Infrastructure.EventAggregator.API.Aggregation;
using Microsoft.Extensions.Logging;

public class StyleSystem : IStyleSystem
{
    private readonly Dictionary<IElement, IComputedStyle> _computedStyles = new();
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleSystem> _logger;

    public StyleSystem(IEventAggregator eventAggregator, ILogger<StyleSystem> logger)
    {
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

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

            // Store the computed style in the dictionary
            _computedStyles[element] = style;
            return style;
        }

        IComputedStyle? parentStyle = null;
        if (element.Parent is IElement parentElement)
        {
            if (NeedsStyleRecalc(parentElement))
            {
                ComputeStyle(parentElement);
            }
            _computedStyles.TryGetValue(parentElement, out parentStyle);
        }

        var computedStyle = new ComputedStyle(element, parentStyle);
        _computedStyles[element] = computedStyle;
        return computedStyle;
    }

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

    private void ComputeStylesRecursive(IElement element)
    {
        bool needsProcessing = element.NeedsStyleRecalc() || element.ChildNeedsStyleRecalc();

        if (element.NeedsStyleRecalc())
        {
            var oldStyle = GetComputedStyle(element);
            var newStyle = ComputeStyle(element);

            if (oldStyle != null && StyleChangeAffectsLayout(oldStyle, newStyle))
            {
                element.SetNeedsLayout();
            }

            element.SetNeedsPaintInvalidation();
        }

        if (element.ChildNeedsStyleRecalc())
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                ComputeStylesRecursive(child);
            }
        }

        // Clear flags after processing
        if (needsProcessing)
        {
            element.ClearNeedsStyleRecalc();
        }
    }

    public IComputedStyle GetComputedStyle(IElement element)
    {
        _computedStyles.TryGetValue(element, out var style);
        return style;
    }

    public bool NeedsStyleRecalc(IElement element)
    {
        return element.NeedsStyleRecalc();
    }

    public void InvalidateStyle(IElement element, bool recursive = true)
    {
        element.SetNeedsStyleRecalc();

        if (recursive)
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                InvalidateStyle(child, true);
            }
        }
    }

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