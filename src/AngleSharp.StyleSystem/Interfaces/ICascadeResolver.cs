namespace AngleSharp.StyleSystem.Core.Interfaces;

using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

/// <summary>
/// Interface for resolving CSS cascade.
/// </summary>
public interface ICascadeResolver
{
    /// <summary>
    /// Resolves the cascade for an element with the given matched rules.
    /// </summary>
    /// <param name="matchedRules">The rules that matched the element.</param>
    /// <param name="element">The element being styled.</param>
    /// <returns>The cascaded style for the element.</returns>
    ICssStyleDeclaration ResolveCascade(IEnumerable<MatchedRule> matchedRules, IElement element);
}