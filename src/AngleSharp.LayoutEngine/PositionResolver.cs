#pragma warning disable CS8600, CS8602, CS8603, CS8625
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using Css.Dom;

/// <summary>
/// Handles the calculation of final positions for elements based on CSS positioning schemes.
/// Supports static, relative, absolute, fixed, and sticky positioning.
/// </summary>
public class PositionResolver
{
    /// <summary>
    /// Resolves positions for all nodes in the layout tree after the basic layout flow has been calculated.
    /// </summary>
    /// <param name="layoutTree">The layout tree to resolve positions for.</param>
    /// <param name="context">The layout context.</param>
    public void ResolvePositions(LayoutTree layoutTree, LayoutContext context)
    {
        // Process all nodes in the tree
        foreach (var node in layoutTree.GetAllNodes())
        {
            ResolveNodePosition(node, context);
        }
    }

    /// <summary>
    /// Resolves positions for a subset of nodes, typically used for incremental updates.
    /// </summary>
    /// <param name="nodes">The nodes to resolve positions for.</param>
    /// <param name="context">The layout context.</param>
    public void ResolvePositionsForNodes(IEnumerable<LayoutNode> nodes, LayoutContext context)
    {
        foreach (var node in nodes)
        {
            ResolveNodePosition(node, context);
        }
    }

    /// <summary>
    /// Resolves the position for a single node based on its position type.
    /// </summary>
    private void ResolveNodePosition(LayoutNode node, LayoutContext context)
    {
        // Skip non-element nodes or nodes without computed style
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        // Different positioning algorithms based on position type
        switch (node.Position)
        {
            case PositionType.Static:
                // Static positioning is already handled by the normal flow
                break;

            case PositionType.Relative:
                ApplyRelativePositioning(node, element.ComputedStyle);
                break;

            case PositionType.Absolute:
                ApplyAbsolutePositioning(node, element.ComputedStyle, context);
                break;

            case PositionType.Fixed:
                ApplyFixedPositioning(node, element.ComputedStyle, context);
                break;

            case PositionType.Sticky:
                ApplyStickyPositioning(node, element.ComputedStyle, context);
                break;
        }
    }

    /// <summary>
    /// Applies relative positioning to a node.
    /// </summary>
    private void ApplyRelativePositioning(LayoutNode node, ICssStyleDeclaration style)
    {
        // Get offset values (relative to original position)
        float offsetTop = ParseLengthOrDefault(style, "top", 0);
        float offsetRight = ParseLengthOrDefault(style, "right", 0);
        float offsetBottom = ParseLengthOrDefault(style, "bottom", 0);
        float offsetLeft = ParseLengthOrDefault(style, "left", 0);

        // Calculate final position
        // For relative positioning, we apply offsets to the normal flow position
        // If both left and right are specified, left takes precedence
        if (offsetLeft != 0)
        {
            node.Box.X += offsetLeft;
        }
        else if (offsetRight != 0)
        {
            node.Box.X -= offsetRight;
        }

        // If both top and bottom are specified, top takes precedence
        if (offsetTop != 0)
        {
            node.Box.Y += offsetTop;
        }
        else if (offsetBottom != 0)
        {
            node.Box.Y -= offsetBottom;
        }
    }

    /// <summary>
    /// Applies absolute positioning to a node.
    /// </summary>
    private void ApplyAbsolutePositioning(LayoutNode node, ICssStyleDeclaration style, LayoutContext context)
    {
        // Find the containing block (nearest positioned ancestor)
        var containingBlock = FindContainingBlock(node);

        // Get the containing block's box
        var containerBox = containingBlock.Box;

        // Default to viewport if no containing block is found
        if (containingBlock == null || containerBox == null)
        {
            containerBox = new LayoutBox
            {
                Width = context.ViewportWidth,
                Height = context.ViewportHeight
            };
        }

        // Get position values
        string top = style.GetPropertyValue("top");
        string right = style.GetPropertyValue("right");
        string bottom = style.GetPropertyValue("bottom");
        string left = style.GetPropertyValue("left");

        // Get width and height
        string width = style.GetPropertyValue("width");
        string height = style.GetPropertyValue("height");

        // Calculate horizontal position
        CalculateHorizontalPosition(node, containerBox, left, right, width);

        // Calculate vertical position
        CalculateVerticalPosition(node, containerBox, top, bottom, height);
    }

    /// <summary>
    /// Applies fixed positioning to a node.
    /// </summary>
    private void ApplyFixedPositioning(LayoutNode node, ICssStyleDeclaration style, LayoutContext context)
    {
        // For fixed positioning, the containing block is the viewport
        var viewportBox = new LayoutBox
        {
            Width = context.ViewportWidth,
            Height = context.ViewportHeight
        };

        // Get position values
        string top = style.GetPropertyValue("top");
        string right = style.GetPropertyValue("right");
        string bottom = style.GetPropertyValue("bottom");
        string left = style.GetPropertyValue("left");

        // Get width and height
        string width = style.GetPropertyValue("width");
        string height = style.GetPropertyValue("height");

        // Calculate horizontal position
        CalculateHorizontalPosition(node, viewportBox, left, right, width);

        // Calculate vertical position
        CalculateVerticalPosition(node, viewportBox, top, bottom, height);
    }

    /// <summary>
    /// Applies sticky positioning to a node.
    /// </summary>
    private void ApplyStickyPositioning(LayoutNode node, ICssStyleDeclaration style, LayoutContext context)
    {
        // For sticky positioning, we start with the normal flow position
        // Then apply constraints to keep the element visible within its containing block

        // Find the containing block (nearest scrollable ancestor)
        var containingBlock = FindScrollableAncestor(node);
        if (containingBlock == null)
        {
            containingBlock = node.Parent;
        }

        if (containingBlock == null)
            return;

        // Get the containing block's box
        var containerBox = containingBlock.Box;

        // Get sticky constraints
        float top = ParseLengthOrDefault(style, "top", float.NaN);
        float right = ParseLengthOrDefault(style, "right", float.NaN);
        float bottom = ParseLengthOrDefault(style, "bottom", float.NaN);
        float left = ParseLengthOrDefault(style, "left", float.NaN);

        // Save original position (from normal flow)
        float originalX = node.Box.X;
        float originalY = node.Box.Y;

        // Apply top constraint
        if (!float.IsNaN(top) && node.Box.Y < containerBox.Y + top)
        {
            node.Box.Y = containerBox.Y + top;
        }

        // Apply bottom constraint
        if (!float.IsNaN(bottom) && node.Box.Y + node.Box.Height > containerBox.Y + containerBox.Height - bottom)
        {
            node.Box.Y = containerBox.Y + containerBox.Height - bottom - node.Box.Height;
        }

        // Apply left constraint
        if (!float.IsNaN(left) && node.Box.X < containerBox.X + left)
        {
            node.Box.X = containerBox.X + left;
        }

        // Apply right constraint
        if (!float.IsNaN(right) && node.Box.X + node.Box.Width > containerBox.X + containerBox.Width - right)
        {
            node.Box.X = containerBox.X + containerBox.Width - right - node.Box.Width;
        }

        // Ensure we don't move the element beyond its normal limits
        if (!float.IsNaN(top) && !float.IsNaN(bottom))
        {
            // If both top and bottom constraints would be violated, prefer top
            if (node.Box.Y < originalY && node.Box.Y + node.Box.Height > containerBox.Y + containerBox.Height - bottom)
            {
                node.Box.Y = containerBox.Y + top;
            }
        }

        if (!float.IsNaN(left) && !float.IsNaN(right))
        {
            // If both left and right constraints would be violated, prefer left
            if (node.Box.X < originalX && node.Box.X + node.Box.Width > containerBox.X + containerBox.Width - right)
            {
                node.Box.X = containerBox.X + left;
            }
        }
    }

    /// <summary>
    /// Calculates the horizontal position for an absolutely positioned element.
    /// </summary>
    private void CalculateHorizontalPosition(LayoutNode node, LayoutBox containerBox, string left, string right, string width)
    {
        bool hasLeft = !string.IsNullOrEmpty(left) && left != "auto";
        bool hasRight = !string.IsNullOrEmpty(right) && right != "auto";
        bool hasWidth = !string.IsNullOrEmpty(width) && width != "auto";

        float leftValue = ParseLengthOrDefault(left, 0);
        float rightValue = ParseLengthOrDefault(right, 0);
        float widthValue = ParseLengthOrDefault(width, node.Box.Width);

        if (hasLeft && hasRight && hasWidth)
        {
            // Over-constrained case: ignore 'right'
            node.Box.X = containerBox.X + leftValue;
            node.Box.Width = widthValue;
        }
        else if (hasLeft && hasRight)
        {
            // Width is determined by left and right
            node.Box.X = containerBox.X + leftValue;
            node.Box.Width = containerBox.Width - leftValue - rightValue;
        }
        else if (hasLeft && hasWidth)
        {
            // Left and width determine position
            node.Box.X = containerBox.X + leftValue;
            node.Box.Width = widthValue;
        }
        else if (hasRight && hasWidth)
        {
            // Right and width determine position
            node.Box.X = containerBox.X + containerBox.Width - rightValue - widthValue;
            node.Box.Width = widthValue;
        }
        else if (hasLeft)
        {
            // Only left is specified
            node.Box.X = containerBox.X + leftValue;
            // Keep intrinsic width
        }
        else if (hasRight)
        {
            // Only right is specified
            node.Box.X = containerBox.X + containerBox.Width - rightValue - node.Box.Width;
            // Keep intrinsic width
        }
        else if (hasWidth)
        {
            // Only width is specified
            node.Box.Width = widthValue;
            // Center horizontally (simplified)
            node.Box.X = containerBox.X + (containerBox.Width - node.Box.Width) / 2;
        }
        else
        {
            // Nothing specified, use automatic margins to center (simplified)
            node.Box.X = containerBox.X + (containerBox.Width - node.Box.Width) / 2;
        }
    }

    /// <summary>
    /// Calculates the vertical position for an absolutely positioned element.
    /// </summary>
    private void CalculateVerticalPosition(LayoutNode node, LayoutBox containerBox, string top, string bottom, string height)
    {
        bool hasTop = !string.IsNullOrEmpty(top) && top != "auto";
        bool hasBottom = !string.IsNullOrEmpty(bottom) && bottom != "auto";
        bool hasHeight = !string.IsNullOrEmpty(height) && height != "auto";

        float topValue = ParseLengthOrDefault(top, 0);
        float bottomValue = ParseLengthOrDefault(bottom, 0);
        float heightValue = ParseLengthOrDefault(height, node.Box.Height);

        if (hasTop && hasBottom && hasHeight)
        {
            // Over-constrained case: ignore 'bottom'
            node.Box.Y = containerBox.Y + topValue;
            node.Box.Height = heightValue;
        }
        else if (hasTop && hasBottom)
        {
            // Height is determined by top and bottom
            node.Box.Y = containerBox.Y + topValue;
            node.Box.Height = containerBox.Height - topValue - bottomValue;
        }
        else if (hasTop && hasHeight)
        {
            // Top and height determine position
            node.Box.Y = containerBox.Y + topValue;
            node.Box.Height = heightValue;
        }
        else if (hasBottom && hasHeight)
        {
            // Bottom and height determine position
            node.Box.Y = containerBox.Y + containerBox.Height - bottomValue - heightValue;
            node.Box.Height = heightValue;
        }
        else if (hasTop)
        {
            // Only top is specified
            node.Box.Y = containerBox.Y + topValue;
            // Keep intrinsic height
        }
        else if (hasBottom)
        {
            // Only bottom is specified
            node.Box.Y = containerBox.Y + containerBox.Height - bottomValue - node.Box.Height;
            // Keep intrinsic height
        }
        else if (hasHeight)
        {
            // Only height is specified
            node.Box.Height = heightValue;
            // Center vertically (simplified)
            node.Box.Y = containerBox.Y + (containerBox.Height - node.Box.Height) / 2;
        }
        else
        {
            // Nothing specified, use automatic margins to center (simplified)
            node.Box.Y = containerBox.Y + (containerBox.Height - node.Box.Height) / 2;
        }
    }

    /// <summary>
    /// Finds the containing block for absolutely positioned elements.
    /// This is the nearest ancestor with a position other than static.
    /// </summary>
    private LayoutNode FindContainingBlock(LayoutNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current.Position != PositionType.Static)
            {
                return current;
            }
            current = current.Parent;
        }
        return null; // If none found, use the initial containing block (viewport)
    }

    /// <summary>
    /// Finds the nearest scrollable ancestor for sticky positioning.
    /// </summary>
    private LayoutNode FindScrollableAncestor(LayoutNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            var element = current.DomNode as ElementNode;
            if (element?.ComputedStyle != null)
            {
                string overflow = element.ComputedStyle.GetPropertyValue("overflow") ?? "visible";
                if (overflow == "scroll" || overflow == "auto" || overflow == "hidden")
                {
                    return current;
                }
            }
            current = current.Parent;
        }
        return null; // Use viewport if no scrollable ancestor is found
    }

    /// <summary>
    /// Parses a CSS length value from a style property.
    /// </summary>
    private float ParseLengthOrDefault(ICssStyleDeclaration style, string propertyName, float defaultValue)
    {
        string value = style.GetPropertyValue(propertyName);
        return ParseLengthOrDefault(value, defaultValue);
    }

    /// <summary>
    /// Parses a CSS length value.
    /// </summary>
    private float ParseLengthOrDefault(string value, float defaultValue)
    {
        if (string.IsNullOrEmpty(value) || value == "auto")
            return defaultValue;

        if (value.EndsWith("px") && float.TryParse(value.TrimEnd('p', 'x'), out float pixels))
            return pixels;

        if (value.EndsWith("%") && float.TryParse(value.TrimEnd('%'), out float percentage))
            return percentage / 100f; // This is a simplification, would need containing block reference

        return defaultValue;
    }
}