namespace LayoutEngine.NG.Style;

using AngleSharp.Css;
using AngleSharp.Css.Dom;

/// <summary>
/// A matched CSS rule with specificity and document order.
/// Represents a rule that matched an element during style resolution.
/// </summary>
public class MatchedRule
{
    /// <summary>
    /// The CSS style rule that matched.
    /// </summary>
    public ICssStyleRule? Rule { get; set; }

    /// <summary>
    /// The specificity of the rule's selector.
    /// In BlinkNG, used for cascade sorting.
    /// </summary>
    public Priority Specificity { get; set; }

    /// <summary>
    /// The origin of the stylesheet (user-agent, user, or author).
    /// </summary>
    public StylesheetOrigin Origin { get; set; }

    /// <summary>
    /// The original index in document order.
    /// In BlinkNG, used for cascade sorting when specificity is equal.
    /// </summary>
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
