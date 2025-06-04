namespace LayoutEngine.NG.Layout;

using AngleSharp.Dom;
using LayoutEngine.NG.Style;

/// <summary>
/// Base class for all layout algorithms in LayoutNG.
/// Follows BlinkNG's LayoutAlgorithm pattern for stateless layout computation.
/// </summary>
public abstract class LayoutAlgorithm
{
    protected readonly LayoutInputNode InputNode;
    protected readonly ConstraintSpace ConstraintSpace;
    protected readonly ComputedStyle Style;

    protected LayoutAlgorithm(LayoutInputNode inputNode, ConstraintSpace constraintSpace)
    {
        InputNode = inputNode ?? throw new ArgumentNullException(nameof(inputNode));
        ConstraintSpace = constraintSpace ?? throw new ArgumentNullException(nameof(constraintSpace));
        Style = inputNode.Style ?? throw new InvalidOperationException("InputNode must have computed style");
    }

    /// <summary>
    /// Performs layout and returns the result.
    /// </summary>
    public abstract LayoutResult Layout();

    /// <summary>
    /// Returns the intrinsic size for the given constraint space.
    /// </summary>
    public virtual PhysicalSize IntrinsicSize(ConstraintSpace constraintSpace)
    {
        return PhysicalSize.Zero;
    }

    /// <summary>
    /// Returns the baseline for this layout algorithm.
    /// </summary>
    public virtual float? Baseline(ConstraintSpace constraintSpace)
    {
        return null;
    }

    /// <summary>
    /// Creates the appropriate layout algorithm for the given input node.
    /// </summary>
    public static LayoutAlgorithm CreateAlgorithm(LayoutInputNode inputNode, ConstraintSpace constraintSpace)
    {
        return inputNode.LayoutObjectType switch
        {
            LayoutObjectType.Block => new BlockLayoutAlgorithm(inputNode, constraintSpace),
            LayoutObjectType.Inline => new InlineLayoutAlgorithm(inputNode, constraintSpace),
            LayoutObjectType.Text => new TextLayoutAlgorithm(inputNode, constraintSpace),
            _ => throw new NotSupportedException($"Layout algorithm not implemented for {inputNode.LayoutObjectType}")
        };
    }
}