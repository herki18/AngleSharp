#pragma warning disable CS8600, CS8602, CS8603, CS8625
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks dirty nodes and propagates layout invalidation to efficiently handle incremental updates.
/// This component is crucial for optimizing reflows by helping the system determine
/// what needs to be recalculated when styles or the DOM changes.
/// </summary>
public class LayoutObserver
{
    private readonly HashSet<LayoutNode> _dirtyNodes = new HashSet<LayoutNode>();
    private readonly HashSet<LayoutNode> _dirtySubtrees = new HashSet<LayoutNode>();
    private readonly HashSet<LayoutNode> _dirtyAncestors = new HashSet<LayoutNode>();

    /// <summary>
    /// Gets whether any nodes are marked as dirty and need layout.
    /// </summary>
    public bool HasDirtyNodes => _dirtyNodes.Count > 0 || _dirtySubtrees.Count > 0 || _dirtyAncestors.Count > 0;

    /// <summary>
    /// Marks the specified node as dirty, requiring its layout to be recalculated.
    /// </summary>
    /// <param name="node">The node to mark as dirty.</param>
    public void MarkDirty(LayoutNode node)
    {
        if (node == null)
            return;

        // Mark this node as needing layout
        _dirtyNodes.Add(node);
        node.IsDirty = true;
    }

    /// <summary>
    /// Marks the specified node and all its descendants as dirty.
    /// </summary>
    /// <param name="node">The root node of the subtree to mark as dirty.</param>
    public void MarkSubtreeDirty(LayoutNode node)
    {
        if (node == null)
            return;

        // Mark this node as needing a subtree layout
        _dirtySubtrees.Add(node);
        node.IsDirty = true;

        // Also mark all descendants as dirty directly
        foreach (var descendant in GetAllDescendants(node))
        {
            _dirtyNodes.Add(descendant);
            descendant.IsDirty = true;
        }
    }

    /// <summary>
    /// Marks the specified nodes as dirty and propagates the change to affected ancestors.
    /// </summary>
    /// <param name="nodes">The nodes to mark as dirty.</param>
    public void MarkDirtyNodes(IEnumerable<LayoutNode> nodes)
    {
        foreach (var node in nodes)
        {
            MarkDirty(node);
        }

        PropagateToAncestors(nodes);
    }

    /// <summary>
    /// Propagates dirtiness up the tree to affected ancestors.
    /// </summary>
    /// <param name="dirtyNodes">The initially dirty nodes.</param>
    public void PropagateToAncestors(IEnumerable<LayoutNode> dirtyNodes)
    {
        foreach (var node in dirtyNodes)
        {
            PropagateToAncestors(node);
        }
    }

    /// <summary>
    /// Propagates dirtiness up the tree to affected ancestors for a single node.
    /// </summary>
    /// <param name="node">The dirty node.</param>
    public void PropagateToAncestors(LayoutNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            // If this ancestor is already in our list, we can stop propagating
            if (_dirtyAncestors.Contains(current))
                break;

            // Mark as an affected ancestor
            _dirtyAncestors.Add(current);
            current.IsDirty = true;

            // Continue up the tree
            current = current.Parent;
        }
    }

    /// <summary>
    /// Clears the dirty state of all nodes after a layout pass.
    /// </summary>
    public void ClearDirty()
    {
        // Clear the dirty flag on all nodes
        foreach (var node in _dirtyNodes)
        {
            node.IsDirty = false;
        }

        foreach (var node in _dirtySubtrees)
        {
            node.IsDirty = false;

            // Clear flags for all descendants
            foreach (var descendant in GetAllDescendants(node))
            {
                descendant.IsDirty = false;
            }
        }

        foreach (var node in _dirtyAncestors)
        {
            node.IsDirty = false;
        }

        // Clear the collections
        _dirtyNodes.Clear();
        _dirtySubtrees.Clear();
        _dirtyAncestors.Clear();
    }

    /// <summary>
    /// Gets all dirty nodes that need their layout recalculated.
    /// </summary>
    /// <returns>A collection of dirty nodes.</returns>
    public IReadOnlyList<LayoutNode> GetDirtyNodes()
    {
        var result = new HashSet<LayoutNode>();

        // Add all individually marked nodes
        foreach (var node in _dirtyNodes)
        {
            result.Add(node);
        }

        // Add all nodes in dirty subtrees
        foreach (var subtree in _dirtySubtrees)
        {
            result.Add(subtree);
            foreach (var descendant in GetAllDescendants(subtree))
            {
                result.Add(descendant);
            }
        }

        // Add all affected ancestors
        foreach (var ancestor in _dirtyAncestors)
        {
            result.Add(ancestor);
        }

        return result.ToList();
    }

    /// <summary>
    /// Determines if a node is directly marked as dirty.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is directly marked as dirty, false otherwise.</returns>
    public bool IsDirectlyDirty(LayoutNode node)
    {
        return _dirtyNodes.Contains(node);
    }

    /// <summary>
    /// Determines if a node is part of a dirty subtree.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is part of a dirty subtree, false otherwise.</returns>
    public bool IsInDirtySubtree(LayoutNode node)
    {
        // Check if the node is in any dirty subtree
        return _dirtySubtrees.Any(subtreeRoot =>
            subtreeRoot == node || IsDescendantOf(node, subtreeRoot));
    }

    /// <summary>
    /// Determines if a node is a dirty ancestor.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is a dirty ancestor, false otherwise.</returns>
    public bool IsDirtyAncestor(LayoutNode node)
    {
        return _dirtyAncestors.Contains(node);
    }

    /// <summary>
    /// Gets all descendants of a node.
    /// </summary>
    private IEnumerable<LayoutNode> GetAllDescendants(LayoutNode node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var descendant in GetAllDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Determines if a node is a descendant of another node.
    /// </summary>
    private bool IsDescendantOf(LayoutNode descendant, LayoutNode ancestor)
    {
        var current = descendant.Parent;
        while (current != null)
        {
            if (current == ancestor)
                return true;
            current = current.Parent;
        }
        return false;
    }
}