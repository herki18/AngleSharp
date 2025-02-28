#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implements an inline formatting context according to the CSS specification.
/// An inline formatting context is established by a block container that contains inline-level content.
/// </summary>
public class InlineFormattingContext : FormattingContext
{
    private readonly List<LineBox> _lineBoxes = new List<LineBox>();

    // State for tracking incremental updates
    private class LayoutState
    {
        public int LineCount { get; set; }
        public float TotalHeight { get; set; }
        public Dictionary<LayoutNode, LineBoxSpan> NodeSpans { get; } = new Dictionary<LayoutNode, LineBoxSpan>();
    }

    private LayoutState _lastState = new LayoutState();

    public InlineFormattingContext(LayoutNode establishingNode)
        : base(establishingNode)
    {
        // Collect participants
        CollectInlineParticipants(establishingNode);
    }

    /// <summary>
    /// Performs a full layout of all inline elements in this formatting context.
    /// </summary>
    public override void Layout(LayoutContext context)
    {
        // Clear previous state
        _lineBoxes.Clear();
        _lastState = new LayoutState();

        // 1. Create text fragments from inline content
        var fragments = CreateTextFragments(context);

        // 2. Break fragments into lines
        PerformLineBreaking(fragments, context);

        // 3. Position line boxes and their contents
        PositionLineBoxes(context);

        // 4. Update the establishing element's height
        UpdateEstablishingBoxHeight();

        // 5. Save state for incremental updates
        SaveLayoutState();
    }

    /// <summary>
    /// Performs an incremental update of the layout.
    /// </summary>
    public override void Reflow(LayoutContext context)
    {
        // Find changed nodes
        var dirtyNodes = _participants.Where(p => p.IsDirty).ToList();

        if (!dirtyNodes.Any())
            return; // Nothing to do

        // For inline formatting, it's usually simpler to perform a full layout
        // because a small change can affect line breaking throughout the context
        Layout(context);
    }

    /// <summary>
    /// Collects all inline elements and text nodes that participate in this formatting context.
    /// </summary>
    private void CollectInlineParticipants(LayoutNode node)
    {
        // For the establishing node, we add it but then also process its children
        if (node == EstablishingNode)
        {
            _participants.Add(node);
            foreach (var child in node.Children)
            {
                // Skip non-renderable children
                if (child.DomNode is NonRenderableNode)
                    continue;

                // Process inline children and inline-block children (differently)
                if (child.DomNode is ElementNode element)
                {
                    var display = element.ComputedStyle?.GetPropertyValue("display") ?? "inline";

                    if (display == "inline-block" || display == "inline-flex" || display == "inline-grid")
                    {
                        // Inline-block creates its own formatting context, but we still add it as a participant
                        _participants.Add(child);
                    }
                    else if (display.StartsWith("inline"))
                    {
                        // Regular inline element
                        _participants.Add(child);
                        CollectInlineParticipants(child);
                    }
                    else if (display == "block")
                    {
                        // Block elements should have their own formatting contexts
                        // and are positioned in the block flow
                    }
                    else
                    {
                        // Other display types
                    }
                }
                else if (child.DomNode is TextNode)
                {
                    // Text nodes participate directly
                    _participants.Add(child);
                }
            }
        }
        else
        {
            // For non-establishing nodes, only process children if the node is inline
            var element = node.DomNode as ElementNode;
            var display = element?.ComputedStyle?.GetPropertyValue("display") ?? "inline";

            if (display.StartsWith("inline") && display != "inline-block" &&
                display != "inline-flex" && display != "inline-grid")
            {
                _participants.Add(node);
                foreach (var child in node.Children)
                {
                    // Skip non-renderable children
                    if (child.DomNode is NonRenderableNode)
                        continue;

                    // Check if this child creates its own formatting context
                    if (child.CreatesFormattingContext)
                    {
                        _participants.Add(child);
                    }
                    else
                    {
                        CollectInlineParticipants(child);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates text fragments from the inline content.
    /// </summary>
    private List<TextFragment> CreateTextFragments(LayoutContext context)
    {
        var fragments = new List<TextFragment>();

        // Process each participant
        foreach (var participant in _participants)
        {
            if (participant == EstablishingNode)
                continue;

            if (participant.DomNode is TextNode textNode)
            {
                // Create fragments from text content
                CreateTextFragmentsFromTextNode(participant, fragments, context);
            }
            else if (participant.DomNode is ElementNode elementNode)
            {
                var display = elementNode.ComputedStyle?.GetPropertyValue("display") ?? "inline";

                if (display == "inline-block" || display == "inline-flex" || display == "inline-grid")
                {
                    // Handle inline-block as atomic inline-level element
                    // Measure its intrinsic size
                    MeasureAtomicInline(participant, context);

                    // Create a fragment for the inline-block
                    fragments.Add(new TextFragment
                    {
                        Node = participant,
                        Text = null, // No text for inline-block
                        Width = participant.Box.BorderBoxWidth,
                        Height = participant.Box.BorderBoxHeight,
                        IsAtomicInline = true
                    });
                }
                else if (display.StartsWith("inline"))
                {
                    // Regular inline elements affect styling but don't create fragments
                    // Text nodes within them create the fragments
                }
            }
        }

        return fragments;
    }

    /// <summary>
    /// Creates text fragments from a text node.
    /// </summary>
    private void CreateTextFragmentsFromTextNode(LayoutNode textNode, List<TextFragment> fragments, LayoutContext context)
    {
        var text = (textNode.DomNode as TextNode)?.Ref.TextContent ?? string.Empty;

        // Skip empty text
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Find the parent inline element for style information
        var parentInlineElement = GetParentInlineElement(textNode);
        var font = GetFontInfoForNode(parentInlineElement, context);

        // In a real implementation, we'd need to split text based on wrapping rules
        // We'll simplify by assuming each word is a fragment

        // Split into words
        var words = text.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            // Calculate the width of this word using the font metrics
            float wordWidth = MeasureTextWidth(word, font);

            fragments.Add(new TextFragment
            {
                Node = textNode,
                Text = word,
                Width = wordWidth,
                Height = font.LineHeight,
                IsAtomicInline = false
            });

            // Add a space fragment if this isn't the last word
            if (word != words.Last())
            {
                float spaceWidth = MeasureTextWidth(" ", font);

                fragments.Add(new TextFragment
                {
                    Node = textNode,
                    Text = " ",
                    Width = spaceWidth,
                    Height = font.LineHeight,
                    IsAtomicInline = false,
                    IsWhitespace = true
                });
            }
        }
    }

    /// <summary>
    /// Finds the nearest parent inline element that can provide style information.
    /// </summary>
    private LayoutNode GetParentInlineElement(LayoutNode node)
    {
        var current = node.Parent;
        while (current != null && current != EstablishingNode)
        {
            var element = current.DomNode as ElementNode;
            if (element?.ComputedStyle != null)
            {
                var display = element.ComputedStyle.GetPropertyValue("display") ?? "inline";
                if (display.StartsWith("inline"))
                    return current;
            }

            current = current.Parent;
        }

        // If no inline parent found, use the establishing node
        return EstablishingNode;
    }

    /// <summary>
    /// Gets font information for a node.
    /// </summary>
    private FontInfo GetFontInfoForNode(LayoutNode node, LayoutContext context)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
        {
            // Use default font info
            return new FontInfo
            {
                Size = context.DefaultFontSize,
                LineHeight = 1.2f * context.DefaultFontSize, // Default line height multiplier
                Family = "sans-serif"
            };
        }

        // Extract font properties
        string fontSizeStr = element.ComputedStyle.GetPropertyValue("font-size") ?? "16px";
        string lineHeightStr = element.ComputedStyle.GetPropertyValue("line-height") ?? "normal";
        string fontFamily = element.ComputedStyle.GetPropertyValue("font-family") ?? "sans-serif";

        // Parse font size
        float fontSize = context.DefaultFontSize;
        if (fontSizeStr.EndsWith("px") && float.TryParse(fontSizeStr.TrimEnd('p', 'x'), out float px))
        {
            fontSize = px;
        }

        // Parse line height
        float lineHeight = 1.2f * fontSize; // Default
        if (lineHeightStr != "normal")
        {
            if (lineHeightStr.EndsWith("px") && float.TryParse(lineHeightStr.TrimEnd('p', 'x'), out float lhPx))
            {
                lineHeight = lhPx;
            }
            else if (float.TryParse(lineHeightStr, out float lhNum))
            {
                lineHeight = lhNum * fontSize;
            }
        }

        return new FontInfo
        {
            Size = fontSize,
            LineHeight = lineHeight,
            Family = fontFamily
        };
    }

    /// <summary>
    /// Measures the width of text using the specified font.
    /// </summary>
    private float MeasureTextWidth(string text, FontInfo font)
    {
        // In a real implementation, this would use font metrics
        // For this example, we'll use a simple approximation
        return text.Length * font.Size * 0.6f;
    }

    /// <summary>
    /// Measures an atomic inline element like an inline-block.
    /// </summary>
    private void MeasureAtomicInline(LayoutNode node, LayoutContext context)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        // Calculate the available width (parent's content width)
        float availableWidth = EstablishingNode.Box.Width;

        // Use BoxModelCalculator to compute dimensions
        var calculator = new BoxModelCalculator(element.ComputedStyle, availableWidth);
        var boxValues = calculator.GetBoxValues();

        // Update the layout box
        node.Box.UpdateFromBoxValues(boxValues);

        // If this creates its own formatting context, lay it out
        if (node.FormattingContext != null)
        {
            var childContext = context.CreateChildContext(node);
            node.FormattingContext.Layout(childContext);
        }
    }

    /// <summary>
    /// Performs line breaking to create line boxes from text fragments.
    /// </summary>
    private void PerformLineBreaking(List<TextFragment> fragments, LayoutContext context)
    {
        float availableWidth = EstablishingNode.Box.Width;
        float x = 0;
        LineBox currentLine = new LineBox();

        foreach (var fragment in fragments)
        {
            // Check if this fragment fits on the current line
            if (x + fragment.Width > availableWidth && !fragment.IsWhitespace)
            {
                // This fragment doesn't fit - finish current line and start a new one
                if (currentLine.Fragments.Any())
                {
                    // Calculate final metrics for this line
                    CalculateLineBoxMetrics(currentLine);
                    _lineBoxes.Add(currentLine);

                    // Start a new line
                    currentLine = new LineBox();
                    x = 0;
                }
            }

            // Skip whitespace at the beginning of a line
            if (x == 0 && fragment.IsWhitespace)
                continue;

            // Add fragment to current line
            fragment.X = x;
            currentLine.Fragments.Add(fragment);

            // Move to next position
            x += fragment.Width;
        }

        // Add the last line if it has content
        if (currentLine.Fragments.Any())
        {
            CalculateLineBoxMetrics(currentLine);
            _lineBoxes.Add(currentLine);
        }
    }

    /// <summary>
    /// Calculates final metrics for a line box.
    /// </summary>
    private void CalculateLineBoxMetrics(LineBox line)
    {
        // Calculate line height based on contained content
        float maxHeight = 0;
        float maxBaseline = 0;

        foreach (var fragment in line.Fragments)
        {
            if (fragment.Height > maxHeight)
            {
                maxHeight = fragment.Height;
            }

            // In a real implementation, we'd also track baselines
            // For simplicity, we'll assume baseline is at 80% of height
            float baseline = fragment.Height * 0.8f;
            if (baseline > maxBaseline)
            {
                maxBaseline = baseline;
            }
        }

        line.Height = maxHeight;
        line.Baseline = maxBaseline;

        // Calculate line width
        if (line.Fragments.Any())
        {
            var lastFragment = line.Fragments.Last();
            line.Width = lastFragment.X + lastFragment.Width;
        }
        else
        {
            line.Width = 0;
        }
    }

    /// <summary>
    /// Positions all line boxes and their contents.
    /// </summary>
    private void PositionLineBoxes(LayoutContext context)
    {
        float y = EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop;

        // Get text alignment
        string textAlign = (EstablishingNode.DomNode as ElementNode)?.ComputedStyle?.GetPropertyValue("text-align") ?? "left";

        foreach (var line in _lineBoxes)
        {
            // Position the line box
            line.Y = y;

            // Align fragments within the line
            AlignFragmentsInLine(line, textAlign);

            // Update fragment Y positions based on the line's baseline
            foreach (var fragment in line.Fragments)
            {
                // By default, position at the baseline
                float fragmentY = line.Y + line.Baseline - (fragment.Height * 0.8f);

                // Apply vertical-align if specified
                if (fragment.Node.DomNode is ElementNode element)
                {
                    string verticalAlign = element.ComputedStyle?.GetPropertyValue("vertical-align") ?? "baseline";

                    switch (verticalAlign)
                    {
                        case "top":
                            fragmentY = line.Y;
                            break;
                        case "middle":
                            fragmentY = line.Y + (line.Height - fragment.Height) / 2;
                            break;
                        case "bottom":
                            fragmentY = line.Y + line.Height - fragment.Height;
                            break;
                        // Other alignment types would be handled similarly
                    }
                }

                fragment.Y = fragmentY;

                // Update the node's layout box
                UpdateFragmentNodeBox(fragment);
            }

            // Move to the next line
            y += line.Height;
        }
    }

    /// <summary>
    /// Aligns fragments within a line box according to text-align.
    /// </summary>
    private void AlignFragmentsInLine(LineBox line, string textAlign)
    {
        if (textAlign == "left" || textAlign == "start" || string.IsNullOrEmpty(textAlign))
        {
            // Left alignment is the default
            return;
        }

        float availableWidth = EstablishingNode.Box.Width;
        float lineWidth = line.Width;
        float offset = 0;

        switch (textAlign)
        {
            case "center":
                offset = (availableWidth - lineWidth) / 2;
                break;
            case "right":
            case "end":
                offset = availableWidth - lineWidth;
                break;
            case "justify":
                // Only justify if this isn't the last line or if it fills most of the width
                if (_lineBoxes.Last() != line || lineWidth > availableWidth * 0.8f)
                {
                    // Count spaces that can be adjusted
                    int spaceCount = line.Fragments.Count(f => f.IsWhitespace);
                    if (spaceCount > 0)
                    {
                        float extraSpacePerWord = (availableWidth - lineWidth) / spaceCount;

                        // Adjust positions
                        float currentOffset = 0;
                        foreach (var fragment in line.Fragments)
                        {
                            fragment.X += currentOffset;

                            if (fragment.IsWhitespace)
                            {
                                currentOffset += extraSpacePerWord;
                            }
                        }
                    }
                }
                break;
        }

        // Apply offset to all fragments if not justify
        if (offset > 0)
        {
            foreach (var fragment in line.Fragments)
            {
                fragment.X += offset;
            }
        }
    }

    /// <summary>
    /// Updates a node's layout box based on fragment position.
    /// </summary>
    private void UpdateFragmentNodeBox(TextFragment fragment)
    {
        if (fragment.IsAtomicInline)
        {
            // For atomic inline elements, update position
            fragment.Node.Box.X = fragment.X;
            fragment.Node.Box.Y = fragment.Y;
        }
        else
        {
            // For text fragments, we need to track the spans in line boxes
            var node = fragment.Node;
            if (!_lastState.NodeSpans.TryGetValue(node, out var span))
            {
                span = new LineBoxSpan
                {
                    StartX = fragment.X,
                    StartY = fragment.Y,
                    EndX = fragment.X + fragment.Width,
                    EndY = fragment.Y + fragment.Height
                };
                _lastState.NodeSpans[node] = span;
            }
            else
            {
                // Expand the span
                span.StartX = Math.Min(span.StartX, fragment.X);
                span.StartY = Math.Min(span.StartY, fragment.Y);
                span.EndX = Math.Max(span.EndX, fragment.X + fragment.Width);
                span.EndY = Math.Max(span.EndY, fragment.Y + fragment.Height);
            }

            // Update the node's box
            node.Box.X = span.StartX;
            node.Box.Y = span.StartY;
            node.Box.Width = span.EndX - span.StartX;
            node.Box.Height = span.EndY - span.StartY;
        }
    }

    /// <summary>
    /// Updates the height of the establishing element to contain all line boxes.
    /// </summary>
    private void UpdateEstablishingBoxHeight()
    {
        if (!_lineBoxes.Any())
        {
            // No lines, use default height
            EstablishingNode.Box.Height = 0;
            return;
        }

        var lastLine = _lineBoxes.Last();
        float totalHeight = lastLine.Y + lastLine.Height - (EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop);

        // Do not shrink an explicit height, only expand it if needed
        var element = EstablishingNode.DomNode as ElementNode;
        if (element?.ComputedStyle == null ||
            element.ComputedStyle.GetPropertyValue("height") == "auto" ||
            float.IsNaN(EstablishingNode.Box.Height))
        {
            EstablishingNode.Box.Height = totalHeight;
        }
        else if (totalHeight > EstablishingNode.Box.Height)
        {
            // Expand height if content overflows an explicit height
            EstablishingNode.Box.Height = totalHeight;
        }
    }

    /// <summary>
    /// Saves layout state for incremental updates.
    /// </summary>
    private void SaveLayoutState()
    {
        _lastState.LineCount = _lineBoxes.Count;
        _lastState.TotalHeight = _lineBoxes.Any() ?
            _lineBoxes.Last().Y + _lineBoxes.Last().Height - _lineBoxes.First().Y : 0;
    }
}

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