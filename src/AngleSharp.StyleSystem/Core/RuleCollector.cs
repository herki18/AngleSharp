namespace AngleSharp.StyleSystem.Core;

using System.Collections.Generic;
using System.Linq;
using Css;
using Css.Dom;
using Dom;

/// <summary>
/// Collects and matches CSS rules for elements.
/// </summary>
public class RuleCollector
{
    private readonly IBrowsingContext _context;
    private readonly StyleSheetManager _stylesheetManager;
    private readonly Dictionary<string, List<MatchedRule>> _selectorMatchCache = new();

    public RuleCollector(IBrowsingContext context, StyleSheetManager stylesheetManager)
    {
        _context = context;
        _stylesheetManager = stylesheetManager;

        // Subscribe to stylesheet changes to invalidate cache
        _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
    }

    /// <summary>
    /// Collects all rules that match the element.
    /// </summary>
    public IEnumerable<MatchedRule> CollectMatchingRules(IElement element, string? pseudoElement = null)
    {
        var matchedRules = new List<MatchedRule>();
        var index = 0;

        // Create a cache key for this element and pseudo-element
        var cacheKey = GetElementCacheKey(element, pseudoElement);

        // Check if we have cached results
        if (_selectorMatchCache.TryGetValue(cacheKey, out var cachedRules))
        {
            return cachedRules;
        }

        // Get all style rules from the StyleSheetManager
        foreach (var entry in _stylesheetManager.GetStylesheets())
        {
            // For each rule in the stylesheet
            foreach (var rule in entry.Stylesheet.Rules.OfType<ICssStyleRule>())
            {
                // Check if the rule's selector matches the element and pseudoElement
                if (DoesSelectorMatch(rule, element, pseudoElement))
                {
                    // Calculate specificity
                    var specificity = CalculateSpecificity(rule.SelectorText);

                    matchedRules.Add(new MatchedRule
                    {
                        Rule = rule,
                        Specificity = new Priority(specificity),
                        Origin = entry.Origin,
                        OriginalIndex = index++
                    });
                }
            }
        }

        // Sort rules by specificity and order
        var sortedRules = matchedRules
            .OrderBy(r => r.Specificity)
            .ThenBy(r => (int)r.Origin)
            .ThenBy(r => r.OriginalIndex)
            .ToList();

        // Cache the results
        _selectorMatchCache[cacheKey] = sortedRules;

        return sortedRules;
    }

    private string GetElementCacheKey(IElement element, string? pseudoElement)
    {
        // Use the element's reference hash code for uniqueness
        int elementHash = element.GetHashCode();

        // Combine with pseudo-element for a complete key
        return $"{elementHash}:{pseudoElement ?? "null"}";
    }

    private void StylesheetManager_StylesheetChanged(object? sender, StylesheetChangedEventArgs e)
    {
        // Any stylesheet change invalidates the entire cache
        ClearCache();
    }

    public void ClearCache()
    {
        _selectorMatchCache.Clear();
    }

    /// <summary>
    /// Checks if a rule's selector matches an element.
    /// </summary>
    private bool DoesSelectorMatch(ICssStyleRule rule, IElement element, string? pseudoElement)
    {
        // In a real implementation, we would use AngleSharp's selector matching
        // For simplicity, we'll do a very basic check here
        try
        {
            var matches = element.Matches(rule.SelectorText);

            // TODO: Handle pseudo-elements properly
            var hasPseudo = rule.SelectorText.Contains(":");

            return matches && (pseudoElement == null || hasPseudo);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Calculates the specificity of a selector.
    /// </summary>
    private uint CalculateSpecificity(string selector)
    {
        // In a real implementation, we would calculate actual specificity
        // For simplicity, we'll return a placeholder value
        return 1;
    }
}