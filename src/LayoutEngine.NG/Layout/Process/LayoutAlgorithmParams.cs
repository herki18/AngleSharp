namespace LayoutEngine.NG.Layout.Process;

using LayoutEngine.NG.Layout.Fragments;
using LayoutEngine.NG.Layout.Inputs;

/// <summary>
/// Parameters passed to layout algorithms.
/// In LayoutNG, this encapsulates all inputs needed for layout computation.
/// </summary>
public class LayoutAlgorithmParams
{
    public BlockNode Node { get; }
    public FragmentGeometry FragmentGeometry { get; }
    public ConstraintSpace ConstraintSpace { get; }
    public BlockBreakToken? BreakToken { get; }
    public EarlyBreak? EarlyBreak { get; }
    public ColumnSpannerPath? ColumnSpannerPath { get; set; }
    public LayoutResult? PreviousResult { get; set; }

    public LayoutAlgorithmParams(
        BlockNode node,
        FragmentGeometry fragmentGeometry,
        ConstraintSpace constraintSpace,
        BlockBreakToken? breakToken = null,
        EarlyBreak? earlyBreak = null)
    {
        Node = node;
        FragmentGeometry = fragmentGeometry;
        ConstraintSpace = constraintSpace;
        BreakToken = breakToken;
        EarlyBreak = earlyBreak;
    }
}