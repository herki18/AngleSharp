namespace LayoutEngine.Core.LayoutStyle.Public;

using LayoutEngine.Core.LayoutNG.Public;
using AngleSharp.Dom;

/// <summary>
/// Style system that works exclusively with LayoutObjects, matching LayoutNG architecture.
/// Handles both DOM-backed and anonymous layout objects.
/// </summary>
public interface ILayoutStyleSystem
{
    /// <summary>
    /// Resolves style for a layout object during tree construction or style recalc.
    /// Handles anonymous objects by synthesizing appropriate styles.
    /// </summary>
    ILayoutComputedStyle ResolveStyle(ILayoutObject layoutObject, ILayoutStyleContext context);

    /// <summary>
    /// Performs style resolution for an entire layout tree.
    /// </summary>
    void ResolveLayoutTreeStyles(ILayoutObject rootLayoutObject);

    /// <summary>
    /// Gets cached computed style for a layout object if available.
    /// </summary>
    ILayoutComputedStyle? GetComputedStyle(ILayoutObject layoutObject);

    /// <summary>
    /// Invalidates cached style for a layout object and its descendants.
    /// </summary>
    void InvalidateStyle(ILayoutObject layoutObject, bool recursive = true);

    /// <summary>
    /// Clears all cached styles.
    /// </summary>
    void ClearStyles();

    /// <summary>
    /// Checks if a layout object needs style recalculation.
    /// </summary>
    bool NeedsStyleRecalc(ILayoutObject layoutObject);
}