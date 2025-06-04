namespace LayoutEngine.NG.Layout;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

/// <summary>
/// Represents the physical output of layout following LayoutNG principles.
/// This is an immutable data structure that represents the geometry and
/// properties of a laid-out box and its children.
/// </summary>
public class PhysicalFragment
{
    /// <summary>
    /// The size of this fragment (border box).
    /// </summary>
    public PhysicalSize Size { get; }

    /// <summary>
    /// The offset of this fragment relative to its parent.
    /// </summary>
    public PhysicalOffset Offset { get; set; }

    /// <summary>
    /// The layout object that generated this fragment.
    /// </summary>
    public LayoutObject? LayoutObject { get; }

    /// <summary>
    /// Child fragments.
    /// </summary>
    public ImmutableList<PhysicalFragment> Children { get; }

    /// <summary>
    /// The margin box size (includes margins).
    /// </summary>
    public PhysicalSize MarginBoxSize { get; }

    /// <summary>
    /// The margins of this fragment.
    /// </summary>
    public PhysicalBoxStrut Margins { get; }

    /// <summary>
    /// The borders of this fragment.
    /// </summary>
    public PhysicalBoxStrut Borders { get; }

    /// <summary>
    /// The padding of this fragment.
    /// </summary>
    public PhysicalBoxStrut Padding { get; }

    /// <summary>
    /// The content box offset (relative to border box).
    /// </summary>
    public PhysicalOffset ContentOffset { get; }

    /// <summary>
    /// The content box size.
    /// </summary>
    public PhysicalSize ContentSize { get; }

    /// <summary>
    /// The baseline position for this fragment (if applicable).
    /// </summary>
    public float? Baseline { get; }

    /// <summary>
    /// Whether this fragment establishes a new formatting context.
    /// </summary>
    public bool IsFormattingContextRoot { get; }

    /// <summary>
    /// The block end margin strut (for margin collapsing).
    /// </summary>
    public MarginStrut EndMarginStrut { get; }

    /// <summary>
    /// Break token for fragmentation (null for now).
    /// </summary>
    public object? BreakToken { get; }

    /// <summary>
    /// Whether this fragment is self-collapsing (empty with collapsible margins).
    /// </summary>
    public bool IsSelfCollapsing { get; }

    private PhysicalFragment(Builder builder)
    {
        Size = builder.Size;
        Offset = builder.Offset;
        LayoutObject = builder.LayoutObject;
        Children = builder.Children.ToImmutableList();
        Margins = builder.Margins;
        Borders = builder.Borders;
        Padding = builder.Padding;
        Baseline = builder.Baseline;
        IsFormattingContextRoot = builder.IsFormattingContextRoot;
        EndMarginStrut = builder.EndMarginStrut;
        BreakToken = builder.BreakToken;
        IsSelfCollapsing = builder.IsSelfCollapsing;

        // Calculate derived properties
        MarginBoxSize = new PhysicalSize(
            Size.Width + Margins.InlineSum,
            Size.Height + Margins.BlockSum
        );

        ContentOffset = new PhysicalOffset(
            Borders.InlineStart + Padding.InlineStart,
            Borders.BlockStart + Padding.BlockStart
        );

        ContentSize = new PhysicalSize(
            Size.Width - Borders.InlineSum - Padding.InlineSum,
            Size.Height - Borders.BlockSum - Padding.BlockSum
        );
    }

    /// <summary>
    /// Creates a builder for constructing NGPhysicalFragment instances.
    /// </summary>
    public static Builder CreateBuilder() => new Builder();

    /// <summary>
    /// Builder for NGPhysicalFragment.
    /// </summary>
    public class Builder
    {
        public PhysicalSize Size { get; set; }
        public PhysicalOffset Offset { get; set; }
        public LayoutObject? LayoutObject { get; set; }
        public List<PhysicalFragment> Children { get; } = new List<PhysicalFragment>();
        public PhysicalBoxStrut Margins { get; set; }
        public PhysicalBoxStrut Borders { get; set; }
        public PhysicalBoxStrut Padding { get; set; }
        public float? Baseline { get; set; }
        public bool IsFormattingContextRoot { get; set; }
        public MarginStrut EndMarginStrut { get; set; }
        public object? BreakToken { get; set; }
        public bool IsSelfCollapsing { get; set; }

        public Builder SetSize(PhysicalSize size)
        {
            Size = size;
            return this;
        }

        public Builder SetOffset(PhysicalOffset offset)
        {
            Offset = offset;
            return this;
        }

        public Builder SetLayoutObject(LayoutObject? layoutObject)
        {
            LayoutObject = layoutObject;
            return this;
        }

        public Builder SetMargins(PhysicalBoxStrut margins)
        {
            Margins = margins;
            return this;
        }

        public Builder SetBorders(PhysicalBoxStrut borders)
        {
            Borders = borders;
            return this;
        }

        public Builder SetPadding(PhysicalBoxStrut padding)
        {
            Padding = padding;
            return this;
        }

        public Builder AddChild(PhysicalFragment child)
        {
            Children.Add(child);
            return this;
        }

        public Builder AddChildren(IEnumerable<PhysicalFragment> children)
        {
            Children.AddRange(children);
            return this;
        }

        public Builder SetBaseline(float? baseline)
        {
            Baseline = baseline;
            return this;
        }

        public Builder SetIsFormattingContextRoot(bool isRoot)
        {
            IsFormattingContextRoot = isRoot;
            return this;
        }

        public Builder SetEndMarginStrut(MarginStrut strut)
        {
            EndMarginStrut = strut;
            return this;
        }

        public Builder SetIsSelfCollapsing(bool isSelfCollapsing)
        {
            IsSelfCollapsing = isSelfCollapsing;
            return this;
        }

        public PhysicalFragment Build()
        {
            return new PhysicalFragment(this);
        }
    }
}

/// <summary>
/// Represents box model spacing (margins, borders, padding) in physical coordinates.
/// </summary>
public struct PhysicalBoxStrut
{
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }
    public float Left { get; }

    public PhysicalBoxStrut(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    // Writing-mode aware accessors
    public float InlineStart => Left;  // Assuming horizontal-tb
    public float InlineEnd => Right;
    public float BlockStart => Top;
    public float BlockEnd => Bottom;

    public float InlineSum => Left + Right;
    public float BlockSum => Top + Bottom;

    public static PhysicalBoxStrut Zero => new PhysicalBoxStrut(0, 0, 0, 0);
}

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