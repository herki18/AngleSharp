namespace LayoutEngine.Core.LayoutNG.Public;

using System.Linq;
using AngleSharp.Dom;

/// <summary>
/// Layout object for flexbox containers (display: flex).
/// </summary>
public class LayoutFlex : LayoutBox
{
    public LayoutFlex(INode? node) : base(node)
    {
    }

    public override LayoutObjectType Type => LayoutObjectType.Flex;

    public override bool IsBlock => true; // Flex containers are block-level

    public override bool EstablishesBlockFormattingContext => true; // Flex containers always establish BFC

    /// <summary>
    /// Gets the flex direction (row, column, etc).
    /// </summary>
    public FlexDirection FlexDirection
    {
        get
        {
            if (Style == null) return FlexDirection.Row;

            return Style.GetPropertyValue("flex-direction") switch
            {
                "column" => FlexDirection.Column,
                "row-reverse" => FlexDirection.RowReverse,
                "column-reverse" => FlexDirection.ColumnReverse,
                _ => FlexDirection.Row
            };
        }
    }

    /// <summary>
    /// Gets the flex wrap setting.
    /// </summary>
    public FlexWrap FlexWrap
    {
        get
        {
            if (Style == null) return FlexWrap.NoWrap;

            return Style.GetPropertyValue("flex-wrap") switch
            {
                "wrap" => FlexWrap.Wrap,
                "wrap-reverse" => FlexWrap.WrapReverse,
                _ => FlexWrap.NoWrap
            };
        }
    }

    /// <summary>
    /// Gets the justify-content value.
    /// </summary>
    public JustifyContent JustifyContent
    {
        get
        {
            if (Style == null) return JustifyContent.FlexStart;

            return Style.GetPropertyValue("justify-content") switch
            {
                "flex-end" => JustifyContent.FlexEnd,
                "center" => JustifyContent.Center,
                "space-between" => JustifyContent.SpaceBetween,
                "space-around" => JustifyContent.SpaceAround,
                "space-evenly" => JustifyContent.SpaceEvenly,
                _ => JustifyContent.FlexStart
            };
        }
    }

    /// <summary>
    /// Gets the align-items value.
    /// </summary>
    public AlignItems AlignItems
    {
        get
        {
            if (Style == null) return AlignItems.Stretch;

            return Style.GetPropertyValue("align-items") switch
            {
                "flex-start" => AlignItems.FlexStart,
                "flex-end" => AlignItems.FlexEnd,
                "center" => AlignItems.Center,
                "baseline" => AlignItems.Baseline,
                _ => AlignItems.Stretch
            };
        }
    }

    /// <summary>
    /// Gets the align-content value.
    /// </summary>
    public AlignContent AlignContent
    {
        get
        {
            if (Style == null) return AlignContent.Stretch;

            return Style.GetPropertyValue("align-content") switch
            {
                "flex-start" => AlignContent.FlexStart,
                "flex-end" => AlignContent.FlexEnd,
                "center" => AlignContent.Center,
                "space-between" => AlignContent.SpaceBetween,
                "space-around" => AlignContent.SpaceAround,
                _ => AlignContent.Stretch
            };
        }
    }

    public override void CreateAnonymousWrappersIfNeeded()
    {
        // Flex containers handle all children as flex items
        // Text nodes need to be wrapped in anonymous blocks
        WrapTextNodesInAnonymousBlocks();
    }

    private void WrapTextNodesInAnonymousBlocks()
    {
        var childrenToProcess = Children.ToList();

        foreach (var child in childrenToProcess)
        {
            if (child.IsText)
            {
                // Create anonymous block to wrap text
                var anonymousBlock = new LayoutAnonymousBlock();

                // Insert anonymous block where text was
                var nextSibling = GetNextSibling(child);
                RemoveChild(child);
                InsertChild(anonymousBlock, nextSibling);

                // Move text into anonymous block
                anonymousBlock.AddChild(child);
            }
        }
    }

    protected override float CalculateContentHeight()
    {
        // Flex layout algorithm would go here
        // For now, return a simple calculation
        return base.CalculateContentHeight();
    }
}

/// <summary>
/// Flex direction values.
/// </summary>
public enum FlexDirection
{
    Row,
    RowReverse,
    Column,
    ColumnReverse
}

/// <summary>
/// Flex wrap values.
/// </summary>
public enum FlexWrap
{
    NoWrap,
    Wrap,
    WrapReverse
}

/// <summary>
/// Justify content values.
/// </summary>
public enum JustifyContent
{
    FlexStart,
    FlexEnd,
    Center,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
}

/// <summary>
/// Align items values.
/// </summary>
public enum AlignItems
{
    FlexStart,
    FlexEnd,
    Center,
    Baseline,
    Stretch
}

/// <summary>
/// Align content values.
/// </summary>
public enum AlignContent
{
    FlexStart,
    FlexEnd,
    Center,
    SpaceBetween,
    SpaceAround,
    Stretch
}