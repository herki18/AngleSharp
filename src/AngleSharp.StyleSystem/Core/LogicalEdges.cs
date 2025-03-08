namespace AngleSharp.StyleSystem;

using Core;
using Css.Values;

/// <summary>
/// Represents logical edges independent of writing mode.
/// </summary>
public readonly struct LogicalEdges
{
    /// <summary>
    /// Gets the block-start edge.
    /// </summary>
    public CssLengthValue BlockStart { get; }

    /// <summary>
    /// Gets the inline-end edge.
    /// </summary>
    public CssLengthValue InlineEnd { get; }

    /// <summary>
    /// Gets the block-end edge.
    /// </summary>
    public CssLengthValue BlockEnd { get; }

    /// <summary>
    /// Gets the inline-start edge.
    /// </summary>
    public CssLengthValue InlineStart { get; }

    public LogicalEdges(CssLengthValue blockStart, CssLengthValue inlineEnd,
        CssLengthValue blockEnd, CssLengthValue inlineStart)
    {
        BlockStart = blockStart;
        InlineEnd = inlineEnd;
        BlockEnd = blockEnd;
        InlineStart = inlineStart;
    }

    /// <summary>
    /// Converts to physical edges based on writing mode.
    /// </summary>
    public Edges ToPhysical(WritingMode writingMode)
    {
        if (writingMode.IsHorizontal)
        {
            return writingMode.IsRightToLeft
                ? new Edges(BlockStart, InlineStart, BlockEnd, InlineEnd)
                : new Edges(BlockStart, InlineEnd, BlockEnd, InlineStart);
        }
        else
        {
            return writingMode.IsRightToLeft
                ? new Edges(InlineEnd, BlockEnd, InlineStart, BlockStart)
                : new Edges(InlineStart, BlockEnd, InlineEnd, BlockStart);
        }
    }
}