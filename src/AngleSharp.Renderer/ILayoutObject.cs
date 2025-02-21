#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS0162 // Unreachable code detected
namespace AngleSharp.Renderer;

using System;
using System.Collections.Generic;
using System.Linq;
using Html.Construction;

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

    public bool IsFirstChild;
    public float ParentMarginBottom;

    public float PreviousMarginBottom; // For margin collapsing, if desired.


    public float ParentGlobalPositionX;
    public float ParentGlobalPositionY;

    public bool IsCollapsedMarginWithParentTop;
    public float ChildMarginTop;


    public bool IsCollapsedMarginWithParentBottom;
    public float ChildMarginBottom;
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
        Arrange2(context);
        return;
        if (Node is not ElementNode elem || elem.ComputedStyle == null)
            return;

        var style = elem.ComputedStyle;
        var lb = elem.Layout; // The LayoutBox we set in Measure()
        if (lb == null) return; // Safety check

        // Calculate parent-child margin collapse with enhanced rules
        float collapsedMarginWithParent = MarginCollapser.CalculateParentChildCollapse(
            elem.Parent as ElementNode,
            elem,
            context.PreviousMarginBottom,
            lb.MarginTop,
            lb.PaddingTop,
            lb.BorderTop
        );

        // Compute final absolute position (this can include relative positioning, etc.).
        (float posX, float posY) = PositioningResolver.CalculateElementPosition(
            style,
            context.ParentX,
            context.ParentY,
            context.AvailableWidth,
            lb.ContentWidth
        );

        // If there is not Parent Child Collapse, the posY is the same as the parent's X
        if (collapsedMarginWithParent == 0)
        {
            posY = context.ParentY;
        }
        else if(context.IsFirstChild)
        {
            posY = context.ParentY - context.ParentMarginBottom + collapsedMarginWithParent;
        }
        else
        {
            posY = context.ParentY - context.PreviousMarginBottom + collapsedMarginWithParent;
        }

        // Store final position
        lb.X = posX;
        lb.Y = posY;

        // Initialize child layout tracking
        float currentY = 0f;
        float previousSiblingBottomMargin = 0f;
        ElementNode? previousSibling = null;

        // Prepare child context
        var childContext = new LayoutContext
        {
            ParentX = posX + lb.BorderLeft + lb.PaddingLeft,
            ParentY = posY + lb.BorderTop + lb.PaddingTop,
            AvailableWidth = lb.ContentWidth,
            IsFirstChild = false,
            ParentMarginBottom = lb.MarginBottom,
            PreviousMarginBottom = 0f  // We'll set this per-child now
        };

        // Layout children with enhanced margin handling
        foreach (var child in elem.Children.Where(node => node is ElementNode or TextNode))
        {
            if (child == null) continue;

            var childLayoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(child);

            if (child is ElementNode childElem && childElem.Layout != null)
            {
                float collapsedMargin;

                if (previousSibling == null)
                {
                    // First child - special handling for parent-child margin collapse
                    collapsedMargin = MarginCollapser.CalculateParentChildCollapse(
                        elem,
                        childElem,
                        lb.MarginTop,
                        childElem.Layout.MarginTop,
                        lb.PaddingTop,
                        lb.BorderTop
                    );

                    childContext.IsFirstChild = true;
                }
                else
                {
                    // Subsequent children - handle sibling margin collapse
                    collapsedMargin = MarginCollapser.CalculateSiblingCollapse(
                        previousSibling,
                        childElem,
                        previousSiblingBottomMargin,
                        childElem.Layout.MarginTop
                    );

                    childContext.IsFirstChild = false;
                }

                // Update child context with current position
                childContext.ParentY = posY + lb.BorderTop + lb.PaddingTop + currentY;
                childContext.PreviousMarginBottom = previousSiblingBottomMargin;

                // Arrange the child
                childLayoutObj.Arrange(childContext);

                // Update tracking variables
                currentY += collapsedMargin + childElem.Layout.BoxHeight;
                previousSiblingBottomMargin = childElem.Layout.MarginBottom;
                previousSibling = childElem;
            }
            else if (child is TextNode textNode && textNode.Layout != null)
            {
                // Handle text nodes without margin collapse
                childContext.ParentY = posY + lb.BorderTop + lb.PaddingTop + currentY;
                childLayoutObj.Arrange(childContext);
                currentY += textNode.Layout.BoxHeight;
            }
        }

        // Update this element’s final box height if it was auto;
        // or adjust if you are recalculating. The code below is just an example:
        if (float.IsNaN(lb.ContentHeight))
        {
            // Add the last child's bottom margin as well
            currentY += previousSiblingBottomMargin;

            lb.BoxHeight = currentY
                + lb.PaddingTop + lb.PaddingBottom
                + lb.BorderTop + lb.BorderBottom;
        }
    }

    public void Arrange2(LayoutContext context)
    {
        if (Node is not ElementNode elem || elem.ComputedStyle == null || elem.Layout == null)
        {
            return;
        }

        var computedStyle = elem.ComputedStyle;
        var layoutBox = elem.Layout;

        // Calculate Global Position
        (float posX, float posY) = PositioningResolver.CalculateElementPosition(
            computedStyle,
            context.ParentX,
            context.ParentY,
            context.AvailableWidth,
            layoutBox.ContentWidth
        );

        layoutBox.X = posX;
        layoutBox.Y = posY;

        // Margin Collapse Parent Child First Child
        float collapsedMargin = CollapseMarginsForElement(elem);


        if (context.IsCollapsedMarginWithParentTop)
        {
            layoutBox.Y = layoutBox.Y + context.ChildMarginTop;
        }


        // Calculate Relative Position



        var children = elem.Children.Where(node => node is ElementNode or TextNode).ToList();
        int count = children.Count;

        for (var childIndex = 0; childIndex < children.Count; childIndex++)
        {
            var child = children[childIndex];
            bool isFirst = (childIndex == 0);
            bool isLast = (childIndex == count - 1);


            var childContext = new LayoutContext()
            {
                ParentX = posX + layoutBox.BorderLeft + layoutBox.PaddingLeft,
                ParentY = posY + layoutBox.BorderTop + layoutBox.PaddingTop,
                AvailableWidth = layoutBox.ContentWidth
            };

            var childLayoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(child);

            // Margin Collapse Parent Child First Child
            // childContext.IsCollapsedMarginWithParentTop = layoutBox.PaddingTop == 0 && layoutBox.BorderTop == 0 && isFirst;
            // if (childContext.IsCollapsedMarginWithParentTop)
            // {
            //     childContext.ChildMarginTop = MarginCollapser.Collapse(layoutBox.MarginTop, child.Layout?.MarginTop ?? 0);
            // }

            // Margin Collapse Siblings

            // Margin Collapse Parent Child Last Child
            // childContext.IsCollapsedMarginWithParentBottom = layoutBox.PaddingBottom == 0 && layoutBox.BorderBottom == 0 && isLast;
            // if (childContext.IsCollapsedMarginWithParentBottom)
            // {
            //     childContext.ChildMarginBottom = MarginCollapser.Collapse(layoutBox.MarginBottom, child.Layout?.MarginBottom ?? 0);
            // }

            childLayoutObj.Arrange(childContext);
        }

        if (children.Count > 0)
        {
            var lastChildNode = children[^1] as ElementNode;
            if (lastChildNode?.Layout is LayoutBox lastChildBox)
            {
                // The last child's final bottom = child’s Y + child’s total height
                // (BoxHeight includes border+padding+content).
                float lastChildBottom = lastChildBox.Y + lastChildBox.BoxHeight;

                // If the parent and last child *can* collapse margins at the bottom,
                // recalculate how far the parent extends.
                if (layoutBox.PaddingBottom == 0 && layoutBox.BorderBottom == 0)
                {
                    float collapsedBottom =
                        MarginCollapser.Collapse(layoutBox.MarginBottom, lastChildBox.MarginBottom);

                    // The parent's new "bottom" = last child's bottom + collapsed margin
                    float newParentBottom = lastChildBottom + collapsedBottom;

                    // So the parent's BoxHeight = (new bottom) - (parent's top)
                    float newHeight = newParentBottom - layoutBox.Y;

                    // If negative margins exceed child’s bottom, you could clamp or allow it:
                    if (newHeight < 0)
                    {
                        newHeight = 0;
                    }

                    layoutBox.BoxHeight = newHeight;
                }
                else
                {
                    // If parent does NOT collapse bottom margin (has padding/border, etc.),
                    // we still need to ensure the parent covers the last child's bottom at least.
                    float needed = lastChildBottom - layoutBox.Y;
                    if (needed > layoutBox.BoxHeight)
                    {
                        layoutBox.BoxHeight = needed;
                    }
                }
            }
        }
    }

    public float CollapseMarginsForElement(ElementNode? elementNode)
    {
        if (elementNode == null)
            return 0;

        var margins = new List<float>();
        var current = elementNode;
        var visited = new HashSet<ElementNode>();

        while (current != null && !visited.Contains(current))
        {
            visited.Add(current);

            if (current.Layout != null)
                margins.Add(current.Layout.MarginTop);

            current = current.Children.FirstOrDefault() as ElementNode;
        }

        if (!margins.Any())
            return 0;

        var positiveMargins = margins.Where(m => m >= 0);
        var negativeMargins = margins.Where(m => m < 0);

        float maxPositive = positiveMargins.Any() ? positiveMargins.Max() : 0;
        float maxNegative = negativeMargins.Any() ? negativeMargins.Min() : 0;

        return maxPositive + maxNegative;
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
    }

    private void ArrangePass(IRenderNode node, LayoutContext context)
    {
        var layoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(node);
        layoutObj.Arrange(context);
    }
}