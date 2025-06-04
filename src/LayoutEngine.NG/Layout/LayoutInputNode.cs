namespace LayoutEngine.NG.Layout;

using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using LayoutEngine.NG.Style;
using LayoutEngine.NG.Layout.Dom;

/// <summary>
/// Represents the input data for layout algorithms, decoupling layout computation
/// from layout objects. Follows BlinkNG's LayoutInputNode pattern.
/// </summary>
public class LayoutInputNode
{
    public INode? Node { get; }
    public ComputedStyle? Style { get; }
    public LayoutObjectType LayoutObjectType { get; }
    public bool IsBlock => LayoutObjectType == LayoutObjectType.Block;
    public bool IsInline => LayoutObjectType == LayoutObjectType.Inline;
    public bool IsText => LayoutObjectType == LayoutObjectType.Text;

    private readonly List<LayoutInputNode> _children = new();
    public IReadOnlyList<LayoutInputNode> Children => _children;

    public LayoutInputNode(INode? node, ComputedStyle? style, LayoutObjectType layoutObjectType)
    {
        Node = node;
        Style = style;
        LayoutObjectType = layoutObjectType;
    }

    public void AddChild(LayoutInputNode child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// Creates a LayoutInputNode from a LayoutObject.
    /// </summary>
    public static LayoutInputNode FromLayoutObject(LayoutObject layoutObject)
    {
        var inputNode = new LayoutInputNode(
            layoutObject.Node,
            layoutObject.Style,
            layoutObject.GetLayoutObjectType()
        );

        // Add children
        var child = layoutObject.FirstChild;
        while (child != null)
        {
            inputNode.AddChild(FromLayoutObject(child));
            child = child.NextSibling;
        }

        return inputNode;
    }

    /// <summary>
    /// Gets the text content for text nodes.
    /// </summary>
    public string? GetTextContent()
    {
        return Node is IText textNode ? textNode.TextContent : null;
    }

    /// <summary>
    /// Checks if this node establishes a new formatting context.
    /// </summary>
    public bool EstablishesFormattingContext()
    {
        if (Style == null) return false;

        return Style.Display == DisplayMode.FlowRoot ||
               Style.Position == PositionMode.Absolute ||
               Style.Position == PositionMode.Fixed ||
               (Style.Overflow != Overflow.Visible);
    }
}