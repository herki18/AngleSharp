namespace AngleSharp.LayoutEngine.FormattingContexts.InlineFormattingContext;

using System.Collections.Generic;

#pragma warning disable CS8625, CS8618
/// <summary>
/// Represents a line box in an inline formatting context.
/// </summary>
public class LineBox
{
    /// <summary>
    /// Y position of the line box.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Height of the line box.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Width of the line box.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Position of the baseline.
    /// </summary>
    public float Baseline { get; set; }

    /// <summary>
    /// Text fragments contained in this line.
    /// </summary>
    public List<TextFragment> Fragments { get; } = new List<TextFragment>();
}