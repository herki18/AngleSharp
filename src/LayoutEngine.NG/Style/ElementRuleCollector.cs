namespace LayoutEngine.NG.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Dom;

/// <summary>
/// Collects CSS rules that match a given element from all active stylesheets.
/// In BlinkNG, this is ElementRuleCollector which is used during style resolution.
/// </summary>
public class ElementRuleCollector
{
    private readonly StyleEngine _styleEngine;
    private readonly LayoutDataManager _layoutDataManager;

    public ElementRuleCollector(StyleEngine styleEngine, LayoutDataManager layoutDataManager)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _layoutDataManager = layoutDataManager ?? throw new ArgumentNullException(nameof(layoutDataManager));
    }

    /// <summary>
    /// Collects all matching rules for an element.
    /// This is the main entry point used by StyleResolver.
    /// </summary>
    /// <param name="element">The element to match rules against.</param>
    /// <param name="context">The style recalc context.</param>
    /// <returns>The collected matching rules organized by origin.</returns>
    public MatchResult CollectMatchingRules(IElement element, StyleRecalcContext context)
    {
        var result = new MatchResult();

        // Collect user agent rules
        CollectUserAgentRules(element, result);

        // Collect user rules (if any)
        CollectUserRules(element, result);

        // Collect author rules
        CollectAuthorRules(element, result);

        // Collect inline style
        CollectInlineStyle(element, result);

        return result;
    }

    /// <summary>
    /// Collects matching rules from user agent stylesheets.
    /// </summary>
    private void CollectUserAgentRules(IElement element, MatchResult result)
    {
        var uaStyleSheets = _styleEngine.GetUserAgentStyleSheets();

        foreach (var styleSheet in uaStyleSheets)
        {
            if (styleSheet.IsDisabled)
                continue;

            CollectMatchingRulesFromStyleSheet(element, styleSheet, result.UserAgentRules);
        }
    }

    /// <summary>
    /// Collects matching rules from user stylesheets.
    /// </summary>
    private void CollectUserRules(IElement element, MatchResult result)
    {
        var userStyleSheets = _styleEngine.GetUserStyleSheets();

        foreach (var styleSheet in userStyleSheets)
        {
            if (styleSheet.IsDisabled)
                continue;

            CollectMatchingRulesFromStyleSheet(element, styleSheet, result.UserRules);
        }
    }

    /// <summary>
    /// Collects matching rules from author stylesheets.
    /// </summary>
    private void CollectAuthorRules(IElement element, MatchResult result)
    {
        var authorStyleSheets = _styleEngine.GetActiveAuthorStyleSheets();

        foreach (var styleSheet in authorStyleSheets)
        {
            if (styleSheet.IsDisabled)
                continue;

            // TODO: Check media queries
            // TODO: Check scope (for <style scoped>)

            CollectMatchingRulesFromStyleSheet(element, styleSheet, result.AuthorRules);
        }
    }

    /// <summary>
    /// Collects matching rules from a single stylesheet.
    /// </summary>
    private void CollectMatchingRulesFromStyleSheet(
        IElement element,
        StyleSheetContents styleSheet,
        List<MatchedRule> matchedRules)
    {
        // Ensure rules are parsed
        styleSheet.ParseRules();

        var ruleSet = styleSheet.RuleSet;
        var candidateRules = ruleSet.GetCandidateRules(element);

        foreach (var ruleData in candidateRules)
        {
            // Use AngleSharp's selector matching
            if (element.Matches(ruleData.Rule.SelectorText))
            {
                matchedRules.Add(new MatchedRule
                {
                    Rule = ruleData.Rule,
                    Specificity = ruleData.Specificity,
                    Origin = ruleData.Origin,
                    Position = ruleData.Position
                });
            }
        }
    }

    /// <summary>
    /// Collects inline style from the element's style attribute.
    /// </summary>
    private void CollectInlineStyle(IElement element, MatchResult result)
    {
        var styleAttr = element.GetAttribute("style");
        if (!string.IsNullOrWhiteSpace(styleAttr))
        {
            // In BlinkNG, inline style is stored in ElementData
            // We store it in ElementLayout instead
            var elementLayout = _layoutDataManager.GetOrCreate(element);

            // Parse inline style if needed
            // TODO: Cache parsed inline style in ElementLayout
            var inlineStyle = ParseInlineStyle(styleAttr);
            if (inlineStyle != null)
            {
                result.InlineStyle = inlineStyle;
            }
        }
    }

    /// <summary>
    /// Parses inline style text into a style declaration.
    /// </summary>
    private ICssStyleDeclaration? ParseInlineStyle(string styleText)
    {
        // TODO: Use AngleSharp's CSS parser
        // For now, return null
        return null;
    }
}

/// <summary>
/// The result of collecting matching rules for an element.
/// Organized by origin for cascade resolution.
/// </summary>
public class MatchResult
{
    /// <summary>
    /// User agent rules that matched.
    /// </summary>
    public List<MatchedRule> UserAgentRules { get; } = new();

    /// <summary>
    /// User rules that matched.
    /// </summary>
    public List<MatchedRule> UserRules { get; } = new();

    /// <summary>
    /// Author rules that matched.
    /// </summary>
    public List<MatchedRule> AuthorRules { get; } = new();

    /// <summary>
    /// The element's inline style (if any).
    /// </summary>
    public ICssStyleDeclaration? InlineStyle { get; set; }

    /// <summary>
    /// Gets all matched rules in cascade order.
    /// </summary>
    public IEnumerable<MatchedRule> GetRulesInCascadeOrder()
    {
        // Order: UA -> User -> Author, then by specificity and position
        return UserAgentRules
            .Concat(UserRules)
            .Concat(AuthorRules)
            .OrderBy(r => r.Origin)
            .ThenBy(r => r.Specificity)
            .ThenBy(r => r.Position);
    }
}

/// <summary>
/// Represents a rule that matched an element.
/// </summary>
public class MatchedRule
{
    /// <summary>
    /// The CSS rule that matched.
    /// </summary>
    public ICssStyleRule Rule { get; set; } = null!;

    /// <summary>
    /// The specificity of the rule's selector.
    /// </summary>
    public Priority Specificity { get; set; }

    /// <summary>
    /// The origin of the stylesheet containing this rule.
    /// </summary>
    public StylesheetOrigin Origin { get; set; }

    /// <summary>
    /// The position in document order.
    /// </summary>
    public int Position { get; set; }
}