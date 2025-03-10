namespace AngleSharp.StyleSystem.Models;

using System;
using AngleSharp.Css;
using AngleSharp.Css.Dom;

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