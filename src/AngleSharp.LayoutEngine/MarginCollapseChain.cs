#pragma warning disable CS8618, CS9264
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a chain of adjacent margins that collapse together according to CSS specifications.
/// Margin collapsing occurs in three basic cases:
/// 1. Adjacent siblings' top and bottom margins
/// 2. Parent and first/last child's margins (when no border, padding, or clearance separates them)
/// 3. Empty blocks with no height, border, or padding, where top and bottom margins collapse together
/// </summary>
public class MarginCollapseChain
{
    /// <summary>
    /// Nodes participating in this margin chain.
    /// </summary>
    public List<LayoutNode> Nodes { get; set; } = new List<LayoutNode>();

    /// <summary>
    /// The calculated collapsed margin value.
    /// </summary>
    public float CollapsedMargin { get; set; }

    /// <summary>
    /// Whether this chain is for top margins (true) or bottom margins (false).
    /// </summary>
    public bool IsTopMargin { get; set; }

    /// <summary>
    /// The index of this chain in the parent formatting context.
    /// Used for tracking and ordering chains.
    /// </summary>
    public int ChainIndex { get; set; }

    /// <summary>
    /// Whether this chain represents an empty block where top and bottom margins collapse.
    /// </summary>
    public bool IsEmptyBlockChain { get; set; }

    /// <summary>
    /// Whether this is a parent-child margin chain.
    /// </summary>
    public bool IsParentChildChain { get; set; }

    /// <summary>
    /// Whether this is a sibling margin chain.
    /// </summary>
    public bool IsSiblingChain { get; set; }

    /// <summary>
    /// The parent node of this margin chain, if it's a parent-child chain.
    /// </summary>
    public LayoutNode ParentNode { get; set; }

    /// <summary>
    /// Indicates if this chain has been processed.
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Creates a new margin collapse chain.
    /// </summary>
    public MarginCollapseChain()
    {
    }

    /// <summary>
    /// Creates a new margin collapse chain with the specified properties.
    /// </summary>
    public MarginCollapseChain(bool isTopMargin, int chainIndex)
    {
        IsTopMargin = isTopMargin;
        ChainIndex = chainIndex;
    }

    /// <summary>
    /// Adds a node to this margin chain.
    /// </summary>
    public void AddNode(LayoutNode node)
    {
        if (!Nodes.Contains(node))
        {
            Nodes.Add(node);
        }
    }

    /// <summary>
    /// Calculates the collapsed margin value for all nodes in this chain.
    /// </summary>
    public void CalculateCollapsedMargin()
    {
        if (!Nodes.Any())
        {
            CollapsedMargin = 0;
            return;
        }

        // Collect all margins to collapse
        var margins = new List<float>();

        foreach (var node in Nodes)
        {
            if (node.Box == null) continue;

            // Add the appropriate margin based on chain type
            if (IsTopMargin)
            {
                margins.Add(node.Box.MarginTop);
            }
            else
            {
                margins.Add(node.Box.MarginBottom);
            }

            // For empty blocks, both top and bottom margins collapse together
            if (IsEmptyBlockChain)
            {
                margins.Add(IsTopMargin ? node.Box.MarginBottom : node.Box.MarginTop);
            }
        }

        // Apply CSS margin collapsing rules
        CollapsedMargin = CollapseMargins(margins);
    }

    /// <summary>
    /// Collapses multiple margins according to CSS rules:
    /// 1. If all margins are positive, use the largest.
    /// 2. If all margins are negative, use the most negative.
    /// 3. If some margins are positive and some negative, add the largest positive to the most negative.
    /// </summary>
    private float CollapseMargins(List<float> margins)
    {
        if (!margins.Any())
            return 0;

        float positiveMax = 0;
        float negativeMin = 0;

        foreach (var margin in margins)
        {
            if (margin > 0)
            {
                positiveMax = Math.Max(positiveMax, margin);
            }
            else if (margin < 0)
            {
                negativeMin = Math.Min(negativeMin, margin);
            }
        }

        // The final collapsed margin is the sum of the largest positive and the smallest negative margin
        return positiveMax + negativeMin;
    }

    /// <summary>
    /// Applies the calculated collapsed margin to all nodes in the chain.
    /// </summary>
    public void ApplyCollapsedMargin()
    {
        if (!Nodes.Any()) return;

        foreach (var node in Nodes)
        {
            if (node.Box == null) continue;

            // Apply the collapsed margin to the effective margin property
            if (IsTopMargin)
            {
                node.Box.EffectiveTopMargin = CollapsedMargin;
                node.Box.HasTopMarginCollapsed = true;
            }
            else
            {
                node.Box.EffectiveBottomMargin = CollapsedMargin;
                node.Box.HasBottomMarginCollapsed = true;
            }

            // Mark that this node participates in a margin collapsed chain
            node.Box.IsInMarginCollapsedChain = true;
        }
    }

    /// <summary>
    /// Checks if this chain contains the specified node.
    /// </summary>
    public bool ContainsNode(LayoutNode node)
    {
        return Nodes.Contains(node);
    }

    /// <summary>
    /// Checks if this chain is affected by changes to any of the specified nodes.
    /// </summary>
    public bool IsAffectedBy(IEnumerable<LayoutNode> changedNodes)
    {
        // Direct intersection - changed nodes in this chain
        if (Nodes.Any(changedNodes.Contains))
            return true;

        // Parent-child relationship - if parent changes, child margins may be affected
        if (IsParentChildChain && ParentNode != null && changedNodes.Contains(ParentNode))
            return true;

        // Adjacent node relationship - if node before or after changes
        if (IsSiblingChain && Nodes.Count > 0)
        {
            var firstNode = Nodes.First();
            var lastNode = Nodes.Last();

            // Get adjacent nodes
            var prevSibling = firstNode.GetPreviousSibling();
            var nextSibling = lastNode.GetNextSibling();

            // Check if adjacent nodes have changed
            if ((prevSibling != null && changedNodes.Contains(prevSibling)) ||
                (nextSibling != null && changedNodes.Contains(nextSibling)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a string representation of this margin chain.
    /// </summary>
    public override string ToString()
    {
        string chainType = IsEmptyBlockChain ? "Empty Block" :
                          IsParentChildChain ? "Parent-Child" :
                          IsSiblingChain ? "Sibling" : "General";

        string marginType = IsTopMargin ? "Top" : "Bottom";

        return $"{chainType} {marginType} Chain: {Nodes.Count} nodes, Margin={CollapsedMargin}";
    }
}