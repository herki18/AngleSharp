#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
namespace AngleSharp.LayoutEngine.Managers;

using System.Collections.Generic;
using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.FormattingContexts;

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