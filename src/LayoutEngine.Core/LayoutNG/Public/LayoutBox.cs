namespace LayoutEngine.Core.LayoutNG.Public;

using System;
using AngleSharp.Dom;
using LayoutEngine.Core.Layout.Internal;

/// <summary>
/// Base class for layout objects that participate in the box model.
/// </summary>
public abstract class LayoutBox : LayoutContainer, ILayoutBox
{
    protected LayoutBox(INode? node) : base(node)
    {
    }

    public override bool IsBox => true;

    public Rect ContentRect { get; set; }

    public Rect PaddingRect { get; set; }

    public Rect BorderRect { get; set; }

    public Rect MarginRect { get; set; }

    public EdgeValues Margins { get; set; }

    public EdgeValues Borders { get; set; }

    public EdgeValues Paddings { get; set; }

    public float LogicalWidth { get; set; }

    public float LogicalHeight { get; set; }

    public Point LocationOffset { get; set; }

    public virtual bool ShrinkToFit => false;

    public virtual bool WidthDependsOnContainingBlock => true;

    public override bool IsPositioned
    {
        get
        {
            if (Style == null) return false;
            var position = Style.GetPropertyValue("position");
            return position == "relative" || position == "absolute" ||
                   position == "fixed" || position == "sticky";
        }
    }

    public virtual void ComputeLogicalWidth()
    {
        if (Style == null) return;

        // This is a simplified version - full implementation would handle:
        // - Auto margins
        // - Min/max width constraints
        // - Percentage widths
        // - Shrink-to-fit sizing
        // - Writing modes

        var widthValue = Style.GetPropertyValue("width");
        if (string.IsNullOrEmpty(widthValue) || widthValue == "auto")
        {
            if (ShrinkToFit)
            {
                // Calculate intrinsic width
                LogicalWidth = CalculateIntrinsicWidth();
            }
            else
            {
                // Fill available width
                LogicalWidth = AvailableLogicalWidth();
            }
        }
        else
        {
            LogicalWidth = ParseLength(widthValue);
        }

        // Apply constraints
        var minWidth = ParseLength(Style.GetPropertyValue("min-width"));
        var maxWidth = ParseLength(Style.GetPropertyValue("max-width"));

        if (minWidth > 0)
            LogicalWidth = Math.Max(LogicalWidth, minWidth);

        if (maxWidth > 0 && maxWidth != float.MaxValue)
            LogicalWidth = Math.Min(LogicalWidth, maxWidth);
    }

    public virtual void ComputeLogicalHeight()
    {
        if (Style == null) return;

        var heightValue = Style.GetPropertyValue("height");
        if (string.IsNullOrEmpty(heightValue) || heightValue == "auto")
        {
            // Calculate height based on content
            LogicalHeight = CalculateContentHeight();
        }
        else
        {
            LogicalHeight = ParseLength(heightValue);
        }

        // Apply constraints
        var minHeight = ParseLength(Style.GetPropertyValue("min-height"));
        var maxHeight = ParseLength(Style.GetPropertyValue("max-height"));

        if (minHeight > 0)
            LogicalHeight = Math.Max(LogicalHeight, minHeight);

        if (maxHeight > 0 && maxHeight != float.MaxValue)
            LogicalHeight = Math.Min(LogicalHeight, maxHeight);
    }

    public virtual void UpdateLocation()
    {
        // Update the position of this box relative to its containing block
        // This handles static, relative, absolute, and fixed positioning

        if (Parent is ILayoutBox parentBox)
        {
            var x = parentBox.ContentRect.X + LocationOffset.X;
            var y = parentBox.ContentRect.Y + LocationOffset.Y;

            // Apply relative positioning offset if needed
            if (IsPositioned && Style != null)
            {
                var position = Style.GetPropertyValue("position");
                if (position == "relative")
                {
                    var left = ParseLength(Style.GetPropertyValue("left"));
                    var top = ParseLength(Style.GetPropertyValue("top"));

                    if (!float.IsNaN(left))
                        x += left;
                    if (!float.IsNaN(top))
                        y += top;
                }
            }

            // Update all box rectangles
            ContentRect = new Rect(
                x + Margins.Left + Borders.Left + Paddings.Left,
                y + Margins.Top + Borders.Top + Paddings.Top,
                LogicalWidth,
                LogicalHeight
            );

            PaddingRect = new Rect(
                x + Margins.Left + Borders.Left,
                y + Margins.Top + Borders.Top,
                LogicalWidth + Paddings.Left + Paddings.Right,
                LogicalHeight + Paddings.Top + Paddings.Bottom
            );

            BorderRect = new Rect(
                x + Margins.Left,
                y + Margins.Top,
                LogicalWidth + Paddings.Left + Paddings.Right + Borders.Left + Borders.Right,
                LogicalHeight + Paddings.Top + Paddings.Bottom + Borders.Top + Borders.Bottom
            );

            MarginRect = new Rect(
                x,
                y,
                LogicalWidth + Paddings.Left + Paddings.Right + Borders.Left + Borders.Right + Margins.Left + Margins.Right,
                LogicalHeight + Paddings.Top + Paddings.Bottom + Borders.Top + Borders.Bottom + Margins.Top + Margins.Bottom
            );
        }
    }

    // Helper methods

    protected virtual float AvailableLogicalWidth()
    {
        if (Parent is ILayoutBox parentBox)
        {
            return parentBox.ContentRect.Width - Margins.Left - Margins.Right;
        }
        return 0;
    }

    protected virtual float CalculateIntrinsicWidth()
    {
        // Override in derived classes to calculate intrinsic width
        return 0;
    }

    protected virtual float CalculateContentHeight()
    {
        // Override in derived classes to calculate content-based height
        return 0;
    }

    protected float ParseLength(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (value == "auto")
            return float.NaN;

        // Simple px parsing - full implementation would handle %, em, rem, etc.
        if (value.EndsWith("px"))
        {
            if (float.TryParse(value.AsSpan(0, value.Length - 2), out var result))
                return result;
        }

        if (float.TryParse(value, out var number))
            return number;

        return 0;
    }
}