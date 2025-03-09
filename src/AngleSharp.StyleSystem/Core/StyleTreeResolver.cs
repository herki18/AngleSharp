namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;
using Css.Dom;

/// <summary>
/// Handles the traversal of element trees for style resolution and coordinates style computation scheduling.
/// </summary>
public class StyleTreeResolver : IStyleTreeResolver
{
    private readonly StyleEngine _styleEngine;
    private readonly IStyleApplicationStrategy _strategy;
    private readonly StyleCache _styleCache;
    private readonly StyleInvalidationTracker _invalidationTracker;
    private readonly Stack<IElement> _processingStack;
    private readonly Dictionary<IElement, IElement> _styleSharingMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleTreeResolver"/> class.
    /// </summary>
    /// <param name="styleEngine">The style engine.</param>
    /// <param name="strategy">The style application strategy.</param>
    /// <param name="styleCache">The style cache.</param>
    /// <param name="invalidationTracker">The style invalidation tracker.</param>
    public StyleTreeResolver(
        StyleEngine styleEngine,
        IStyleApplicationStrategy strategy,
        StyleCache styleCache,
        StyleInvalidationTracker invalidationTracker)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _styleCache = styleCache ?? throw new ArgumentNullException(nameof(styleCache));
        _invalidationTracker = invalidationTracker ?? throw new ArgumentNullException(nameof(invalidationTracker));
        _processingStack = new Stack<IElement>();
        _styleSharingMap = new Dictionary<IElement, IElement>();
    }

    /// <inheritdoc/>
    public IElement? CurrentElement => _processingStack.Count > 0 ? _processingStack.Peek() : null;

    /// <inheritdoc/>
    public bool IsResolving => _processingStack.Count > 0;

    /// <inheritdoc/>
    public void ResolveStylesForSubtree(IElement element, bool forceRecalculate = false)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // Clear style sharing map for this resolution pass
        _styleSharingMap.Clear();

        foreach (var current in _strategy.GetElementTraversalOrder(element))
        {
            // Skip if already up-to-date and not forced
            if (!forceRecalculate && !_invalidationTracker.NeedsStyleRecalculation(current))
                continue;

            // Skip subtrees that don't need styling
            if (_strategy.ShouldSkipSubtree(current))
            {
                _invalidationTracker.MarkAsUpToDate(current);
                continue;
            }

            var context = _strategy.CreateStyleContext(current);
            ResolveElementStyle(current, context.ParentStyle);
        }
    }

    /// <inheritdoc/>
    public IComputedStyle ResolveElementStyle(IElement element, IComputedStyle? parentStyle = null, string? pseudoElement = null)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // Check for circular references
        if (_processingStack.Contains(element))
        {
            // Prevent infinite recursion by using parent style or empty style
            return parentStyle ?? CreateEmptyStyle(element);
        }

        // Check cache first
        var cacheKey = new StyleCacheKey(element, pseudoElement);
        if (_styleCache.TryGetValue(cacheKey, out var cachedStyle))
        {
            return cachedStyle;
        }

        // Check if we can share style
        if (pseudoElement == null && TryGetSharedStyle(element, out var sharedStyle))
        {
            _styleCache.Store(cacheKey, sharedStyle);
            _invalidationTracker.MarkAsUpToDate(element);
            return sharedStyle;
        }

        // Compute parent style if not provided
        if (parentStyle == null && element.ParentElement != null)
        {
            _processingStack.Push(element);
            try
            {
                parentStyle = ResolveElementStyle(element.ParentElement);
            }
            finally
            {
                _processingStack.Pop();
            }
        }

        // Compute the style
        _processingStack.Push(element);
        try
        {
            var computedStyle = ComputeElementStyleInternal(element, parentStyle, pseudoElement);
            _styleCache.Store(cacheKey, computedStyle);
            _invalidationTracker.MarkAsUpToDate(element);
            return computedStyle;
        }
        finally
        {
            _processingStack.Pop();
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IElement> GetElementsNeedingStyleResolution(IElement root)
    {
        return _invalidationTracker.GetElementsToUpdate(root);
    }

    /// <inheritdoc/>
    public void ClearCache()
    {
        _styleCache.Clear();
        _styleSharingMap.Clear();
    }

    /// <inheritdoc/>
    public bool CanShareStyle(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // Check if element is already mapped for style sharing
        if (_styleSharingMap.ContainsKey(element))
            return true;

        // Look for potential style donors with the same tag, class, and parent
        if (element.ParentElement != null)
        {
            foreach (var sibling in element.ParentElement.Children)
            {
                if (sibling == element || !_invalidationTracker.IsUpToDate(sibling))
                    continue;

                if (_strategy.CanShareStyleWith(element, sibling))
                {
                    _styleSharingMap[element] = sibling;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to get a shared style from another element.
    /// </summary>
    /// <param name="element">The element to find shared style for.</param>
    /// <param name="sharedStyle">The shared computed style, if found.</param>
    /// <returns>True if a shared style was found; otherwise, false.</returns>
    private bool TryGetSharedStyle(IElement element, out IComputedStyle sharedStyle)
    {
        sharedStyle = null!;

        if (!CanShareStyle(element))
            return false;

        if (!_styleSharingMap.TryGetValue(element, out var donor))
            return false;

        var donorCacheKey = new StyleCacheKey(donor, null);
        if (!_styleCache.TryGetValue(donorCacheKey, out sharedStyle))
            return false;

        return true;
    }

    /// <summary>
    /// Computes the style for an element.
    /// </summary>
    /// <param name="element">The element to compute style for.</param>
    /// <param name="parentStyle">The parent element's computed style.</param>
    /// <param name="pseudoElement">Optional pseudo-element selector.</param>
    /// <returns>The computed style.</returns>
    private IComputedStyle ComputeElementStyleInternal(IElement element, IComputedStyle? parentStyle, string? pseudoElement)
    {
        // Get style rules that match this element
        var matchedRules = _styleEngine.RuleCollector.CollectMatchingRules(element, pseudoElement);

        // Cascade the matched rules
        var cascadedStyle = _styleEngine.CascadeResolver.ResolveCascade(matchedRules, element);

        // Apply inheritance
        var inheritedStyle = _styleEngine.InheritanceProcessor.ApplyInheritance(cascadedStyle, parentStyle);

        // Extract variables
        _styleEngine.VariableResolver.ExtractVariablesFromStyle(element, inheritedStyle);

        // Build the computed style
        var computedStyle = _styleEngine.ComputedStyleBuilder.BuildComputedStyle(inheritedStyle, element, parentStyle);
        return computedStyle!;
    }

    /// <summary>
    /// Creates an empty style for an element when a proper style cannot be computed.
    /// </summary>
    /// <param name="element">The element to create style for.</param>
    /// <returns>A basic computed style.</returns>
    private IComputedStyle CreateEmptyStyle(IElement element)
    {
        var factory = _styleEngine.StyleFactory as ComputedStyleFactory;
        if (factory != null)
        {
            // Create empty style declaration
            var emptyStyle = new CssStyleDeclaration(_styleEngine.Context);
            return factory.CreateComputedStyle(element, null, emptyStyle);
        }

        throw new InvalidOperationException("Unable to create an empty style: StyleFactory is not available or is not a ComputedStyleFactory");
    }
}