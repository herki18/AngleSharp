namespace LayoutEngine.Core.LayoutStyle.Internal;

using System;
using System.Collections.Generic;
using LayoutEngine.Core.LayoutNG.Public;
using LayoutEngine.Core.LayoutStyle.Public;
using AngleSharp.Dom;
using Events;
using Microsoft.Extensions.Logging;
using Infrastructure.EventAggregator.API.Aggregation;

/// <summary>
/// Main implementation of the LayoutNG-aligned style system.
/// </summary>
public class LayoutStyleSystem : ILayoutStyleSystem
{
    private readonly ILayoutStyleResolver _styleResolver;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<LayoutStyleSystem> _logger;
    private readonly Dictionary<ILayoutObject, ILayoutComputedStyle> _styleCache = new();

    public LayoutStyleSystem(
        ILayoutStyleResolver styleResolver,
        IEventAggregator eventAggregator,
        ILogger<LayoutStyleSystem> logger)
    {
        _styleResolver = styleResolver ?? throw new ArgumentNullException(nameof(styleResolver));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ILayoutComputedStyle ResolveStyle(ILayoutObject layoutObject, ILayoutStyleContext context)
    {
        _logger.LogDebug("Resolving style for {Type} layout object (Anonymous: {IsAnonymous})",
            layoutObject.Type, layoutObject.IsAnonymous);

        // Check cache first
        if (_styleCache.TryGetValue(layoutObject, out var cached))
        {
            _logger.LogDebug("Using cached style for layout object");
            return cached;
        }

        // Resolve style through the resolver
        var computedStyle = _styleResolver.ResolveStyle(layoutObject, context);

        // Cache the result
        _styleCache[layoutObject] = computedStyle;

        // Update the layout object's style reference
        layoutObject.Style = computedStyle;

        _logger.LogDebug("Style resolved: display={Display}, position={Position}",
            computedStyle.Display, computedStyle.Position);

        return computedStyle;
    }

    public void ResolveLayoutTreeStyles(ILayoutObject rootLayoutObject)
    {
        _logger.LogInformation("Resolving styles for entire layout tree");

        if (rootLayoutObject.Element?.OwnerDocument == null)
        {
            throw new InvalidOperationException("Root layout object must have an associated document");
        }

        var context = new LayoutStyleContext(rootLayoutObject.Element.OwnerDocument)
        {
            CurrentLayoutObject = rootLayoutObject
        };

        // Clear existing cache
        _styleCache.Clear();

        // Recursively resolve styles
        ResolveStyleRecursive(rootLayoutObject, context);

        _logger.LogInformation("Layout tree style resolution complete. Cached {Count} styles", _styleCache.Count);

        // Publish completion event
        _eventAggregator.Publish(new LayoutTreeStylesResolvedEvent(_styleCache));
    }

    private void ResolveStyleRecursive(ILayoutObject layoutObject, ILayoutStyleContext context)
    {
        // Skip if already processed
        if (_styleCache.ContainsKey(layoutObject))
        {
            return;
        }

        // Resolve style for current object
        var computedStyle = ResolveStyle(layoutObject, context);

        // Clear style recalc flag
        layoutObject.ClearNeedsStyleRecalc();

        // Process children if this is a container
        if (layoutObject is ILayoutContainer container)
        {
            foreach (var child in container.Children)
            {
                // Create child context with this object's style as parent
                var childContext = context.CreateChildContext(child, computedStyle);
                ResolveStyleRecursive(child, childContext);
            }
        }

        // Clear child needs style recalc flag
        layoutObject.ClearChildNeedsStyleRecalc();
    }

    public ILayoutComputedStyle? GetComputedStyle(ILayoutObject layoutObject)
    {
        return _styleCache.TryGetValue(layoutObject, out var style) ? style : null;
    }

    public void InvalidateStyle(ILayoutObject layoutObject, bool recursive = true)
    {
        _logger.LogDebug("Invalidating style for {Type} layout object (recursive: {Recursive})",
            layoutObject.Type, recursive);

        var invalidatedObjects = new List<ILayoutObject>();

        // Mark for style recalc
        layoutObject.SetNeedsStyleRecalc();
        invalidatedObjects.Add(layoutObject);

        // Remove from cache
        _styleCache.Remove(layoutObject);

        if (recursive && layoutObject is ILayoutContainer container)
        {
            InvalidateStyleRecursive(container, invalidatedObjects);
        }

        // Publish invalidation event
        _eventAggregator.Publish(new LayoutStyleInvalidatedEvent(invalidatedObjects));
    }

    private void InvalidateStyleRecursive(ILayoutContainer container, List<ILayoutObject> invalidatedObjects)
    {
        foreach (var child in container.Children)
        {
            child.SetNeedsStyleRecalc();
            invalidatedObjects.Add(child);
            _styleCache.Remove(child);

            if (child is ILayoutContainer childContainer)
            {
                InvalidateStyleRecursive(childContainer, invalidatedObjects);
            }
        }
    }

    public void ClearStyles()
    {
        _logger.LogDebug("Clearing all cached styles");
        _styleCache.Clear();
    }

    public bool NeedsStyleRecalc(ILayoutObject layoutObject)
    {
        return layoutObject.NeedsStyleRecalc() || layoutObject.ChildNeedsStyleRecalc();
    }
}

/// <summary>
/// Implementation of style context.
/// </summary>
internal class LayoutStyleContext : ILayoutStyleContext
{
    public IDocument Document { get; }
    public ILayoutComputedStyle? ParentStyle { get; private set; }
    public ILayoutObject CurrentLayoutObject { get; set; } = null!;
    public bool IsPseudoElement { get; set; }
    public string? PseudoElementType { get; set; }

    public LayoutStyleContext(IDocument document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    public ILayoutStyleContext CreateChildContext(ILayoutObject childLayoutObject, ILayoutComputedStyle parentStyle)
    {
        return new LayoutStyleContext(Document)
        {
            ParentStyle = parentStyle,
            CurrentLayoutObject = childLayoutObject,
            IsPseudoElement = IsPseudoElement,
            PseudoElementType = PseudoElementType
        };
    }
}