namespace LayoutEngine.NG.Style;

using AngleSharp.Dom;
using LayoutEngine.NG.Layout.Dom;

/// <summary>
/// Context for style recalculation, following BlinkNG's style recalc architecture.
/// Carries state and configuration through the style recalc process.
/// </summary>
public class StyleRecalcContext
{
    /// <summary>
    /// The parent computed style for inheritance.
    /// In BlinkNG, style inheritance is a key part of the cascade.
    /// </summary>
    public ComputedStyle? ParentStyle { get; set; }

    /// <summary>
    /// The layout parent style (may differ from DOM parent).
    /// In BlinkNG, used for cases like position:absolute elements.
    /// </summary>
    public ComputedStyle? LayoutParentStyle { get; set; }

    /// <summary>
    /// Whether we're in a subtree style recalc.
    /// In BlinkNG, affects optimization strategies.
    /// </summary>
    public bool IsSubtreeRecalc { get; set; }

    /// <summary>
    /// The current recalc change type.
    /// </summary>
    public StyleChangeType ChangeType { get; set; } = StyleChangeType.NoChange;

    /// <summary>
    /// Whether animations are being updated.
    /// In BlinkNG, animation updates have special handling.
    /// </summary>
    public bool IsAnimationUpdate { get; set; }

    /// <summary>
    /// Whether we're recalculating styles for a container query.
    /// In BlinkNG, container queries need special context.
    /// </summary>
    public bool IsContainerQueryRecalc { get; set; }

    /// <summary>
    /// The container element for container query context.
    /// </summary>
    public IElement? ContainerElement { get; set; }

    /// <summary>
    /// Whether forced colors mode is active.
    /// In BlinkNG, affects color computation.
    /// </summary>
    public bool ForcedColorsMode { get; set; }

    /// <summary>
    /// Creates a child context for descendant recalc.
    /// In BlinkNG, context is propagated down the tree.
    /// </summary>
    public StyleRecalcContext CreateChildContext(ComputedStyle? childParentStyle = null)
    {
        return new StyleRecalcContext
        {
            ParentStyle = childParentStyle ?? ParentStyle,
            LayoutParentStyle = LayoutParentStyle,
            IsSubtreeRecalc = IsSubtreeRecalc,
            ChangeType = IsSubtreeRecalc ? ChangeType : StyleChangeType.NoChange,
            IsAnimationUpdate = IsAnimationUpdate,
            IsContainerQueryRecalc = IsContainerQueryRecalc,
            ContainerElement = ContainerElement,
            ForcedColorsMode = ForcedColorsMode
        };
    }

    /// <summary>
    /// Creates a style recalc context from ancestor styles.
    /// In BlinkNG, this is used to initialize context for style recalculation.
    /// </summary>
    public static StyleRecalcContext FromAncestors(IElement? element, LayoutDataManager layoutDataManager)
    {
        var context = new StyleRecalcContext();

        if (element == null)
            return context;

        // Find parent style
        var parent = element.ParentElement;
        if (parent != null)
        {
            var parentLayout = layoutDataManager.GetOrCreate(parent);
            context.ParentStyle = parentLayout.GetComputedStyle();
            context.LayoutParentStyle = context.ParentStyle;
        }

        return context;
    }
}