using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.Css.Dom;

/// <summary>
/// Collects CSS rules that match an element - mirrors Blink's ElementRuleCollector
/// </summary>
public interface IElementRuleCollector
{
    /// <summary>
    /// Collect all CSS rules that match the given element
    /// Returns: List of ICssRule (AngleSharp type) with specificity
    /// </summary>
    IMatchResult CollectMatchingRules(IElement element, IStyleRecalcContext context);
}

/// <summary>
/// Result of rule collection - mirrors Blink's MatchResult
/// </summary>
public interface IMatchResult
{
    IReadOnlyList<IMatchedRule> UserAgentRules { get; }
    IReadOnlyList<IMatchedRule> AuthorRules { get; }
    ICssStyleDeclaration? InlineStyle { get; } // AngleSharp type
}

/// <summary>
/// A single matched CSS rule with metadata
/// </summary>
public interface IMatchedRule
{
    ICssRule Rule { get; } // AngleSharp type
    int Specificity { get; }
    int DocumentOrder { get; }
}