namespace LayoutEngine.Core.Style.Internal;

using System;
using AngleSharp.Css;
using AngleSharp.Css.Dom;

/// <summary>
/// A matched CSS rule with specificity and document order
/// </summary>
public class MatchedRule
{
    public ICssStyleRule? Rule { get; set; }
    public Priority Specificity { get; set; }
    public StylesheetOrigin Origin { get; set; }
    public int OriginalIndex { get; set; }

    public MatchedRule()
    {
        Specificity = new Priority(0, 0, 0, 0);
    }

    public MatchedRule(ICssStyleRule? rule, Priority specificity, StylesheetOrigin origin, int originalIndex)
    {
        Rule = rule;
        Specificity = specificity;
        Origin = origin;
        OriginalIndex = originalIndex;
    }
}