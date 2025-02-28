#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implements a block formatting context according to the CSS specification.
/// A block formatting context is a region in which blocks are laid out, and in which floats interact with each other.
/// </summary>
public class BlockFormattingContext : FormattingContext
{
    private readonly IMarginManager _marginManager;

    // Stores the previous layout state for incremental updates
    private class LayoutState
    {
        public Dictionary<LayoutNode, Rect> NodePositions { get; } = new Dictionary<LayoutNode, Rect>();
        public float ContentHeight { get; set; }
    }

    private LayoutState _lastState = new LayoutState();

    public BlockFormattingContext(LayoutNode establishingNode, IMarginManager marginManager)
        : base(establishingNode)
    {
        _marginManager = marginManager;

        // Collect participants for this formatting context
        CollectParticipants(establishingNode);
    }

    /// <summary>
    /// Performs a full layout of all block elements in this formatting context.
    /// </summary>
    public override void Layout(LayoutContext context)
    {
        // Start with a clean slate
        _lastState = new LayoutState();

        // 1. Calculate widths for all block elements
        MeasureBlockWidths(context);

        // 2. Resolve margin collapsing
        ResolveMargins();

        // 3. Position all blocks in flow
        PositionBlocks(context);

        // 4. Update the establishing element's height
        UpdateEstablishingBoxHeight();

        // Save final state for incremental updates
        SaveLayoutState();
    }

    /// <summary>
    /// Performs an incremental layout update for changed elements.
    /// </summary>
    public override void Reflow(LayoutContext context)
    {
        // Identify which nodes are dirty
        var dirtyNodes = _participants.Where(p => p.IsDirty).ToList();

        if (!dirtyNodes.Any())
            return; // Nothing to do

        // Check if we need a full reflow
        if (NeedsFullReflow(dirtyNodes))
        {
            // Do a full layout pass
            Layout(context);
            return;
        }

        // Partial update - update sizes of changed nodes
        foreach (var node in dirtyNodes)
        {
            MeasureBlockWidth(node, context);
        }

        // Update margin collapsing for affected areas
        ResolveAffectedMargins(dirtyNodes);

        // Reposition affected nodes and those that follow
        RepositionAffectedBlocks(dirtyNodes, context);

        // Update the establishing element's height
        UpdateEstablishingBoxHeight();

        // Save the new state
        SaveLayoutState();
    }

    /// <summary>
    /// Determines if a full reflow is needed based on the changed nodes.
    /// </summary>
    private bool NeedsFullReflow(List<LayoutNode> dirtyNodes)
    {
        // If the establishing node is dirty, we need a full reflow
        if (dirtyNodes.Contains(EstablishingNode))
            return true;

        // If too many nodes are dirty, full reflow is more efficient
        if (dirtyNodes.Count > _participants.Count * 0.5)
            return true;

        // Check if any node has a width change or display change
        foreach (var node in dirtyNodes)
        {
            var element = node.DomNode as ElementNode;
            if (element?.ComputedStyle == null)
                continue;

            var oldWidth = _lastState.NodePositions.TryGetValue(node, out var oldRect) ? oldRect.Width : 0;
            if (Math.Abs((int)(node.Box.Width - oldWidth)) > 0.1f)
                return true;

            // Check if display type changed
            // Would need to compare old and new styles
        }

        return false;
    }

    /// <summary>
    /// Calculates widths for all block elements in the context.
    /// </summary>
    private void MeasureBlockWidths(LayoutContext context)
    {
        foreach (var participant in _participants)
        {
            MeasureBlockWidth(participant, context);
        }
    }

    /// <summary>
    /// Calculates the width for a single block element.
    /// </summary>
    private void MeasureBlockWidth(LayoutNode node, LayoutContext context)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        // Calculate available width for the node based on its containing block
        float availableWidth = CalculateAvailableWidth(node, context);

        // Use BoxModelCalculator to compute content width from style rules
        var calculator = new BoxModelCalculator(element.ComputedStyle, availableWidth);
        var boxValues = calculator.GetBoxValues();

        // Apply constraints if needed
        ApplySizeConstraints(node, boxValues);

        // Update the layout box
        node.Box.UpdateFromBoxValues(boxValues);
    }

    /// <summary>
    /// Calculates the available width for a node based on its containing block.
    /// </summary>
    private float CalculateAvailableWidth(LayoutNode node, LayoutContext context)
    {
        if (node == EstablishingNode)
        {
            // For the establishing element, use viewport width or parent width
            return context.ViewportWidth;
        }

        // Find the containing block (nearest block formatting context ancestor)
        var containingBlock = node.Parent;
        while (containingBlock != null && !containingBlock.CreatesFormattingContext && containingBlock != EstablishingNode)
        {
            containingBlock = containingBlock.Parent;
        }

        // Use the content width of the containing block
        return containingBlock?.Box.Width ?? context.ViewportWidth;
    }

    /// <summary>
    /// Applies min/max constraints to the calculated dimensions.
    /// </summary>
    private void ApplySizeConstraints(LayoutNode node, BoxValues boxValues)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        // Apply min-width if specified
        string minWidth = element.ComputedStyle.GetPropertyValue("min-width");
        if (!string.IsNullOrEmpty(minWidth) && minWidth != "0" && minWidth != "0px" && minWidth.EndsWith("px"))
        {
            if (float.TryParse(minWidth.TrimEnd('p', 'x'), out float minWidthPx))
            {
                boxValues.ContentWidth = Math.Max(boxValues.ContentWidth, minWidthPx);
            }
        }

        // Apply max-width if specified
        string maxWidth = element.ComputedStyle.GetPropertyValue("max-width");
        if (!string.IsNullOrEmpty(maxWidth) && maxWidth != "none" && maxWidth.EndsWith("px"))
        {
            if (float.TryParse(maxWidth.TrimEnd('p', 'x'), out float maxWidthPx))
            {
                boxValues.ContentWidth = Math.Min(boxValues.ContentWidth, maxWidthPx);
            }
        }
    }

    /// <summary>
    /// Resolves margin collapsing for all blocks in the formatting context.
    /// </summary>
    private void ResolveMargins()
    {
        // Use the margin manager to process all nodes in this context
        _marginManager.ProcessFormattingContext(this, _participants);
    }

    /// <summary>
    /// Resolves margin collapsing for specific affected areas.
    /// </summary>
    private void ResolveAffectedMargins(List<LayoutNode> changedNodes)
    {
        // Find all margin chains affected by the changed nodes
        var affectedChains = _marginManager.FindAffectedMarginChains(changedNodes, _participants);

        // Resolve each chain
        foreach (var chain in affectedChains)
        {
            _marginManager.ResolveMarginChain(chain);
        }
    }

    /// <summary>
    /// Positions all blocks in the formatting context.
    /// </summary>
    private void PositionBlocks(LayoutContext context)
    {
        // Set the initial position of the establishing box
        EstablishingNode.Box.X = 0;
        EstablishingNode.Box.Y = 0;

        // Track the current Y position as we lay out children
        float currentY = EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop;
        LayoutNode previousNode = null;

        // Get direct children only - each nested formatting context will handle its own children
        var directChildren = GetDirectFormattingChildren();

        // Position each child
        foreach (var child in directChildren)
        {
            PositionBlock(child, previousNode, ref currentY, context);
            previousNode = child;
        }
    }

    /// <summary>
    /// Positions an individual block in the flow.
    /// </summary>
    private void PositionBlock(LayoutNode node, LayoutNode previousNode, ref float currentY, LayoutContext context)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        // Calculate the horizontal position
        float x = EstablishingNode.Box.X + EstablishingNode.Box.BorderLeft + EstablishingNode.Box.PaddingLeft;

        // Handle margin-left: auto, margin-right: auto for centering
        HandleAutoMargins(node, x, EstablishingNode.Box.Width);

        // Apply margin-left to the position
        x += node.Box.MarginLeft;

        // Calculate vertical position with margin collapsing
        float y = currentY;

        // If there is a previous sibling, apply margin collapsing
        if (previousNode != null)
        {
            // The effective collapsed margin should already be calculated by the margin manager
            // We just need to apply it
            float collapsedMargin = node.Box.EffectiveTopMargin;
            y += collapsedMargin;
        }

        // Set the final position
        node.Box.X = x;
        node.Box.Y = y;

        // If this node establishes its own formatting context, lay it out
        if (node.FormattingContext != null)
        {
            var childContext = context.CreateChildContext(node);
            node.FormattingContext.Layout(childContext);
        }

        // Update currentY for the next element
        currentY = node.Box.Y + node.Box.Height + node.Box.PaddingBottom + node.Box.BorderBottom;

        // For auto-height elements, we need to adjust the height based on children
        // if (float.IsNaN(element.ComputedStyle.GetProperty("height")?.RawValue as float? ?? float.NaN))
        // {
        //     // Height would be determined by content
        // }
    }

    /// <summary>
    /// Handles auto margins for horizontal centering.
    /// </summary>
    private void HandleAutoMargins(LayoutNode node, float containerX, float containerWidth)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        string marginLeft = element.ComputedStyle.GetPropertyValue("margin-left");
        string marginRight = element.ComputedStyle.GetPropertyValue("margin-right");

        bool isMarginLeftAuto = marginLeft == "auto";
        bool isMarginRightAuto = marginRight == "auto";

        // If both margins are auto, center the element
        if (isMarginLeftAuto && isMarginRightAuto)
        {
            float availableSpace = containerWidth - node.Box.BorderBoxWidth;
            float margin = Math.Max(0, availableSpace / 2);

            node.Box.MarginLeft = margin;
            node.Box.MarginRight = margin;
        }
        // If only margin-left is auto, it gets the available space
        else if (isMarginLeftAuto)
        {
            float availableSpace = containerWidth - node.Box.BorderBoxWidth - node.Box.MarginRight;
            node.Box.MarginLeft = Math.Max(0, availableSpace);
        }
        // If only margin-right is auto, it gets the available space
        else if (isMarginRightAuto)
        {
            float availableSpace = containerWidth - node.Box.BorderBoxWidth - node.Box.MarginLeft;
            node.Box.MarginRight = Math.Max(0, availableSpace);
        }
    }

    /// <summary>
    /// Repositions blocks affected by changes and all blocks that follow them.
    /// </summary>
    private void RepositionAffectedBlocks(List<LayoutNode> changedNodes, LayoutContext context)
    {
        // Find the first dirty node in document order
        var orderedParticipants = _participants.OrderBy(p => GetNodeDepthFirstIndex(p)).ToList();
        var firstDirtyIndex = -1;

        for (int i = 0; i < orderedParticipants.Count; i++)
        {
            if (changedNodes.Contains(orderedParticipants[i]))
            {
                firstDirtyIndex = i;
                break;
            }
        }

        if (firstDirtyIndex == -1)
            return; // No dirty nodes found

        // Get the starting Y position
        float currentY;
        LayoutNode previousNode = null;

        if (firstDirtyIndex > 0)
        {
            // Start after the last unchanged node
            previousNode = orderedParticipants[firstDirtyIndex - 1];
            currentY = previousNode.Box.Y + previousNode.Box.Height +
                     previousNode.Box.PaddingBottom + previousNode.Box.BorderBottom;
        }
        else
        {
            // Start at the beginning
            currentY = EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop;
        }

        // Reposition each affected node
        for (int i = firstDirtyIndex; i < orderedParticipants.Count; i++)
        {
            PositionBlock(orderedParticipants[i], previousNode, ref currentY, context);
            previousNode = orderedParticipants[i];
        }
    }

    /// <summary>
    /// Updates the height of the establishing element to contain all children.
    /// </summary>
    private void UpdateEstablishingBoxHeight()
    {
        // Find the bottom-most point of all children
        float maxY = EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop;

        foreach (var participant in _participants)
        {
            if (participant == EstablishingNode)
                continue;

            float bottom = participant.Box.Y + participant.Box.Height +
                          participant.Box.PaddingBottom + participant.Box.BorderBottom;

            if (bottom > maxY)
                maxY = bottom;
        }

        // Calculate the required height
        float requiredHeight = maxY - (EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop);

        // Do not shrink an explicit height, only expand it if needed
        var element = EstablishingNode.DomNode as ElementNode;
        if (element?.ComputedStyle == null ||
            element.ComputedStyle.GetPropertyValue("height") == "auto" ||
            float.IsNaN(EstablishingNode.Box.Height))
        {
            EstablishingNode.Box.Height = requiredHeight;
        }
        else if (requiredHeight > EstablishingNode.Box.Height)
        {
            // Expand height if content overflows an explicit height
            EstablishingNode.Box.Height = requiredHeight;
        }
    }

    /// <summary>
    /// Saves the current layout state for future incremental updates.
    /// </summary>
    private void SaveLayoutState()
    {
        _lastState = new LayoutState();

        // Save positions of all nodes
        foreach (var participant in _participants)
        {
            _lastState.NodePositions[participant] = new Rect(
                participant.Box.X,
                participant.Box.Y,
                participant.Box.Width,
                participant.Box.Height
            );
        }

        // Save the content height
        _lastState.ContentHeight = EstablishingNode.Box.Height;
    }

    /// <summary>
    /// Gets the direct children that are part of this formatting context.
    /// </summary>
    private List<LayoutNode> GetDirectFormattingChildren()
    {
        var result = new List<LayoutNode>();

        foreach (var child in EstablishingNode.Children)
        {
            // Skip non-renderable nodes
            if (child.DomNode is NonRenderableNode)
                continue;

            // Add this child
            result.Add(child);
        }

        return result;
    }

    /// <summary>
    /// Gets a depth-first traversal index for a node.
    /// </summary>
    private int GetNodeDepthFirstIndex(LayoutNode node)
    {
        // This could be optimized by pre-computing indices
        var allNodes = new List<LayoutNode>();
        CollectNodesDepthFirst(EstablishingNode, allNodes);
        return allNodes.IndexOf(node);
    }

    /// <summary>
    /// Collects nodes in depth-first order.
    /// </summary>
    private void CollectNodesDepthFirst(LayoutNode node, List<LayoutNode> result)
    {
        result.Add(node);

        foreach (var child in node.Children)
        {
            CollectNodesDepthFirst(child, result);
        }
    }
}