namespace AngleSharp.LayoutEngine.FormattingContexts.FlexFormattingContext;

#pragma warning disable CS8618
/// <summary>
/// Metrics for a flex item (used for incremental updates).
/// </summary>
public class FlexItemMetrics
{
    /// <summary>
    /// The main size.
    /// </summary>
    public float MainSize { get; set; }

    /// <summary>
    /// The cross size.
    /// </summary>
    public float CrossSize { get; set; }

    /// <summary>
    /// The position along the main axis.
    /// </summary>
    public float MainPosition { get; set; }

    /// <summary>
    /// The position along the cross axis.
    /// </summary>
    public float CrossPosition { get; set; }
}