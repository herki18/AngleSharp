namespace LayoutEngine.NG.Layout.Core;

/// <summary>
/// Represents an inline-block element in the layout tree.
/// In LayoutNG, inline-blocks participate in inline flow but establish
/// a block formatting context internally.
/// </summary>
public class LayoutInlineBlock : LayoutBlockFlow
{
    /// <summary>
    /// The baseline position for inline alignment.
    /// In LayoutNG, inline-blocks align by their baseline in line boxes.
    /// </summary>
    public float InlineBaseline { get; set; }

    /// <summary>
    /// Whether this inline-block uses the last line box baseline.
    /// In LayoutNG, controlled by the vertical-align property.
    /// </summary>
    public bool UsesLastLineBoxBaseline { get; set; }

    /// <summary>
    /// The vertical alignment type for this inline-block.
    /// In LayoutNG, affects positioning within the line box.
    /// </summary>
    public InlineVerticalAlign VerticalAlign { get; set; } = InlineVerticalAlign.Baseline;

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.InlineBlock;
    }

    /// <summary>
    /// Inline-blocks always establish a new block formatting context.
    /// This is a key characteristic that differentiates them from regular inlines.
    /// </summary>
    public override bool EstablishesFormattingContext()
    {
        return true;
    }

    /// <summary>
    /// Gets the baseline for inline alignment.
    /// In LayoutNG, this is used by the inline layout algorithm.
    /// </summary>
    public float GetInlineBaseline()
    {
        if (UsesLastLineBoxBaseline && LineBoxes != null)
        {
            // Use the baseline of the last line box
            // In real LayoutNG, this would access the last NGLineBoxFragment
            return InlineBaseline;
        }
        // Default to the bottom margin edge for empty inline-blocks
        // or first line baseline for non-empty ones
        return HasInlineContent ? Baseline : ContentSize.Height;
    }

    /// <summary>
    /// Computes the position adjustment for vertical alignment.
    /// In LayoutNG, used by the line box layout algorithm.
    /// </summary>
    public float ComputeVerticalAlignmentOffset(float lineBoxHeight, float lineBoxBaseline)
    {
        switch (VerticalAlign)
        {
            case InlineVerticalAlign.Baseline:
                return lineBoxBaseline - GetInlineBaseline();
            case InlineVerticalAlign.Top:
                return 0;
            case InlineVerticalAlign.Middle:
                return (lineBoxHeight - ContentSize.Height) / 2;
            case InlineVerticalAlign.Bottom:
                return lineBoxHeight - ContentSize.Height;
            case InlineVerticalAlign.TextTop:
            case InlineVerticalAlign.TextBottom:
                // Simplified - in real LayoutNG these would use font metrics
                return lineBoxBaseline - GetInlineBaseline();
            default:
                return 0;
        }
    }
}

/// <summary>
/// CSS vertical-align values for inline-level elements.
/// </summary>
public enum InlineVerticalAlign
{
    Baseline,
    Top,
    Middle,
    Bottom,
    TextTop,
    TextBottom,
    // Length and percentage values would be handled separately in real LayoutNG
}