namespace AngleSharp.Renderer;

using System;
using System.Collections.Generic;
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

    private readonly bool _enableLogging = true;

    public BlockLayoutObject(IRenderNode node)
    {
        Node = node;
    }

    private void Log(string message)
    {
        if (_enableLogging)
        {
            Console.WriteLine($"[Layout] {message}");
        }
    }

    /// <summary>
    /// 1st pass: compute the node’s “content width” and “content height” if known.
    /// We don’t set final X, Y, or fully finalize height if auto. We store partial results in Node.Layout.
    /// </summary>
    public void Measure(LayoutContext context)
    {
        if (Node is not ElementNode elem || elem.ComputedStyle == null)
        {
            Log($"Skipping measure for non-element or null style: {Node?.Ref?.NodeName}");
            return;
        }

        Log($"--- MEASURING {elem.Ref.TagName} - {elem.Id ?? "no-id"} ---");
        Log($"Available width: {context.AvailableWidth}");

        var style = elem.ComputedStyle;
        var box = new BoxModelCalculator(style, context.AvailableWidth);

        Log($"Box model: Margin({box.MarginTop},{box.MarginRight},{box.MarginBottom},{box.MarginLeft})");
        Log($"Box model: Border({box.BorderTop},{box.BorderRight},{box.BorderBottom},{box.BorderLeft})");
        Log($"Box model: Padding({box.PaddingTop},{box.PaddingRight},{box.PaddingBottom},{box.PaddingLeft})");
        Log($"Box model: Content({box.ContentWidth}x{box.ContentHeight})");

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

        Log($"Final dimensions: Content({contentWidth}x{contentHeight}), Total({totalWidth}x{totalHeight})");

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

        Log($"Stored in Layout: BoxWidth={elem.Layout.BoxWidth}, BoxHeight={elem.Layout.BoxHeight}");

        Log($"Measuring {elem.Children.Count()} children with content width: {contentWidth}");

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

            Log($"Auto height adjusted to {totalHeight} based on child content");
        }
    }

    public void Arrange(LayoutContext context)
    {
        if (Node is not ElementNode elem || elem.ComputedStyle == null || elem.Layout == null)
        {
            return;
        }

        Log("");
        Log("========================================");
        Log($"ARRANGING {elem.Ref.TagName} - {elem.Id ?? "no-id"}");
        Log($"Parent position: ({context.ParentX}, {context.ParentY})");
        Log($"Available width: {context.AvailableWidth}");

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

        Log($"Initial position from resolver: ({posX}, {posY})");

        layoutBox.X = posX;
        layoutBox.Y = posY;

        // For elements that can collapse margins (no border/padding at top),
        // use CollapseMarginsForElement to handle the entire chain of first children
        if (!context.HasPreviousSibling && layoutBox.BorderTop == 0 && layoutBox.PaddingTop == 0)
        {
            Log("Element qualifies for parent-child margin collapsing");
            if (!layoutBox.IsInMarginToCollapsedChain)
            {
                // Calculate the collapsed margin through entire descendant chain
                float collapsedMargin = CollapseMarginsForElement(elem);
                Log($"Calculated collapsed margin: {collapsedMargin} (not in existing chain)");

                // Root of collapse chain - apply the full margin
                layoutBox.Y += collapsedMargin;
                posY += collapsedMargin;
                Log($"Position after margin collapse: ({layoutBox.X}, {layoutBox.Y})");
            }
            else
            {
                // Part of existing chain - use parent's collapsed margin
                Log($"Element is part of existing margin collapsed chain");
                layoutBox.Y = posY;
                layoutBox.MarginTop = 0;
                Log($"Using parent's collapsed margin, setting MarginTop=0");
            }
        }
        else
        {
            Log($"No parent-child margin collapsing: HasPreviousSibling={context.HasPreviousSibling}, BorderTop={layoutBox.BorderTop}, PaddingTop={layoutBox.PaddingTop}");
            layoutBox.Y = posY;
        }

        // Continue with child layouts...
        var children = elem.Children.Where(node => node is ElementNode or TextNode).ToList();
        int childCount = children.Count;
        Log($"Processing {childCount} children");

        // PRE-PROCESSING STEP: Identify empty block chains
        var emptyBlockChains = IdentifyEmptyBlockChains(children);
        Log($"Found {emptyBlockChains.Count} empty block chains");

        Console.WriteLine($"Found {emptyBlockChains.Count} empty block chains");
        foreach (var chain in emptyBlockChains)
        {
            Log($"Chain from index {chain.FirstIndex} to {chain.LastIndex} with {chain.Elements.Count} elements");
            Log($"  Collapsed margin: {chain.CollapsedMargin}");
            foreach (var el in chain.Elements)
            {
                Log($"  > {el.Ref.TagName} - {el.Id ?? "no-id"} with margin-top: {el.Layout?.MarginTop} margin-bottom: {el.Layout?.MarginBottom}");
            }
        }

        int count = children.Count;
        float previousMarginBottom = 0f;
        ElementNode? previousSibling = null;
        float childPosY = posY + layoutBox.BorderTop + layoutBox.PaddingTop; // Track child Y positions
        int skipToIndex = -1; // Used to skip over processed chains
        ElementNode? lastContentChild = null; // Track the last child with actual content
        float effectiveBottomMargin = 0f; // Store the effective bottom margin after collapsing

        for (var childIndex = 0; childIndex < children.Count; childIndex++)
        {
            // Skip if we're already processed as part of a chain
            if (childIndex <= skipToIndex) continue;

            var child = children[childIndex];
            bool isFirst = (childIndex == 0);
            bool isLast = (childIndex == count - 1);

            Log($"Processing child {childIndex}/{childCount-1}: {child.Ref.NodeName} - {child.Id ?? "no-id"}, isFirst={isFirst}, isLast={isLast}");

            // Check if this is the start of an empty block chain
            EmptyBlockChain? currentChain = null;
            if (child is ElementNode childElement)
            {
                currentChain = emptyBlockChains.FirstOrDefault(c => c.FirstIndex == childIndex);
            }

            if (currentChain != null)
            {
                Log($"  Processing as part of empty block chain (indices {currentChain.FirstIndex}-{currentChain.LastIndex})");

                // Position the first element at the current Y position
                var firstElement = currentChain.Elements[0];
                if (firstElement.Layout != null)
                {
                    float elementX = posX + layoutBox.BorderLeft + layoutBox.PaddingLeft;
                    Log($"  Positioning first element of chain at X={elementX}, Y={childPosY}");
                    firstElement.Layout.X = elementX;
                    firstElement.Layout.Y = childPosY;
                    firstElement.Layout.IsInMarginToCollapsedChain = true;

                    // Set child context for recursive arrangement
                    var childContext = new LayoutContext()
                    {
                        ParentX = firstElement.Layout.X,
                        ParentY = firstElement.Layout.Y,
                        AvailableWidth = layoutBox.ContentWidth,
                        HasPreviousSibling = false,
                        PreviousSiblingMarginBottom = 0,
                        CurrentSiblingMarginTop = firstElement.Layout.MarginTop
                    };

                    var childLayoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(firstElement);
                    childLayoutObj.Arrange(childContext);

                    Log($"  First element of chain arranged: Y={firstElement.Layout.Y}, Height={firstElement.Layout.BoxHeight}");
                }

                // For subsequent elements, apply the collapsed margin of the entire chain
                // This is crucial - d1.2 and d1.3 should both be at the same Y position
                // if d1.2 is a zero-height block with only margins
                float chainY = childPosY;
                bool allEmptyBlocks = true;

                // Check if all blocks in the chain are actually empty (zero height)
                foreach (var chainElem in currentChain.Elements)
                {
                    if (chainElem.Layout != null && chainElem.Layout.BoxHeight > 0)
                    {
                        allEmptyBlocks = false;
                        break;
                    }
                }

                if (allEmptyBlocks)
                {
                    // For chains of pure empty blocks, position all at same Y with collapsed margin
                    chainY = childPosY;
                    Log($"  All blocks in chain are empty - positioning at starting Y: {chainY}");
                }
                else if (firstElement.Layout != null)
                {
                    // If first element has height, next element starts after it plus margin
                    chainY += firstElement.Layout.BoxHeight + currentChain.CollapsedMargin;
                    Log($"  First element has height - positioning after it: {chainY}");
                }

                Log($"  Chain contains all empty blocks: {allEmptyBlocks}");

                // Position all remaining elements in the chain
                for (int i = 1; i < currentChain.Elements.Count; i++)
                {
                    var chainElement = currentChain.Elements[i];
                    if (chainElement.Layout == null) continue;

                    float elementX = posX + layoutBox.BorderLeft + layoutBox.PaddingLeft;

                    // If the previous element was truly empty (zero height),
                    // position this element at the same Y position
                    var prevElement = currentChain.Elements[i - 1];
                    if (prevElement.Layout != null && prevElement.Layout.BoxHeight == 0)
                    {
                        chainElement.Layout.X = elementX;
                        chainElement.Layout.Y = prevElement.Layout.Y;
                        Log($"  Positioning chain element {i} at same Y as previous (empty): X={elementX}, Y={prevElement.Layout.Y}");
                    }
                    else
                    {
                        chainElement.Layout.X = elementX;
                        chainElement.Layout.Y = chainY;
                        Log($"  Positioning chain element {i} at: X={elementX}, Y={chainY}");
                    }

                    chainElement.Layout.IsInMarginToCollapsedChain = true;

                    // Set child context for recursive arrangement
                    var childContext = new LayoutContext()
                    {
                        ParentX = chainElement.Layout.X,
                        ParentY = chainElement.Layout.Y,
                        AvailableWidth = layoutBox.ContentWidth,
                        HasPreviousSibling = true,
                        PreviousSiblingMarginBottom = 0, // Already collapsed
                        CurrentSiblingMarginTop = 0 // Already collapsed
                    };

                    var childLayoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(chainElement);
                    childLayoutObj.Arrange(childContext);

                    Log($"  Chain element {i} arranged: Y={chainElement.Layout.Y}, Height={chainElement.Layout.BoxHeight}");

                    // Only update chainY if this element has height
                    if (chainElement.Layout.BoxHeight > 0)
                    {
                        float oldChainY = chainY;
                        chainY += chainElement.Layout.BoxHeight;
                        Log($"  Updating chainY: {oldChainY} -> {chainY} (added height {chainElement.Layout.BoxHeight})");
                    }
                }

                // Update for after the chain
                previousSibling = currentChain.Elements.LastOrDefault() as ElementNode;
                if (previousSibling?.Layout != null)
                {
                    // Calculate the proper Y position after the chain
                    float oldChildPosY = childPosY;

                    if (allEmptyBlocks)
                    {
                        // For chains of completely empty blocks, only add the collapsed margin
                        childPosY += currentChain.CollapsedMargin;
                        Log($"  All blocks were empty - updating childPosY: {oldChildPosY} -> {childPosY} (added collapsed margin {currentChain.CollapsedMargin})");
                    }
                    else
                    {
                        // For chains with non-empty blocks, take the max
                        childPosY = Math.Max(
                            previousSibling.Layout.Y + previousSibling.Layout.BoxHeight,
                            firstElement.Layout?.Y + firstElement.Layout?.BoxHeight ?? 0
                        );
                        Log($"  Chain had non-empty blocks - updating childPosY: {oldChildPosY} -> {childPosY}");
                    }

                    previousMarginBottom = previousSibling.Layout.MarginBottom;
                    Log($"  Setting previousMarginBottom to {previousMarginBottom}");
                }

                // Skip to the end of the chain
                skipToIndex = currentChain.LastIndex;
                Log($"  Chain processing complete - setting skipToIndex={skipToIndex}");
            }
            else
            {
                // Normal (non-chain) element processing
                Log($"  Processing as normal element (not part of chain)");

                if (child is ElementNode currentElement && previousSibling != null && previousSibling.Layout != null)
                {
                    // Add the previous sibling's height to childPosY
                    float oldChildPosY = childPosY;
                    childPosY += previousSibling.Layout.BoxHeight;
                    Log($"  Added previous sibling height: {oldChildPosY} -> {childPosY} (added {previousSibling.Layout.BoxHeight})");

                    // Calculate collapsed margins between siblings
                    Log($"  Calculating sibling margin collapse: prev bottom={previousMarginBottom}, current top={currentElement.Layout?.MarginTop ?? 0f}");
                    float collapsedMargin = MarginCollapser.CalculateSiblingCollapse(
                        previousSibling,
                        currentElement,
                        previousMarginBottom,
                        currentElement.Layout?.MarginTop ?? 0f
                    );
                    Log($"  Collapsed margin between siblings: {collapsedMargin}");

                    // Adjust the Y position based on the collapsed margin
                    float oldPosWithMargin = childPosY;
                    childPosY += collapsedMargin;
                    Log($"  Applied collapsed margin: {oldPosWithMargin} -> {childPosY}");
                }
                else if (isFirst)
                {
                    Log($"  First child - no sibling margin collapse needed");
                }
                else
                {
                    Log($"  No margin collapse calculation - previous sibling not valid or current not element");
                }

                Log($"  Final Y position for child: {childPosY}");

                // Create child context with final position
                var childContext = new LayoutContext()
                {
                    ParentX = posX + layoutBox.BorderLeft + layoutBox.PaddingLeft,
                    ParentY = childPosY,
                    AvailableWidth = layoutBox.ContentWidth,
                    HasPreviousSibling = previousSibling != null,
                    PreviousSiblingMarginBottom = previousMarginBottom,
                    CurrentSiblingMarginTop = child is ElementNode currentElem ? currentElem.Layout?.MarginTop ?? 0f : 0f
                };

                Log($"  Creating child context: ParentX={childContext.ParentX}, ParentY={childContext.ParentY}, Width={childContext.AvailableWidth}");

                // Arrange the child with its context
                var childLayoutObj = LayoutObjectFactory.GetOrCreateLayoutObject(child);
                childLayoutObj.Arrange(childContext);

                // Update previous sibling information for next iteration
                if (child is ElementNode childElem)
                {
                    previousSibling = childElem;
                    if (childElem.Layout != null)
                    {
                        Log($"  Child arranged: Position=({childElem.Layout.X}, {childElem.Layout.Y}), Size={childElem.Layout.BoxWidth}x{childElem.Layout.BoxHeight}");
                        previousMarginBottom = childElem.Layout.MarginBottom;
                        Log($"  Updated previousSibling to current child, previousMarginBottom={previousMarginBottom}");
                    }
                }
                else
                {
                    Log($"  Child arranged (non-element or null layout)");
                }
            }
        }

        Log($"Arrangement complete for {elem.Ref.NodeName} - {elem.Id ?? "no-id"} with {childCount} children");
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
        Log($"Calculating collapsed margin for chain with {chain.Elements.Count} elements:");

        foreach (var element in chain.Elements)
        {
            if (element.ComputedStyle != null && element.Layout != null)
            {
                Log($"  Element {element.Ref.TagName} - {element.Id ?? "no-id"}: margin-top={element.Layout.MarginTop}, margin-bottom={element.Layout.MarginBottom}");
                margins.Add(element.Layout.MarginTop);
                margins.Add(element.Layout.MarginBottom);
            }
        }

        chain.CollapsedMargin = CollapseMarginList(margins);
        Log($"  Final collapsed margin: {chain.CollapsedMargin}");
    }

    /// <summary>
    /// Collapses a list of margins into a single margin value
    /// </summary>
    private float CollapseMarginList(List<float> margins)
    {
        if (!margins.Any()) return 0;

        Log($"Collapsing {margins.Count} margins: [{string.Join(", ", margins)}]");
        float result = margins[0];

        for (int i = 1; i < margins.Count; i++)
        {
            float before = result;
            result = MarginCollapser.Collapse(result, margins[i]);
            Log($"  Collapsed {before} with {margins[i]} = {result}");
        }

        return result;
    }

    /// <summary>
    /// Determines whether an element is an empty block that participates in margin collapsing
    /// </summary>
    private bool IsEmptyBlock(ElementNode element)
    {
        if (element == null || element.Layout == null || element.ComputedStyle == null)
        {
            Log($"Not empty block: null element or missing layout/style");
            return false;
        }

        string display = element.ComputedStyle.GetPropertyValue("display") ?? "";
        if (display == "none" || (display != "block" && display != "flow-root"))
        {
            Log($"Not empty block: display={display} (not block or flow-root)");
            return false;
        }

        // More checks with logging...
        string minHeight = element.ComputedStyle.GetPropertyValue("min-height") ?? "";
        bool hasMinHeight = !string.IsNullOrEmpty(minHeight) && minHeight != "0" && minHeight != "0px" && minHeight != "auto";
        if (hasMinHeight)
        {
            Log($"Not empty block: has min-height={minHeight}");
            return false;
        }

        bool hasInFlowContent = element.Children.Any(child =>
            (child is ElementNode elem && !IsOutOfFlowPosition(elem)) ||
            (child is TextNode text && !string.IsNullOrWhiteSpace(text.Ref.TextContent)));

        if (hasInFlowContent)
        {
            Log($"Not empty block: has in-flow content");
            return false;
        }

        float paddingTop = element.Layout.PaddingTop;
        float paddingBottom = element.Layout.PaddingBottom;
        float borderTop = element.Layout.BorderTop;
        float borderBottom = element.Layout.BorderBottom;

        if (paddingTop > 0 || paddingBottom > 0 || borderTop > 0 || borderBottom > 0)
        {
            Log($"Not empty block: has padding/border (top-padding={paddingTop}, bottom-padding={paddingBottom}, top-border={borderTop}, bottom-border={borderBottom})");
            return false;
        }

        bool hasExplicitHeight = element.ComputedStyle.GetPropertyValue("height") != null &&
                                element.ComputedStyle.GetPropertyValue("height") != "auto";

        if (hasExplicitHeight)
        {
            Log($"Not empty block: has explicit height={element.ComputedStyle.GetPropertyValue("height")}");
            return false;
        }

        Log($"Element IS an empty block: {element.Ref.TagName} - {element.Id ?? "no-id"}");
        return true;
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
        {
            Log("No elements to collapse margins for");
            return 0;
        }
        Log($"Calculating parent-child margin collapse for {elementNode.Ref.TagName} - {elementNode.Id ?? "no-id"}");

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
        Log($"Final collapsed margin: {maxPositive + maxNegative} (max positive={maxPositive}, max negative={maxNegative})");
        return maxPositive + maxNegative;
    }
}