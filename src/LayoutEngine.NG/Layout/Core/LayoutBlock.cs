namespace LayoutEngine.NG.Layout.Core;

/// <summary>
/// Base class for block-level layout objects.
/// In LayoutNG, LayoutBlock is an intermediate class between LayoutBox and LayoutBlockFlow.
/// This matches the actual Blink inheritance hierarchy.
/// </summary>
public class LayoutBlock : LayoutBox
{
    /// <summary>
    /// Whether this block has block-level children only.
    /// In LayoutNG, this affects the layout algorithm used.
    /// </summary>
    public bool ChildrenInline { get; set; } = false;

    /// <summary>
    /// The block's baseline position.
    /// In LayoutNG, used for baseline alignment.
    /// </summary>
    public float BaselinePosition { get; set; }

    /// <summary>
    /// Whether this block's margins collapse with its children.
    /// In LayoutNG, part of margin collapsing logic.
    /// </summary>
    public bool CreatesNewFormattingContext { get; set; }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Block;
    }

    /// <summary>
    /// Determines if this block establishes a block formatting context.
    /// In LayoutNG, this is a key concept affecting child layout.
    /// </summary>
    public override bool EstablishesFormattingContext()
    {
        // Base implementation - derived classes may override
        return CreatesNewFormattingContext;
    }

    /// <summary>
    /// Whether this block should avoid floats.
    /// In LayoutNG, used for clear property implementation.
    /// </summary>
    public bool AvoidsFloats { get; set; }
}