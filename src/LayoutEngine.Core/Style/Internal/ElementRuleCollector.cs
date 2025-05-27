namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using Internal;
using Microsoft.Extensions.Logging;
using Public;

/// <summary>
/// Collects CSS rules that match an element - mirrors Blink's ElementRuleCollector
/// </summary>
public class ElementRuleCollector : IElementRuleCollector
{
    private readonly ICssParser _cssParser;
    private readonly IStyleSheetManager _styleSheetManager;
    private readonly ILogger<ElementRuleCollector> _logger;

    public ElementRuleCollector(
        ICssParser cssParser,
        IStyleSheetManager styleSheetManager,
        ILogger<ElementRuleCollector> logger)
    {
        _cssParser = cssParser ?? throw new ArgumentNullException(nameof(cssParser));
        _styleSheetManager = styleSheetManager ?? throw new ArgumentNullException(nameof(styleSheetManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IMatchResult CollectMatchingRules(IElement element, IStyleRecalcContext context)
    {
        _logger.LogDebug("Collecting matching rules for element: {TagName}#{Id}.{Classes}",
            element.TagName, element.Id, string.Join(".", element.ClassList));

        var userAgentRules = CollectUserAgentRules(element);
        var authorRules = CollectAuthorRules(element);
        var inlineStyle = CollectInlineStyle(element);

        _logger.LogDebug("Collected {UserAgentCount} user agent rules, {AuthorCount} author rules",
            userAgentRules.Count, authorRules.Count);

        return new MatchResult(userAgentRules, authorRules, inlineStyle);
    }

    private IReadOnlyList<MatchedRule> CollectUserAgentRules(IElement element)
    {
        var rules = new List<MatchedRule>();
        int ruleIndex = 0;

        var userAgentStylesheets = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.UserAgent);

        foreach (var stylesheet in userAgentStylesheets)
        {
            CollectRulesFromStylesheet(stylesheet, element, rules, ref ruleIndex, StylesheetOrigin.UserAgent);
        }

        return rules;
    }

    private IReadOnlyList<MatchedRule> CollectAuthorRules(IElement element)
    {
        var matchedRules = new List<MatchedRule>();
        int ruleIndex = 0;

        var authorStylesheets = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author);

        foreach (var stylesheet in authorStylesheets)
        {
            CollectRulesFromStylesheet(stylesheet, element, matchedRules, ref ruleIndex, StylesheetOrigin.Author);
        }

        return matchedRules;
    }

    private void CollectRulesFromStylesheet(
        ICssStyleSheet stylesheet,
        IElement element,
        List<MatchedRule> matchedRules,
        ref int ruleIndex,
        StylesheetOrigin origin)
    {
        foreach (var rule in stylesheet.Rules)
        {
            CollectRulesRecursive(rule, element, matchedRules, ref ruleIndex, origin);
        }
    }

    private void CollectRulesRecursive(
        ICssRule rule,
        IElement element,
        List<MatchedRule> matchedRules,
        ref int ruleIndex,
        StylesheetOrigin origin)
    {
        switch (rule.Type)
        {
            case CssRuleType.Style:
                if (rule is ICssStyleRule styleRule &&
                    styleRule.TryMatch(element, element.OwnerDocument!.DocumentElement, out var specificity))
                {
                    matchedRules.Add(new MatchedRule(styleRule, specificity, origin, ruleIndex++));
                }
                break;

            case CssRuleType.Media:
            case CssRuleType.Supports:
            case CssRuleType.Document:
                if (rule is ICssGroupingRule groupingRule)
                {
                    foreach (var nestedRule in groupingRule.Rules)
                    {
                        CollectRulesRecursive(nestedRule, element, matchedRules, ref ruleIndex, origin);
                    }
                }
                break;
        }
    }

    private ICssStyleDeclaration? CollectInlineStyle(IElement element)
    {
        var styleAttribute = element.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(styleAttribute))
            return null;

        _logger.LogDebug("Parsing inline style: {StyleAttribute}", styleAttribute);

        try
        {
            return _cssParser.ParseDeclaration(styleAttribute);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse inline style: {StyleAttribute}", styleAttribute);
            return null;
        }
    }
}