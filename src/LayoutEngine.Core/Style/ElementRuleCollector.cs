using AngleSharp.Dom;
using AngleSharp.Css.Dom;
using AngleSharp.Css;
using Microsoft.Extensions.Logging;

namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Parser;

/// <summary>
/// Collects CSS rules that match an element - mirrors Blink's ElementRuleCollector
/// </summary>
public class ElementRuleCollector : IElementRuleCollector
{
    private readonly ICssParser _cssParser;
    private readonly IUserAgentStyleProvider _userAgentStyles;
    private readonly IStyleSheetManager _styleSheetManager;
    private readonly ILogger<ElementRuleCollector> _logger;

    public ElementRuleCollector(
        ICssParser cssParser,
        IUserAgentStyleProvider userAgentStyles,
        IStyleSheetManager styleSheetManager,
        ILogger<ElementRuleCollector> logger)
    {
        _cssParser = cssParser ?? throw new ArgumentNullException(nameof(cssParser));
        _userAgentStyles = userAgentStyles ?? throw new ArgumentNullException(nameof(userAgentStyles));
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

    private IReadOnlyList<IMatchedRule> CollectUserAgentRules(IElement element)
    {
        var rules = new List<IMatchedRule>();
        var userAgentRule = _userAgentStyles.GetRuleForElement(element);

        if (userAgentRule != null)
        {
            rules.Add(new MatchedRule
            {
                Rule = userAgentRule,
                Specificity = Priority.Zero,
                Origin = StylesheetOrigin.UserAgent,
                OriginalIndex = 0
            });
        }

        return rules;
    }

    private IReadOnlyList<IMatchedRule> CollectAuthorRules(IElement element)
    {
        var matchedRules = new List<IMatchedRule>();
        int ruleIndex = 0;

        // Get all author stylesheets from the StyleSheetManager
        var authorStylesheets = _styleSheetManager.GetStylesheetsByOrigin(StylesheetOrigin.Author);

        foreach (var stylesheet in authorStylesheets)
        {
            CollectRulesFromStylesheet(stylesheet, element, matchedRules, ref ruleIndex);
        }

        return matchedRules;
    }

    private void CollectRulesFromStylesheet(
        ICssStyleSheet stylesheet,
        IElement element,
        List<IMatchedRule> matchedRules,
        ref int ruleIndex)
    {
        foreach (var rule in stylesheet.Rules)
        {
            CollectRulesRecursive(rule, element, matchedRules, ref ruleIndex);
        }
    }

    private void CollectRulesRecursive(
        ICssRule rule,
        IElement element,
        List<IMatchedRule> matchedRules,
        ref int ruleIndex)
    {
        switch (rule.Type)
        {
            case CssRuleType.Style:
                if (rule is ICssStyleRule styleRule)
                {
                    if (styleRule.TryMatch(element, element.Owner.DocumentElement, out var specificity))
                    {
                        matchedRules.Add(new MatchedRule
                        {
                            Rule = styleRule,
                            Specificity = specificity,
                            Origin = StylesheetOrigin.Author,
                            OriginalIndex = ruleIndex++
                        });
                    }
                }
                break;

            case CssRuleType.Media:
            case CssRuleType.Supports:
            case CssRuleType.Document:
                if (rule is ICssGroupingRule groupingRule)
                {
                    foreach (var nestedRule in groupingRule.Rules)
                    {
                        CollectRulesRecursive(nestedRule, element, matchedRules, ref ruleIndex);
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
            var inlineDeclaration = _cssParser.ParseDeclaration(styleAttribute);
            return inlineDeclaration;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse inline style: {StyleAttribute}", styleAttribute);
            return null;
        }
    }
}