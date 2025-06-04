namespace LayoutEngine.NG.Layout.Process;

using LayoutEngine.NG.Layout.Fragments;

/// <summary>
/// Represents the result of a block layout operation.
/// </summary>
public class LayoutResult
{
    /// <summary>
    /// The physical fragment produced by layout.
    /// </summary>
    public PhysicalFragment PhysicalFragment { get; }

    /// <summary>
    /// The intrinsic block size (for height: auto).
    /// </summary>
    public float IntrinsicBlockSize { get; }

    /// <summary>
    /// The block size for fragmentation.
    /// </summary>
    public float FragmentainerBlockSize { get; }

    /// <summary>
    /// The final BFC block offset.
    /// </summary>
    public float BfcBlockOffset { get; }

    /// <summary>
    /// The end margin strut for subsequent margin collapsing.
    /// </summary>
    public MarginStrut EndMarginStrut { get; }

    /// <summary>
    /// Whether this result depends on percentage block size.
    /// </summary>
    public bool DependsOnPercentageBlockSize { get; }

    public LayoutResult(
        PhysicalFragment physicalFragment,
        float intrinsicBlockSize,
        MarginStrut endMarginStrut,
        float bfcBlockOffset = 0,
        bool dependsOnPercentageBlockSize = false)
    {
        PhysicalFragment = physicalFragment;
        IntrinsicBlockSize = intrinsicBlockSize;
        FragmentainerBlockSize = physicalFragment.Size.Height;
        EndMarginStrut = endMarginStrut;
        BfcBlockOffset = bfcBlockOffset;
        DependsOnPercentageBlockSize = dependsOnPercentageBlockSize;
    }
}