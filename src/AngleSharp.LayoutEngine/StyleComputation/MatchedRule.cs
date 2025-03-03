namespace AngleSharp.LayoutEngine.StyleComputation;

using System;
using System.Linq;
using Css;
using Css.Dom;

/// <summary>
/// Represents a CSS rule that matched an element, along with its specificity and origin.
/// </summary>
public class MatchedRule
{
    /// <summary>
    /// The CSS style rule that matched.
    /// </summary>
    public ICssStyleRule Rule { get; }

    /// <summary>
    /// The specificity of the match.
    /// </summary>
    public Priority Specificity { get; }

    /// <summary>
    /// The origin of the stylesheet containing the rule.
    /// </summary>
    public StylesheetOrigin Origin { get; }

    /// <summary>
    /// The original index for rules with equal specificity.
    /// </summary>
    public int OriginalIndex { get; }

    /// <summary>
    /// Creates a new instance of MatchedRule.
    /// </summary>
    public MatchedRule(ICssStyleRule rule, Priority specificity, StylesheetOrigin origin, int originalIndex)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Specificity = specificity;
        Origin = origin;
        OriginalIndex = originalIndex;
    }

    /// <summary>
    /// Whether this rule has an !important declaration for the specified property.
    /// </summary>
    public bool HasImportantFor(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return false;

        var property = Rule.Style.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        return property != null && property.IsImportant;
    }
}