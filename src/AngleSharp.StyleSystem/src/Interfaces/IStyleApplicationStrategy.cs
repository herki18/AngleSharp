namespace AngleSharp.StyleSystem.Interfaces;

using System.Collections.Generic;
using AngleSharp.Dom;

/// <summary>
/// Defines a strategy for applying styles to elements in a document.
/// This interface determines which elements to style, how to traverse the DOM,
/// and when style sharing is possible.
/// </summary>
public interface IStyleApplicationStrategy
{
    /// <summary>
    /// Determines whether style computation should skip the specified element's subtree.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>True if the subtree should be skipped; otherwise, false.</returns>
    /// <remarks>
    /// This is used to optimize style computation by avoiding unnecessary work on elements
    /// that don't require styling, such as script elements or elements with display:none.
    /// </remarks>
    bool ShouldSkipSubtree(IElement element);

    /// <summary>
    /// Determines whether an element can share computed styles with a potential donor element.
    /// </summary>
    /// <param name="target">The element that needs styling.</param>
    /// <param name="donor">The potential style donor element.</param>
    /// <returns>True if styles can be shared; otherwise, false.</returns>
    /// <remarks>
    /// Style sharing is an optimization that reduces memory usage and computation time
    /// by reusing style data between similar elements.
    /// </remarks>
    bool CanShareStyleWith(IElement target, IElement donor);

    /// <summary>
    /// Gets the order in which elements should be traversed for styling.
    /// </summary>
    /// <param name="root">The root element of the subtree to traverse.</param>
    /// <returns>An enumerable of elements in the desired traversal order.</returns>
    /// <remarks>
    /// The traversal order can affect performance and style inheritance.
    /// </remarks>
    IEnumerable<IElement> GetElementTraversalOrder(IElement root);

    /// <summary>
    /// Creates a style context for an element, which includes information needed for style computation.
    /// </summary>
    /// <param name="element">The element to create a context for.</param>
    /// <returns>A style context for the element.</returns>
    /// <remarks>
    /// The style context includes parent style, visibility information, and potential style donors.
    /// </remarks>
    StyleContext CreateStyleContext(IElement element);
}

/// <summary>
/// Contains context information for style computation.
/// </summary>
public class StyleContext
{
    /// <summary>
    /// Gets or sets the element for which styles are being computed.
    /// </summary>
    public IElement Element { get; set; } = null!;

    /// <summary>
    /// Gets or sets the computed style of the parent element, if available.
    /// </summary>
    public IComputedStyle? ParentStyle { get; set; }

    /// <summary>
    /// Gets or sets whether the element is in the normal document flow.
    /// </summary>
    /// <remarks>
    /// Elements with position:absolute or position:fixed are not in the normal document flow.
    /// </remarks>
    public bool IsInDocumentFlow { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the element is visible.
    /// </summary>
    /// <remarks>
    /// Elements with display:none or visibility:hidden are not visible.
    /// </remarks>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets a potential element from which styles can be shared.
    /// </summary>
    /// <remarks>
    /// This is used for style sharing optimization.
    /// </remarks>
    public IElement? StyleDonor { get; set; }

    /// <summary>
    /// Gets or sets whether the element has content that needs to be styled.
    /// </summary>
    /// <remarks>
    /// Some elements, like empty divs with no background, may not need full styling.
    /// </remarks>
    public bool HasContent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the element is contained within a style containment boundary.
    /// </summary>
    /// <remarks>
    /// Elements with contain:style create a new style containment boundary.
    /// </remarks>
    public bool IsContained { get; set; } = false;

    /// <summary>
    /// Gets or sets the containment root element, if any.
    /// </summary>
    public IElement? ContainmentRoot { get; set; }
}