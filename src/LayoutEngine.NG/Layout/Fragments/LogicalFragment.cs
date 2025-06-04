namespace LayoutEngine.NG.Layout.Fragments;

using Process;

/// <summary>
/// Represents a fragment in logical coordinates.
/// In LayoutNG, this provides a logical view of physical fragments.
/// </summary>
public class LogicalFragment
{
    public WritingDirectionMode WritingDirection { get; }
    public PhysicalFragment PhysicalFragment { get; }

    public LogicalFragment(WritingDirectionMode writingDirection, PhysicalFragment physicalFragment)
    {
        WritingDirection = writingDirection;
        PhysicalFragment = physicalFragment;
    }

    /// <summary>
    /// Gets the inline size of the fragment.
    /// </summary>
    public float InlineSize
    {
        get
        {
            if (IsHorizontalWritingMode(WritingDirection.WritingMode))
                return PhysicalFragment.Size.Width;
            return PhysicalFragment.Size.Height;
        }
    }

    /// <summary>
    /// Gets the block size of the fragment.
    /// </summary>
    public float BlockSize
    {
        get
        {
            if (IsHorizontalWritingMode(WritingDirection.WritingMode))
                return PhysicalFragment.Size.Height;
            return PhysicalFragment.Size.Width;
        }
    }

    private static bool IsHorizontalWritingMode(WritingMode mode)
    {
        return mode == WritingMode.HorizontalTb;
    }
}