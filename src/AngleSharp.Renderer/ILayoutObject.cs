namespace AngleSharp.Renderer;

using System;

/// <summary>
/// Each layout object encapsulates the logic for measuring and arranging an IRenderNode.
/// You can have a BlockLayoutObject, InlineLayoutObject, etc.
/// </summary>
public interface ILayoutObject
{
    /// <summary>
    /// The node being laid out (ElementNode, TextNode, etc.).
    /// </summary>
    IRenderNode Node { get; }

    /// <summary>
    /// First pass: figure out the node's *intrinsic* or "preferred" size.
    /// For block layout, you might compute your content width/height here (but not final).
    /// For text or flex, you might measure text or children to determine needed space.
    /// </summary>
    void Measure(LayoutContext context);

    /// <summary>
    /// Second pass: finalize positions/sizes after measuring.
    /// Place children in the container (for block or flex or inline).
    /// Possibly recalculate height if "auto".
    /// </summary>
    void Arrange(LayoutContext context);
}

/// <summary>
/// Holds the parent coordinates, available space, or any pass-specific info.
/// You can expand this as needed (e.g. storing pass count or partial reflow flags).
/// </summary>
public struct LayoutContext
{
    public float ParentX;
    public float ParentY;
    public float AvailableWidth;

    public float PreviousMarginBottom; // For margin collapsing, if desired.
}

/// <summary>
/// A factory method that picks which ILayoutObject to use based on the node's style.
/// For now, we do a simple display check: "block" => BlockLayoutObject, else Inline.
/// You could add "flex", "grid", etc. here as you expand.
/// </summary>
public static class LayoutObjectFactory
{
    public static ILayoutObject GetOrCreateLayoutObject(IRenderNode node)
    {
        // If it's an ElementNode, we read display property:
        if (node is ElementNode elem)
        {
            var style = elem.ComputedStyle;
            if (style != null)
            {
                var display = style.GetPropertyValue("display") ?? "inline";
                if (display == "block")
                    return new BlockLayoutObject(elem);
                // else if (display == "flex") return new FlexLayoutObject(elem);
                // else if (display == "inline-block") ...
                // else if (display == "grid") ...
            }

            // default
            return new InlineLayoutObject(elem);
        }
        else if (node is TextNode text)
        {
            // For text, we can treat it as inline
            return new InlineLayoutObject(text);
        }

        // fallback
        return new BlockLayoutObject(node);
    }
}

/// <summary>
/// A simple block layout object that does a 2-pass approach.
/// In practice, you’ll adapt your existing BoxModelCalculator, constraints,
/// margin collapsing, etc. in these two methods.
/// </summary>
public class BlockLayoutObject : ILayoutObject
{
    public IRenderNode Node { get; }

    public BlockLayoutObject(IRenderNode node)
    {
        Node = node;
    }

    /// <summary>
    /// 1st pass: compute the node’s “content width” and “content height” if known.
    /// We don’t set final X, Y, or fully finalize height if auto. We store partial results in Node.Layout.
    /// </summary>
    public void Measure(LayoutContext context)
    {
        if (Node is not ElementNode elem || elem.ComputedStyle == null)
        {
            return;
        }

        var style = elem.ComputedStyle;
        var box = new BoxModelCalculator(style, context.AvailableWidth);

        var constraints = new LayoutConstraints(style);
        string boxSizing = style.GetPropertyValue("box-sizing") ?? "content-box";

        // The same logic from single-pass:
        float contentWidth, totalWidth, contentHeight, totalHeight;

        // Width
        if (boxSizing == "border-box")
        {
            totalWidth = Math.Clamp(box.BoxWidth, constraints.MinWidth, constraints.MaxWidth);
            contentWidth = totalWidth - (box.PaddingLeft + box.PaddingRight + box.BorderLeft + box.BorderRight);
        }
        else
        {
            contentWidth = Math.Clamp(box.ContentWidth, constraints.MinWidth, constraints.MaxWidth);
            totalWidth = contentWidth + (box.PaddingLeft + box.PaddingRight + box.BorderLeft + box.BorderRight);
        }

        // Height
        string heightStr = style.GetPropertyValue("height") ?? string.Empty;
        bool isHeightAuto = string.IsNullOrEmpty(heightStr) || heightStr.Equals("auto", StringComparison.OrdinalIgnoreCase);

        if (boxSizing == "border-box")
        {
            totalHeight = Math.Clamp(box.BoxHeight, constraints.MinHeight, constraints.MaxHeight);
            contentHeight = totalHeight - (box.PaddingTop + box.PaddingBottom + box.BorderTop + box.BorderBottom);
        }
        else
        {
            contentHeight = Math.Clamp(box.ContentHeight, constraints.MinHeight, constraints.MaxHeight);
            totalHeight = contentHeight + (box.PaddingTop + box.PaddingBottom + box.BorderTop + box.BorderBottom);
        }

        // Store partial results (like your single pass does at the end)
        if (elem.Layout == null)
            elem.Layout = new LayoutBox(0, 0, totalWidth, totalHeight);
        else
        {
            elem.Layout.BoxWidth = totalWidth;
            elem.Layout.BoxHeight = totalHeight;
        }

        // Copy box model metrics
        var lb = elem.Layout;
        lb.MarginTop = box.MarginTop;
        lb.MarginRight = box.MarginRight;
        lb.MarginBottom = box.MarginBottom;
        lb.MarginLeft = box.MarginLeft;

        lb.BorderTop = box.BorderTop;
        lb.BorderRight = box.BorderRight;
        lb.BorderBottom = box.BorderBottom;
        lb.BorderLeft = box.BorderLeft;

        lb.PaddingTop = box.PaddingTop;
        lb.PaddingRight = box.PaddingRight;
        lb.PaddingBottom = box.PaddingBottom;
        lb.PaddingLeft = box.PaddingLeft;

        // 5) Measure children in normal block flow (stacked).
        //    Child's available width is parent's content width.
        float usedChildHeight = 0f;

        // Create a child context so each child sees "276px" (for example) if the parent is 300 border-box
        var childContext = new LayoutContext
        {
            AvailableWidth = contentWidth,
            // For measure, you often just need the width.
            // X/Y is not as critical here, but let's set them anyway:
            ParentX = 0f,
            ParentY = 0f,
            PreviousMarginBottom = 0f
        };

        foreach (var child in elem.Children)
        {
            if (child == null) continue;

            // Let the child's layout object measure itself
            var childLayoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(child);
            childLayoutObj.Measure(childContext);

            // If you want the parent's auto-height to account for child heights:
            if (child is ElementNode cElem && cElem.Layout != null)
            {
                usedChildHeight += cElem.Layout.BoxHeight;
            }
            else if (child is TextNode tNode && tNode.Layout != null)
            {
                usedChildHeight += tNode.Layout.BoxHeight;
            }
        }

        // 6) If height is auto, finalize the parent's height after seeing child sizes
        if (isHeightAuto)
        {
            contentHeight = usedChildHeight;
            totalHeight = contentHeight
                          + lb.PaddingTop + lb.PaddingBottom
                          + lb.BorderTop + lb.BorderBottom;

            lb.BoxHeight = totalHeight;
        }
    }

    /// <summary>
    /// 2nd pass: finalize X, Y, plus lay out children.
    /// Then fix up height if it was auto, etc.
    /// </summary>
    public void Arrange(LayoutContext context)
    {
        if (Node is not ElementNode elem || elem.ComputedStyle == null)
            return;

        var style = elem.ComputedStyle;
        var lb = elem.Layout; // The LayoutBox we set in Measure()
        if (lb == null) return; // Safety check

        // 1️⃣ Collapse the parent's previous bottom margin with our top margin.
        // (For the very first element on the page, context.PreviousMarginBottom will be 0.)
        float collapsedMarginTop = MarginCollapser.Collapse(context.PreviousMarginBottom, lb.MarginTop);

        // 2️⃣ Compute final absolute position
        (float posX, float posY) = PositioningResolver.ComputePosition(
            style,
            context.ParentX,
            context.ParentY,
            context.AvailableWidth,
            lb.ContentWidth // or lb.BoxWidth if border-box
        );
        posY += collapsedMarginTop;

        // Store final XY
        lb.X = posX;
        lb.Y = posY;

        // 3️⃣ Layout children in block flow
        float childOffsetY = 0f;
        var childContext = new LayoutContext
        {
            ParentX = posX + lb.BorderLeft + lb.PaddingLeft,
            ParentY = posY + lb.BorderTop + lb.PaddingTop,
            AvailableWidth = lb.ContentWidth,
            PreviousMarginBottom = 0f
        };

        foreach (var child in elem.Children)
        {
            if (child == null) continue;

            // Let each child measure & arrange in the parent's context
            // But we already did MeasurePass globally, so you could skip or do partial measure here if needed.
            float updatedOffsetY = childOffsetY;
            var cLayoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(child);
            cLayoutObj.Arrange(childContext);

            // Once arranged, see how tall child is
            if (child is ElementNode cElem && cElem.Layout != null)
            {
                float childHeight = cElem.Layout.BoxHeight;
                float marginBottom = cElem.Layout.MarginBottom;
                updatedOffsetY += (childHeight + marginBottom + cElem.Layout.MarginTop);
            }
            else if (child is TextNode tNode && tNode.Layout != null)
            {
                updatedOffsetY += tNode.Layout.BoxHeight;
            }

            childOffsetY = updatedOffsetY;
        }

        // // 4️⃣ If height was auto, finalize using total children size
        if (float.IsNaN(lb.ContentHeight))
        {
            lb.BoxHeight = childOffsetY
                           + lb.PaddingTop + lb.PaddingBottom
                           + lb.BorderTop + lb.BorderBottom;
        }
    }
}

public class InlineLayoutObject : ILayoutObject
{
    public IRenderNode Node { get; }

    public InlineLayoutObject(IRenderNode node)
    {
        Node = node;
    }

    public void Measure(LayoutContext context)
    {
        if (Node is TextNode text)
        {
            var content = text.Ref.TextContent ?? string.Empty;
            // naive measure
            float approxWidth = content.Length * 7f;
            float lineHeight = 16f;

            if (text.Layout == null)
            {
                text.Layout = new LayoutBox(0, 0, approxWidth, lineHeight);
            }
            else
            {
                text.Layout.BoxWidth = approxWidth;
                text.Layout.BoxHeight = lineHeight;
            }
        }
        else if (Node is ElementNode elem)
        {
            // For an inline element node, you might measure similarly or
            // treat it as inline-block if needed. This sample is extremely naive.
            float fallbackWidth = 50f;
            float fallbackHeight = 16f;

            if (elem.Layout == null)
                elem.Layout = new LayoutBox(0, 0, fallbackWidth, fallbackHeight);
        }
    }

    public void Arrange(LayoutContext context)
    {
        // For a naive inline approach, we just place it at (context.ParentX, context.ParentY).
        // Real inline layout needs line boxes, wrapping, etc.

        if (Node.Layout != null)
        {
            Node.Layout.X = context.ParentX;
            Node.Layout.Y = context.ParentY;

            // If we had multiple inlines in a line, we'd increment X, etc.
        }
    }
}

public class LayoutEngineV2
{
    public void LayoutDocument(IRenderNode rootNode, float viewportWidth, float viewportHeight)
    {
        if (rootNode == null) return;

        // We do a measure pass top-down
        var initialCtx = new LayoutContext
        {
            ParentX = 0,
            ParentY = 0,
            AvailableWidth = viewportWidth,
            PreviousMarginBottom = 0
        };

        MeasurePass(rootNode, initialCtx);

        // Then an arrange pass top-down
        ArrangePass(rootNode, initialCtx);
    }

    private void MeasurePass(IRenderNode node, LayoutContext context)
    {
        var layoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(node);
        layoutObj.Measure(context);

        // foreach (var child in node.Children)
        // {
        //     if (child == null) continue;
        //     var childCtx = context; // you might refine per child
        //     MeasurePass(child, childCtx);
        // }
    }

    private void ArrangePass(IRenderNode node, LayoutContext context)
    {
        var layoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(node);
        layoutObj.Arrange(context);

        // foreach (var child in node.Children)
        // {
        //     if (child == null) continue;
        //     var childCtx = context; // again, refine per child if needed
        //     ArrangePass(child, childCtx);
        // }
    }
}