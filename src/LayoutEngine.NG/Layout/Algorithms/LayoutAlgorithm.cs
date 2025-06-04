namespace LayoutEngine.NG.Layout.Algorithms;

using System;
using Fragments;
using Inputs;
using LayoutEngine.NG.Layout.Inline;
using LayoutEngine.NG.Style;
using Process;

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
        Style = inputNode.GetStyle() ?? throw new InvalidOperationException("InputNode must have computed style");
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
        if (inputNode.IsBlock)
        {
            if (inputNode is BlockNode blockNode)
            {
                return new BlockLayoutAlgorithm(blockNode, constraintSpace);
            }
            // Fallback for legacy code
            return new BlockLayoutAlgorithm(
                new BlockNode(inputNode.GetLayoutBox()),
                constraintSpace);
        }

        if (inputNode.IsInline)
        {
            if (inputNode is InlineNode inlineNode)
            {
                return new InlineLayoutAlgorithm(inlineNode, constraintSpace);
            }
            // Fallback for legacy code
            return new InlineLayoutAlgorithm(
                new InlineNode(inputNode.GetLayoutBox()),
                constraintSpace);
        }

        if (inputNode.IsText)
        {
            return new TextLayoutAlgorithm(inputNode, constraintSpace);
        }

        throw new NotSupportedException($"Layout algorithm not implemented for {inputNode.LayoutObjectType}");
    }
}

/// <summary>
/// Inline layout algorithm (stub for now).
/// </summary>
public class InlineLayoutAlgorithm : LayoutAlgorithm
{
    private readonly InlineNode _node;

    public InlineLayoutAlgorithm(InlineNode node, ConstraintSpace constraintSpace)
        : base(node, constraintSpace)
    {
        _node = node;
    }

    public override LayoutResult Layout()
    {
        // TODO: Implement inline layout algorithm
        var fragment = PhysicalFragment.CreateBuilder()
            .SetSize(new PhysicalSize(100, 20))
            .SetOffset(PhysicalOffset.Zero)
            .SetLayoutObject(_node.GetLayoutBox())
            .Build();

        return new LayoutResult(fragment, 20, new MarginStrut(), 0, false);
    }
}

/// <summary>
/// Text layout algorithm (stub for now).
/// </summary>
public class TextLayoutAlgorithm : LayoutAlgorithm
{
    public TextLayoutAlgorithm(LayoutInputNode node, ConstraintSpace constraintSpace)
        : base(node, constraintSpace)
    {
    }

    public override LayoutResult Layout()
    {
        // TODO: Implement text layout algorithm
        var textContent = InputNode.GetTextContent() ?? "";
        var width = textContent.Length * 8f; // Simplified
        var height = 20f; // Simplified line height

        var fragment = PhysicalFragment.CreateBuilder()
            .SetSize(new PhysicalSize(width, height))
            .SetOffset(PhysicalOffset.Zero)
            .SetLayoutObject(InputNode.GetLayoutBox())
            .SetBaseline(height * 0.8f)
            .Build();

        return new LayoutResult(fragment, height, new MarginStrut(), 0, false);
    }
}