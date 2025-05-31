namespace LayoutEngine.Core.LayoutStyle.Public;

using LayoutEngine.Core.LayoutNG.Public;
using AngleSharp.Css.Dom;
using System.Collections.Generic;

/// <summary>
/// Resolves styles for layout objects, handling both element-backed and anonymous objects.
/// </summary>
public interface ILayoutStyleResolver
{
    /// <summary>
    /// Resolves style for a single layout object.
    /// </summary>
    ILayoutComputedStyle ResolveStyle(ILayoutObject layoutObject, ILayoutStyleContext context);

    /// <summary>
    /// Collects matching CSS rules for a layout object with an element.
    /// Returns empty for anonymous objects.
    /// </summary>
    IEnumerable<IMatchedLayoutRule> CollectMatchingRules(ILayoutObject layoutObject);

    /// <summary>
    /// Synthesizes style for an anonymous layout object based on its type and parent.
    /// </summary>
    ILayoutComputedStyle SynthesizeAnonymousStyle(ILayoutObject anonymousObject, ILayoutStyleContext context);

    /// <summary>
    /// Applies CSS cascade to produce final computed values.
    /// </summary>
    ICssStyleDeclaration ApplyCascade(IEnumerable<IMatchedLayoutRule> matchedRules, ICssStyleDeclaration? inlineStyle);

    /// <summary>
    /// Applies inheritance from parent style.
    /// </summary>
    void ApplyInheritance(ICssStyleDeclaration childDeclaration, ILayoutComputedStyle? parentStyle);

    /// <summary>
    /// Computes final values for all properties.
    /// </summary>
    void ComputeFinalValues(ICssStyleDeclaration declaration, ILayoutStyleContext context);
}

/// <summary>
/// A CSS rule matched to a layout object with specificity.
/// </summary>
public interface IMatchedLayoutRule
{
    /// <summary>
    /// The CSS rule that matched.
    /// </summary>
    ICssStyleRule Rule { get; }

    /// <summary>
    /// The specificity of the match.
    /// </summary>
    int Specificity { get; }

    /// <summary>
    /// The origin of the stylesheet.
    /// </summary>
    StylesheetOrigin Origin { get; }

    /// <summary>
    /// Document order for cascade resolution.
    /// </summary>
    int DocumentOrder { get; }
}

/// <summary>
/// Origin of a stylesheet.
/// </summary>
public enum StylesheetOrigin
{
    UserAgent,
    User,
    Author
}