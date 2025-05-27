namespace LayoutEngine.Core.Style.Internal;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using LayoutEngine.Core.Style.Public;

/// <summary>
/// Result of CSS rule matching for an element
/// </summary>
public class MatchResult : IMatchResult
{
    public IReadOnlyList<MatchedRule> UserAgentRules { get; }
    public IReadOnlyList<MatchedRule> AuthorRules { get; }
    public ICssStyleDeclaration? InlineStyle { get; }

    public MatchResult(
        IReadOnlyList<MatchedRule> userAgentRules,
        IReadOnlyList<MatchedRule> authorRules,
        ICssStyleDeclaration? inlineStyle)
    {
        UserAgentRules = userAgentRules ?? Array.Empty<MatchedRule>();
        AuthorRules = authorRules ?? Array.Empty<MatchedRule>();
        InlineStyle = inlineStyle;
    }
}