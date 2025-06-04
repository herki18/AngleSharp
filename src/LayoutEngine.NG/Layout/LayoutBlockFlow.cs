namespace LayoutEngine.NG.Layout;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using Style;
using AngleSharp.Css.Values;

/// <summary>
/// Represents a block flow layout object.
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
               (Style.Position == PositionMode.Absolute || Style.Position == PositionMode.Fixed) ||
               Style.Display == DisplayMode.InlineBlock ||
               Style.Display == DisplayMode.FlowRoot;
    }

    /// <summary>
    /// Performs block layout algorithm using PhysicalFragment.
    /// </summary>
    public void LayoutBlock(LayoutConstraintSpace constraintSpace)
    {
        // Apply box model from computed style
        ApplyBoxModelFromStyle();

        // Calculate content width
        float contentWidth = CalculateContentWidth(constraintSpace);
        float currentY = Padding.Top;

        var childFragments = new List<PhysicalFragment>();

        // Layout children
        var child = FirstChild;
        while (child != null)
        {
            if (child is LayoutBox childBox)
            {
                // Layout child box
                childBox.Layout();

                // Position child
                var childOffset = new PhysicalOffset(
                    Padding.Left + childBox.Margin.Left,
                    currentY + childBox.Margin.Top
                );

                // Create child fragment using builder
                var childFragment = PhysicalFragment.CreateBuilder()
                    .SetLayoutObject(childBox)
                    .SetSize(childBox.BorderBoxSize)
                    .SetOffset(childOffset)
                    .SetMargins(new PhysicalBoxStrut(
                        childBox.Margin.Top,
                        childBox.Margin.Right,
                        childBox.Margin.Bottom,
                        childBox.Margin.Left))
                    .SetBorders(new PhysicalBoxStrut(
                        childBox.Border.Top,
                        childBox.Border.Right,
                        childBox.Border.Bottom,
                        childBox.Border.Left))
                    .SetPadding(new PhysicalBoxStrut(
                        childBox.Padding.Top,
                        childBox.Padding.Right,
                        childBox.Padding.Bottom,
                        childBox.Padding.Left))
                    .Build();

                childFragments.Add(childFragment);

                // Update position for next child
                currentY += childBox.Margin.Top + childBox.BorderBoxSize.Height + childBox.Margin.Bottom;
                childBox.NeedsLayout = false;
            }
            else if (child is LayoutText textChild)
            {
                // Layout text child
                var textFragment = LayoutTextChild(textChild, contentWidth, currentY);
                if (textFragment != null)
                {
                    childFragments.Add(textFragment);
                    currentY += textFragment.Size.Height;
                }
            }

            child = child.NextSibling;
        }

        // Set content height based on children
        float contentHeight = currentY + Padding.Bottom;
        ContentSize = new PhysicalSize(contentWidth, contentHeight);

        // Update baseline
        UpdateBaseline();

        // Create physical fragment for this block using builder
        PhysicalFragment = PhysicalFragment.CreateBuilder()
            .SetLayoutObject(this)
            .SetSize(new PhysicalSize(
                contentWidth + Padding.Left + Padding.Right + Border.Left + Border.Right,
                contentHeight + Border.Top + Border.Bottom))
            .SetOffset(PhysicalOffset.Zero)
            .SetMargins(new PhysicalBoxStrut(Margin.Top, Margin.Right, Margin.Bottom, Margin.Left))
            .SetBorders(new PhysicalBoxStrut(Border.Top, Border.Right, Border.Bottom, Border.Left))
            .SetPadding(new PhysicalBoxStrut(Padding.Top, Padding.Right, Padding.Bottom, Padding.Left))
            .AddChildren(childFragments)
            .SetBaseline(BaselinePosition)
            .Build();
    }

    /// <summary>
    /// Applies box model properties from computed style.
    /// In LayoutNG, this converts CSS values to physical pixel values.
    /// </summary>
    private void ApplyBoxModelFromStyle()
    {
        if (Style == null)
            return;

        // Convert margin values from CssLengthValue to pixels
        Margin = ConvertLengthBoxToPixels(Style.Margin,
            0); // TODO: Pass containing block size for percentage resolution

        // Convert padding values
        Padding = ConvertLengthBoxToPixels(Style.Padding, 0);

        // Convert border values
        Border = ConvertBorderBoxToPixels(Style.Border);

        // Apply overflow
        var overflowValue = Style.GetPropertyValue("overflow")?.ToString();
        if (!string.IsNullOrEmpty(overflowValue))
        {
            OverflowX = OverflowY = ParseOverflow(overflowValue);
        }

        // Handle overflow-x and overflow-y separately if set
        var overflowXValue = Style.GetPropertyValue("overflow-x")?.ToString();
        if (!string.IsNullOrEmpty(overflowXValue))
        {
            OverflowX = ParseOverflow(overflowXValue);
        }

        var overflowYValue = Style.GetPropertyValue("overflow-y")?.ToString();
        if (!string.IsNullOrEmpty(overflowYValue))
        {
            OverflowY = ParseOverflow(overflowYValue);
        }
    }

    /// <summary>
    /// Converts a LengthBox to pixel values.
    /// In LayoutNG, this is part of resolving computed values to used values.
    /// </summary>
    private BoxSpacing ConvertLengthBoxToPixels(LengthBox lengthBox, float containingBlockSize)
    {
        return new BoxSpacing
        {
            Top = ConvertLengthToPixels(lengthBox.Top, containingBlockSize),
            Right = ConvertLengthToPixels(lengthBox.Right, containingBlockSize),
            Bottom = ConvertLengthToPixels(lengthBox.Bottom, containingBlockSize),
            Left = ConvertLengthToPixels(lengthBox.Left, containingBlockSize)
        };
    }

    /// <summary>
    /// Converts a BorderBox to pixel values.
    /// </summary>
    private BoxSpacing ConvertBorderBoxToPixels(BorderBox borderBox)
    {
        // Border widths don't support percentages, so we don't need containing block size
        return new BoxSpacing
        {
            Top = ConvertLengthToPixels(borderBox.Top, 0),
            Right = ConvertLengthToPixels(borderBox.Right, 0),
            Bottom = ConvertLengthToPixels(borderBox.Bottom, 0),
            Left = ConvertLengthToPixels(borderBox.Left, 0)
        };
    }

    /// <summary>
    /// Converts a single CssLengthValue to pixels.
    /// In LayoutNG, this is the core of value resolution.
    /// </summary>
    private float ConvertLengthToPixels(CssLengthValue length, float containingBlockSize)
    {
        switch (length.Type)
        {
            case CssLengthValue.Unit.Px:
                // Pixels are already in the correct unit
                return (float)length.Value;

            case CssLengthValue.Unit.Em:
                // TODO: Resolve em units based on computed font size
                // For now, assume 1em = 16px
                return (float)(length.Value * 16);

            case CssLengthValue.Unit.Rem:
                // TODO: Resolve rem units based on root element font size
                // For now, assume 1rem = 16px
                return (float)(length.Value * 16);

            case CssLengthValue.Unit.Percent:
                // Percentages are relative to containing block
                return (float)((length.Value / 100) * containingBlockSize);

            case CssLengthValue.Unit.Vw:
                // TODO: Get actual viewport width
                // For now, assume 1vw = 10px
                return (float)(length.Value * 10);

            case CssLengthValue.Unit.Vh:
                // TODO: Get actual viewport height
                // For now, assume 1vh = 10px
                return (float)(length.Value * 10);

            default:
                // For unhandled units, return the raw value
                // TODO: Implement proper unit conversion
                return (float)length.Value;
        }
    }

    /// <summary>
    /// Calculates the content width based on constraints and style.
    /// </summary>
    private float CalculateContentWidth(LayoutConstraintSpace constraintSpace)
    {
        // Simplified width calculation
        // In real LayoutNG, this would handle auto, percentages, min/max-width, etc.
        float availableWidth = constraintSpace.AvailableWidth;

        // Check for explicit width in style
        var widthProperty = Style?.GetPropertyValue("width")?.ToString();
        if (!string.IsNullOrEmpty(widthProperty) && widthProperty != "auto")
        {
            // TODO: Parse width value properly
            // For now, use available width
        }

        // Subtract margins, borders, and padding from available width
        float contentWidth = availableWidth -
                             (Margin.Left + Margin.Right +
                              Border.Left + Border.Right +
                              Padding.Left + Padding.Right);

        return System.Math.Max(0, contentWidth);
    }

    /// <summary>
    /// Layouts a text child and returns a PhysicalFragment.
    /// </summary>
    private PhysicalFragment? LayoutTextChild(LayoutText text, float availableWidth, float currentY)
    {
        if (string.IsNullOrWhiteSpace(text.Text))
            return null;

        // Simplified text layout
        float lineHeight = 20; // Default line height
        int estimatedCharsPerLine = (int)(availableWidth / 8); // Rough estimate
        int lines = Math.Max(1, (text.Text.Length + estimatedCharsPerLine - 1) / estimatedCharsPerLine);
        float textHeight = lines * lineHeight;

        // Create physical fragment for the text using builder
        return PhysicalFragment.CreateBuilder()
            .SetLayoutObject(text)
            .SetOffset(new PhysicalOffset(Padding.Left, currentY))
            .SetSize(new PhysicalSize(availableWidth, textHeight))
            .SetBaseline(lineHeight * 0.8f) // Simplified baseline
            .Build();
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