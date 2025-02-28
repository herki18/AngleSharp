namespace AngleSharp.LayoutEngine.Core;

using System.Collections.Generic;
using System.Linq;
using AngleSharp.LayoutEngine.Box;
using DOM;
using FormattingContexts;
using FormattingContexts.Enums;

#pragma warning disable CS8618, CS9264, CS8603
/// <summary>
/// A node in the layout tree, containing only layout information.
/// </summary>
public class LayoutNode
{
    /// <summary>
    /// The corresponding DOM node.
    /// </summary>
    public IRenderNode DomNode { get; }

    /// <summary>
    /// The parent layout node.
    /// </summary>
    public LayoutNode Parent { get; set; } = null!;

    /// <summary>
    /// Child layout nodes.
    /// </summary>
    public List<LayoutNode> Children { get; } = new();

    /// <summary>
    /// The layout box containing position and size information.
    /// </summary>
    public LayoutBox Box { get; } = new LayoutBox();

    /// <summary>
    /// The formatting context this node establishes, if any.
    /// </summary>
    public IFormattingContext FormattingContext { get; set; }

    /// <summary>
    /// Whether this node creates its own formatting context.
    /// </summary>
    public bool CreatesFormattingContext { get; set; }

    /// <summary>
    /// Whether this node needs layout due to changes.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Gets the node's ID for identification in the tree.
    /// </summary>
    public string Id => (DomNode as ElementNode)?.Id!;

    /// <summary>
    /// Gets or sets the element's display type.
    /// </summary>
    public DisplayType Display { get; set; } = DisplayType.Inline;

    /// <summary>
    /// Gets or sets the element's position type.
    /// </summary>
    public PositionType Position { get; set; } = PositionType.Static;

    /// <summary>
    /// Gets or sets the element's float value.
    /// </summary>
    public FloatType Float { get; set; } = FloatType.None;

    /// <summary>
    /// Gets or sets the element's computed width.
    /// </summary>
    public StyleValue Width { get; set; } = StyleValue.Auto;

    /// <summary>
    /// Gets or sets the element's computed height.
    /// </summary>
    public StyleValue Height { get; set; } = StyleValue.Auto;

    /// <summary>
    /// Creates a new layout node for the specified DOM node.
    /// </summary>
    public LayoutNode(IRenderNode domNode)
    {
        DomNode = domNode;
    }

    /// <summary>
    /// Gets all descendant nodes (not including this node).
    /// </summary>
    public IEnumerable<LayoutNode> GetDescendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets all previous siblings of this node.
    /// </summary>
    public IEnumerable<LayoutNode> GetPreviousSiblings()
    {
        if (Parent == null) return Enumerable.Empty<LayoutNode>();

        int myIndex = Parent.Children.IndexOf(this);
        if (myIndex <= 0) return Enumerable.Empty<LayoutNode>();

        return Parent.Children.Take(myIndex);
    }

    /// <summary>
    /// Gets the previous sibling node, if any.
    /// </summary>
    public LayoutNode GetPreviousSibling()
    {
        if (Parent == null) return null;

        int myIndex = Parent.Children.IndexOf(this);
        if (myIndex <= 0) return null;

        return Parent.Children[myIndex - 1];
    }

    /// <summary>
    /// Gets all next siblings of this node.
    /// </summary>
    public IEnumerable<LayoutNode> GetNextSiblings()
    {
        if (Parent == null) return Enumerable.Empty<LayoutNode>();

        int myIndex = Parent.Children.IndexOf(this);
        if (myIndex < 0 || myIndex >= Parent.Children.Count - 1)
            return Enumerable.Empty<LayoutNode>();

        return Parent.Children.Skip(myIndex + 1);
    }

    /// <summary>
    /// Gets the next sibling node, if any.
    /// </summary>
    public LayoutNode GetNextSibling()
    {
        if (Parent == null) return null;

        int myIndex = Parent.Children.IndexOf(this);
        if (myIndex < 0 || myIndex >= Parent.Children.Count - 1)
            return null;

        return Parent.Children[myIndex + 1];
    }

    /// <summary>
    /// Determines if this node is an empty block that participates in margin collapsing.
    /// </summary>
    public bool IsEmptyBlock()
    {
        // An empty block has no in-flow children, no height, and no padding/border
        if (Display != DisplayType.Block) return false;

        // Check if we have any rendered children
        if (Children.Any(c => !(c.DomNode is NonRenderableNode))) return false;

        // Check if the element has explicit height
        if (Height is StyleLengthValue length && length.Value > 0) return false;

        // Check for padding/border
        if (Box.PaddingTop > 0 || Box.PaddingBottom > 0 ||
            Box.BorderTop > 0 || Box.BorderBottom > 0)
            return false;

        return true;
    }
}