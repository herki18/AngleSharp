namespace LayoutEngine.NG.Style;

using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using System.Collections.Generic;

/// <summary>
/// Interface for CSS cascade resolution, following BlinkNG patterns.
/// Resolves the cascade by applying rules in the correct order based on
/// origin, specificity, and declaration order.
/// </summary>
public interface ICascadeResolver
{
    /// <summary>
    /// Resolves the CSS cascade for matched rules on an element.
    /// In BlinkNG, this is a core part of style computation.
    /// </summary>
    /// <param name="matchedRules">Rules that matched the element's selectors</param>
    /// <param name="element">The element to resolve styles for</param>
    /// <returns>The resolved style declaration after cascade</returns>
    ICssStyleDeclaration ResolveCascade(IEnumerable<MatchedRule> matchedRules, IElement element);
}