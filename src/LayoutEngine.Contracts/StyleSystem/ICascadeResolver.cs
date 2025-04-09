namespace LayoutEngine.Contracts.StyleSystem;

using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

public interface ICascadeResolver
{
    ICssStyleDeclaration ResolveCascade(IEnumerable<MatchedRule> matchedRules, IElement element);
}