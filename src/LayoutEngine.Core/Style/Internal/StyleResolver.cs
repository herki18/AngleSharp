namespace LayoutEngine.Core.Style.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;

/// <summary>
/// Main style resolution orchestrator - mirrors Blink's StyleResolver
/// </summary>
public class StyleResolver : IStyleResolver
{
    private readonly IElementRuleCollector _ruleCollector;
    private readonly IStyleBuilder _styleBuilder;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleResolver> _logger;
    private readonly Dictionary<IElement, IComputedStyle> _computedStyleCache = new();

    public StyleResolver(
        IElementRuleCollector ruleCollector,
        IStyleBuilder styleBuilder,
        IEventAggregator eventAggregator,
        ILogger<StyleResolver> logger)
    {
        _ruleCollector = ruleCollector ?? throw new ArgumentNullException(nameof(ruleCollector));
        _styleBuilder = styleBuilder ?? throw new ArgumentNullException(nameof(styleBuilder));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IComputedStyle ResolveStyle(IElement element, IStyleRecalcContext context)
    {
        _logger.LogDebug("Resolving style for element: {TagName}", element.TagName);

        var contextWithElement = context.WithElement(element);

        // Step 1: Collect matching CSS rules
        var matchResult = _ruleCollector.CollectMatchingRules(element, contextWithElement);

        // Step 2: Apply matched properties
        _styleBuilder.ApplyMatchedProperties(matchResult, contextWithElement);

        // Step 3: Apply inheritance from parent
        if (context.ParentStyle != null)
        {
            _styleBuilder.ApplyInheritance(context.ParentStyle);
        }

        // Step 4: Build final computed style
        var computedStyle = _styleBuilder.TakeStyle(element);

        // Step 5: Cache the result
        _computedStyleCache[element] = computedStyle;

        return computedStyle;
    }

    public void RecalcDocumentStyle(IDocument document)
    {
        _logger.LogDebug("Recalculating styles for entire document");

        if (document.DocumentElement != null)
        {
            var context = new StyleRecalcContext(document);
            RecalcStyleRecursive(document.DocumentElement, context);
        }

        // Publish completion event
        var elements = _computedStyleCache.Keys.ToList();
        _eventAggregator.Publish(new StyleComputedEvent(elements, _computedStyleCache));
    }

    private void RecalcStyleRecursive(IElement element, IStyleRecalcContext context)
    {
        // Step 1: Resolve style for this element
        var computedStyle = ResolveStyle(element, context);

        // Step 2: Clear invalidation flag
        element.ClearNeedsStyleRecalc();

        // Step 3: Create context for children with this element's style as parent
        var childContext = context.WithParent(computedStyle.Declaration);

        // Step 4: Recursively process children
        foreach (var child in element.Children.OfType<IElement>())
        {
            if (child.NeedsStyleRecalc() || child.ChildNeedsStyleRecalc())
            {
                RecalcStyleRecursive(child, childContext);
            }
        }
    }

    public IComputedStyle? GetComputedStyle(IElement element)
    {
        return _computedStyleCache.TryGetValue(element, out var style) ? style : null;
    }

    public void ClearStyles()
    {
        _computedStyleCache.Clear();
    }
}