namespace AngleSharp.LayoutEngine.Adapters
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.LayoutEngine.Core;
    using AngleSharp.LayoutEngine.DOM;
    using System;
    using Css;

    /// <summary>
    /// Adapter that implements AngleSharp's IRenderDimensions interface to allow
    /// seamless integration with AngleSharp's unit conversion system.
    /// </summary>
    public class AngleSharpRenderDimensionsAdapter : IRenderDimensions
    {
        private readonly LayoutContext _layoutContext;

        /// <summary>
        /// Creates a new instance of the render dimensions adapter.
        /// </summary>
        /// <param name="layoutContext">The layout context providing dimensions.</param>
        public AngleSharpRenderDimensionsAdapter(LayoutContext layoutContext)
        {
            _layoutContext = layoutContext ?? throw new ArgumentNullException(nameof(layoutContext));
        }

        /// <summary>
        /// Gets the render height in pixels.
        /// </summary>
        public virtual double RenderHeight => _layoutContext.ViewportHeight;

        /// <summary>
        /// Gets the render width in pixels.
        /// </summary>
        public virtual double RenderWidth => _layoutContext.ViewportWidth;

        /// <summary>
        /// Gets the font size in pixels.
        /// </summary>
        public virtual double FontSize => _layoutContext.DefaultFontSize;

        /// <summary>
        /// Converts a CSS length value to pixels using AngleSharp's conversion.
        /// </summary>
        /// <param name="value">The CSS length value to convert.</param>
        /// <param name="mode">The render mode (horizontal or vertical).</param>
        /// <returns>The value in pixels.</returns>
        public float ConvertToPixel(CssLengthValue value, RenderMode mode = RenderMode.Horizontal)
        {
            return (float)value.ToPixel(this, mode);
        }

        /// <summary>
        /// Creates a render dimensions adapter for a specific node context.
        /// </summary>
        /// <param name="node">The node to create dimensions for.</param>
        /// <param name="baseAdapter">The base adapter to inherit from.</param>
        /// <returns>A new render dimensions adapter for the specific context.</returns>
        public static AngleSharpRenderDimensionsAdapter CreateForNode(LayoutNode node, AngleSharpRenderDimensionsAdapter baseAdapter)
        {
            return new ElementRenderDimensionsAdapter(node, baseAdapter);
        }

        /// <summary>
        /// Adapter for element-specific render dimensions.
        /// </summary>
        private class ElementRenderDimensionsAdapter : AngleSharpRenderDimensionsAdapter
        {
            private readonly LayoutNode _node;
            private readonly AngleSharpRenderDimensionsAdapter _baseAdapter;

            public ElementRenderDimensionsAdapter(LayoutNode node, AngleSharpRenderDimensionsAdapter baseAdapter)
                : base(baseAdapter._layoutContext)
            {
                _node = node ?? throw new ArgumentNullException(nameof(node));
                _baseAdapter = baseAdapter ?? throw new ArgumentNullException(nameof(baseAdapter));
            }

            /// <summary>
            /// Gets the render height (container height for percentage calculations).
            /// </summary>
            public override double RenderHeight => _node.Parent?.Box?.Height ?? _baseAdapter.RenderHeight;

            /// <summary>
            /// Gets the render width (container width for percentage calculations).
            /// </summary>
            public override double RenderWidth => _node.Parent?.Box?.Width ?? _baseAdapter.RenderWidth;

            /// <summary>
            /// Gets the font size for the element.
            /// </summary>
            public override double FontSize
            {
                get
                {
                    var element = _node.DomNode as ElementNode;
                    if (element?.ComputedStyle != null)
                    {
                        var property = element.ComputedStyle.GetProperty("font-size") as ICssProperty;
                        if (property?.RawValue is CssLengthValue lengthValue && lengthValue.Type == CssLengthValue.Unit.Px)
                        {
                            return lengthValue.Value;
                        }

                        var fontSize = element.ComputedStyle.GetPropertyValue("font-size");
                        if (!string.IsNullOrEmpty(fontSize) && fontSize.EndsWith("px"))
                        {
                            if (float.TryParse(fontSize.TrimEnd('p', 'x'), out float size))
                            {
                                return size;
                            }
                        }
                    }

                    return _baseAdapter.FontSize;
                }
            }
        }
    }
}