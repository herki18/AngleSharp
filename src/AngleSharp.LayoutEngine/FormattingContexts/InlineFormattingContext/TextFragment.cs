namespace AngleSharp.LayoutEngine.FormattingContexts.InlineFormattingContext;

using Core;

#pragma warning disable CS8625, CS8618
/// <summary>
/// Represents a text fragment or atomic inline element in a line box.
/// </summary>
public class TextFragment
{
    /// <summary>
    /// The layout node this fragment belongs to.
    /// </summary>
    public LayoutNode Node { get; set; }

    /// <summary>
    /// The text content of this fragment.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// X position of the fragment.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y position of the fragment.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Width of the fragment.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Height of the fragment.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Whether this fragment represents an atomic inline element.
    /// </summary>
    public bool IsAtomicInline { get; set; }

    /// <summary>
    /// Whether this fragment is a whitespace character.
    /// </summary>
    public bool IsWhitespace { get; set; }
}