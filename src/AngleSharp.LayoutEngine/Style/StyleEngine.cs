#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace AngleSharp.LayoutEngine.Style;

using System.Collections.Generic;
using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.DOM;
using AngleSharp.LayoutEngine.FormattingContexts.Enums;
using TextNode = DOM.TextNode;

/// <summary>
/// Processes CSS styles and provides layout properties for the layout engine,
/// leveraging AngleSharp's built-in style computation capabilities.
/// </summary>
public class StyleEngine
{
    private readonly Dictionary<IRenderNode, LayoutNodeStyle> _styleCache = new Dictionary<IRenderNode, LayoutNodeStyle>();

    /// <summary>
    /// Creates a new style engine.
    /// </summary>
    public StyleEngine()
    {
        // No need to store stylesheets since AngleSharp handles them
    }

    /// <summary>
    /// Computes styles for all nodes in the layout tree.
    /// </summary>
    /// <param name="layoutTree">The layout tree to compute styles for.</param>
    public void ComputeStyles(LayoutTree layoutTree)
    {
        // Clear any existing cached styles
        _styleCache.Clear();

        // Process the tree from top to bottom
        ComputeStylesRecursive(layoutTree.Root);

        // Apply computed styles to layout properties
        ApplyLayoutProperties(layoutTree);
    }

    /// <summary>
    /// Updates styles for the specified nodes and their descendants.
    /// Used for incremental updates when the DOM changes.
    /// </summary>
    /// <param name="dirtyNodes">The nodes whose styles need to be recomputed.</param>
    public void UpdateStyles(IEnumerable<LayoutNode> dirtyNodes)
    {
        foreach (var node in dirtyNodes)
        {
            // Remove existing cached style for this node and its descendants
            RemoveStylesRecursive(node);

            // Recompute styles for this node and its descendants
            ComputeStylesRecursive(node);

            // Update layout properties for this node
            ApplyLayoutPropertiesToNode(node);
        }
    }

    /// <summary>
    /// Gets the cached layout style for a DOM node.
    /// </summary>
    /// <param name="node">The node to get style for.</param>
    /// <returns>The layout node style, or null if not available.</returns>
    public LayoutNodeStyle GetNodeStyle(IRenderNode node)
    {
        if (_styleCache.TryGetValue(node, out var style))
        {
            return style;
        }
        return null;
    }

    /// <summary>
    /// Recursively computes styles for a node and its descendants.
    /// </summary>
    private void ComputeStylesRecursive(LayoutNode node)
    {
        var domNode = node.DomNode;

        // Skip non-element nodes for style computation
        if (domNode is ElementNode element)
        {
            var domElement = element.Ref;

            // AngleSharp already computes the full style including cascade and inheritance
            var computedStyle = element.ComputedStyle;

            if (computedStyle != null)
            {
                // Create layout node style with the essential properties for layout
                var nodeStyle = new LayoutNodeStyle
                {
                    Display = ParseDisplayType(computedStyle.GetPropertyValue("display") ?? "inline"),
                    Position = ParsePositionType(computedStyle.GetPropertyValue("position") ?? "static"),
                    Float = ParseFloatType(computedStyle.GetPropertyValue("float") ?? "none"),
                    Width = ParseStyleValue(computedStyle.GetPropertyValue("width") ?? "auto"),
                    Height = ParseStyleValue(computedStyle.GetPropertyValue("height") ?? "auto")
                };

                // Cache the style
                _styleCache[domNode] = nodeStyle;

                // Process children
                foreach (var child in node.Children)
                {
                    ComputeStylesRecursive(child);
                }
            }
        }
        else if (domNode is TextNode)
        {
            // Text nodes inherit parent style
            // We don't need to store style for text nodes since they don't have layout properties

            // Process any children (though text nodes typically don't have children)
            foreach (var child in node.Children)
            {
                ComputeStylesRecursive(child);
            }
        }
    }

    /// <summary>
    /// Recursively removes cached styles for a node and its descendants.
    /// </summary>
    private void RemoveStylesRecursive(LayoutNode node)
    {
        var domNode = node.DomNode;

        // Remove cached style for this node
        _styleCache.Remove(domNode);

        // Remove for all descendants
        foreach (var child in node.Children)
        {
            RemoveStylesRecursive(child);
        }
    }

    /// <summary>
    /// Applies computed styles to layout properties for all nodes.
    /// </summary>
    private void ApplyLayoutProperties(LayoutTree layoutTree)
    {
        foreach (var node in layoutTree.GetAllNodes())
        {
            ApplyLayoutPropertiesToNode(node);
        }
    }

    /// <summary>
    /// Applies computed styles to layout properties for a specific node.
    /// </summary>
    private void ApplyLayoutPropertiesToNode(LayoutNode node)
    {
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        var computedStyle = element.ComputedStyle;

        // Extract and apply layout properties
        node.Display = ParseDisplayType(computedStyle.GetPropertyValue("display") ?? "inline");
        node.Position = ParsePositionType(computedStyle.GetPropertyValue("position") ?? "static");
        node.Float = ParseFloatType(computedStyle.GetPropertyValue("float") ?? "none");
        node.Width = ParseStyleValue(computedStyle.GetPropertyValue("width") ?? "auto");
        node.Height = ParseStyleValue(computedStyle.GetPropertyValue("height") ?? "auto");
    }

    /// <summary>
    /// Parses a display value into a DisplayType enum.
    /// </summary>
    private DisplayType ParseDisplayType(string display)
    {
        return display.ToLowerInvariant() switch
        {
            "none" => DisplayType.None,
            "block" => DisplayType.Block,
            "inline" => DisplayType.Inline,
            "inline-block" => DisplayType.InlineBlock,
            "flex" => DisplayType.Flex,
            "inline-flex" => DisplayType.Flex, // Inline-flex still creates a flex formatting context
            "grid" => DisplayType.Grid,
            "inline-grid" => DisplayType.Grid, // Inline-grid still creates a grid formatting context
            "table" => DisplayType.Table,
            "table-cell" => DisplayType.TableCell,
            "table-row" => DisplayType.TableRow,
            _ => DisplayType.Inline // Default
        };
    }

    /// <summary>
    /// Parses a position value into a PositionType enum.
    /// </summary>
    private PositionType ParsePositionType(string position)
    {
        return position.ToLowerInvariant() switch
        {
            "static" => PositionType.Static,
            "relative" => PositionType.Relative,
            "absolute" => PositionType.Absolute,
            "fixed" => PositionType.Fixed,
            "sticky" => PositionType.Sticky,
            _ => PositionType.Static // Default
        };
    }

    /// <summary>
    /// Parses a float value into a FloatType enum.
    /// </summary>
    private FloatType ParseFloatType(string float_)
    {
        return float_.ToLowerInvariant() switch
        {
            "left" => FloatType.Left,
            "right" => FloatType.Right,
            "none" => FloatType.None,
            _ => FloatType.None // Default
        };
    }

    /// <summary>
    /// Parses a style value into a StyleValue object.
    /// </summary>
    private StyleValue ParseStyleValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "auto")
            return StyleValue.Auto;

        if (value.EndsWith("px") && float.TryParse(value.TrimEnd('p', 'x'), out float pixels))
            return StyleValue.FromPixels(pixels);

        if (value.EndsWith("%") && float.TryParse(value.TrimEnd('%'), out float percentage))
            return StyleValue.FromPercentage(percentage);

        return StyleValue.Auto;
    }
}