namespace AngleSharp.StyleSystem.Interfaces;

using System.Collections.Generic;
using AngleSharp.Dom;

/// <summary>
/// Defines the strategy for how styles are applied to elements in the document tree.
/// This includes traversal order, style sharing decisions, and optimization rules.
/// </summary>
public interface IStyleApplicationStrategy
{
    /// <summary>
    /// Determines if a subtree starting at the specified element can be skipped during style computation.
    /// </summary>
    /// <param name="element">The root element of the potential subtree to skip.</param>
    /// <returns>True if the subtree can be skipped; otherwise, false.</returns>
    bool ShouldSkipSubtree(IElement element);

    /// <summary>
    /// Determines if the target element can potentially share style with the donor element.
    /// </summary>
    /// <param name="target">The element that might receive the shared style.</param>
    /// <param name="donor">The element that might donate its computed style.</param>
    /// <returns>True if style sharing is possible; otherwise, false.</returns>
    bool CanShareStyleWith(IElement target, IElement donor);

    /// <summary>
    /// Determines the traversal order for style computation.
    /// </summary>
    /// <param name="root">The root element where traversal begins.</param>
    /// <returns>An enumerable of elements in the order they should be processed.</returns>
    IEnumerable<IElement> GetElementTraversalOrder(IElement root);

    /// <summary>
    /// Creates a style context for an element, identifying information necessary for style computation.
    /// </summary>
    /// <param name="element">The element to create a context for.</param>
    /// <returns>A context object containing information needed for style computation.</returns>
    StyleContext CreateStyleContext(IElement element);
}

/// <summary>
/// Represents the context needed for computing an element's style.
/// </summary>
public class StyleContext
{
    /// <summary>
    /// Gets or sets the element for which styles are being computed.
    /// </summary>
    public IElement Element { get; set; } = null!;

    /// <summary>
    /// Gets or sets the parent element's computed style.
    /// </summary>
    public IComputedStyle? ParentStyle { get; set; }

    /// <summary>
    /// Gets or sets whether this element is in the normal document flow.
    /// </summary>
    public bool IsInDocumentFlow { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this element is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the potential style donor element, if style sharing is possible.
    /// </summary>
    public IElement? StyleDonor { get; set; }
}