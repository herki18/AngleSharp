namespace AngleSharp.StyleSystem.Core;

using AngleSharp.Css.Values;

/// <summary>
/// Represents edges (margin, border, padding).
/// </summary>
public readonly struct Edges
{
    /// <summary>
    /// Gets the top edge.
    /// </summary>
    public CssLengthValue Top { get; }

    /// <summary>
    /// Gets the right edge.
    /// </summary>
    public CssLengthValue Right { get; }

    /// <summary>
    /// Gets the bottom edge.
    /// </summary>
    public CssLengthValue Bottom { get; }

    /// <summary>
    /// Gets the left edge.
    /// </summary>
    public CssLengthValue Left { get; }

    /// <summary>
    /// Creates a new Edges instance.
    /// </summary>
    public Edges(CssLengthValue top, CssLengthValue right, CssLengthValue bottom, CssLengthValue left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>
    /// Gets all edges as an array in clockwise order (top, right, bottom, left).
    /// </summary>
    public CssLengthValue[] ToArray() => new[] { Top, Right, Bottom, Left };

    /// <summary>
    /// Creates uniform edges with the same value for all sides.
    /// </summary>
    public static Edges Uniform(CssLengthValue value) => new Edges(value, value, value, value);
}

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

    /// <summary>
    /// Creates a new LogicalEdges instance.
    /// </summary>
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

    /// <summary>
    /// Creates uniform logical edges with the same value for all sides.
    /// </summary>
    public static LogicalEdges Uniform(CssLengthValue value) =>
        new LogicalEdges(value, value, value, value);
}