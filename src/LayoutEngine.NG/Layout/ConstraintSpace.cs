namespace LayoutEngine.NG.Layout;

using System;

/// <summary>
/// Represents the constraint space for layout algorithms following LayoutNG principles.
/// This is an immutable data structure that flows down the layout tree carrying
/// sizing constraints and contextual information.
/// </summary>
public class ConstraintSpace
{
    /// <summary>
    /// Available inline size (width in horizontal writing mode).
    /// </summary>
    public float AvailableInlineSize { get; }

    /// <summary>
    /// Available block size (height in horizontal writing mode).
    /// float.PositiveInfinity means unconstrained.
    /// </summary>
    public float AvailableBlockSize { get; }

    /// <summary>
    /// Whether the inline size is fixed (definite).
    /// </summary>
    public bool IsFixedInlineSize { get; }

    /// <summary>
    /// Whether the block size is fixed (definite).
    /// </summary>
    public bool IsFixedBlockSize { get; }

    /// <summary>
    /// Whether this constraint space establishes a new formatting context.
    /// </summary>
    public bool IsNewFormattingContext { get; }

    /// <summary>
    /// The percentage resolution size for the inline axis.
    /// May differ from AvailableInlineSize for replaced elements, etc.
    /// </summary>
    public float PercentageResolutionInlineSize { get; }

    /// <summary>
    /// The percentage resolution size for the block axis.
    /// May be indefinite (float.NaN) if the containing block height is auto.
    /// </summary>
    public float PercentageResolutionBlockSize { get; }

    /// <summary>
    /// Whether to shrink-to-fit in the inline axis.
    /// True for floats, absolute positioning with auto width, etc.
    /// </summary>
    public bool IsShrinkToFit { get; }

    /// <summary>
    /// Exclusion space for floats (simplified for now).
    /// In real LayoutNG, this would be NGExclusionSpace.
    /// </summary>
    public ExclusionSpace ExclusionSpace { get; }

    /// <summary>
    /// Margin strut for margin collapsing.
    /// </summary>
    public MarginStrut MarginStrut { get; }

    /// <summary>
    /// The BFC (Block Formatting Context) offset.
    /// Position of the current block formatting context root.
    /// </summary>
    public BfcOffset BfcOffset { get; }

    /// <summary>
    /// Whether the BFC offset has been established.
    /// </summary>
    public bool HasBfcOffset { get; }

    /// <summary>
    /// The clearance offset if any (for clear property).
    /// </summary>
    public float? ClearanceOffset { get; }

    /// <summary>
    /// Whether this is a constraint space for the root element.
    /// </summary>
    public bool IsRootElement { get; }

    private ConstraintSpace(Builder builder)
    {
        AvailableInlineSize = builder.AvailableInlineSize;
        AvailableBlockSize = builder.AvailableBlockSize;
        IsFixedInlineSize = builder.IsFixedInlineSize;
        IsFixedBlockSize = builder.IsFixedBlockSize;
        IsNewFormattingContext = builder.IsNewFormattingContext;
        PercentageResolutionInlineSize = builder.PercentageResolutionInlineSize;
        PercentageResolutionBlockSize = builder.PercentageResolutionBlockSize;
        IsShrinkToFit = builder.IsShrinkToFit;
        ExclusionSpace = builder.ExclusionSpace;
        MarginStrut = builder.MarginStrut;
        BfcOffset = builder.BfcOffset;
        HasBfcOffset = builder.HasBfcOffset;
        ClearanceOffset = builder.ClearanceOffset;
        IsRootElement = builder.IsRootElement;
    }

    /// <summary>
    /// Creates a builder for constructing NGConstraintSpace instances.
    /// </summary>
    public static Builder CreateBuilder() => new Builder();

    /// <summary>
    /// Builder pattern for NGConstraintSpace following LayoutNG conventions.
    /// </summary>
    public class Builder
    {
        public float AvailableInlineSize { get; set; } = 0;
        public float AvailableBlockSize { get; set; } = float.PositiveInfinity;
        public bool IsFixedInlineSize { get; set; } = false;
        public bool IsFixedBlockSize { get; set; } = false;
        public bool IsNewFormattingContext { get; set; } = false;
        public float PercentageResolutionInlineSize { get; set; } = 0;
        public float PercentageResolutionBlockSize { get; set; } = float.NaN;
        public bool IsShrinkToFit { get; set; } = false;
        public ExclusionSpace ExclusionSpace { get; set; } = new ExclusionSpace();
        public MarginStrut MarginStrut { get; set; } = new MarginStrut();
        public BfcOffset BfcOffset { get; set; } = new BfcOffset();
        public bool HasBfcOffset { get; set; } = false;
        public float? ClearanceOffset { get; set; } = null;
        public bool IsRootElement { get; set; } = false;

        public Builder SetAvailableSize(float inlineSize, float blockSize)
        {
            AvailableInlineSize = inlineSize;
            AvailableBlockSize = blockSize;
            return this;
        }

        public Builder SetIsFixedSize(bool fixedInline, bool fixedBlock)
        {
            IsFixedInlineSize = fixedInline;
            IsFixedBlockSize = fixedBlock;
            return this;
        }

        public Builder SetPercentageResolution(float inlineSize, float blockSize)
        {
            PercentageResolutionInlineSize = inlineSize;
            PercentageResolutionBlockSize = blockSize;
            return this;
        }

        public Builder SetIsNewFormattingContext(bool isNew)
        {
            IsNewFormattingContext = isNew;
            return this;
        }

        public Builder SetMarginStrut(MarginStrut strut)
        {
            MarginStrut = strut;
            return this;
        }

        public Builder SetBfcOffset(BfcOffset offset)
        {
            BfcOffset = offset;
            HasBfcOffset = true;
            return this;
        }

        public ConstraintSpace Build()
        {
            // Apply defaults
            if (PercentageResolutionInlineSize == 0 && AvailableInlineSize > 0)
            {
                PercentageResolutionInlineSize = AvailableInlineSize;
            }

            return new ConstraintSpace(this);
        }
    }
}

/// <summary>
/// Represents margin collapsing state following LayoutNG principles.
/// </summary>
public struct MarginStrut
{
    /// <summary>
    /// The positive margin component.
    /// </summary>
    public float PositiveMargin { get; set; }

    /// <summary>
    /// The negative margin component.
    /// </summary>
    public float NegativeMargin { get; set; }

    /// <summary>
    /// Whether this strut is empty (no margins).
    /// </summary>
    public bool IsEmpty => PositiveMargin == 0 && NegativeMargin == 0;

    /// <summary>
    /// The sum of margins after collapsing.
    /// </summary>
    public float Sum => PositiveMargin + NegativeMargin;

    /// <summary>
    /// Appends a margin value to this strut.
    /// </summary>
    public void Append(float margin)
    {
        if (margin > 0)
        {
            PositiveMargin = Math.Max(PositiveMargin, margin);
        }
        else if (margin < 0)
        {
            NegativeMargin = Math.Min(NegativeMargin, margin);
        }
    }

    /// <summary>
    /// Merges another margin strut into this one.
    /// </summary>
    public void Merge(MarginStrut other)
    {
        PositiveMargin = Math.Max(PositiveMargin, other.PositiveMargin);
        NegativeMargin = Math.Min(NegativeMargin, other.NegativeMargin);
    }
}

/// <summary>
/// Represents the Block Formatting Context offset.
/// </summary>
public struct BfcOffset
{
    /// <summary>
    /// The inline offset from the BFC root.
    /// </summary>
    public float InlineOffset { get; set; }

    /// <summary>
    /// The block offset from the BFC root.
    /// </summary>
    public float BlockOffset { get; set; }

    public BfcOffset(float inlineOffset, float blockOffset)
    {
        InlineOffset = inlineOffset;
        BlockOffset = blockOffset;
    }
}

/// <summary>
/// Simplified exclusion space for floats.
/// In real LayoutNG, this would track float positions for wrapping.
/// </summary>
public class ExclusionSpace
{
    // Simplified for now - real implementation would track float rectangles
    public bool HasExclusions { get; set; } = false;
}