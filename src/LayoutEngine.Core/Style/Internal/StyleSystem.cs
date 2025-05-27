namespace LayoutEngine.Core.Style.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;

public class StyleSystem : IStyleSystem
{
    private readonly IStyleResolver _styleResolver;
    private readonly IStyleSheetManager _styleSheetManager;
    private readonly Dictionary<IElement, IComputedStyle> _computedStyleCache = new();
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleSystem> _logger;

    public StyleSystem(
        IStyleResolver styleResolver,
        IStyleSheetManager styleSheetManager,
        IEventAggregator eventAggregator,
        ILogger<StyleSystem> logger)
    {
        _styleResolver = styleResolver ?? throw new ArgumentNullException(nameof(styleResolver));
        _styleSheetManager = styleSheetManager ?? throw new ArgumentNullException(nameof(styleSheetManager));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IComputedStyle ComputeStyle(IElement element)
    {
        _logger.LogDebug("Computing style for element: {TagName}#{Id}", element.TagName, element.Id);

        // Get parent style for inheritance
        ICssStyleDeclaration? parentDeclaration = null;
        if (element.Parent is IElement parentElement &&
            _computedStyleCache.TryGetValue(parentElement, out var parentStyle))
        {
            parentDeclaration = parentStyle.Declaration;
        }

        var context = new StyleRecalcContext(element.OwnerDocument, parentDeclaration)
        {
            CurrentElement = element
        };

        var computedStyle = _styleResolver.ResolveStyle(element, context);
        _computedStyleCache[element] = computedStyle;

        return computedStyle;
    }

    public void ComputeDocumentStyles(IDocument document)
    {
        _logger.LogInformation("Computing styles for document");

        // Ensure stylesheets are loaded
        _styleSheetManager.AttachToDocument(document);

        // Clear previous styles
        _computedStyleCache.Clear();

        if (document.DocumentElement != null)
        {
            ComputeStylesRecursive(document.DocumentElement);
        }

        var elements = _computedStyleCache.Keys.ToList();
        var styles = new Dictionary<IElement, IComputedStyle>(_computedStyleCache);

        _eventAggregator.Publish(new StyleComputedEvent(elements, styles));
    }

    private void ComputeStylesRecursive(IElement element)
    {
        // Check if this element needs style recalc
        if (element.NeedsStyleRecalc())
        {
            var oldStyle = GetComputedStyle(element);
            var newStyle = ComputeStyle(element);

            // Clear the flag after computing
            element.ClearNeedsStyleRecalc();

            // Check if layout needs invalidation
            if (oldStyle == null || StyleChangeAffectsLayout(oldStyle, newStyle))
            {
                element.SetNeedsLayout();
            }
            else if (StyleChangeAffectsPaint(oldStyle, newStyle))
            {
                element.SetNeedsPaintInvalidation();
            }
        }

        // Process children
        foreach (var child in element.Children.OfType<IElement>())
        {
            if (child.NeedsStyleRecalc() || child.ChildNeedsStyleRecalc())
            {
                ComputeStylesRecursive(child);
            }
        }
    }

    public IComputedStyle? GetComputedStyle(IElement element)
    {
        return _computedStyleCache.TryGetValue(element, out var style) ? style : null;
    }

    public bool NeedsStyleRecalc(IElement element)
    {
        return element.NeedsStyleRecalc() || element.ChildNeedsStyleRecalc();
    }

    public void InvalidateStyle(IElement element, bool recursive = true)
    {
        _logger.LogDebug("Invalidating style for element: {TagName}#{Id}", element.TagName, element.Id);

        var invalidatedElements = new List<IElement>();
        element.SetNeedsStyleRecalc();
        invalidatedElements.Add(element);

        if (recursive)
        {
            InvalidateStyleRecursive(element, invalidatedElements);
        }

        _eventAggregator.Publish(new StyleInvalidatedEvent(invalidatedElements));
    }

    private void InvalidateStyleRecursive(IElement element, List<IElement> invalidatedElements)
    {
        foreach (var child in element.Children.OfType<IElement>())
        {
            child.SetNeedsStyleRecalc();
            invalidatedElements.Add(child);
            InvalidateStyleRecursive(child, invalidatedElements);
        }
    }

    private bool StyleChangeAffectsLayout(IComputedStyle oldStyle, IComputedStyle newStyle)
    {
        // Properties that affect layout
        var layoutProperties = new[]
        {
            "display", "position", "float", "clear",
            "width", "height", "min-width", "min-height", "max-width", "max-height",
            "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
            "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
            "font-size", "font-weight", "line-height",
            "flex", "flex-basis", "flex-grow", "flex-shrink", "align-items", "justify-content",
            "grid-template-columns", "grid-template-rows"
        };

        return layoutProperties.Any(prop =>
            oldStyle.GetPropertyValue(prop) != newStyle.GetPropertyValue(prop));
    }

    private bool StyleChangeAffectsPaint(IComputedStyle oldStyle, IComputedStyle newStyle)
    {
        // Properties that only affect paint (not layout)
        var paintProperties = new[]
        {
            "color", "background-color", "background-image",
            "border-color", "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
            "opacity", "visibility", "z-index",
            "box-shadow", "text-shadow"
        };

        return paintProperties.Any(prop =>
            oldStyle.GetPropertyValue(prop) != newStyle.GetPropertyValue(prop));
    }

    public void ClearStyles()
    {
        _logger.LogDebug("Clearing all computed styles");
        _computedStyleCache.Clear();
    }
}