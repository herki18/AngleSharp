using System.Collections.Generic;
using AngleSharp.Css.Dom;

namespace LayoutEngine.Core.Style.Public;

using Internal;

/// <summary>
/// Result of rule collection - mirrors Blink's MatchResult
/// </summary>
public interface IMatchResult
{
    IReadOnlyList<MatchedRule> UserAgentRules { get; }
    IReadOnlyList<MatchedRule> AuthorRules { get; }
    ICssStyleDeclaration? InlineStyle { get; } // AngleSharp type
}