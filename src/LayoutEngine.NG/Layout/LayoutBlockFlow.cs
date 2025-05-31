namespace LayoutEngine.NG.Layout;

using Style;

/// <summary>
/// Represents a block flow layout object.
/// In LayoutNG, this handles normal block flow layout (not flex, grid, or table).
/// This is the most common type of block layout and inherits from LayoutBlock.
/// </summary>
public class LayoutBlockFlow : LayoutBlock
{
    /// <summary>
    /// The intrinsic size of this block flow.
    /// In LayoutNG, this is used for sizing calculations.
    /// </summary>
    public float IntrinsicBlockSize { get; set; }

    /// <summary>
    /// Whether this block flow contains any inline content.
    /// In LayoutNG, this determines if we need inline layout.
    /// </summary>
    public bool HasInlineContent { get; set; }

    /// <summary>
    /// The baseline position for this block.
    /// In LayoutNG, used for vertical alignment.
    /// </summary>
    public float Baseline { get; set; }

    /// <summary>
    /// Whether this block flow is a root element.
    /// In LayoutNG, the root has special layout rules.
    /// </summary>
    public bool IsDocumentElement { get; set; }

    /// <summary>
    /// Cached line boxes for inline content.
    /// In LayoutNG, line boxes are created during inline layout.
    /// Note: In real LayoutNG, this would be NGLineBoxFragment objects.
    /// </summary>
    public object? LineBoxes { get; set; }

    /// <summary>
    /// Whether margins collapse through this block.
    /// In LayoutNG, margin collapsing is a complex part of block layout.
    /// </summary>
    public bool MarginsCollapseThrough { get; set; }

    /// <summary>
    /// The block's collapsed margin values.
    /// In LayoutNG, tracks margin collapsing state.
    /// </summary>
    public MarginStrut MarginStrut { get; set; }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Block;
    }

    /// <summary>
    /// Whether this block establishes a new block formatting context.
    /// In LayoutNG, this is determined by various CSS properties.
    /// </summary>
    public override bool EstablishesFormattingContext()
    {
        if (Style == null)
            return false;

        // Following BlinkNG rules for BFC creation
        // A block establishes a new BFC if it:
        // - Has overflow other than visible
        // - Is absolutely positioned
        // - Is a float
        // - Has display: flow-root (in our case, we'll check other conditions)
        return (OverflowX != Overflow.Visible || OverflowY != Overflow.Visible) ||
               (Style.Position == PositionType.Absolute || Style.Position == PositionType.Fixed) ||
               Style.Display == DisplayType.InlineBlock;
    }
}

/// <summary>
/// Represents margin collapsing state in LayoutNG.
/// </summary>
public struct MarginStrut
{
    /// <summary>
    /// The positive margin component.
    /// </summary>
    public float PositiveMargin { get; set; }

    /// <summary>
    /// The negative margin component.
    /// </summary>
    public float NegativeMargin { get; set; }

    /// <summary>
    /// Gets the sum of margins after collapsing.
    /// In LayoutNG, this follows CSS margin collapsing rules.
    /// </summary>
    public float Sum => PositiveMargin + NegativeMargin;
}