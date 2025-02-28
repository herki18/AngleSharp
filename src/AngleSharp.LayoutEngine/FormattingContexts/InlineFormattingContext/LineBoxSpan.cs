namespace AngleSharp.LayoutEngine.FormattingContexts.InlineFormattingContext;

#pragma warning disable CS8625, CS8618
/// <summary>
/// Represents the span of a node's fragments across line boxes.
/// </summary>
public class LineBoxSpan
{
    /// <summary>
    /// Start X position of the span.
    /// </summary>
    public float StartX { get; set; }

    /// <summary>
    /// Start Y position of the span.
    /// </summary>
    public float StartY { get; set; }

    /// <summary>
    /// End X position of the span.
    /// </summary>
    public float EndX { get; set; }

    /// <summary>
    /// End Y position of the span.
    /// </summary>
    public float EndY { get; set; }
}