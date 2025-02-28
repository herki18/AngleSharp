#pragma warning disable CS8618, CS9264
#pragma warning disable CS8603 // Possible null reference return.
namespace AngleSharp.LayoutEngine;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Layout tree that parallels the DOM tree but contains only layout information.
/// </summary>
public class LayoutTree
{
    /// <summary>
    /// Root node of the layout tree.
    /// </summary>
    public LayoutNode Root { get; private set; } = null!;

    /// <summary>
    /// Maps DOM nodes to their corresponding layout nodes for quick lookup.
    /// </summary>
    private readonly Dictionary<IRenderNode, LayoutNode> _nodeMapping = new();

    /// <summary>
    /// Maps node IDs to layout nodes for quick lookup.
    /// </summary>
    private readonly Dictionary<string, LayoutNode> _idMapping = new();

    /// <summary>
    /// Builds a layout tree from a DOM tree.
    /// </summary>
    public void BuildFromDOM(IRenderNode domRoot)
    {
        // Clear any existing tree
        _nodeMapping.Clear();
        _idMapping.Clear();

        // Create the root node
        Root = CreateNode(domRoot, null!);

        // Process the tree recursively
        BuildTreeRecursive(domRoot, Root);
    }

    /// <summary>
    /// Finds a layout node corresponding to a DOM node.
    /// </summary>
    public LayoutNode FindNodeForDomNode(IRenderNode domNode)
    {
        _nodeMapping.TryGetValue(domNode, out var layoutNode);
        return layoutNode;
    }

    /// <summary>
    /// Finds a layout node by its ID.
    /// </summary>
    public LayoutNode FindNodeById(string id)
    {
        _idMapping.TryGetValue(id, out var layoutNode);
        return layoutNode;
    }

    /// <summary>
    /// Finds layout nodes corresponding to a collection of DOM nodes.
    /// </summary>
    public List<LayoutNode> FindNodesForDomNodes(IEnumerable<IRenderNode> domNodes)
    {
        var result = new List<LayoutNode>();
        foreach (var domNode in domNodes)
        {
            var layoutNode = FindNodeForDomNode(domNode);
            if (layoutNode != null)
            {
                result.Add(layoutNode);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all layout nodes in the tree.
    /// </summary>
    public IEnumerable<LayoutNode> GetAllNodes()
    {
        return TraversePreOrder(Root);
    }

    /// <summary>
    /// Pre-order traversal of the layout tree.
    /// </summary>
    private IEnumerable<LayoutNode> TraversePreOrder(LayoutNode node)
    {
        if (node == null) yield break;

        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in TraversePreOrder(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Recursively builds the layout tree from the DOM tree.
    /// </summary>
    private void BuildTreeRecursive(IRenderNode domNode, LayoutNode layoutNode)
    {
        if (domNode == null || layoutNode == null) return;

        // Skip non-renderable nodes
        if (domNode is NonRenderableNode)
        {
            return;
        }

        // Process children
        foreach (var domChild in domNode.Children)
        {
            // Skip non-renderable children
            if (domChild is NonRenderableNode)
            {
                continue;
            }

            var layoutChild = CreateNode(domChild, layoutNode);
            layoutNode.Children.Add(layoutChild);
            BuildTreeRecursive(domChild, layoutChild);
        }
    }

    /// <summary>
    /// Creates a layout node from a DOM node with appropriate initialization.
    /// </summary>
    private LayoutNode CreateNode(IRenderNode domNode, LayoutNode parent)
    {
        var node = new LayoutNode(domNode)
        {
            Parent = parent
        };

        // Store in maps for quick lookups
        _nodeMapping[domNode] = node;

        // If element has an ID, store it in the ID map
        if (domNode is ElementNode element && !string.IsNullOrEmpty(element.Id))
        {
            _idMapping[element.Id] = node;
        }

        return node;
    }
}

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

/// <summary>
/// Enumeration of display types.
/// </summary>
public enum DisplayType
{
    None,
    Block,
    Inline,
    InlineBlock,
    Flex,
    Grid,
    Table,
    TableCell,
    TableRow
}

/// <summary>
/// Enumeration of position types.
/// </summary>
public enum PositionType
{
    Static,
    Relative,
    Absolute,
    Fixed,
    Sticky
}

/// <summary>
/// Enumeration of float types.
/// </summary>
public enum FloatType
{
    None,
    Left,
    Right
}

/// <summary>
/// Base class for style values.
/// </summary>
public abstract class StyleValue
{
    /// <summary>
    /// Represents an 'auto' value.
    /// </summary>
    public static readonly StyleValue Auto = new StyleAutoValue();

    /// <summary>
    /// Creates a length value from a pixel amount.
    /// </summary>
    public static StyleValue FromPixels(float pixels)
    {
        return new StyleLengthValue(pixels, StyleUnit.Px);
    }

    /// <summary>
    /// Creates a percentage value.
    /// </summary>
    public static StyleValue FromPercentage(float percentage)
    {
        return new StyleLengthValue(percentage, StyleUnit.Percentage);
    }
}

/// <summary>
/// Represents an 'auto' value in CSS.
/// </summary>
public class StyleAutoValue : StyleValue
{
    public override string ToString() => "auto";
}

/// <summary>
/// Represents a length value in CSS.
/// </summary>
public class StyleLengthValue : StyleValue
{
    /// <summary>
    /// The numeric value.
    /// </summary>
    public float Value { get; }

    /// <summary>
    /// The unit of the value.
    /// </summary>
    public StyleUnit Unit { get; }

    /// <summary>
    /// Creates a new length value with the specified value and unit.
    /// </summary>
    public StyleLengthValue(float value, StyleUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Converts the value to pixels based on the specified context.
    /// </summary>
    public float ToPixels(LayoutContext context, float containerSize = 0)
    {
        switch (Unit)
        {
            case StyleUnit.Px:
                return Value;
            case StyleUnit.Percentage:
                return containerSize * Value / 100f;
            case StyleUnit.Em:
                return Value * context.DefaultFontSize;
            case StyleUnit.Rem:
                return Value * context.DefaultFontSize;
            case StyleUnit.Vh:
                return context.ViewportHeight * Value / 100f;
            case StyleUnit.Vw:
                return context.ViewportWidth * Value / 100f;
            default:
                return Value; // Default to pixel value
        }
    }

    public override string ToString() => $"{Value}{Unit.ToString().ToLowerInvariant()}";
}

/// <summary>
/// Enumeration of CSS units.
/// </summary>
public enum StyleUnit
{
    Px,
    Percentage,
    Em,
    Rem,
    Vh,
    Vw,
    Vmin,
    Vmax
}