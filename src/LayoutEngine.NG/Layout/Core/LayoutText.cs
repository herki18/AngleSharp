namespace LayoutEngine.NG.Layout.Core;

/// <summary>
/// Represents a text node in the layout tree.
/// In LayoutNG, text nodes are leaf nodes that contain actual text content.
/// </summary>
public class LayoutText : LayoutObject
{
    private string? _text;
    private bool _isWhitespaceOnly;
    private bool _hasTab;
    private bool _hasBreakableChar;
    private bool _hasHangableChar;

    /// <summary>
    /// The text content of this layout object.
    /// In LayoutNG, this is the transformed text after CSS text-transform, etc.
    /// </summary>
    public string? Text
    {
        get => _text;
        set
        {
            _text = value;
            // In LayoutNG, text metrics are cached and updated here
            UpdateTextMetrics();
        }
    }

    /// <summary>
    /// Whether this text consists only of whitespace.
    /// In LayoutNG, whitespace-only text may be collapsed or removed.
    /// </summary>
    public bool IsWhitespaceOnly => _isWhitespaceOnly;

    /// <summary>
    /// Whether this text contains tab characters.
    /// In LayoutNG, tabs require special handling in layout.
    /// </summary>
    public bool HasTab => _hasTab;

    /// <summary>
    /// Whether the text can be broken for line wrapping.
    /// In LayoutNG, used by the line breaking algorithm.
    /// </summary>
    public bool HasBreakableCharacter => _hasBreakableChar;

    /// <summary>
    /// Whether the text contains hangable characters (like trailing spaces).
    /// In LayoutNG, hangable characters affect line box width calculations.
    /// </summary>
    public bool HasHangableCharacter => _hasHangableChar;

    /// <summary>
    /// The minimum width required for this text.
    /// In LayoutNG, used for intrinsic sizing calculations.
    /// </summary>
    public float MinIntrinsicWidth { get; set; }

    /// <summary>
    /// The maximum width this text would take if not wrapped.
    /// In LayoutNG, used for intrinsic sizing calculations.
    /// </summary>
    public float MaxIntrinsicWidth { get; set; }

    /// <summary>
    /// Cached text shaping results.
    /// In LayoutNG, text shaping is performed once and cached.
    /// Note: In real LayoutNG, this would be ShapeResult objects.
    /// </summary>
    public object? ShapeResult { get; set; }

    /// <summary>
    /// Whether this text node is generated content.
    /// In LayoutNG, generated content (::before, ::after) has special handling.
    /// </summary>
    public bool IsGeneratedContent { get; set; }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Text;
    }

    /// <summary>
    /// Text nodes never establish formatting contexts.
    /// </summary>
    public override bool EstablishesFormattingContext()
    {
        return false;
    }

    /// <summary>
    /// Updates cached text metrics.
    /// In LayoutNG, this would calculate widths, check for special characters, etc.
    /// </summary>
    private void UpdateTextMetrics()
    {
        if (string.IsNullOrEmpty(_text))
        {
            _isWhitespaceOnly = true;
            _hasTab = false;
            _hasBreakableChar = false;
            _hasHangableChar = false;
            MinIntrinsicWidth = 0;
            MaxIntrinsicWidth = 0;
            return;
        }

        // Simplified metrics calculation
        // In real LayoutNG, this would use platform text APIs
        _isWhitespaceOnly = string.IsNullOrWhiteSpace(_text);
        _hasTab = _text.Contains('\t');
        _hasBreakableChar = _text.Contains(' ') || _text.Contains('-');
        _hasHangableChar = _text.EndsWith(" ") || _text.EndsWith("\t");

        // Simplified width calculation
        // In real LayoutNG, this would use font metrics and text shaping
        MinIntrinsicWidth = _text.Length * 8; // Simplified
        MaxIntrinsicWidth = _text.Length * 10; // Simplified
    }

    /// <summary>
    /// Gets the text content for layout after whitespace processing.
    /// In LayoutNG, this handles white-space CSS property.
    /// </summary>
    public string GetTextForLayout()
    {
        if (string.IsNullOrEmpty(_text))
            return string.Empty;

        // Simplified - in real LayoutNG this would handle white-space property
        return _text;
    }
}