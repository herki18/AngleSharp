#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618, CS9264
namespace AngleSharp.LayoutEngine.DOM;

using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;

/// <summary>
/// Represents a render tree, which is a simplified representation of the DOM
/// that contains only the nodes that will participate in layout.
/// </summary>
public class RenderTree
{
    private readonly Dictionary<INode, IRenderNode> _domToRenderMap = new Dictionary<INode, IRenderNode>();
    private readonly HashSet<IRenderNode> _addedNodes = new HashSet<IRenderNode>();
    private readonly HashSet<IRenderNode> _removedNodes = new HashSet<IRenderNode>();
    private readonly HashSet<IRenderNode> _changedNodes = new HashSet<IRenderNode>();
    private RenderTree _previousTree;

    /// <summary>
    /// Gets the root node of the render tree.
    /// </summary>
    public IRenderNode Root { get; }

    /// <summary>
    /// Creates a new render tree with the specified root node.
    /// </summary>
    /// <param name="root">The root node of the render tree.</param>
    public RenderTree(IRenderNode root)
    {
        Root = root;
        BuildNodeMap(root);
    }

    /// <summary>
    /// Creates a new render tree with the specified root node and previous tree for change tracking.
    /// </summary>
    /// <param name="root">The root node of the render tree.</param>
    /// <param name="previousTree">The previous tree for change tracking.</param>
    public RenderTree(IRenderNode root, RenderTree previousTree)
    {
        Root = root;
        _previousTree = previousTree;
        BuildNodeMap(root);

        if (_previousTree != null)
        {
            TrackChanges();
        }
    }

    /// <summary>
    /// Builds a mapping from DOM nodes to render nodes for efficient lookup.
    /// </summary>
    /// <param name="node">The node to start building from.</param>
    private void BuildNodeMap(IRenderNode node)
    {
        if (node == null)
            return;

        // Map this node
        if (node.Ref != null)
        {
            _domToRenderMap[node.Ref] = node;
        }

        // Process children
        foreach (var child in node.Children)
        {
            BuildNodeMap(child);
        }
    }

    /// <summary>
    /// Finds a render node corresponding to a DOM node.
    /// </summary>
    /// <param name="domNode">The DOM node to find the render node for.</param>
    /// <returns>The corresponding render node, or null if not found.</returns>
    public IRenderNode FindNodeByDomNode(INode domNode)
    {
        if (domNode == null)
            return null;

        _domToRenderMap.TryGetValue(domNode, out var renderNode);
        return renderNode;
    }

    /// <summary>
    /// Finds a render node by its ID.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    /// <returns>The render node with the specified ID, or null if not found.</returns>
    public IRenderNode FindNodeById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return FindNodeById(Root, id);
    }

    /// <summary>
    /// Recursively searches for a node with the specified ID.
    /// </summary>
    /// <param name="node">The node to start searching from.</param>
    /// <param name="id">The ID to search for.</param>
    /// <returns>The node with the specified ID, or null if not found.</returns>
    private IRenderNode FindNodeById(IRenderNode node, string id)
    {
        if (node == null)
            return null;

        // Check if this node has the ID
        if (node.Id == id)
            return node;

        // Check children
        foreach (var child in node.Children)
        {
            var result = FindNodeById(child, id);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Gets all nodes in the render tree.
    /// </summary>
    /// <returns>An enumerable of all render nodes.</returns>
    public IEnumerable<IRenderNode> GetAllNodes()
    {
        if (Root == null)
            return Enumerable.Empty<IRenderNode>();

        return TraverseNodes(Root);
    }

    /// <summary>
    /// Traverses the render tree in pre-order.
    /// </summary>
    /// <param name="node">The node to start traversal from.</param>
    /// <returns>An enumerable of all nodes in the subtree.</returns>
    private IEnumerable<IRenderNode> TraverseNodes(IRenderNode node)
    {
        if (node == null)
            yield break;

        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in TraverseNodes(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets nodes that were added compared to the previous tree.
    /// </summary>
    /// <returns>An enumerable of added nodes.</returns>
    public IEnumerable<IRenderNode> GetAddedNodes()
    {
        return _addedNodes;
    }

    /// <summary>
    /// Gets nodes that were removed compared to the previous tree.
    /// </summary>
    /// <returns>An enumerable of removed nodes.</returns>
    public IEnumerable<IRenderNode> GetRemovedNodes()
    {
        return _removedNodes;
    }

    /// <summary>
    /// Gets nodes whose properties changed compared to the previous tree.
    /// </summary>
    /// <returns>An enumerable of changed nodes.</returns>
    public IEnumerable<IRenderNode> GetChangedNodes()
    {
        return _changedNodes;
    }

    /// <summary>
    /// Tracks changes between this tree and the previous tree.
    /// </summary>
    private void TrackChanges()
    {
        if (_previousTree == null)
            return;

        // Find added nodes
        foreach (var node in GetAllNodes())
        {
            if (node.Ref != null && _previousTree.FindNodeByDomNode(node.Ref) == null)
            {
                _addedNodes.Add(node);
            }
        }

        // Find removed nodes
        foreach (var node in _previousTree.GetAllNodes())
        {
            if (node.Ref != null && FindNodeByDomNode(node.Ref) == null)
            {
                _removedNodes.Add(node);
            }
        }

        // Find changed nodes (excluding added and removed)
        foreach (var node in GetAllNodes())
        {
            if (_addedNodes.Contains(node))
                continue;

            var previousNode = node.Ref != null ? _previousTree.FindNodeByDomNode(node.Ref) : null;
            if (previousNode != null && HasNodeChanged(previousNode, node))
            {
                _changedNodes.Add(node);
            }
        }
    }

    /// <summary>
    /// Determines if a node has changed compared to another node.
    /// </summary>
    /// <param name="oldNode">The old node.</param>
    /// <param name="newNode">The new node.</param>
    /// <returns>True if the node has changed, false otherwise.</returns>
    private bool HasNodeChanged(IRenderNode oldNode, IRenderNode newNode)
    {
        // Compare node types
        if (oldNode.GetType() != newNode.GetType())
            return true;

        // For elements, check style
        if (oldNode is ElementNode oldElement && newNode is ElementNode newElement)
        {
            // Check ID change
            if (oldElement.Id != newElement.Id)
                return true;

            // For a basic change detection, we'd check key properties
            // In a real implementation, this would be more comprehensive
            if (oldElement.SpecifiedStyle != newElement.SpecifiedStyle ||
                oldElement.ComputedStyle != newElement.ComputedStyle)
                return true;

            // Changes in class could affect styling
            var oldClasses = oldElement.Ref?.ClassName ?? string.Empty;
            var newClasses = newElement.Ref?.ClassName ?? string.Empty;
            if (oldClasses != newClasses)
                return true;
        }
        // For text nodes, check content
        else if (oldNode is DOM.TextNode oldText && newNode is DOM.TextNode newText)
        {
            var oldContent = oldText.Ref?.TextContent ?? string.Empty;
            var newContent = newText.Ref?.TextContent ?? string.Empty;
            if (oldContent != newContent)
                return true;
        }

        // No changes detected
        return false;
    }
}