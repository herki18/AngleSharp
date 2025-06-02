namespace LayoutEngine.NG.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

/// <summary>
/// Indexes CSS rules by selector type for efficient matching during style resolution.
/// In BlinkNG, this is critical for performance when matching thousands of rules.
/// </summary>
internal class RuleSet
{
    // Rules indexed by ID selector (e.g., #header)
    private readonly Dictionary<string, List<RuleData>> _idRules = new();

    // Rules indexed by class selector (e.g., .container)
    private readonly Dictionary<string, List<RuleData>> _classRules = new();

    // Rules indexed by tag selector (e.g., div)
    private readonly Dictionary<string, List<RuleData>> _tagRules = new();

    // Rules with pseudo-class selectors (e.g., :hover)
    private readonly List<RuleData> _pseudoRules = new();

    // Universal rules and complex selectors
    private readonly List<RuleData> _universalRules = new();

    // All rules in document order (for cascade)
    private readonly List<RuleData> _allRules = new();

    /// <summary>
    /// Adds a style rule to the rule set with appropriate indexing.
    /// </summary>
    /// <param name="rule">The CSS style rule to add.</param>
    /// <param name="position">The position in document order.</param>
    /// <param name="origin">The stylesheet origin.</param>
    internal void AddRule(ICssStyleRule rule, int position, StylesheetOrigin origin)
    {
        if (rule.Selector == null)
            return;

        // Create rule data
        var ruleData = new RuleData(rule, position, origin);
        _allRules.Add(ruleData);

        // Analyze selector and add to appropriate indexes
        // In BlinkNG, this is done by RuleSet::AddRule and SelectorChecker
        IndexRule(ruleData);
    }

    /// <summary>
    /// Gets candidate rules that might match the given element.
    /// This is a performance-critical method in style resolution.
    /// </summary>
    /// <param name="element">The element to find matching rules for.</param>
    /// <returns>Rules that potentially match the element.</returns>
    internal IEnumerable<RuleData> GetCandidateRules(IElement element)
    {
        // Collect from ID rules
        var id = element.Id;
        if (!string.IsNullOrEmpty(id) && _idRules.TryGetValue(id, out var idMatches))
        {
            foreach (var rule in idMatches)
                yield return rule;
        }

        // Collect from class rules
        foreach (var className in element.ClassList)
        {
            if (_classRules.TryGetValue(className, out var classMatches))
            {
                foreach (var rule in classMatches)
                    yield return rule;
            }
        }

        // Collect from tag rules
        var tagName = element.LocalName.ToLowerInvariant();
        if (_tagRules.TryGetValue(tagName, out var tagMatches))
        {
            foreach (var rule in tagMatches)
                yield return rule;
        }

        // Always check universal rules
        foreach (var rule in _universalRules)
            yield return rule;

        // Check pseudo rules if element has any pseudo states
        // TODO: Only return if element actually has the pseudo state
        foreach (var rule in _pseudoRules)
            yield return rule;
    }

    /// <summary>
    /// Clears all indexed rules.
    /// </summary>
    internal void Clear()
    {
        _idRules.Clear();
        _classRules.Clear();
        _tagRules.Clear();
        _pseudoRules.Clear();
        _universalRules.Clear();
        _allRules.Clear();
    }

    /// <summary>
    /// Gets all rules in document order.
    /// </summary>
    internal IReadOnlyList<RuleData> AllRules => _allRules;

    /// <summary>
    /// Analyzes a rule's selector and adds it to the appropriate indexes.
    /// In BlinkNG, this is done by CSSSelectorParser and RuleFeatureSet.
    /// </summary>
    private void IndexRule(RuleData ruleData)
    {
        var selector = ruleData.Rule.Selector;

        // Handle different selector types
        // In AngleSharp, the selector might be a single selector or a group
        var selectorText = selector.Text;

        // If it contains comma, it's a selector list
        if (selectorText.Contains(","))
        {
            // For selector lists, we index based on the first selector
            // In real BlinkNG, each selector would be indexed separately
            var firstSelector = selectorText.Split(',')[0].Trim();
            IndexBySelectorText(ruleData, firstSelector);
        }
        else
        {
            IndexBySelectorText(ruleData, selectorText);
        }
    }

    /// <summary>
    /// Indexes a rule based on its selector text.
    /// </summary>
    private void IndexBySelectorText(RuleData ruleData, string selectorText)
    {
        // Get the rightmost simple selector for indexing
        var rightmost = GetRightmostSelector(selectorText);
        IndexByRightmostSelector(ruleData, rightmost);
    }

    /// <summary>
    /// Gets the rightmost simple selector for indexing.
    /// In BlinkNG, this is the "subject" of the selector.
    /// </summary>
    private string GetRightmostSelector(string selectorText)
    {
        // Simplified - in reality would parse selector structure
        var text = selectorText.Trim();

        // Extract the rightmost simple selector
        var parts = text.Split(new[] { ' ', '>', '+', '~' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.LastOrDefault() ?? "*";
    }

    /// <summary>
    /// Indexes a rule based on its rightmost selector.
    /// </summary>
    private void IndexByRightmostSelector(RuleData ruleData, string rightmost)
    {
        // ID selector
        if (rightmost.StartsWith("#"))
        {
            var id = rightmost.Substring(1);
            if (!_idRules.TryGetValue(id, out var list))
            {
                list = new List<RuleData>();
                _idRules[id] = list;
            }
            list.Add(ruleData);
        }
        // Class selector
        else if (rightmost.StartsWith("."))
        {
            var className = rightmost.Substring(1);
            if (!_classRules.TryGetValue(className, out var list))
            {
                list = new List<RuleData>();
                _classRules[className] = list;
            }
            list.Add(ruleData);
        }
        // Pseudo-class selector
        else if (rightmost.Contains(":"))
        {
            _pseudoRules.Add(ruleData);
        }
        // Universal selector
        else if (rightmost == "*")
        {
            _universalRules.Add(ruleData);
        }
        // Tag selector
        else
        {
            var tagName = rightmost.ToLowerInvariant();
            if (!_tagRules.TryGetValue(tagName, out var list))
            {
                list = new List<RuleData>();
                _tagRules[tagName] = list;
            }
            list.Add(ruleData);
        }
    }
}

/// <summary>
/// Represents a CSS rule with metadata for matching and cascade resolution.
/// </summary>
internal class RuleData
{
    /// <summary>
    /// The CSS style rule.
    /// </summary>
    public ICssStyleRule Rule { get; }

    /// <summary>
    /// Position in document order (for cascade).
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// The origin of the stylesheet containing this rule.
    /// </summary>
    public StylesheetOrigin Origin { get; }

    /// <summary>
    /// Cached specificity of the rule's selector.
    /// </summary>
    public Priority Specificity { get; }

    public RuleData(ICssStyleRule rule, int position, StylesheetOrigin origin)
    {
        Rule = rule;
        Position = position;
        Origin = origin;

        // Cache specificity
        // In AngleSharp, specificity is a Priority struct
        // If the selector doesn't have specificity, use a default value
        if (rule.Selector != null)
        {
            Specificity = rule.Selector.Specificity;
        }
        else
        {
            // Default specificity (0,0,0,0)
            // Priority constructor takes: inline, id, class, type
            Specificity = new Priority(0, 0, 0, 0);
        }
    }
}