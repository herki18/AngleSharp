namespace AngleSharp.Renderer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#pragma warning disable CS0219, CS0162
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

    public void Arrange(LayoutContext context)
    {
        if (Node is not ElementNode elem || elem.ComputedStyle == null || elem.Layout == null)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("----------------------------------------");
        Console.WriteLine($"Arranging {elem.Ref.TagName} - {elem.Id}");

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

        Console.WriteLine($"ParentY {context.ParentY}");
        Console.WriteLine($"posY {posY}");
        Console.WriteLine($"layoutBox.Y {layoutBox.Y}");

        // For elements that can collapse margins (no border/padding at top),
        // use CollapseMarginsForElement to handle the entire chain of first children
        if (!context.HasPreviousSibling && layoutBox.BorderTop == 0 && layoutBox.PaddingTop == 0)
        {
            if (!layoutBox.IsInMarginToCollapsedChain)
            {
                // Calculate the collapsed margin through entire descendant chain
                float collapsedMargin = CollapseMarginsForElement(elem);

                // Root of collapse chain - apply the full margin
                layoutBox.Y += collapsedMargin;
                posY += collapsedMargin;
            }
            else
            {
                // Part of existing chain - use parent's collapsed margin
                layoutBox.Y = posY;
                layoutBox.MarginTop = 0;
            }
        }
        else
        {
            layoutBox.Y = posY;
        }

        // Continue with child layouts...
        var children = elem.Children.Where(node => node is ElementNode or TextNode).ToList();

        // PRE-PROCESSING STEP: Identify empty block chains
        var emptyBlockChains = IdentifyEmptyBlockChains(children);

        int count = children.Count;
        float previousMarginBottom = 0f;
        ElementNode? previousSibling = null;
        float childPosY = posY; // Track child Y positions separately

        for (var childIndex = 0; childIndex < children.Count; childIndex++)
        {
            var child = children[childIndex];
            bool isFirst = (childIndex == 0);
            bool isLast = (childIndex == count - 1);

            Console.WriteLine($"Arranging child {child.Ref.NodeName} - {child.Id} under parent {elem.Ref.TagName} - {elem.Id}");

            // Handle margin collapsing between siblings
            if (child is ElementNode currentElement && previousSibling != null && previousSibling.Layout != null)
            {
                // Add the previous sibling's height to childPosY
                childPosY += previousSibling.Layout.BoxHeight;

                float collapsedMargin = MarginCollapser.CalculateSiblingCollapse(
                    previousSibling,
                    currentElement,
                    previousMarginBottom,
                    currentElement.Layout?.MarginTop ?? 0f
                );

                // Adjust the Y position based on the collapsed margin
                childPosY += collapsedMargin;
            }

            Console.WriteLine($"Final Y position for {child.Ref.NodeName} - {child.Id}: {childPosY}");

            var childContext = new LayoutContext()
            {
                ParentX = posX + layoutBox.BorderLeft + layoutBox.PaddingLeft,
                ParentY = childPosY + layoutBox.BorderTop + layoutBox.PaddingTop,
                AvailableWidth = layoutBox.ContentWidth,
                HasPreviousSibling = previousSibling != null,
                PreviousSiblingMarginBottom = previousMarginBottom,
                CurrentSiblingMarginTop = child is ElementNode currentElem ? currentElem.Layout?.MarginTop ?? 0f : 0f
            };

            var childLayoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(child);
            childLayoutObj.Arrange(childContext);

            // Update previous sibling information for next iteration
            if (child is ElementNode childElem)
            {
                previousSibling = childElem;
                previousMarginBottom = childElem.Layout?.MarginBottom ?? 0f;
            }
        }
    }

    /// <summary>
    /// Represents a chain of consecutive empty block elements
    /// </summary>
    private class EmptyBlockChain
    {
        public List<ElementNode> Elements { get; } = new List<ElementNode>();
        public int FirstIndex { get; set; }
        public int LastIndex { get; set; }
        public float CollapsedMargin { get; set; }
    }

    /// <summary>
    /// Identifies chains of consecutive empty blocks and calculates their collapsed margins
    /// </summary>
    private List<EmptyBlockChain> IdentifyEmptyBlockChains(List<IRenderNode> children)
    {
        var chains = new List<EmptyBlockChain>();
        EmptyBlockChain? currentChain = null;

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is ElementNode element && IsEmptyBlock(element))
            {
                if (currentChain == null)
                {
                    currentChain = new EmptyBlockChain { FirstIndex = i };
                }

                currentChain.Elements.Add(element);
                currentChain.LastIndex = i;
            }
            else if (currentChain != null)
            {
                // End of chain - calculate collapsed margin
                CalculateChainCollapsedMargin(currentChain);

                // Only add chains with more than one element (single empty blocks are handled by normal code)
                if (currentChain.Elements.Count > 1)
                {
                    chains.Add(currentChain);
                }
                currentChain = null;
            }
        }

        // Handle chain at the end
        if (currentChain != null)
        {
            CalculateChainCollapsedMargin(currentChain);

            // Only add chains with more than one element
            if (currentChain.Elements.Count > 1)
            {
                chains.Add(currentChain);
            }
        }

        return chains;
    }

    /// <summary>
    /// Calculates the collapsed margin for a chain of empty blocks
    /// </summary>
    private void CalculateChainCollapsedMargin(EmptyBlockChain chain)
    {
        var margins = new List<float>();

        foreach (var element in chain.Elements)
        {
            if (element.ComputedStyle != null && element.Layout != null)
            {
                margins.Add(element.Layout.MarginTop);
                margins.Add(element.Layout.MarginBottom);
            }
        }

        chain.CollapsedMargin = CollapseMarginList(margins);
    }

    /// <summary>
    /// Collapses a list of margins into a single margin value
    /// </summary>
    private float CollapseMarginList(List<float> margins)
    {
        if (!margins.Any()) return 0;

        float result = margins[0];
        for (int i = 1; i < margins.Count; i++)
        {
            result = MarginCollapser.Collapse(result, margins[i]);
        }

        return result;
    }

    /// <summary>
    /// Determines whether an element is an empty block that participates in margin collapsing
    /// </summary>
    private bool IsEmptyBlock(ElementNode element)
    {
        if (element == null || element.Layout == null) return false;

        // Check if element has any in-flow children
        bool hasInFlowContent = element.Children.Any(child =>
            (child is ElementNode && !IsOutOfFlowPosition(child)) ||
            (child is TextNode text && !string.IsNullOrWhiteSpace(text.Ref.TextContent)));

        // Check if element has padding or border
        float paddingTop = element.Layout.PaddingTop;
        float paddingBottom = element.Layout.PaddingBottom;
        float borderTop = element.Layout.BorderTop;
        float borderBottom = element.Layout.BorderBottom;

        return !hasInFlowContent && paddingTop == 0 && paddingBottom == 0 &&
               borderTop == 0 && borderBottom == 0;
    }

    /// <summary>
    /// Checks if a node has an out-of-flow positioning (absolute, fixed)
    /// </summary>
    private bool IsOutOfFlowPosition(IRenderNode node)
    {
        if (node is not ElementNode element || element.ComputedStyle == null)
            return false;

        string position = element.ComputedStyle.GetPropertyValue("position") ?? "";
        string float_ = element.ComputedStyle.GetPropertyValue("float") ?? "";

        return position == "absolute" || position == "fixed" || float_ != "none";
    }

    /// <summary>
    /// Calculates the collapsed margin between a parent element and its child elements,
    /// traversing down the DOM tree as deep as possible according to CSS margin collapsing rules.
    /// This method specifically handles parent-child margin collapsing, not sibling margin collapsing.
    ///
    /// The method follows these rules:
    /// 1. Only considers elements that can participate in margin collapsing (no border/padding)
    /// 2. Traverses down through first children until a stopping condition is met
    /// 3. Takes the largest positive margin and the smallest negative margin
    /// 4. Marks all elements in the collapse chain to prevent double-counting
    /// </summary>
    /// <param name="elementNode">The parent element to start margin collapse calculation from</param>
    /// <returns>The final collapsed margin value combining the largest positive and smallest negative margins</returns>
    public float CollapseMarginsForElement(ElementNode? elementNode)
    {
        if (elementNode == null || !elementNode.Children.Any())
            return 0;

        var margins = new List<float>();
        var current = elementNode;
        var visited = new HashSet<ElementNode>();
        HashSet<ElementNode> partOfChain = new HashSet<ElementNode>();

        while (current != null && !visited.Contains(current))
        {
            visited.Add(current);

            if (current.Layout != null)
            {
                margins.Add(current.Layout.MarginTop);
                partOfChain.Add(current);
            }

            current = current.Children.FirstOrDefault(node => node is not NonRenderableNode) as ElementNode;
        }

        if (!margins.Any())
            return 0;

        var positiveMargins = margins.Where(m => m >= 0);
        var negativeMargins = margins.Where(m => m < 0);

        float maxPositive = positiveMargins.Any() ? positiveMargins.Max() : 0;
        float maxNegative = negativeMargins.Any() ? negativeMargins.Min() : 0;

        if (!positiveMargins.Any() && !negativeMargins.Any())
        {
            return 0;
        }

        foreach (var node in partOfChain)
        {
            node.Layout!.IsInMarginToCollapsedChain = true;
        }

        return maxPositive + maxNegative;
    }
}