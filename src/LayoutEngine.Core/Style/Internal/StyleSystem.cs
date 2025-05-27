namespace LayoutEngine.Core.Style.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;

public class StyleSystem : IStyleSystem
{
    private readonly IStyleResolver _styleResolver;
    private readonly IStyleSheetManager _styleSheetManager;
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

        // Create context for this element
        IStyleRecalcContext context = new StyleRecalcContext(element.OwnerDocument)
        {
            CurrentElement = element
        };

        // Get parent style for inheritance
        if (element.ParentElement != null)
        {
            var parentStyle = GetComputedStyle(element.ParentElement);
            if (parentStyle != null)
            {
                context = context.WithParent(parentStyle.Declaration);
            }
        }

        return _styleResolver.ResolveStyle(element, context);
    }

    public void ComputeDocumentStyles(IDocument document)
    {
        _logger.LogInformation("Computing styles for document");

        // Ensure stylesheets are loaded
        _styleSheetManager.AttachToDocument(document);

        // Delegate to StyleResolver for document-wide recalculation
        _styleResolver.RecalcDocumentStyle(document);
    }

    public IComputedStyle? GetComputedStyle(IElement element)
    {
        return _styleResolver.GetComputedStyle(element);
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

    public void ClearStyles()
    {
        _logger.LogDebug("Clearing all computed styles");
        _styleResolver.ClearStyles();
    }
}