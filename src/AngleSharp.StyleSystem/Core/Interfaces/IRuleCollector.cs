namespace AngleSharp.StyleSystem.Core.Interfaces;

using System.Collections.Generic;
using AngleSharp.Dom;

/// <summary>
/// Manages rule collection and matching for CSS style rules.
/// </summary>
public interface IRuleCollector
{
    /// <summary>
    /// Collects all rules that match the specified element.
    /// </summary>
    /// <param name="element">The element to match.</param>
    /// <param name="pseudoElement">Optional pseudo-element selector.</param>
    /// <returns>Collection of matched rules with their specificity and origin.</returns>
    IEnumerable<MatchedRule> CollectMatchingRules(IElement element, string? pseudoElement = null);

    /// <summary>
    /// Clears the rule collector's internal cache.
    /// </summary>
    void ClearCache();
}