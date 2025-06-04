namespace LayoutEngine.NG.Layout.Inline;

using System.Collections.Generic;
using Core;
using Inputs;

/// <summary>
/// Represents an inline-level input node in LayoutNG.
/// This is a specialized LayoutInputNode for inline layout algorithms.
/// </summary>
public class InlineNode : LayoutInputNode
{
    private readonly List<InlineItem> _items = new();

    public InlineNode(LayoutBox? box) : base(box, LayoutInputNodeType.Inline)
    {
    }

    /// <summary>
    /// Gets the inline items for this node.
    /// </summary>
    public IReadOnlyList<InlineItem> GetItems() => _items;

    /// <summary>
    /// Adds an inline item to this node.
    /// </summary>
    public void AddItem(InlineItem item)
    {
        _items.Add(item);
    }

    public override string ToString()
    {
        var box = GetLayoutBox();
        if (box == null)
            return "InlineNode(null)";

        var type = box.GetType().Name;
        var node = box.GetNode();

        if (node != null)
        {
            var tag = node.NodeName?.ToLower() ?? "unknown";
            return $"InlineNode({type}, <{tag}>, {_items.Count} items)";
        }

        return $"InlineNode({type}, {_items.Count} items)";
    }
}

/// <summary>
/// Represents an item within an inline formatting context.
/// In LayoutNG, inline items are the atomic units of inline layout.
/// </summary>
public class InlineItem
{
    public InlineItemType Type { get; set; }
    public LayoutObject? LayoutObject { get; set; }
    public string? Text { get; set; }
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }

    public override string ToString()
    {
        return Type switch
        {
            InlineItemType.Text => $"InlineItem(Text: '{Text}')",
            InlineItemType.AtomicInline => $"InlineItem(AtomicInline: {LayoutObject?.GetType().Name})",
            InlineItemType.OpenTag => $"InlineItem(OpenTag: {LayoutObject?.GetType().Name})",
            InlineItemType.CloseTag => $"InlineItem(CloseTag: {LayoutObject?.GetType().Name})",
            _ => $"InlineItem({Type})"
        };
    }
}

/// <summary>
/// Types of inline items.
/// </summary>
public enum InlineItemType
{
    Text,
    AtomicInline,
    OpenTag,
    CloseTag,
    LineBreak,
    BiDiControl
}