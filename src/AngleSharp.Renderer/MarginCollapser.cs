namespace AngleSharp.Renderer;

using System;
using System.Linq;
using Css.Dom;
using Css.Values;

#pragma warning disable CS8604, CS1591
public static class MarginCollapser
{
    public static float CalculateParentChildCollapse(
        ElementNode? parent,
        ElementNode child,
        float parentMarginTop,
        float childMarginTop,
        float parentPaddingTop,
        float parentBorderTop)
    {
        // If parent has padding or border, no collapse occurs
        if (parentPaddingTop > 0 || parentBorderTop > 0)
        {
            return childMarginTop;
        }

        // Check if this is the first in-flow child
        bool isFirstChild = IsFirstInFlowChild(parent, child);

        // If parent has no padding/border and this is first child,
        // collapse parent's top margin with child's top margin
        if (isFirstChild)
        {
            return MarginCollapser.Collapse(parentMarginTop, childMarginTop);
        }

        return childMarginTop;
    }

    public static float CalculateSiblingCollapse(
        ElementNode previousSibling,
        ElementNode currentElement,
        float previousMarginBottom,
        float currentMarginTop)
    {
        // If previous sibling is empty block, handle special case
        if (IsEmptyBlock(previousSibling))
        {
            float previousCollapsedMargin = CollapseEmptyBlockMargins(previousSibling);
            return MarginCollapser.Collapse(previousCollapsedMargin, currentMarginTop);
        }

        // Handle floating and clearance
        if (HasFloatingElements(previousSibling) || HasClearance(currentElement))
        {
            return currentMarginTop; // No margin collapse in these cases
        }

        return MarginCollapser.Collapse(previousMarginBottom, currentMarginTop);
    }

    public static bool IsFirstInFlowChild(ElementNode? parent, ElementNode child)
    {
        if (parent == null || child == null) return false;

        // Get all children that are elements or text nodes
        var inFlowChildren = parent.Children
            .Where(node => node is ElementNode || node is TextNode)
            .ToList();

        // If there are no children, this can't be the first child
        if (!inFlowChildren.Any()) return false;

        // Get the first in-flow child
        var firstInFlowChild = inFlowChildren.FirstOrDefault();

        // Check if our child is the first in-flow child
        bool isFirst = firstInFlowChild == child;

        return isFirst;
    }

    private static bool IsEmptyBlock(ElementNode element)
    {
        if (element == null) return false;

        // Check if element has any in-flow children
        bool hasInFlowContent = element.Children.Any(child =>
            child is ElementNode elem && !IsOutOfFlowElement(elem));

        // Check if element has padding or border
        var style = element.ComputedStyle;
        if (style == null) return false;

        float padding = ParsePx(style, "padding-top") + ParsePx(style, "padding-bottom");
        float border = ParsePx(style, "border-top-width") + ParsePx(style, "border-bottom-width");

        return !hasInFlowContent && padding == 0 && border == 0;
    }

    private static float CollapseEmptyBlockMargins(ElementNode element)
    {
        if (element?.ComputedStyle == null) return 0;

        float topMargin = ParsePx(element.ComputedStyle, "margin-top");
        float bottomMargin = ParsePx(element.ComputedStyle, "margin-bottom");

        return MarginCollapser.Collapse(topMargin, bottomMargin);
    }

    private static bool IsOutOfFlowElement(ElementNode element)
    {
        if (element?.ComputedStyle == null) return false;

        string position = element.ComputedStyle.GetPropertyValue("position") ?? "";
        string float_ = element.ComputedStyle.GetPropertyValue("float") ?? "";

        return position == "absolute" || position == "fixed" || float_ != "none";
    }

    private static bool HasFloatingElements(ElementNode element)
    {
        if (element?.ComputedStyle == null) return false;
        return element.ComputedStyle.GetPropertyValue("float") != "none";
    }

    private static bool HasClearance(ElementNode element)
    {
        if (element?.ComputedStyle == null) return false;
        string clear = element.ComputedStyle.GetPropertyValue("clear") ?? "";
        return clear != "none";
    }

    private static float ParsePx(ICssStyleDeclaration style, string property)
    {
        var raw = style.GetProperty(property)?.RawValue;
        return raw is CssLengthValue lv ? (float)lv.Value : 0f;
    }

    /// <summary>
    /// Collapses two vertical margins according to CSS rules:
    /// 1. If both are positive, use the maximum.
    /// 2. If both are negative, use the minimum (most negative).
    /// 3. If one is positive and one is negative, add them.
    /// </summary>
    public static float Collapse(float marginA, float marginB)
    {
        // If only one margin is non-zero, short-circuit
        if (marginA == 0) return marginB;
        if (marginB == 0) return marginA;

        // Both non-zero
        bool aPos = marginA > 0;
        bool bPos = marginB > 0;

        // Same sign => pick the "extreme"
        if (aPos && bPos)
        {
            return MathF.Max(marginA, marginB);
        }

        if (!aPos && !bPos)
        {
            // both negative, pick the more negative (i.e. min)
            return MathF.Min(marginA, marginB);
        }

        // Different signs => sum them up
        return marginA + marginB;
    }

    /// <summary>
    /// If a block is empty (no in-flow children, no padding/border),
    /// it can collapse its own top/bottom margin together or even with the parent.
    /// This function returns the single (collapsed) margin if we should treat it as empty.
    /// Otherwise, returns 0 to indicate no special empty collapse.
    /// </summary>
    public static float CollapseEmptyBlock(float marginTop, float marginBottom, bool isTrulyEmpty)
    {
        if (isTrulyEmpty)
        {
            // For an empty block:
            // The top and bottom margin collapse into a single margin
            // whose size is the max of the absolute values (with sign logic).
            return Collapse(marginTop, marginBottom);
        }

        return 0f;
    }
}