namespace LayoutEngine.NG.Layout.Fragments;

using Inputs;
using Process;

/// <summary>
/// Represents the geometry of a fragment during layout.
/// In LayoutNG, this contains sizing and spacing information.
/// </summary>
public class FragmentGeometry
{
    public LogicalSize BorderBoxSize { get; set; }
    public BoxStrut Border { get; set; }
    public BoxStrut Padding { get; set; }
    public BoxStrut Scrollbar { get; set; }

    /// <summary>
    /// Calculate initial fragment geometry for a node.
    /// </summary>
    public static FragmentGeometry Calculate(
        ConstraintSpace constraintSpace,
        BlockNode node,
        BlockBreakToken? breakToken,
        bool isIntrinsic)
    {
        // Simplified implementation
        return new FragmentGeometry
        {
            BorderBoxSize = new LogicalSize(
                constraintSpace.AvailableInlineSize,
                constraintSpace.AvailableBlockSize),
            Border = new BoxStrut(),
            Padding = new BoxStrut(),
            Scrollbar = new BoxStrut()
        };
    }
}

/// <summary>
/// Represents logical dimensions (inline/block).
/// </summary>
public struct LogicalSize
{
    public float InlineSize { get; }
    public float BlockSize { get; }

    public LogicalSize(float inlineSize, float blockSize)
    {
        InlineSize = inlineSize;
        BlockSize = blockSize;
    }

    public static LogicalSize Empty => new LogicalSize(0, 0);
}

/// <summary>
/// Represents spacing on all four sides of a box.
/// </summary>
public struct BoxStrut
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }

    public float InlineStart => Left; // Assuming LTR
    public float InlineEnd => Right;
    public float BlockStart => Top;
    public float BlockEnd => Bottom;

    public float InlineSum() => Left + Right;
    public float BlockSum() => Top + Bottom;

    public static BoxStrut operator +(BoxStrut a, BoxStrut b)
    {
        return new BoxStrut
        {
            Top = a.Top + b.Top,
            Right = a.Right + b.Right,
            Bottom = a.Bottom + b.Bottom,
            Left = a.Left + b.Left
        };
    }
}