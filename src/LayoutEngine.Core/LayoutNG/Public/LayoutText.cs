namespace LayoutEngine.Core.LayoutNG.Public;

using System;
using AngleSharp.Dom;
using Style.Public;

/// <summary>
/// Layout object for text nodes.
/// </summary>
public class LayoutText : LayoutObject
{
    private string _text;

    public LayoutText(IText textNode) : base(textNode)
    {
        _text = textNode.TextContent ?? string.Empty;
    }

    /// <summary>
    /// Creates a layout text object with explicit text content (for generated content).
    /// </summary>
    public LayoutText(string text) : base(null)
    {
        _text = text ?? string.Empty;
    }

    public override LayoutObjectType Type => LayoutObjectType.Text;

    public override bool IsText => true;

    public override bool IsInline => true;

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                SetNeedsLayout();
            }
        }
    }

    /// <summary>
    /// Gets the text node if this layout object is associated with one.
    /// </summary>
    public IText? TextNode => Node as IText;

    /// <summary>
    /// Checks if this text node contains only whitespace.
    /// </summary>
    public bool IsWhitespace => string.IsNullOrWhiteSpace(_text);

    /// <summary>
    /// Gets the trimmed text content.
    /// </summary>
    public string TrimmedText => _text.Trim();

    /// <summary>
    /// Gets the length of the text.
    /// </summary>
    public int Length => _text.Length;

    /// <summary>
    /// Gets whether this text should be collapsed according to CSS white-space rules.
    /// </summary>
    public bool ShouldCollapseWhitespace
    {
        get
        {
            if (Parent?.Style == null) return true;

            var whiteSpace = Parent.Style.GetPropertyValue("white-space");
            return whiteSpace != "pre" && whiteSpace != "pre-wrap" && whiteSpace != "pre-line";
        }
    }

    /// <summary>
    /// Gets the processed text according to CSS white-space rules.
    /// </summary>
    public string ProcessedText
    {
        get
        {
            if (!ShouldCollapseWhitespace)
                return _text;

            // Collapse whitespace
            return System.Text.RegularExpressions.Regex.Replace(_text, @"\s+", " ");
        }
    }

    public override void UpdateStyle(IComputedStyle? oldStyle, IComputedStyle newStyle)
    {
        // Text nodes inherit style from their parent
        // They don't have their own style
        base.UpdateStyle(oldStyle, newStyle);
    }

    /// <summary>
    /// Measures the text dimensions using the parent's style.
    /// </summary>
    public (float width, float height) MeasureText()
    {
        if (Parent?.Style == null)
            return (0, 0);

        // This is a simplified version
        // Real implementation would use font metrics
        var fontSize = ParseFontSize(Parent.Style.GetPropertyValue("font-size"));
        var processedText = ProcessedText;

        // Simple approximation
        float charWidth = fontSize * 0.6f;
        float lineHeight = fontSize * 1.2f;

        return (processedText.Length * charWidth, lineHeight);
    }

    private float ParseFontSize(string? fontSize)
    {
        if (string.IsNullOrEmpty(fontSize))
            return 16; // Default

        if (fontSize.EndsWith("px"))
        {
            if (float.TryParse(fontSize.AsSpan(0, fontSize.Length - 2), out var size))
                return size;
        }

        return 16; // Default
    }
}