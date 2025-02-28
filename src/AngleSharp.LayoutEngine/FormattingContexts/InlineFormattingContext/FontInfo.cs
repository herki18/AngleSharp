namespace AngleSharp.LayoutEngine.FormattingContexts.InlineFormattingContext;

#pragma warning disable CS8625, CS8618
/// <summary>
/// Contains font information for text measurement.
/// </summary>
public class FontInfo
{
    /// <summary>
    /// Font size in pixels.
    /// </summary>
    public float Size { get; set; }

    /// <summary>
    /// Line height in pixels.
    /// </summary>
    public float LineHeight { get; set; }

    /// <summary>
    /// Font family.
    /// </summary>
    public string Family { get; set; }
}