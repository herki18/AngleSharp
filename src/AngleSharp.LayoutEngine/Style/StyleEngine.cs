#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618, CS9264
namespace AngleSharp.LayoutEngine.Style
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Dom;
    using AngleSharp.LayoutEngine.Adapters;
    using AngleSharp.LayoutEngine.Core;
    using AngleSharp.LayoutEngine.DOM;
    using AngleSharp.LayoutEngine.FormattingContexts.Enums;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Box;
    using Css;
    using TextNode = DOM.TextNode;

    /// <summary>
    /// Processes CSS styles and provides layout properties for the layout engine,
    /// leveraging AngleSharp's built-in style computation capabilities.
    /// </summary>
    public class StyleEngine
    {
        private readonly Dictionary<IRenderNode, LayoutNodeStyle> _styleCache = new Dictionary<IRenderNode, LayoutNodeStyle>();
        private ViewportSynchronizer _viewportSynchronizer;

        /// <summary>
        /// Creates a new style engine.
        /// </summary>
        public StyleEngine()
        {
        }

        /// <summary>
        /// Sets the viewport synchronizer for consistent dimensions.
        /// </summary>
        /// <param name="synchronizer">The viewport synchronizer to use.</param>
        public void SetViewportSynchronizer(ViewportSynchronizer synchronizer)
        {
            _viewportSynchronizer = synchronizer;
        }

        /// <summary>
        /// Computes styles for all nodes in the layout tree.
        /// </summary>
        /// <param name="layoutTree">The layout tree to compute styles for.</param>
        public void ComputeStyles(LayoutTree layoutTree)
        {
            if (layoutTree == null)
                throw new ArgumentNullException(nameof(layoutTree));

            // Clear any existing cached styles
            _styleCache.Clear();

            // Create a layout context for style computation
            var context = _viewportSynchronizer?.CreateLayoutContext()
                ?? new LayoutContext(800, 600); // Default fallback

            // Process the tree from top to bottom
            ComputeStylesRecursive(layoutTree.Root, context);

            // Apply computed styles to layout properties
            ApplyLayoutProperties(layoutTree, context);
        }

        /// <summary>
        /// Updates styles for the specified nodes and their descendants.
        /// Used for incremental updates when the DOM changes.
        /// </summary>
        /// <param name="dirtyNodes">The nodes whose styles need to be recomputed.</param>
        public void UpdateStyles(IEnumerable<LayoutNode> dirtyNodes)
        {
            if (dirtyNodes == null || !dirtyNodes.Any())
                return;

            // Create a layout context for style computation
            var context = _viewportSynchronizer?.CreateLayoutContext()
                ?? new LayoutContext(800, 600); // Default fallback

            // Remove existing styles from cache for dirty nodes and their descendants
            foreach (var node in dirtyNodes)
            {
                RemoveStylesRecursive(node);
            }

            // For each dirty node, recompute styles
            foreach (var node in dirtyNodes)
            {
                ComputeStylesRecursive(node, context);
            }

            // Update layout properties for the dirty nodes
            foreach (var node in dirtyNodes)
            {
                ApplyLayoutPropertiesToNode(node, context);
            }
        }

        /// <summary>
        /// Gets the cached layout style for a DOM node.
        /// </summary>
        /// <param name="node">The node to get style for.</param>
        /// <returns>The layout node style, or null if not available.</returns>
        public LayoutNodeStyle GetNodeStyle(IRenderNode node)
        {
            if (node == null)
                return null;

            if (_styleCache.TryGetValue(node, out var style))
            {
                return style;
            }

            return null;
        }

        /// <summary>
        /// Recursively computes styles for a node and its descendants.
        /// </summary>
        private void ComputeStylesRecursive(LayoutNode node, LayoutContext context)
        {
            if (node == null)
                return;

            var domNode = node.DomNode;

            // Skip non-element nodes for style computation
            if (domNode is ElementNode element)
            {
                // AngleSharp already computes the full style including cascade and inheritance
                var computedStyle = element.ComputedStyle;

                if (computedStyle != null)
                {
                    // Create render dimensions adapter for this element's context
                    var baseAdapter = new AngleSharpRenderDimensionsAdapter(context);
                    var elementAdapter = AngleSharpRenderDimensionsAdapter.CreateForNode(node, baseAdapter);

                    // Create style resolver for direct value access
                    var styleResolver = new DirectStyleResolver(computedStyle, elementAdapter);

                    // Create layout node style with the essential properties for layout
                    var nodeStyle = new LayoutNodeStyle
                    {
                        Display = ParseDisplayType(computedStyle.GetPropertyValue("display") ?? "inline"),
                        Position = ParsePositionType(computedStyle.GetPropertyValue("position") ?? "static"),
                        Float = ParseFloatType(computedStyle.GetPropertyValue("float") ?? "none"),

                        // Use direct resolution for width and height
                        Width = ResolveStyleValue(styleResolver, "width"),
                        Height = ResolveStyleValue(styleResolver, "height", RenderMode.Vertical),

                        // Store style resolver for later use
                        StyleResolver = styleResolver
                    };

                    // Cache the style
                    _styleCache[domNode] = nodeStyle;

                    // Process children
                    foreach (var child in node.Children)
                    {
                        ComputeStylesRecursive(child, context);
                    }
                }
            }
            else if (domNode is TextNode)
            {
                // Text nodes inherit parent style
                // Process any children (though text nodes typically don't have children)
                foreach (var child in node.Children)
                {
                    ComputeStylesRecursive(child, context);
                }
            }
        }

        /// <summary>
        /// Recursively removes cached styles for a node and its descendants.
        /// </summary>
        private void RemoveStylesRecursive(LayoutNode node)
        {
            if (node == null)
                return;

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
        private void ApplyLayoutProperties(LayoutTree layoutTree, LayoutContext context)
        {
            foreach (var node in layoutTree.GetAllNodes())
            {
                ApplyLayoutPropertiesToNode(node, context);
            }
        }

        /// <summary>
        /// Applies computed styles to layout properties for a specific node.
        /// </summary>
        private void ApplyLayoutPropertiesToNode(LayoutNode node, LayoutContext context)
        {
            if (node == null)
                return;

            var element = node.DomNode as ElementNode;
            if (element?.ComputedStyle == null)
                return;

            // Get the computed style and cached style information
            var computedStyle = element.ComputedStyle;

            if (_styleCache.TryGetValue(element, out var nodeStyle))
            {
                // Apply layout properties from cached style
                node.Display = nodeStyle.Display;
                node.Position = nodeStyle.Position;
                node.Float = nodeStyle.Float;
                node.Width = nodeStyle.Width;
                node.Height = nodeStyle.Height;

                // Use the style resolver for box model calculation
                if (nodeStyle.StyleResolver != null)
                {
                    // Calculate box model values using the style resolver
                    var calculator = new BoxModelCalculator(computedStyle, nodeStyle.StyleResolver);
                    var boxValues = calculator.GetBoxValues();

                    // Update the node's box with the calculated values
                    if (node.Box != null)
                    {
                        node.Box.UpdateFromBoxValues(boxValues);
                    }
                }
            }
            else
            {
                // Fallback to direct extraction if cached style is not available
                node.Display = ParseDisplayType(computedStyle.GetPropertyValue("display") ?? "inline");
                node.Position = ParsePositionType(computedStyle.GetPropertyValue("position") ?? "static");
                node.Float = ParseFloatType(computedStyle.GetPropertyValue("float") ?? "none");
                node.Width = ParseStyleValue(computedStyle.GetPropertyValue("width") ?? "auto");
                node.Height = ParseStyleValue(computedStyle.GetPropertyValue("height") ?? "auto");

                // Use legacy box model calculation
                var calculator = new BoxModelCalculator(computedStyle, context.AvailableWidth);
                var boxValues = calculator.GetBoxValues();

                // Update the node's box with the calculated values
                if (node.Box != null)
                {
                    node.Box.UpdateFromBoxValues(boxValues);
                }
            }
        }

        /// <summary>
        /// Resolves a style value using the direct style resolver.
        /// </summary>
        private StyleValue ResolveStyleValue(DirectStyleResolver resolver, string propertyName, RenderMode mode = RenderMode.Horizontal)
        {
            // Check if the property is auto
            if (resolver.IsAuto(propertyName))
            {
                return StyleValue.Auto;
            }

            // Resolve the length value
            float value = resolver.ResolveLengthValue(propertyName, float.NaN, mode);

            // If NaN, it's not a valid length
            if (float.IsNaN(value))
            {
                return StyleValue.Auto;
            }

            // Return as a pixel length
            return StyleValue.FromPixels(value);
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
}