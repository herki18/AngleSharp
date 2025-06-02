namespace LayoutEngine.NG.Style;

/// <summary>
/// Style difference between old and new computed styles.
/// In BlinkNG: ComputedStyle::Difference
/// </summary>
internal enum StyleDifference
{
    Equal,
    NeedsReattachLayoutTree,
    NeedsFullLayout,
    NeedsPositionedMovementLayout,
    NeedsSimplifiedLayout
}