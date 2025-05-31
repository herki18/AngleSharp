namespace LayoutEngine.Core.LayoutNG.Public;

using AngleSharp.Dom;

/// <summary>
/// Layout system interface that works with the LayoutObject tree instead of DOM.
/// This replaces the old ILayoutSystem interface.
/// </summary>
public interface ILayoutSystemNG
{
    /// <summary>
    /// Gets the layout object tree.
    /// </summary>
    LayoutObjectTree LayoutTree { get; }

    /// <summary>
    /// Builds the initial layout tree from a document.
    /// </summary>
    void BuildLayoutTree(IDocument document);

    /// <summary>
    /// Performs layout computation on the layout tree.
    /// </summary>
    void PerformLayout();

    /// <summary>
    /// Gets the layout object for a DOM node.
    /// </summary>
    ILayoutObject? GetLayoutObject(INode node);

    /// <summary>
    /// Gets the layout object for a DOM element.
    /// </summary>
    ILayoutObject? GetLayoutObject(IElement element);

    /// <summary>
    /// Checks if any layout object needs style recalculation.
    /// </summary>
    bool NeedsStyleRecalc();

    /// <summary>
    /// Checks if any layout object needs layout.
    /// </summary>
    bool NeedsLayout();

    /// <summary>
    /// Handles DOM mutations and updates the layout tree accordingly.
    /// </summary>
    void HandleDOMMutation(IMutationRecord mutation);

    /// <summary>
    /// Forces a complete rebuild of the layout tree.
    /// </summary>
    void RebuildLayoutTree();

    /// <summary>
    /// Gets layout metrics for a specific layout object.
    /// </summary>
    LayoutMetrics? GetLayoutMetrics(ILayoutObject layoutObject);
}

/// <summary>
/// Contains computed layout metrics for a layout object.
/// </summary>
public class LayoutMetrics
{
    /// <summary>
    /// Content box (excludes padding, border, margin).
    /// </summary>
    public LayoutEngine.Core.Layout.Internal.Rect ContentBox { get; set; }

    /// <summary>
    /// Padding box (content + padding).
    /// </summary>
    public LayoutEngine.Core.Layout.Internal.Rect PaddingBox { get; set; }

    /// <summary>
    /// Border box (content + padding + border).
    /// </summary>
    public LayoutEngine.Core.Layout.Internal.Rect BorderBox { get; set; }

    /// <summary>
    /// Margin box (content + padding + border + margin).
    /// </summary>
    public LayoutEngine.Core.Layout.Internal.Rect MarginBox { get; set; }

    /// <summary>
    /// Scroll dimensions if this is a scroll container.
    /// </summary>
    public ScrollMetrics? ScrollMetrics { get; set; }

    /// <summary>
    /// Whether this object is visible (not clipped, not display:none).
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// The containing block for positioned descendants.
    /// </summary>
    public ILayoutBox? ContainingBlock { get; set; }
}

/// <summary>
/// Scroll-related metrics for scroll containers.
/// </summary>
public class ScrollMetrics
{
    /// <summary>
    /// Total scrollable width.
    /// </summary>
    public float ScrollWidth { get; set; }

    /// <summary>
    /// Total scrollable height.
    /// </summary>
    public float ScrollHeight { get; set; }

    /// <summary>
    /// Current horizontal scroll position.
    /// </summary>
    public float ScrollLeft { get; set; }

    /// <summary>
    /// Current vertical scroll position.
    /// </summary>
    public float ScrollTop { get; set; }

    /// <summary>
    /// Visible width (viewport).
    /// </summary>
    public float ClientWidth { get; set; }

    /// <summary>
    /// Visible height (viewport).
    /// </summary>
    public float ClientHeight { get; set; }
}