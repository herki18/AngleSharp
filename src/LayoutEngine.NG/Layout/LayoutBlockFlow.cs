namespace LayoutEngine.NG.Layout;

using Style;

/// <summary>
/// Represents a block flow layout object.
/// In LayoutNG, this handles normal block flow layout (not flex, grid, or table).
/// This is the most common type of block layout and inherits from LayoutBlock.
/// </summary>
public class LayoutBlockFlow : LayoutBlock
{
    /// <summary>
    /// The intrinsic size of this block flow.
    /// In LayoutNG, this is used for sizing calculations.
    /// </summary>
    public float IntrinsicBlockSize { get; set; }

    /// <summary>
    /// Whether this block flow contains any inline content.
    /// In LayoutNG, this determines if we need inline layout.
    /// </summary>
    public bool HasInlineContent { get; set; }

    /// <summary>
    /// The baseline position for this block.
    /// In LayoutNG, used for vertical alignment.
    /// </summary>
    public float Baseline { get; set; }

    /// <summary>
    /// Whether this block flow is a root element.
    /// In LayoutNG, the root has special layout rules.
    /// </summary>
    public bool IsDocumentElement { get; set; }

    /// <summary>
    /// Cached line boxes for inline content.
    /// In LayoutNG, line boxes are created during inline layout.
    /// Note: In real LayoutNG, this would be NGLineBoxFragment objects.
    /// </summary>
    public object? LineBoxes { get; set; }

    /// <summary>
    /// Whether margins collapse through this block.
    /// In LayoutNG, margin collapsing is a complex part of block layout.
    /// </summary>
    public bool MarginsCollapseThrough { get; set; }

    /// <summary>
    /// The block's collapsed margin values.
    /// In LayoutNG, tracks margin collapsing state.
    /// </summary>
    public MarginStrut MarginStrut { get; set; }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Block;
    }

    /// <summary>
    /// Whether this block establishes a new block formatting context.
    /// In LayoutNG, this is determined by various CSS properties.
    /// </summary>
    public override bool EstablishesFormattingContext()
    {
        if (Style == null)
            return false;

        // Following BlinkNG rules for BFC creation
        // A block establishes a new BFC if it:
        // - Has overflow other than visible
        // - Is absolutely positioned
        // - Is a float
        // - Has display: flow-root (in our case, we'll check other conditions)

        return (OverflowX != Overflow.Visible || OverflowY != Overflow.Visible) ||
               (Style.Position == PositionType.Absolute || Style.Position == PositionType.Fixed) ||
               Style.Display == DisplayType.InlineBlock ||
               Style.Display == DisplayType.FlowRoot;
    }

    /// <summary>
    /// Performs block layout algorithm.
    /// In LayoutNG, this is the core of block layout.
    /// </summary>
    public void LayoutBlock(LayoutConstraintSpace constraintSpace)
    {
        // Apply box model properties from style
        ApplyBoxModelFromStyle();

        // Calculate content box width
        float contentWidth = CalculateContentWidth(constraintSpace);

        // Initialize position for children
        float currentY = Padding.Top;

        // Layout children
        var child = FirstChild;
        while (child != null)
        {
            if (child is LayoutBox childBox)
            {
                // Create constraint space for child
                var childConstraintSpace = new LayoutConstraintSpace
                {
                    AvailableWidth = contentWidth,
                    AvailableHeight = float.MaxValue, // No height constraint for now
                    IsFixedWidth = true,
                    IsFixedHeight = false,
                    IsNewFormattingContext = childBox.EstablishesFormattingContext()
                };

                // Position child
                childBox.Location = new PhysicalOffset(Padding.Left, currentY);

                // Layout child based on its type
                if (childBox is LayoutBlockFlow childBlockFlow)
                {
                    childBlockFlow.LayoutBlock(childConstraintSpace);
                }
                else if (childBox is LayoutInline childInline)
                {
                    // Inline layout - simplified
                    LayoutInlineChild(childInline, childConstraintSpace);
                }
                else
                {
                    // Generic box layout
                    childBox.ContentSize = new PhysicalSize(contentWidth, 50); // Default height
                }

                // Update position for next child
                currentY += childBox.Margin.Top;
                currentY += childBox.BorderBoxSize.Height;
                currentY += childBox.Margin.Bottom;

                // Clear child's needs layout flag
                childBox.NeedsLayout = false;

                // Create fragment for child
                childBox.Fragment = childBox.CreateFragment();
            }
            else if (child is LayoutText textChild)
            {
                // Text layout - simplified
                LayoutTextChild(textChild, contentWidth, ref currentY);
            }

            child = child.NextSibling;
        }

        // Set content height based on children
        float contentHeight = currentY + Padding.Bottom;
        ContentSize = new PhysicalSize(contentWidth, contentHeight);

        // Update baseline
        UpdateBaseline();

        // Create fragment
        Fragment = CreateFragment();
    }

    /// <summary>
    /// Applies box model properties from computed style.
    /// </summary>
    private void ApplyBoxModelFromStyle()
    {
        if (Style == null)
            return;

        // Apply margins (simplified - in real implementation would parse CSS values)
        Margin = new BoxSpacing
        {
            Top = 10,    // Default margin
            Right = 10,
            Bottom = 10,
            Left = 10
        };

        // Apply padding
        Padding = new BoxSpacing
        {
            Top = 5,     // Default padding
            Right = 5,
            Bottom = 5,
            Left = 5
        };

        // Apply borders
        Border = new BoxSpacing
        {
            Top = 1,     // Default border
            Right = 1,
            Bottom = 1,
            Left = 1
        };

        // Apply overflow
        var overflowValue = Style.GetPropertyValue("overflow")?.ToString();
        if (!string.IsNullOrEmpty(overflowValue))
        {
            OverflowX = OverflowY = ParseOverflow(overflowValue);
        }
    }

    /// <summary>
    /// Calculates the content width based on constraints and style.
    /// </summary>
    private float CalculateContentWidth(LayoutConstraintSpace constraintSpace)
    {
        // Simplified width calculation
        // In real LayoutNG, this would handle auto, percentages, etc.

        float availableWidth = constraintSpace.AvailableWidth;

        // Subtract margins, borders, and padding
        float contentWidth = availableWidth -
            (Margin.Left + Margin.Right +
             Border.Left + Border.Right +
             Padding.Left + Padding.Right);

        return System.Math.Max(0, contentWidth);
    }

    /// <summary>
    /// Layouts an inline child (simplified).
    /// </summary>
    private void LayoutInlineChild(LayoutInline inline, LayoutConstraintSpace constraintSpace)
    {
        // Simplified inline layout
        // In real LayoutNG, this would create line boxes
        inline.LineHeight = 20; // Default line height

        // For now, treat as a single line
        var inlineWidth = constraintSpace.AvailableWidth;
        var inlineHeight = inline.LineHeight;

        // Note: In real implementation, we'd measure text and create line boxes
        HasInlineContent = true;
    }

    /// <summary>
    /// Layouts a text child (simplified).
    /// </summary>
    private void LayoutTextChild(LayoutText text, float availableWidth, ref float currentY)
    {
        if (string.IsNullOrWhiteSpace(text.Text))
            return;

        // Simplified text layout
        // In real LayoutNG, this would use text shaping and line breaking

        float lineHeight = 20; // Default line height
        int estimatedCharsPerLine = (int)(availableWidth / 8); // Rough estimate
        int lines = (text.Text.Length + estimatedCharsPerLine - 1) / estimatedCharsPerLine;

        float textHeight = lines * lineHeight;

        // Create a fragment for the text
        text.Fragment = new Fragment
        {
            LayoutObject = text,
            Offset = new PhysicalOffset(Padding.Left, currentY),
            Size = new PhysicalSize(availableWidth, textHeight)
        };

        currentY += textHeight;
        HasInlineContent = true;
    }

    /// <summary>
    /// Updates the baseline position.
    /// </summary>
    private void UpdateBaseline()
    {
        // Simplified baseline calculation
        // In real LayoutNG, this would consider first line box, etc.

        if (HasInlineContent)
        {
            Baseline = Padding.Top + 20; // Assume first line baseline
        }
        else if (FirstChild != null)
        {
            // Use first child's baseline
            Baseline = Padding.Top;
        }
        else
        {
            // Empty block - baseline at bottom
            Baseline = ContentSize.Height;
        }
    }

    /// <summary>
    /// Parses CSS overflow value.
    /// </summary>
    private Overflow ParseOverflow(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "visible" => Overflow.Visible,
            "hidden" => Overflow.Hidden,
            "scroll" => Overflow.Scroll,
            "auto" => Overflow.Auto,
            "clip" => Overflow.Clip,
            _ => Overflow.Visible
        };
    }
}

/// <summary>
/// Represents margin collapsing state in LayoutNG.
/// </summary>
public struct MarginStrut
{
    /// <summary>
    /// The positive margin component.
    /// </summary>
    public float PositiveMargin { get; set; }

    /// <summary>
    /// The negative margin component.
    /// </summary>
    public float NegativeMargin { get; set; }

    /// <summary>
    /// Gets the sum of margins after collapsing.
    /// In LayoutNG, this follows CSS margin collapsing rules.
    /// </summary>
    public float Sum => PositiveMargin + NegativeMargin;
}