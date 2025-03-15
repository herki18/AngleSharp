namespace AngleSharp.StyleSystem.Computation;
using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Events;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;

public class RuleCollector : IRuleCollector, IDisposable
{
    private readonly IBrowsingContext _context;
    private readonly IStyleSheetManager _stylesheetManager;
    private readonly Dictionary<string, List<MatchedRule>> _selectorMatchCache = new();
    private readonly ICssSelectorParser _selectorParser;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISubscriptionToken[] _subscriptionTokens;

    private static readonly HashSet<string> _pseudoElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "before", "after", "first-line", "first-letter",
        "selection", "backdrop", "placeholder", "marker",
        "spelling-error", "grammar-error"
    };

    public RuleCollector(
        IBrowsingContext context,
        IStyleSheetManager stylesheetManager,
        IEventAggregator eventAggregator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _stylesheetManager = stylesheetManager ?? throw new ArgumentNullException(nameof(stylesheetManager));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        _selectorParser = _context.GetService<ICssSelectorParser>() ??
            throw new InvalidOperationException("CSS Selector Parser service not available");

        _subscriptionTokens = new[]
        {
            _eventAggregator.Subscribe<StylesheetChangedEvent>(_ => ClearCache()),
            _eventAggregator.Subscribe<StylesheetsRefreshedEvent>(_ => ClearCache())
        };
    }

    public IEnumerable<MatchedRule> CollectMatchingRules(IElement element, string? pseudoElement = null)
    {
        var cacheKey = GetElementCacheKey(element, pseudoElement);
        if (_selectorMatchCache.TryGetValue(cacheKey, out var cachedRules))
        {
            return cachedRules;
        }

        var matchedRules = new List<MatchedRule>();
        var index = 0;

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

        var sortedRules = matchedRules
            .OrderBy(r => (int)r.Origin)
            .ThenBy(r => r.Specificity)
            .ThenBy(r => r.OriginalIndex)
            .ToList();

        _selectorMatchCache[cacheKey] = sortedRules;
        return sortedRules;
    }

    public void ClearCache()
    {
        _selectorMatchCache.Clear();
    }

    private string GetElementCacheKey(IElement element, string? pseudoElement)
    {
        int elementHash = element.GetHashCode();
        return $"{elementHash}:{pseudoElement ?? "null"}";
    }

    // Rest of the implementation remains the same

    // (Keeping the remaining methods from the original implementation)

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

    private Priority CalculateSpecificity(string selectorText)
    {
        if (string.IsNullOrWhiteSpace(selectorText))
            return Priority.Zero;
        try
        {
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
        }
        byte inlines = 0;
        byte ids = 0;
        byte classes = 0;
        byte tags = 0;
        if (selectorText.Contains("[style]", StringComparison.OrdinalIgnoreCase))
        {
            inlines = 1;
        }
        ids = CountOccurrences(selectorText, '#');
        classes += CountOccurrences(selectorText, '.');
        classes += CountAttributeSelectors(selectorText);
        classes += CountPseudoClasses(selectorText);
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
            if (index + 1 < selectorText.Length && selectorText[index + 1] == ':')
            {
                index += 2;
                continue;
            }
            if (index + 1 < selectorText.Length && selectorText[index + 1] != ':')
            {
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
        while ((index = selectorText.IndexOf("::", index)) >= 0)
        {
            count++;
            index += 2;
        }
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
            while (index < selectorText.Length && char.IsWhiteSpace(selectorText[index]))
            {
                index++;
            }
            if (index >= selectorText.Length)
            {
                break;
            }
            if (selectorText[index] == '>' || selectorText[index] == '+' ||
                selectorText[index] == '~' || selectorText[index] == ',')
            {
                index++;
                continue;
            }
            if (selectorText[index] == '[' || selectorText[index] == '#' ||
                selectorText[index] == '.' || selectorText[index] == ':')
            {
                index++;
                continue;
            }
            if (char.IsLetter(selectorText[index]) || selectorText[index] == '*' || selectorText[index] == '_')
            {
                if (selectorText[index] == '*')
                {
                    index++;
                    continue;
                }
                count++;
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

    public void Dispose()
    {
        foreach (var token in _subscriptionTokens)
        {
            token.Dispose();
        }
    }
}