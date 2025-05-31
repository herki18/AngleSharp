namespace LayoutEngine.Core.LayoutStyle.Public;

using LayoutEngine.Core.LayoutNG.Public;
using AngleSharp.Dom;

/// <summary>
/// Context passed during layout object style resolution.
/// Carries parent style and document information.
/// </summary>
public interface ILayoutStyleContext
{
    /// <summary>
    /// The document being styled (needed for stylesheet access).
    /// </summary>
    IDocument Document { get; }

    /// <summary>
    /// Parent layout object's computed style for inheritance.
    /// Null for root layout object.
    /// </summary>
    ILayoutComputedStyle? ParentStyle { get; }

    /// <summary>
    /// The layout object currently being styled.
    /// </summary>
    ILayoutObject CurrentLayoutObject { get; }

    /// <summary>
    /// Creates a new context for a child layout object.
    /// </summary>
    ILayoutStyleContext CreateChildContext(ILayoutObject childLayoutObject, ILayoutComputedStyle parentStyle);

    /// <summary>
    /// Whether we're in a pseudo-element context.
    /// </summary>
    bool IsPseudoElement { get; }

    /// <summary>
    /// The pseudo-element type if applicable.
    /// </summary>
    string? PseudoElementType { get; }
}