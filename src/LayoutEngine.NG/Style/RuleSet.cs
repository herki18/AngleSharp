namespace LayoutEngine.NG.Style;

using AngleSharp.Css.Dom;
using System.Collections.Generic;
using AngleSharp.Css;
using AngleSharp.Dom;

/// <summary>
/// Represents a collection of CSS rules organized for efficient matching.
/// In BlinkNG, RuleSet partitions and indexes rules for fast selector matching.
/// </summary>
public class RuleSet
{
    /// <summary>
    /// Rules indexed by class name.
    /// In BlinkNG, this allows fast lookup for class selectors.
    /// </summary>
    public Dictionary<string, List<RuleData>> ClassRules { get; } = new();

    /// <summary>
    /// Rules indexed by ID.
    /// In BlinkNG, ID selectors are kept separate for performance.
    /// </summary>
    public Dictionary<string, List<RuleData>> IdRules { get; } = new();

    /// <summary>
    /// Rules indexed by tag name.
    /// </summary>
    public Dictionary<string, List<RuleData>> TagRules { get; } = new();

    /// <summary>
    /// Universal rules that apply to all elements.
    /// </summary>
    public List<RuleData> UniversalRules { get; } = new();

    /// <summary>
    /// Rules with pseudo-classes.
    /// In BlinkNG, pseudo-classes need special handling.
    /// </summary>
    public List<RuleData> PseudoRules { get; } = new();

    /// <summary>
    /// Adds a rule to the appropriate index.
    /// In BlinkNG, this is FindBestRuleSetAndAdd.
    /// </summary>
    public void AddRule(ICssStyleRule rule, int index, StylesheetOrigin origin)
    {
        // Skeleton implementation
        // In BlinkNG, this would analyze the selector and add to appropriate buckets
    }

    /// <summary>
    /// Gets all rules that could potentially match an element.
    /// In BlinkNG, this is used during style resolution.
    /// </summary>
    public IEnumerable<RuleData> GetCandidateRules(IElement element)
    {
        // Skeleton implementation
        yield break;
    }
}

/// <summary>
/// Wrapper for a CSS rule with metadata.
/// In BlinkNG, RuleData contains the rule and additional matching information.
/// </summary>
public class RuleData
{
    /// <summary>
    /// The CSS rule.
    /// </summary>
    public ICssStyleRule? Rule { get; set; }

    /// <summary>
    /// The specificity of the rule's selector.
    /// </summary>
    public Priority Specificity { get; set; }

    /// <summary>
    /// The position in the stylesheet (for cascade order).
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// The origin of the stylesheet containing this rule.
    /// </summary>
    public StylesheetOrigin Origin { get; set; }

    /// <summary>
    /// Whether this rule has any important declarations.
    /// In BlinkNG, this is cached for performance.
    /// </summary>
    public bool HasImportantDeclarations { get; set; }
}