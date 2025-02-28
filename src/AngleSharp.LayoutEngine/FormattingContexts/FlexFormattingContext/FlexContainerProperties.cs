namespace AngleSharp.LayoutEngine.FormattingContexts.FlexFormattingContext;

#pragma warning disable CS8618
/// <summary>
/// Properties of a flex container.
/// </summary>
public class FlexContainerProperties
{
    /// <summary>
    /// Direction of the main axis.
    /// </summary>
    public FlexDirection Direction { get; set; } = FlexDirection.Row;

    /// <summary>
    /// Wrapping behavior.
    /// </summary>
    public FlexWrap Wrap { get; set; } = FlexWrap.NoWrap;

    /// <summary>
    /// Alignment of items along the main axis.
    /// </summary>
    public FlexJustifyContent JustifyContent { get; set; } = FlexJustifyContent.FlexStart;

    /// <summary>
    /// Alignment of items along the cross axis.
    /// </summary>
    public FlexAlignItems AlignItems { get; set; } = FlexAlignItems.Stretch;

    /// <summary>
    /// Alignment of lines within the container along the cross axis.
    /// </summary>
    public FlexAlignContent AlignContent { get; set; } = FlexAlignContent.Stretch;

    /// <summary>
    /// Available size along the main axis.
    /// </summary>
    public float AvailableMainSize { get; set; }

    /// <summary>
    /// Available size along the cross axis.
    /// </summary>
    public float AvailableCrossSize { get; set; }

    /// <summary>
    /// Actual size along the main axis.
    /// </summary>
    public float MainSize { get; set; }

    /// <summary>
    /// Actual size along the cross axis.
    /// </summary>
    public float CrossSize { get; set; }

    /// <summary>
    /// Whether the main axis is horizontal.
    /// </summary>
    public bool IsHorizontal => Direction == FlexDirection.Row || Direction == FlexDirection.RowReverse;
}