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

    // List of known pseudo-elements (for legacy syntax detection)
    private static readonly HashSet<string> _pseudoElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "before", "after", "first-line", "first-letter",
        "selection", "backdrop", "placeholder", "marker",
        "spelling-error", "grammar-error"
    };

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
            var selectorText = rule.SelectorText;

            // Case 1: Looking for a pseudo-element
            if (pseudoElement != null)
            {
                // Check if this selector contains the requested pseudo-element
                // Check both modern (::) and legacy (:) syntax
                if (!ContainsPseudoElement(selectorText, pseudoElement))
                {
                    return false;
                }

                // Extract the base selector (the part before the pseudo-element)
                string baseSelector = ExtractBaseSelector(selectorText, pseudoElement);

                // Check if the base selector matches the element
                return string.IsNullOrEmpty(baseSelector) || element.Matches(baseSelector);
            }

            // Case 2: Looking for a regular element (no pseudo-element)
            // First check if the selector matches the element
            bool matches = element.Matches(selectorText);

            // Then make sure it doesn't contain pseudo-elements
            return matches && !HasAnyPseudoElement(selectorText);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool ContainsPseudoElement(string selectorText, string pseudoElement)
    {
        // Check for both modern (::) and legacy (:) syntax
        return selectorText.Contains($"::{pseudoElement}", StringComparison.OrdinalIgnoreCase) ||
               selectorText.Contains($":{pseudoElement}", StringComparison.OrdinalIgnoreCase);
    }

    private string ExtractBaseSelector(string selectorText, string pseudoElement)
    {
        // Find the position of the pseudo-element in the selector (both syntaxes)
        int doubleColonPos = selectorText.IndexOf($"::{pseudoElement}", StringComparison.OrdinalIgnoreCase);
        int singleColonPos = selectorText.IndexOf($":{pseudoElement}", StringComparison.OrdinalIgnoreCase);

        int pseudoPos;
        if (doubleColonPos >= 0)
        {
            pseudoPos = doubleColonPos;
        }
        else if (singleColonPos >= 0)
        {
            pseudoPos = singleColonPos;
        }
        else
        {
            return selectorText; // Shouldn't happen as we already checked for the pseudo-element
        }

        // Return everything before the pseudo-element part
        return selectorText.Substring(0, pseudoPos).Trim();
    }

    private bool HasAnyPseudoElement(string selectorText)
    {
        // Check for double-colon pseudo-elements (modern syntax)
        if (selectorText.Contains("::"))
            return true;

        // Check for single-colon pseudo-elements (legacy syntax)
        int colonPos = -1;
        while ((colonPos = selectorText.IndexOf(':', colonPos + 1)) >= 0)
        {
            // Skip double-colons as we've already checked them
            if (colonPos + 1 < selectorText.Length && selectorText[colonPos + 1] == ':')
            {
                colonPos++;
                continue;
            }

            // Extract the pseudo name
            string pseudoName = ExtractPseudoName(selectorText, colonPos + 1);

            // Check if it's a known pseudo-element
            if (_pseudoElements.Contains(pseudoName))
                return true;
        }

        return false;
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
                if (!_pseudoElements.Contains(pseudoName))
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
                if (_pseudoElements.Contains(pseudoName))
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

        if (endIndex > startIndex)
            return selectorText.Substring(startIndex, endIndex - startIndex);

        return string.Empty;
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