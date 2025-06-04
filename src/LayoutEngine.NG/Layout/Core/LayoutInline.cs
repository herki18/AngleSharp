namespace LayoutEngine.NG.Layout.Core;

using LayoutEngine.NG.Style;

/// <summary>
/// Represents an inline layout object in the layout tree.
/// In LayoutNG, inline elements participate in inline formatting contexts.
/// Following Blink's hierarchy, LayoutInline inherits directly from LayoutObject,
/// not from LayoutBox, because inlines don't have a principal box.
/// </summary>
public class LayoutInline : LayoutObject
{
    /// <summary>
    /// Whether this inline should always create a line box.
    /// In LayoutNG, some inlines don't generate line boxes if empty.
    /// </summary>
    public bool AlwaysCreateLineBoxes { get; set; }

    /// <summary>
    /// The inline's contribution to the line box height.
    /// In LayoutNG, this affects line box calculations.
    /// </summary>
    public float LineHeight { get; set; }

    /// <summary>
    /// Whether this inline is part of a continuation.
    /// In LayoutNG, inline elements can be split across block boundaries.
    /// </summary>
    public bool IsInContinuation { get; set; }

    /// <summary>
    /// The continuation for this inline if it's split.
    /// In LayoutNG, handles inline splitting across blocks.
    /// </summary>
    public LayoutInline? Continuation { get; set; }

    /// <summary>
    /// First line style variant if different from normal style.
    /// In LayoutNG, ::first-line can affect inline layout.
    /// </summary>
    public ComputedStyle? FirstLineStyle { get; set; }

    /// <summary>
    /// Whether this inline contains block-level children.
    /// In LayoutNG, this creates an inline-block or anonymous blocks.
    /// </summary>
    public bool ContainsBlockLevelChildren { get; set; }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Inline;
    }

    /// <summary>
    /// Inline elements don't establish formatting contexts.
    /// In LayoutNG, they participate in their container's inline formatting context.
    /// </summary>
    public override bool EstablishesFormattingContext()
    {
        return false;
    }

    /// <summary>
    /// Gets the effective line height for this inline.
    /// In LayoutNG, used for line box calculations.
    /// </summary>
    public float GetLineHeight()
    {
        if (Style == null)
            return 0;

        // Simplified - in real LayoutNG this would consider font metrics, line-height property, etc.
        return LineHeight > 0 ? LineHeight : 16; // Default line height
    }
}