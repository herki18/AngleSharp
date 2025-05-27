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
    private readonly ILogger<ElementRuleCollector> _logger;

    public ElementRuleCollector(
        ICssParser cssParser,
        IUserAgentStyleProvider userAgentStyles,
        ILogger<ElementRuleCollector> logger)
    {
        _cssParser = cssParser ?? throw new ArgumentNullException(nameof(cssParser));
        _userAgentStyles = userAgentStyles ?? throw new ArgumentNullException(nameof(userAgentStyles));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IMatchResult CollectMatchingRules(IElement element, IStyleRecalcContext context)
    {
        _logger.LogDebug("Collecting matching rules for element: {TagName}", element.TagName);

        var userAgentRules = CollectUserAgentRules(element);
        var authorRules = CollectAuthorRules(element); // TODO: Implement for stylesheets
        var inlineStyle = CollectInlineStyle(element);

        return new MatchResult(userAgentRules, authorRules, inlineStyle);
    }

    private IReadOnlyList<IMatchedRule> CollectUserAgentRules(IElement element)
    {
        var rules = new List<IMatchedRule>();
        var userAgentRule = _userAgentStyles.GetRuleForElement(element);

        if (userAgentRule != null)
        {
            rules.Add(new MatchedRule(userAgentRule, specificity: 0, documentOrder: 0));
        }

        return rules;
    }

    private IReadOnlyList<IMatchedRule> CollectAuthorRules(IElement element)
    {
        // TODO: Implement stylesheet rule matching
        // For now, return empty list since we're focusing on inline styles
        return Array.Empty<IMatchedRule>();
    }

    private ICssStyleDeclaration? CollectInlineStyle(IElement element)
    {
        var styleAttribute = element.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(styleAttribute))
            return null;

        _logger.LogDebug("Parsing inline style: {StyleAttribute}", styleAttribute);

        // Use AngleSharp to parse inline style
        var inlineDeclaration = _cssParser.ParseDeclaration(styleAttribute);
        return inlineDeclaration;
    }
}