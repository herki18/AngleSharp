using AngleSharp.Css.Dom;
using AngleSharp.Css;

namespace LayoutEngine.Core.Style;

using System;
using System.Collections.Generic;

/// <summary>
/// Result of CSS rule matching for an element
/// </summary>
public class MatchResult : IMatchResult
{
    public IReadOnlyList<IMatchedRule> UserAgentRules { get; }
    public IReadOnlyList<IMatchedRule> AuthorRules { get; }
    public ICssStyleDeclaration? InlineStyle { get; }

    public MatchResult(
        IReadOnlyList<IMatchedRule> userAgentRules,
        IReadOnlyList<IMatchedRule> authorRules,
        ICssStyleDeclaration? inlineStyle)
    {
        UserAgentRules = userAgentRules ?? Array.Empty<IMatchedRule>();
        AuthorRules = authorRules ?? Array.Empty<IMatchedRule>();
        InlineStyle = inlineStyle;
    }
}

/// <summary>
/// A matched CSS rule with specificity and document order
/// </summary>
public class MatchedRule : IMatchedRule
{
    public ICssRule Rule { get; }
    public int Specificity { get; }
    public int DocumentOrder { get; }

    public MatchedRule(ICssRule rule, int specificity, int documentOrder)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Specificity = specificity;
        DocumentOrder = documentOrder;
    }
}