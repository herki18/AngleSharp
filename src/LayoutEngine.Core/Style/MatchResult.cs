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

public class MatchedRule
{
    // The CSS style rule that matched
    public ICssStyleRule? Rule { get; set; }

    // The specificity of the matching selector (for cascade ordering)
    public Priority Specificity { get; set; }

    // The origin of the stylesheet (user agent, user, or author)
    public StylesheetOrigin Origin { get; set; }

    // The original index for stable sorting
    public int OriginalIndex { get; set; }

    public MatchedRule()
    {

    }

    public MatchedRule(ICssStyleRule? rule, Priority priority, StylesheetOrigin author, Int32 originalIndex)
    {
        Rule = rule;
        Specificity = priority;
        Origin = author;
        OriginalIndex = originalIndex;
    }
}

/// <summary>
/// The origin of a stylesheet.
/// </summary>
public enum StylesheetOrigin
{
    UserAgent,
    User,
    Author
}