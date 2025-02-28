#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;



/// <summary>
/// Interface for margin management operations.
/// </summary>
public interface IMarginManager
{
    /// <summary>
    /// Processes margin collapsing for all nodes in a formatting context.
    /// </summary>
    void ProcessFormattingContext(IFormattingContext context, IEnumerable<LayoutNode> participants);

    /// <summary>
    /// Finds margin chains affected by changes to the specified nodes.
    /// </summary>
    IEnumerable<MarginCollapseChain> FindAffectedMarginChains(IEnumerable<LayoutNode> changedNodes, IEnumerable<LayoutNode> allNodes);

    /// <summary>
    /// Resolves margin collapsing for a chain of adjacent margins.
    /// </summary>
    void ResolveMarginChain(MarginCollapseChain chain);

    /// <summary>
    /// Collapses two adjacent margins according to CSS rules.
    /// </summary>
    float CollapseMargins(float marginA, float marginB);
}



/// <summary>
/// Implementation of the margin manager that handles margin collapsing according to CSS specifications.
/// </summary>
public class MarginManager : IMarginManager
{
    /// <summary>
    /// Processes margin collapsing for all participating nodes in a formatting context.
    /// </summary>
    public void ProcessFormattingContext(IFormattingContext context, IEnumerable<LayoutNode> participants)
    {
        // We'll use depth-first ordering to process nodes in document order
        var orderedNodes = OrderNodesByDepthFirst(participants, context.EstablishingNode);

        // Process parent-child top margin collapse chains
        ProcessParentChildTopMargins(orderedNodes);

        // Process sibling margin collapse chains
        ProcessSiblingMargins(orderedNodes);

        // Process empty block margin collapses
        ProcessEmptyBlockMargins(orderedNodes);

        // Process parent-child bottom margin collapse chains
        ProcessParentChildBottomMargins(orderedNodes);
    }

    /// <summary>
    /// Finds margin chains affected by changes to the specified nodes.
    /// </summary>
    public IEnumerable<MarginCollapseChain> FindAffectedMarginChains(IEnumerable<LayoutNode> changedNodes, IEnumerable<LayoutNode> allNodes)
    {
        var result = new List<MarginCollapseChain>();
        var affectedNodes = new HashSet<LayoutNode>(changedNodes);

        // Add ancestors of changed nodes since they can be part of margin chains
        foreach (var node in changedNodes)
        {
            var current = node.Parent;
            while (current != null)
            {
                affectedNodes.Add(current);
                current = current.Parent;
            }
        }

        // Add siblings of changed nodes since they can be part of margin chains
        foreach (var node in changedNodes)
        {
            if (node.Parent != null)
            {
                foreach (var sibling in node.Parent.Children.OfType<LayoutNode>())
                {
                    if (sibling != node)
                    {
                        affectedNodes.Add(sibling);
                    }
                }
            }
        }

        // Find top margin chains
        foreach (var node in affectedNodes)
        {
            var topChain = AnalyzeTopMarginChain(node);
            if (topChain != null && topChain.Nodes.Count > 0)
            {
                result.Add(topChain);
            }
        }

        // Find bottom margin chains
        foreach (var node in affectedNodes)
        {
            var bottomChain = AnalyzeBottomMarginChain(node);
            if (bottomChain != null && bottomChain.Nodes.Count > 0)
            {
                result.Add(bottomChain);
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves margin collapsing for a chain of adjacent margins.
    /// </summary>
    public void ResolveMarginChain(MarginCollapseChain chain)
    {
        if (chain.Nodes.Count == 0)
            return;

        // Collect all margins in the chain
        var margins = new List<float>();
        foreach (var node in chain.Nodes)
        {
            if (chain.IsTopMargin)
            {
                margins.Add(node.Box.MarginTop);
            }
            else
            {
                margins.Add(node.Box.MarginBottom);
            }
        }

        // Calculate the collapsed margin value
        chain.CollapsedMargin = CollapseMarginList(margins);

        // Apply the collapsed margin to all nodes in the chain
        foreach (var node in chain.Nodes)
        {
            if (chain.IsTopMargin)
            {
                node.Box.EffectiveTopMargin = chain.CollapsedMargin;
                node.Box.HasTopMarginCollapsed = true;

                // For first child in parent-child chain, mark it as part of a chain
                if (node == chain.Nodes[0])
                {
                    node.Box.IsInMarginCollapsedChain = true;
                }
            }
            else
            {
                node.Box.EffectiveBottomMargin = chain.CollapsedMargin;
                node.Box.HasBottomMarginCollapsed = true;

                // For last child in parent-child chain, mark it as part of a chain
                if (node == chain.Nodes[chain.Nodes.Count - 1])
                {
                    node.Box.IsInMarginCollapsedChain = true;
                }
            }
        }
    }

    /// <summary>
    /// Collapses two adjacent margins according to CSS rules.
    /// </summary>
    public float CollapseMargins(float marginA, float marginB)
    {
        // If both margins are positive, use the largest
        if (marginA >= 0 && marginB >= 0)
            return Math.Max(marginA, marginB);

        // If both margins are negative, use the most negative
        if (marginA <= 0 && marginB <= 0)
            return Math.Min(marginA, marginB);

        // If margins have different signs, add them together
        return marginA + marginB;
    }

    #region Private Helper Methods

    /// <summary>
    /// Collapses a list of margins into a single margin value.
    /// </summary>
    private float CollapseMarginList(List<float> margins)
    {
        if (margins.Count == 0)
            return 0;

        // Find largest positive margin
        float maxPositive = 0;
        foreach (var margin in margins)
        {
            if (margin > 0 && margin > maxPositive)
            {
                maxPositive = margin;
            }
        }

        // Find most negative margin
        float minNegative = 0;
        foreach (var margin in margins)
        {
            if (margin < 0 && margin < minNegative)
            {
                minNegative = margin;
            }
        }

        // Return sum of largest positive and most negative
        return maxPositive + minNegative;
    }

    /// <summary>
    /// Orders nodes by depth-first traversal starting from the root.
    /// </summary>
    private List<LayoutNode> OrderNodesByDepthFirst(IEnumerable<LayoutNode> nodes, LayoutNode root)
    {
        var result = new List<LayoutNode>();
        var visited = new HashSet<LayoutNode>();

        // Add nodes in depth-first order
        DepthFirstTraversal(root, result, visited, nodes.ToHashSet());

        return result;
    }

    /// <summary>
    /// Performs a depth-first traversal of the layout tree.
    /// </summary>
    private void DepthFirstTraversal(LayoutNode node, List<LayoutNode> result, HashSet<LayoutNode> visited, HashSet<LayoutNode> includedNodes)
    {
        if (node == null || visited.Contains(node))
            return;

        visited.Add(node);

        if (includedNodes.Contains(node))
        {
            result.Add(node);
        }

        foreach (var child in node.Children.OfType<LayoutNode>())
        {
            DepthFirstTraversal(child, result, visited, includedNodes);
        }
    }

    /// <summary>
    /// Processes parent-child top margin collapsing.
    /// </summary>
    private void ProcessParentChildTopMargins(List<LayoutNode> orderedNodes)
    {
        var processedNodes = new HashSet<LayoutNode>();

        foreach (var node in orderedNodes)
        {
            if (processedNodes.Contains(node))
                continue;

            var chain = AnalyzeTopMarginChain(node);
            if (chain != null && chain.Nodes.Count > 0)
            {
                ResolveMarginChain(chain);

                foreach (var chainNode in chain.Nodes)
                {
                    processedNodes.Add(chainNode);
                }
            }
        }
    }

    /// <summary>
    /// Processes sibling margin collapsing.
    /// </summary>
    private void ProcessSiblingMargins(List<LayoutNode> orderedNodes)
    {
        // Group nodes by parent
        var nodesByParent = orderedNodes.GroupBy(n => n.Parent);

        foreach (var group in nodesByParent)
        {
            var parent = group.Key;
            if (parent == null)
                continue;

            // Process each consecutive pair of siblings
            LayoutNode previousSibling = null;

            foreach (var node in group.OrderBy(n => parent.Children.IndexOf(n)))
            {
                if (previousSibling != null)
                {
                    // Create and resolve a sibling margin chain
                    var chain = new MarginCollapseChain
                    {
                        IsTopMargin = false,
                        Nodes = new List<LayoutNode> { previousSibling, node }
                    };

                    ResolveMarginChain(chain);
                }

                previousSibling = node;
            }
        }
    }

    /// <summary>
    /// Processes empty block margin collapsing.
    /// </summary>
    private void ProcessEmptyBlockMargins(List<LayoutNode> orderedNodes)
    {
        foreach (var node in orderedNodes)
        {
            if (IsEmptyBlock(node))
            {
                // For empty blocks, top and bottom margins collapse
                var chain = new MarginCollapseChain
                {
                    IsTopMargin = true,
                    Nodes = new List<LayoutNode> { node }
                };

                // Special case: use both top and bottom margins
                var margins = new List<float> { node.Box.MarginTop, node.Box.MarginBottom };
                chain.CollapsedMargin = CollapseMarginList(margins);

                // Apply to both top and bottom margins
                node.Box.EffectiveTopMargin = chain.CollapsedMargin;
                node.Box.EffectiveBottomMargin = chain.CollapsedMargin;
                node.Box.HasTopMarginCollapsed = true;
                node.Box.HasBottomMarginCollapsed = true;
                node.Box.IsInMarginCollapsedChain = true;
            }
        }
    }

    /// <summary>
    /// Processes parent-child bottom margin collapsing.
    /// </summary>
    private void ProcessParentChildBottomMargins(List<LayoutNode> orderedNodes)
    {
        var processedNodes = new HashSet<LayoutNode>();

        foreach (var node in orderedNodes)
        {
            if (processedNodes.Contains(node))
                continue;

            var chain = AnalyzeBottomMarginChain(node);
            if (chain != null && chain.Nodes.Count > 0)
            {
                ResolveMarginChain(chain);

                foreach (var chainNode in chain.Nodes)
                {
                    processedNodes.Add(chainNode);
                }
            }
        }
    }

    /// <summary>
    /// Analyzes a top margin chain starting from the specified node.
    /// </summary>
    private MarginCollapseChain AnalyzeTopMarginChain(LayoutNode node)
    {
        var chain = new MarginCollapseChain
        {
            IsTopMargin = true,
            Nodes = new List<LayoutNode>()
        };

        if (!CanCollapseWithParentTop(node))
            return chain;

        // Add the current node to the chain
        chain.Nodes.Add(node);

        // Check if parent can collapse its top margin with this node
        var parent = node.Parent;
        if (parent != null && CanCollapseWithParentTop(node))
        {
            // Check if this is the first in-flow child
            if (IsFirstInFlowChild(parent, node))
            {
                // Add parent to the chain
                chain.Nodes.Add(parent);

                // Recursively check for further parent-child collapsing
                var grandparent = parent.Parent;
                if (grandparent != null && CanCollapseWithParentTop(parent) && IsFirstInFlowChild(grandparent, parent))
                {
                    // Recursively find more ancestors
                    var ancestorChain = AnalyzeTopMarginChain(parent);
                    if (ancestorChain.Nodes.Count > 1)
                    {
                        // Add ancestors to our chain, excluding the parent which we already added
                        for (int i = 1; i < ancestorChain.Nodes.Count; i++)
                        {
                            chain.Nodes.Add(ancestorChain.Nodes[i]);
                        }
                    }
                }
            }
        }

        return chain;
    }

    /// <summary>
    /// Analyzes a bottom margin chain starting from the specified node.
    /// </summary>
    private MarginCollapseChain AnalyzeBottomMarginChain(LayoutNode node)
    {
        var chain = new MarginCollapseChain
        {
            IsTopMargin = false,
            Nodes = new List<LayoutNode>()
        };

        if (!CanCollapseWithParentBottom(node))
            return chain;

        // Add the current node to the chain
        chain.Nodes.Add(node);

        // Check if parent can collapse its bottom margin with this node
        var parent = node.Parent;
        if (parent != null && CanCollapseWithParentBottom(node))
        {
            // Check if this is the last in-flow child
            if (IsLastInFlowChild(parent, node))
            {
                // Add parent to the chain
                chain.Nodes.Add(parent);

                // Recursively check for further parent-child collapsing
                var grandparent = parent.Parent;
                if (grandparent != null && CanCollapseWithParentBottom(parent) && IsLastInFlowChild(grandparent, parent))
                {
                    // Recursively find more ancestors
                    var ancestorChain = AnalyzeBottomMarginChain(parent);
                    if (ancestorChain.Nodes.Count > 1)
                    {
                        // Add ancestors to our chain, excluding the parent which we already added
                        for (int i = 1; i < ancestorChain.Nodes.Count; i++)
                        {
                            chain.Nodes.Add(ancestorChain.Nodes[i]);
                        }
                    }
                }
            }
        }

        return chain;
    }

    /// <summary>
    /// Checks if a node can collapse its top margin with its parent's top margin.
    /// </summary>
    private bool CanCollapseWithParentTop(LayoutNode node)
    {
        if (node.Parent == null)
            return false;

        // Check if node has border or padding at the top
        if (node.Box.BorderTop > 0 || node.Box.PaddingTop > 0)
            return false;

        // Check if parent has border or padding at the top
        if (node.Parent.Box.BorderTop > 0 || node.Parent.Box.PaddingTop > 0)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a node can collapse its bottom margin with its parent's bottom margin.
    /// </summary>
    private bool CanCollapseWithParentBottom(LayoutNode node)
    {
        if (node.Parent == null)
            return false;

        // Check if node has border or padding at the bottom
        if (node.Box.BorderBottom > 0 || node.Box.PaddingBottom > 0)
            return false;

        // Check if parent has border or padding at the bottom
        if (node.Parent.Box.BorderBottom > 0 || node.Parent.Box.PaddingBottom > 0)
            return false;

        return true;
    }

    /// <summary>
    /// Determines if a node is the first in-flow child of its parent.
    /// </summary>
    private bool IsFirstInFlowChild(LayoutNode parent, LayoutNode child)
    {
        foreach (var sibling in parent.Children.OfType<LayoutNode>())
        {
            // Skip non-rendered nodes
            if (sibling.DomNode is NonRenderableNode)
                continue;

            // Skip out-of-flow elements
            if (IsOutOfFlow(sibling))
                continue;

            // Found the first in-flow child
            return sibling == child;
        }

        return false;
    }

    /// <summary>
    /// Determines if a node is the last in-flow child of its parent.
    /// </summary>
    private bool IsLastInFlowChild(LayoutNode parent, LayoutNode child)
    {
        LayoutNode lastInFlow = null;

        foreach (var sibling in parent.Children.OfType<LayoutNode>())
        {
            // Skip non-rendered nodes
            if (sibling.DomNode is NonRenderableNode)
                continue;

            // Skip out-of-flow elements
            if (IsOutOfFlow(sibling))
                continue;

            lastInFlow = sibling;
        }

        return lastInFlow == child;
    }

    /// <summary>
    /// Determines if a node is out of flow.
    /// </summary>
    private bool IsOutOfFlow(LayoutNode node)
    {
        // Check position
        if (node.Position == PositionType.Absolute || node.Position == PositionType.Fixed)
            return true;

        // Check float
        if (node.Float != FloatType.None)
            return true;

        return false;
    }

    /// <summary>
    /// Determines if a node is an empty block that participates in margin collapsing.
    /// </summary>
    private bool IsEmptyBlock(LayoutNode node)
    {
        // Node must be a block
        if (node.Display != DisplayType.Block)
            return false;

        // Check if it has in-flow children
        bool hasInFlowContent = node.Children.Any(child =>
            !(child.DomNode is NonRenderableNode) && !IsOutOfFlow(child));

        if (hasInFlowContent)
            return false;

        // Check for padding or border
        if (node.Box.PaddingTop > 0 || node.Box.PaddingBottom > 0 ||
            node.Box.BorderTop > 0 || node.Box.BorderBottom > 0)
            return false;

        // Check for height
        if (node.Height is StyleLengthValue length && length.Value > 0)
            return false;

        return true;
    }

    #endregion
}