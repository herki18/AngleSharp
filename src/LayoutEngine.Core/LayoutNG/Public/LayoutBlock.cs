namespace LayoutEngine.Core.LayoutNG.Public;

using System;
using System.Linq;
using AngleSharp.Dom;

/// <summary>
/// Layout object for block-level elements (div, p, section, etc).
/// </summary>
public class LayoutBlock : LayoutBox
{
    public LayoutBlock(INode? node) : base(node)
    {
    }

    public override LayoutObjectType Type => LayoutObjectType.Block;

    public override bool IsBlock => true;

    public override bool EstablishesBlockFormattingContext
    {
        get
        {
            if (Style == null) return false;

            // Check conditions that establish a new BFC
            var overflow = Style.GetPropertyValue("overflow");
            if (overflow != "visible" && overflow != "unset")
                return true;

            var display = Style.GetPropertyValue("display");
            if (display == "flow-root" || display == "table" || display == "table-cell")
                return true;

            var position = Style.GetPropertyValue("position");
            if (position == "absolute" || position == "fixed")
                return true;

            var float_ = Style.GetPropertyValue("float");
            if (float_ == "left" || float_ == "right")
                return true;

            return false;
        }
    }

    public override void CreateAnonymousWrappersIfNeeded()
    {
        // Check if we need to create anonymous blocks to wrap inline content
        // This happens when we have both inline and block children

        bool hasInlineChildren = false;
        bool hasBlockChildren = false;

        foreach (var child in Children)
        {
            if (child.IsBlock)
                hasBlockChildren = true;
            else if (child.IsInline || child.IsText)
                hasInlineChildren = true;
        }

        if (hasInlineChildren && hasBlockChildren)
        {
            // Need to wrap consecutive inline/text children in anonymous blocks
            WrapInlineChildrenInAnonymousBlocks();
        }
    }

    private void WrapInlineChildrenInAnonymousBlocks()
    {
        var childrenToProcess = Children.ToList();
        LayoutAnonymousBlock? currentAnonymousBlock = null;
        ILayoutObject? insertBefore = null;

        foreach (var child in childrenToProcess)
        {
            if (child.IsBlock)
            {
                // End current anonymous block if any
                currentAnonymousBlock = null;
                insertBefore = GetNextSibling(child);
            }
            else if (child.IsInline || child.IsText)
            {
                if (currentAnonymousBlock == null)
                {
                    // Create new anonymous block
                    currentAnonymousBlock = new LayoutAnonymousBlock();
                    InsertChild(currentAnonymousBlock, child);
                    insertBefore = GetNextSibling(currentAnonymousBlock);
                }

                // Move inline/text child into anonymous block
                RemoveChild(child);
                currentAnonymousBlock.AddChild(child);
            }
        }
    }

    protected override bool IsAnonymousWrapperNeeded(ILayoutContainer anonymous)
    {
        if (anonymous is LayoutAnonymousBlock)
        {
            // Anonymous block is needed if it contains children
            // and we still have mixed inline/block content
            if (anonymous.ChildCount == 0)
                return false;

            // Check if we still need the wrapper
            bool hasOtherBlockChildren = Children.Any(c => c != anonymous && c.IsBlock);
            return hasOtherBlockChildren;
        }

        return base.IsAnonymousWrapperNeeded(anonymous);
    }

    protected override float CalculateContentHeight()
    {
        // Sum of all child margin boxes
        float height = 0;
        float previousMarginBottom = 0;

        foreach (var child in Children)
        {
            if (child is ILayoutBox box)
            {
                // Handle margin collapsing
                float marginTop = box.Margins.Top;
                float collapsedMargin = Math.Max(previousMarginBottom, marginTop);

                height += collapsedMargin + box.BorderRect.Height;
                previousMarginBottom = box.Margins.Bottom;
            }
        }

        height += previousMarginBottom;
        return height;
    }
}

/// <summary>
/// Anonymous block layout object created to wrap inline content.
/// </summary>
public class LayoutAnonymousBlock : LayoutBlock
{
    public LayoutAnonymousBlock() : base(null)
    {
    }

    public override LayoutObjectType Type => LayoutObjectType.AnonymousBlock;

    public override bool IsAnonymous => true;
}