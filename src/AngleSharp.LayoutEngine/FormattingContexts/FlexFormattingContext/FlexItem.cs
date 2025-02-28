namespace AngleSharp.LayoutEngine.FormattingContexts.FlexFormattingContext;

using Core;

#pragma warning disable CS8618
/// <summary>
/// Represents a flex item.
/// </summary>
public class FlexItem
{
    /// <summary>
    /// The layout node for this flex item.
    /// </summary>
    public LayoutNode Node { get; set; }

    /// <summary>
    /// The flex grow factor.
    /// </summary>
    public float FlexGrow { get; set; }

    /// <summary>
    /// The flex shrink factor.
    /// </summary>
    public float FlexShrink { get; set; }

    /// <summary>
    /// The flex basis type.
    /// </summary>
    public FlexBasis FlexBasis { get; set; }

    /// <summary>
    /// The flex basis value.
    /// </summary>
    public float FlexBasisValue { get; set; }

    /// <summary>
    /// The self-alignment along the cross axis.
    /// </summary>
    public FlexAlignSelf AlignSelf { get; set; }

    /// <summary>
    /// The hypothetical main size (after flex basis calculation but before distribution).
    /// </summary>
    public float HypotheticalMainSize { get; set; }

    /// <summary>
    /// The final main size.
    /// </summary>
    public float MainSize { get; set; }

    /// <summary>
    /// The final cross size.
    /// </summary>
    public float CrossSize { get; set; }

    /// <summary>
    /// The position along the main axis.
    /// </summary>
    public float MainPosition { get; set; }

    /// <summary>
    /// The position along the cross axis.
    /// </summary>
    public float CrossPosition { get; set; }
}