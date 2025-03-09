namespace AngleSharp.StyleSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Css;
using Css.Dom;
using Css.Parser;
using Dom;

public class RuleCollector
{
    private readonly IBrowsingContext _context;
    private readonly StyleSheetManager _stylesheetManager;
    private readonly Dictionary<string, List<MatchedRule>> _selectorMatchCache = new();
    private readonly ICssSelectorParser _selectorParser;

    public RuleCollector(IBrowsingContext context, StyleSheetManager stylesheetManager)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _stylesheetManager = stylesheetManager ?? throw new ArgumentNullException(nameof(stylesheetManager));
        _stylesheetManager.StylesheetChanged += StylesheetManager_StylesheetChanged;
        _selectorParser = _context.GetService<ICssSelectorParser>() ?? throw new InvalidOperationException();
    }

    public IEnumerable<MatchedRule> CollectMatchingRules(IElement element, string? pseudoElement = null)
    {
        var matchedRules = new List<MatchedRule>();
        var index = 0;
        var cacheKey = GetElementCacheKey(element, pseudoElement);

        if (_selectorMatchCache.TryGetValue(cacheKey, out var cachedRules))
        {
            return cachedRules;
        }

        foreach (var entry in _stylesheetManager.GetStylesheets())
        {
            foreach (var rule in entry.Stylesheet.Rules.OfType<ICssStyleRule>())
            {
                if (DoesSelectorMatch(rule, element, pseudoElement))
                {
                    var specificity = CalculateSpecificity(rule.SelectorText);
                    matchedRules.Add(new MatchedRule
                    {
                        Rule = rule,
                        Specificity = specificity,
                        Origin = entry.Origin,
                        OriginalIndex = index++
                    });
                }
            }
        }

        // Sort rules by specificity, origin, and source order
        var sortedRules = matchedRules
            .OrderBy(r => (int)r.Origin)
            .ThenBy(r => r.Specificity)
            .ThenBy(r => r.OriginalIndex)
            .ToList();

        _selectorMatchCache[cacheKey] = sortedRules;
        return sortedRules;
    }

    private string GetElementCacheKey(IElement element, string? pseudoElement)
    {
        int elementHash = element.GetHashCode();
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
        try
        {
            var matches = element.Matches(rule.SelectorText);

            // Handle pseudo-elements if specified
            if (pseudoElement != null)
            {
                // Only match if the selector contains the specified pseudo-element
                return matches && rule.SelectorText.Contains($"::{pseudoElement}");
            }

            // For normal elements, exclude rules targeting pseudo-elements
            return matches && !HasPseudoElement(rule.SelectorText);
        }
        catch
        {
            return false;
        }
    }

    private bool HasPseudoElement(string selectorText)
    {
        // Check for double-colon pseudo-elements like ::before, ::after, etc.
        return selectorText.Contains("::");
    }

    /// <summary>
    /// Calculates the specificity of a CSS selector according to the CSS specification.
    /// </summary>
    /// <param name="selectorText">The CSS selector text to analyze.</param>
    /// <returns>A Priority object representing the specificity.</returns>
    private Priority CalculateSpecificity(string selectorText)
    {
        if (string.IsNullOrWhiteSpace(selectorText))
            return Priority.Zero;

        try
        {
            // Try to use AngleSharp's built-in selector parsing
            if (_selectorParser != null)
            {
                var selector = _selectorParser.ParseSelector(selectorText);
                if (selector != null)
                {
                    return selector.Specificity;
                }
            }
        }
        catch
        {
            // Fall back to manual calculation if parsing fails
        }

        // Manual fallback calculation
        byte inlines = 0;
        byte ids = 0;
        byte classes = 0;
        byte tags = 0;

        // Count inline styles (style attribute)
        if (selectorText.Contains("[style]", StringComparison.OrdinalIgnoreCase))
        {
            inlines = 1;
        }

        // Count ID selectors
        ids = CountOccurrences(selectorText, '#');

        // Count class selectors, attribute selectors, and pseudo-classes
        classes += CountOccurrences(selectorText, '.');
        classes += CountAttributeSelectors(selectorText);
        classes += CountPseudoClasses(selectorText);

        // Count element type selectors and pseudo-elements
        tags += CountElementSelectors(selectorText);
        tags += CountPseudoElements(selectorText);

        return new Priority(inlines, ids, classes, tags);
    }

    private byte CountOccurrences(string text, char c)
    {
        return (byte)text.Count(ch => ch == c);
    }

    private byte CountAttributeSelectors(string selectorText)
    {
        byte count = 0;
        int startIndex = 0;

        while ((startIndex = selectorText.IndexOf('[', startIndex)) >= 0)
        {
            int endIndex = selectorText.IndexOf(']', startIndex);
            if (endIndex > startIndex)
            {
                count++;
                startIndex = endIndex + 1;
            }
            else
            {
                break;
            }
        }

        return count;
    }

    private byte CountPseudoClasses(string selectorText)
    {
        byte count = 0;
        int index = 0;

        while ((index = selectorText.IndexOf(':', index)) >= 0)
        {
            // Skip double-colon pseudo-elements
            if (index + 1 < selectorText.Length && selectorText[index + 1] == ':')
            {
                index += 2;
                continue;
            }

            // Count single-colon pseudo-classes
            if (index + 1 < selectorText.Length && selectorText[index + 1] != ':')
            {
                // Exclude pseudo-elements with single colon (legacy syntax)
                string pseudoName = ExtractPseudoName(selectorText, index + 1);
                if (!IsPseudoElement(pseudoName))
                {
                    count++;
                }
            }

            index++;
        }

        return count;
    }

    private byte CountPseudoElements(string selectorText)
    {
        byte count = 0;
        int index = 0;

        // Count double-colon pseudo-elements
        while ((index = selectorText.IndexOf("::", index)) >= 0)
        {
            count++;
            index += 2;
        }

        // Count single-colon pseudo-elements (legacy syntax)
        index = 0;
        while ((index = selectorText.IndexOf(':', index)) >= 0)
        {
            if (index + 1 < selectorText.Length && selectorText[index + 1] != ':')
            {
                string pseudoName = ExtractPseudoName(selectorText, index + 1);
                if (IsPseudoElement(pseudoName))
                {
                    count++;
                }
            }

            index++;
        }

        return count;
    }

    private string ExtractPseudoName(string selectorText, int startIndex)
    {
        int endIndex = startIndex;
        while (endIndex < selectorText.Length)
        {
            char c = selectorText[endIndex];
            if (!char.IsLetterOrDigit(c) && c != '-')
            {
                break;
            }
            endIndex++;
        }

        return selectorText.Substring(startIndex, endIndex - startIndex);
    }

    private bool IsPseudoElement(string name)
    {
        // Common pseudo-elements
        return name == "before" || name == "after" || name == "first-line" ||
               name == "first-letter" || name == "selection" || name == "backdrop" ||
               name == "placeholder" || name == "marker" || name == "spelling-error" ||
               name == "grammar-error";
    }

    private byte CountElementSelectors(string selectorText)
    {
        byte count = 0;
        int index = 0;

        while (index < selectorText.Length)
        {
            // Skip whitespace
            while (index < selectorText.Length && char.IsWhiteSpace(selectorText[index]))
            {
                index++;
            }

            if (index >= selectorText.Length)
            {
                break;
            }

            // Skip combinators
            if (selectorText[index] == '>' || selectorText[index] == '+' ||
                selectorText[index] == '~' || selectorText[index] == ',')
            {
                index++;
                continue;
            }

            // Skip attribute selectors, IDs, classes, and pseudo-selectors
            if (selectorText[index] == '[' || selectorText[index] == '#' ||
                selectorText[index] == '.' || selectorText[index] == ':')
            {
                index++;
                continue;
            }

            // Start of a potential element selector
            if (char.IsLetter(selectorText[index]) || selectorText[index] == '*' || selectorText[index] == '_')
            {
                // Special handling for universal selector
                if (selectorText[index] == '*')
                {
                    // Universal selector doesn't count towards specificity
                    index++;
                    continue;
                }

                // Found an element selector
                count++;

                // Skip to the end of the element name
                while (index < selectorText.Length &&
                       (char.IsLetterOrDigit(selectorText[index]) ||
                        selectorText[index] == '-' ||
                        selectorText[index] == '_'))
                {
                    index++;
                }
            }
            else
            {
                index++;
            }
        }

        return count;
    }
}