namespace LayoutEngine.Core.LayoutStyle.Public;

using LayoutEngine.Core.LayoutNG.Public;
using AngleSharp.Css.Dom;

/// <summary>
/// Synthesizes styles for anonymous layout objects according to CSS specifications.
/// </summary>
public interface IAnonymousStyleSynthesizer
{
    /// <summary>
    /// Creates a synthesized style for an anonymous block.
    /// Anonymous blocks inherit all inheritable properties and get display:block.
    /// </summary>
    ILayoutComputedStyle SynthesizeAnonymousBlockStyle(ILayoutObject anonymousBlock, ILayoutComputedStyle parentStyle);

    /// <summary>
    /// Creates a synthesized style for an anonymous inline.
    /// Anonymous inlines inherit all inheritable properties and get display:inline.
    /// </summary>
    ILayoutComputedStyle SynthesizeAnonymousInlineStyle(ILayoutObject anonymousInline, ILayoutComputedStyle parentStyle);

    /// <summary>
    /// Creates a synthesized style for an anonymous table wrapper.
    /// </summary>
    ILayoutComputedStyle SynthesizeAnonymousTableWrapperStyle(ILayoutObject anonymousTable, ILayoutComputedStyle parentStyle);

    /// <summary>
    /// Creates a synthesized style for an anonymous flex item wrapper.
    /// </summary>
    ILayoutComputedStyle SynthesizeAnonymousFlexItemStyle(ILayoutObject anonymousFlexItem, ILayoutComputedStyle parentStyle);

    /// <summary>
    /// Creates a synthesized style for any anonymous layout object based on its type.
    /// </summary>
    ILayoutComputedStyle SynthesizeStyle(ILayoutObject anonymousObject, ILayoutComputedStyle parentStyle);

    /// <summary>
    /// Determines which properties should be inherited from parent for the given anonymous type.
    /// </summary>
    ICssStyleDeclaration CreateInheritedDeclaration(ILayoutComputedStyle parentStyle, LayoutObjectType anonymousType);
}