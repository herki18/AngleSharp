namespace LayoutEngine.Core.LayoutNG.Public;

using AngleSharp.Dom;
using LayoutEngine.Core.Style.Public;

/// <summary>
/// Base interface for all layout objects in the layout tree.
/// Mirrors Blink's LayoutObject class hierarchy.
/// </summary>
public interface ILayoutObject
{
    /// <summary>
    /// Gets the type of this layout object.
    /// </summary>
    LayoutObjectType Type { get; }

    /// <summary>
    /// Gets the DOM node associated with this layout object.
    /// Can be null for anonymous layout objects.
    /// </summary>
    INode? Node { get; }

    /// <summary>
    /// Gets the DOM element if this layout object is associated with an element.
    /// Returns null for text nodes and anonymous objects.
    /// </summary>
    IElement? Element { get; }

    /// <summary>
    /// Gets the parent layout object.
    /// </summary>
    ILayoutObject? Parent { get; }

    /// <summary>
    /// Gets the computed style for this layout object.
    /// </summary>
    IComputedStyle? Style { get; set; }

    /// <summary>
    /// Gets whether this is an anonymous layout object (no corresponding DOM node).
    /// </summary>
    bool IsAnonymous { get; }

    /// <summary>
    /// Gets whether this layout object is a text node.
    /// </summary>
    bool IsText { get; }

    /// <summary>
    /// Gets whether this layout object is inline.
    /// </summary>
    bool IsInline { get; }

    /// <summary>
    /// Gets whether this layout object is a block.
    /// </summary>
    bool IsBlock { get; }

    /// <summary>
    /// Gets whether this layout object is a box (has box model).
    /// </summary>
    bool IsBox { get; }

    /// <summary>
    /// Gets whether this layout object is positioned (relative, absolute, fixed, sticky).
    /// </summary>
    bool IsPositioned { get; }

    /// <summary>
    /// Gets whether this layout object is a replaced element.
    /// </summary>
    bool IsReplaced { get; }

    /// <summary>
    /// Gets whether this layout object establishes a block formatting context.
    /// </summary>
    bool EstablishesBlockFormattingContext { get; }

    // Invalidation flags

    /// <summary>
    /// Checks if this layout object needs style recalculation.
    /// </summary>
    bool NeedsStyleRecalc();

    /// <summary>
    /// Checks if any child needs style recalculation.
    /// </summary>
    bool ChildNeedsStyleRecalc();

    /// <summary>
    /// Marks this layout object as needing style recalculation.
    /// </summary>
    void SetNeedsStyleRecalc();

    /// <summary>
    /// Clears the style recalculation flag.
    /// </summary>
    void ClearNeedsStyleRecalc();

    /// <summary>
    /// Marks children as needing style recalculation.
    /// </summary>
    void SetChildNeedsStyleRecalc();

    /// <summary>
    /// Clears the child needs style recalculation flag.
    /// </summary>
    void ClearChildNeedsStyleRecalc();

    /// <summary>
    /// Checks if this layout object needs layout.
    /// </summary>
    bool NeedsLayout();

    /// <summary>
    /// Checks if any child needs layout.
    /// </summary>
    bool ChildNeedsLayout();

    /// <summary>
    /// Marks this layout object as needing layout.
    /// </summary>
    void SetNeedsLayout();

    /// <summary>
    /// Clears the layout flag.
    /// </summary>
    void ClearNeedsLayout();

    /// <summary>
    /// Marks children as needing layout.
    /// </summary>
    void SetChildNeedsLayout();

    /// <summary>
    /// Clears the child needs layout flag.
    /// </summary>
    void ClearChildNeedsLayout();

    /// <summary>
    /// Checks if this layout object needs paint invalidation.
    /// </summary>
    bool NeedsPaintInvalidation();

    /// <summary>
    /// Marks this layout object as needing paint invalidation.
    /// </summary>
    void SetNeedsPaintInvalidation();

    /// <summary>
    /// Clears the paint invalidation flag.
    /// </summary>
    void ClearNeedsPaintInvalidation();

    // Tree management

    /// <summary>
    /// Sets the parent layout object.
    /// </summary>
    void SetParent(ILayoutObject? parent);

    /// <summary>
    /// Removes this layout object from the tree.
    /// </summary>
    void Remove();

    /// <summary>
    /// Destroys this layout object and cleans up resources.
    /// </summary>
    void Destroy();

    /// <summary>
    /// Updates the style for this layout object.
    /// </summary>
    void UpdateStyle(IComputedStyle? oldStyle, IComputedStyle newStyle);

    /// <summary>
    /// Called when a child is added to propagate invalidation flags.
    /// </summary>
    void ChildAdded(ILayoutObject child);

    /// <summary>
    /// Called when a child is removed to update invalidation flags.
    /// </summary>
    void ChildRemoved(ILayoutObject child);
}