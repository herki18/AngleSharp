namespace AngleSharp.StyleSystem.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Core.Interfaces;
using Css.Dom;

public class StyleTreeResolver : IStyleTreeResolver
{
    private readonly StyleEngine _styleEngine;
    private readonly IStyleApplicationStrategy _strategy;
    private readonly StyleCache _styleCache;
    private readonly StyleInvalidationTracker _invalidationTracker;
    private readonly Stack<IElement> _processingStack;
    private readonly Dictionary<IElement, IElement> _styleSharingMap;

    // Settings for optimization batching
    private const int OptimizationBatchSize = 50;
    private readonly List<PropertyTreeNode> _pendingOptimizations = new List<PropertyTreeNode>();

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

    public IElement? CurrentElement => _processingStack.Count > 0 ? _processingStack.Peek() : null;

    public bool IsResolving => _processingStack.Count > 0;

    public void ResolveStylesForSubtree(IElement element, bool forceRecalculate = false)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        _styleSharingMap.Clear();
        _pendingOptimizations.Clear();

        // Use a breadth-first traversal for better style sharing opportunities
        var elementsByLevel = CollectElementsByLevel(element);
        int totalElements = elementsByLevel.Sum(level => level.Count);

        // Process elements level by level (top-down)
        foreach (var levelElements in elementsByLevel)
        {
            // First pass: identify style donors and create style contexts
            var contexts = new Dictionary<IElement, StyleContext>();
            foreach (var current in levelElements)
            {
                if (!forceRecalculate && !_invalidationTracker.NeedsStyleRecalculation(current))
                    continue;

                if (_strategy.ShouldSkipSubtree(current))
                {
                    _invalidationTracker.MarkAsUpToDate(current);
                    continue;
                }

                var context = _strategy.CreateStyleContext(current);
                contexts[current] = context;
            }

            // Second pass: process elements with similar contexts together
            // Group elements by potential style similarity to maximize sharing
            var similarityGroups = GroupElementsBySimilarity(contexts);

            foreach (var group in similarityGroups)
            {
                foreach (var current in group)
                {
                    ResolveElementStyle(current, contexts[current].ParentStyle);

                    // Add the property tree node to pending optimizations
                    if (GetPropertyTreeNode(current) is PropertyTreeNode node)
                    {
                        _pendingOptimizations.Add(node);

                        // Perform batch optimization when we reach the threshold
                        if (_pendingOptimizations.Count >= OptimizationBatchSize)
                        {
                            OptimizeBatch();
                        }
                    }
                }
            }
        }

        // Optimize any remaining nodes
        if (_pendingOptimizations.Count > 0)
        {
            OptimizeBatch();
        }
    }

    private void OptimizeBatch()
    {
        // First sort by element depth to optimize parent nodes before children
        // This improves hierarchical optimization
        var sortedNodes = _pendingOptimizations
            .OrderBy(node => GetNodeDepth(node))
            .ToList();

        foreach (var node in sortedNodes)
        {
            _styleEngine.PropertyTreeManager.OptimizeTree(node);
        }

        _pendingOptimizations.Clear();
    }

    private int GetNodeDepth(PropertyTreeNode node)
    {
        int depth = 0;
        var current = node;

        while (current.GetParent() != null)
        {
            depth++;
            current = current.GetParent()!;
        }

        return depth;
    }

    private List<List<IElement>> CollectElementsByLevel(IElement root)
    {
        var result = new List<List<IElement>>();
        var currentLevel = new List<IElement> { root };

        while (currentLevel.Count > 0)
        {
            result.Add(currentLevel);
            var nextLevel = new List<IElement>();

            foreach (var element in currentLevel)
            {
                foreach (var child in element.Children)
                {
                    nextLevel.Add(child);
                }
            }

            currentLevel = nextLevel;
        }

        return result;
    }

    private List<List<IElement>> GroupElementsBySimilarity(Dictionary<IElement, StyleContext> contexts)
    {
        // Group elements that are likely to have similar styles
        // This increases chances of style sharing
        var groups = new Dictionary<string, List<IElement>>();

        foreach (var entry in contexts)
        {
            var element = entry.Key;
            var similarityKey = ComputeSimilarityKey(element);

            if (!groups.TryGetValue(similarityKey, out var group))
            {
                group = new List<IElement>();
                groups[similarityKey] = group;
            }

            group.Add(element);
        }

        return groups.Values.ToList();
    }

    private string ComputeSimilarityKey(IElement element)
    {
        // Compute a key based on factors that influence style similarity
        // Elements with the same key are likely to have similar styles
        return $"{element.NodeName}|{element.ClassName}|{element.Id}|{element.ParentElement?.NodeName ?? "none"}";
    }

    public IComputedStyle ResolveElementStyle(IElement element, IComputedStyle? parentStyle = null, string? pseudoElement = null)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (_processingStack.Contains(element))
        {
            return parentStyle ?? CreateEmptyStyle(element);
        }

        var cacheKey = new StyleCacheKey(element, pseudoElement);
        if (_styleCache.TryGetValue(cacheKey, out var cachedStyle))
        {
            return cachedStyle;
        }

        if (pseudoElement == null && TryGetSharedStyle(element, out var sharedStyle))
        {
            _styleCache.Store(cacheKey, sharedStyle);
            _invalidationTracker.MarkAsUpToDate(element);
            return sharedStyle;
        }

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

    public IEnumerable<IElement> GetElementsNeedingStyleResolution(IElement root)
    {
        return _invalidationTracker.GetElementsToUpdate(root);
    }

    public void ClearCache()
    {
        _styleCache.Clear();
        _styleSharingMap.Clear();
        _pendingOptimizations.Clear();
    }

    public bool CanShareStyle(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (_styleSharingMap.ContainsKey(element))
            return true;

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

    private IComputedStyle ComputeElementStyleInternal(IElement element, IComputedStyle? parentStyle, string? pseudoElement)
    {
        var matchedRules = _styleEngine.RuleCollector.CollectMatchingRules(element, pseudoElement);
        var cascadedStyle = _styleEngine.CascadeResolver.ResolveCascade(matchedRules, element);
        var inheritedStyle = _styleEngine.InheritanceProcessor.ApplyInheritance(cascadedStyle, parentStyle);
        _styleEngine.VariableResolver.ExtractVariablesFromStyle(element, inheritedStyle);
        var computedStyle = _styleEngine.ComputedStyleBuilder.BuildComputedStyle(inheritedStyle, element, parentStyle);
        return computedStyle!;
    }

    private IComputedStyle CreateEmptyStyle(IElement element)
    {
        var factory = _styleEngine.StyleFactory as ComputedStyleFactory;
        if (factory != null)
        {
            var emptyStyle = new CssStyleDeclaration(_styleEngine.Context);
            return factory.CreateComputedStyle(element, null, emptyStyle);
        }

        throw new InvalidOperationException("Unable to create an empty style: StyleFactory is not available or is not a ComputedStyleFactory");
    }

    private PropertyTreeNode? GetPropertyTreeNode(IElement element)
    {
        // Extract the PropertyTreeNode from the element's computed style
        if (_styleCache.TryGetValue(new StyleCacheKey(element, null), out var style) &&
            style is ComputedStyle computedStyle)
        {
            return computedStyle.PropertyTreeNode;
        }

        return null;
    }
}