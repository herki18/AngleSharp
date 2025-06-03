namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css.Dom;
using LayoutEngine.NG.Layout.Dom;

/// <summary>
/// StyleResolver computes the immutable ComputedStyle for a DOM element.
///
/// - It collects all matching rules for the element (cascade).
/// - It applies the cascade to produce an ICssStyleDeclaration.
/// - It merges inherited properties from the parent using AngleSharp's UpdateDeclarations.
/// - It relies on AngleSharp's property metadata and converter logic to ensure every property
///   returns the correct computed value (cascade, inherit, or initial value).
/// - No manual setting of initial values is needed: AngleSharp handles this.
/// - The result is wrapped in a ComputedStyle for typed, read-only access.
/// - Computed styles are cached for performance.
/// </summary>
public class StyleResolver : IStyleResolver
{
    private ICascadeResolver _cascadeResolver;
    private readonly LayoutDataManager _layoutDataManager;
    private readonly Dictionary<string, ComputedStyle> _matchedPropertiesCache = new();

    public StyleResolver(ICascadeResolver cascadeResolver, LayoutDataManager layoutDataManager)
    {
        _cascadeResolver = cascadeResolver ?? throw new ArgumentNullException(nameof(cascadeResolver));
        _layoutDataManager = layoutDataManager ?? throw new ArgumentNullException(nameof(layoutDataManager));
    }

    public ICascadeResolver CascadeResolver
    {
        get => _cascadeResolver;
        set => _cascadeResolver = value ?? throw new ArgumentNullException(nameof(value));
    }

    public ComputedStyle ResolveStyle(IElement element, ComputedStyle? parentStyle = null)
    {
        var context = new StyleRecalcContext
        {
            ParentStyle = parentStyle,
            LayoutParentStyle = parentStyle
        };
        return ResolveStyle(element, context);
    }

    public ComputedStyle ResolveStyle(IElement element, StyleRecalcContext context)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var docLayout = _layoutDataManager.GetOrCreate(element.OwnerDocument!);
        var styleEngine = docLayout.StyleEngine;

        // 1. Collect matching rules
        var matchResult = styleEngine.ElementRuleCollector.CollectMatchingRules(element, context);
        var allMatchedRules = matchResult.GetRulesInCascadeOrder().ToList();

        // 2. Apply cascade to get resolved style declaration
        var resolvedDeclaration = _cascadeResolver.ResolveCascade(allMatchedRules, element);

        // 3. Merge inherited properties from parent, if any
        if (context.ParentStyle != null && resolvedDeclaration is CssStyleDeclaration cssStyleDeclaration)
        {
            // This will only merge properties that CanBeInherited, and only if not already set
            cssStyleDeclaration.UpdateDeclarations(context.ParentStyle.Raw);
        }

        // 4. Wrap in ComputedStyle (immutable)
        var computedStyle = new ComputedStyle(resolvedDeclaration);

        // 5. Optionally cache
        var cacheKey = GenerateCacheKey(element, allMatchedRules);
        if (!string.IsNullOrEmpty(cacheKey))
            _matchedPropertiesCache[cacheKey] = computedStyle;

        return computedStyle;
    }

    private string GenerateCacheKey(IElement element, List<MatchedRule> rules)
    {
        var parts = new List<string>
        {
            element.LocalName,
            element.Id ?? "",
            string.Join(",", element.ClassList.OrderBy(c => c))
        };
        parts.Add(rules.Count.ToString());
        return string.Join("|", parts);
    }

    public void InvalidateMatchedPropertiesCache() => _matchedPropertiesCache.Clear();

    public void ComputeFont(IElement element, ComputedStyle fontStyle, IEnumerable<ICssProperty> fontProperties)
    {
        // Not needed for immutable ComputedStyle; font computation is handled by the cascade and inheritance.
    }
}