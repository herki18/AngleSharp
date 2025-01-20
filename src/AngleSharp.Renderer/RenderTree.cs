#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace AngleSharp.Renderer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Values;
    using AngleSharp.Dom;
    using Css;
    using Css.RenderTree;

    public class RenderTree
    {
        private readonly IBrowsingContext _context;
        private readonly IWindow _window;
        private readonly IEnumerable<ICssStyleSheet> _defaultSheets;
        private readonly IRenderDevice _device;

        public RenderTree(IWindow window, IRenderDevice? device = null)
        {
            var ctx = window.Document.Context;
            var defaultStyleSheetProvider = ctx.GetServices<ICssDefaultStyleSheetProvider>();
            _context = ctx;
            _device = device ?? ctx.GetService<IRenderDevice>() ?? throw new ArgumentNullException(nameof(device));
            _defaultSheets = defaultStyleSheetProvider.Select(m => m.Default).Where(m => m is not null);
            _window = window;
        }

        public IRenderNode? RenderDocument()
        {
            var document = _window.Document;
            var currentSheets = document.GetStyleSheets().OfType<ICssStyleSheet>();
            var stylesheets = _defaultSheets.Concat(currentSheets).ToList();
            var collection = new StyleCollection(stylesheets, _device);
            var rootStyle = collection.ComputeCascadedStyle(document.DocumentElement);
            var rootFontSize = ((CssLengthValue?)rootStyle.GetProperty(PropertyNames.FontSize)?.RawValue)?.Value ?? 16;
            return RenderElement(rootFontSize, document.DocumentElement, collection);
        }

        private ElementRenderNode? RenderElement(
            Double rootFontSize,
            IElement reference,
            StyleCollection collection,
            ICssStyleDeclaration? parentComputedStyles = null)
        {
            // Merge all stylesheets, including default stylesheets
            var style = collection.ComputeCascadedStyle(reference);

            var computedStyle = Compute(rootFontSize, style, parentComputedStyles);
            if (parentComputedStyles != null)
            {
                computedStyle?.UpdateDeclarations(parentComputedStyles);
            }

            var children = new List<IRenderNode?>();

            foreach (var child in reference.ChildNodes)
            {
                if (child is IText text)
                {
                    children.Add(RenderText(text));
                }
                else if (child is IElement element)
                {
                    children.Add(RenderElement(rootFontSize, element, collection, computedStyle));
                }
            }

            // compute unitless line-height after rendering children
            if (computedStyle?.GetProperty(PropertyNames.LineHeight).RawValue is CssLengthValue { Type: CssLengthValue.Unit.None } unitlessLineHeight)
            {
                var fontSize = computedStyle.GetProperty(PropertyNames.FontSize).RawValue is CssLengthValue { Type: CssLengthValue.Unit.Px } fontSizeLength ? fontSizeLength.Value : rootFontSize;
                var pixelValue = unitlessLineHeight.Value * fontSize;
                var computedLineHeight = new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);

                // create a new property because SetProperty would change the parent value
                var lineHeightProperty = _context.CreateProperty(PropertyNames.LineHeight);
                lineHeightProperty.RawValue = computedLineHeight;
                computedStyle.SetDeclarations(new[] { lineHeightProperty });
            }

            var node = new ElementRenderNode(reference, children!, style, computedStyle);

            foreach (var child in children)
            {
                if (child is ElementRenderNode elementChild)
                {
                    elementChild.Parent = node;
                }
                else if (child is TextRenderNode textChild)
                {
                    textChild.Parent = node;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return node;
        }

        private IRenderNode? RenderText(IText text) => new TextRenderNode(text);

        private CssStyleDeclaration? Compute(Double rootFontSize, ICssStyleDeclaration style, ICssStyleDeclaration? parentStyle)
        {
            var computedStyle = new CssStyleDeclaration(_context);
            var fontSize = ComputeFontSize(rootFontSize, style, parentStyle);

            var declarations = style.OfType<CssProperty>().Select(property =>
            {
                var name = property.Name;
                var value = property.RawValue;
                if (name == PropertyNames.FontSize)
                {
                    // font-size was already computed
                    value = new CssLengthValue(fontSize, CssLengthValue.Unit.Px);
                }
                else if (value is CssLengthValue { IsAbsolute: true, Type: not CssLengthValue.Unit.Px } absoluteLength)
                {
                    value = new CssLengthValue(absoluteLength.ToPixel(_device), CssLengthValue.Unit.Px);
                }
                else if (value is CssLengthValue { Type: CssLengthValue.Unit.Percent } percentLength)
                {
                    if (name == PropertyNames.VerticalAlign || name == PropertyNames.LineHeight)
                    {
                        var pixelValue = percentLength.Value / 100 * fontSize;
                        value = new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
                    }
                    else
                    {
                        // TODO: compute for other properties that should be absolute
                    }
                }
                else if (value is CssLengthValue { IsRelative: true, Type: not CssLengthValue.Unit.None } relativeLength)
                {
                    var pixelValue = relativeLength.Type switch
                    {
                        CssLengthValue.Unit.Em => relativeLength.Value * fontSize,
                        CssLengthValue.Unit.Rem => relativeLength.Value * rootFontSize,
                        _ => relativeLength.ToPixel(_device),
                    };
                    value = new CssLengthValue(pixelValue, CssLengthValue.Unit.Px);
                }

                return new CssProperty(name, property.Converter, property.Flags, value, property.IsImportant);
            });

            computedStyle.SetDeclarations(declarations);

            return computedStyle;

        }

        private Double ComputeFontSize(double rootFontSize, ICssStyleDeclaration style, ICssStyleDeclaration? parentStyle)
        {
            var parentFontSize = ((CssLengthValue?)parentStyle?.GetProperty(PropertyNames.FontSize)?.RawValue)?.ToPixel(_device) ?? rootFontSize;
            var fontSize = parentFontSize;
            // compute font-size first because other properties may depend on it
            if (style.GetProperty(PropertyNames.FontSize) is { RawValue: not null } fontSizeProperty)
            {
                fontSize = GetFontSizeInPixels(fontSizeProperty.RawValue, rootFontSize, parentFontSize, parentStyle);
            }

            return fontSize;
        }

        private Double GetFontSizeInPixels(ICssValue value, Double rootFontSize, Double parentFontSize, ICssStyleDeclaration? parentStyle)
        {
            return value switch
            {
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.XxSmall => 9D /
                    16 * rootFontSize,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.XSmall => 10D /
                    16 * rootFontSize,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.Small => 13D / 16 *
                    rootFontSize,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.Medium => 16D /
                    16 * rootFontSize,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.Large => 18D / 16 *
                    rootFontSize,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.XLarge => 24D /
                    16 * rootFontSize,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.XxLarge => 32D /
                    16 * rootFontSize,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.XxxLarge => 48D /
                    16 * rootFontSize,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.Smaller =>
                    ComputeRelativeFontSize(constLength, rootFontSize, parentStyle),
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.Larger =>
                    ComputeRelativeFontSize(constLength, rootFontSize, parentStyle),
                CssLengthValue { Type: CssLengthValue.Unit.Px } length => length.Value,
                CssLengthValue { IsAbsolute: true } length => length.ToPixel(_device),
                CssLengthValue
                {
                    Type: CssLengthValue.Unit.Vh or CssLengthValue.Unit.Vw or CssLengthValue.Unit.Vmax
                    or CssLengthValue.Unit.Vmin
                } length => length.ToPixel(_device),
                CssLengthValue { IsRelative: true } length => ComputeRelativeFontSize(length, rootFontSize, parentStyle),
                ICssSpecialValue specialValue when specialValue.CssText == CssKeywords.Inherit ||
                                                   specialValue.CssText == CssKeywords.Unset => parentFontSize,
                ICssSpecialValue specialValue when specialValue.CssText == CssKeywords.Initial => rootFontSize,
                _ => throw new InvalidOperationException("Font size must be a length"),
            };
        }

        private Double ComputeRelativeFontSize(ICssValue value, Double rootFontSize, ICssStyleDeclaration? parentStyle)
        {
            var ancestorValue = parentStyle?.GetProperty(PropertyNames.FontSize)?.RawValue;
            var ancestorPixels = ancestorValue switch
            {
                CssLengthValue { IsAbsolute: true } ancestorLength => ancestorLength.ToPixel(_device),
                null => rootFontSize,
                _ => throw new InvalidOperationException(),
            };

            // set a minimum size of 9px for relative sizes
            return Math.Max(9, value switch
            {
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.Smaller => ancestorPixels / 1.2,
                CssConstantValue<CssLengthValue> constLength when constLength.CssText == CssKeywords.Larger => ancestorPixels * 1.2,
                CssLengthValue { Type: CssLengthValue.Unit.Rem } length => length.Value * rootFontSize,
                CssLengthValue { Type: CssLengthValue.Unit.Em } length => length.Value * ancestorPixels,
                CssLengthValue { Type: CssLengthValue.Unit.Percent } length => length.Value / 100 * ancestorPixels,
                _ => throw new InvalidOperationException(),
            });
        }
    }

    public interface ILayoutStrategy
    {
        /// <summary>
        /// Return true if this strategy can handle the given element node's display type, etc.
        /// </summary>
        bool CanHandle(ElementRenderNode node);

        /// <summary>
        /// Perform the layout for this element node.
        /// - parentX, parentY: the top-left of the containing block
        /// - containerWidth: the width available for this node
        /// - offsetYSoFar: how far we've already stacked in the parent (for block flow)
        /// - engine: reference back to the layout engine (for child recursion, text layout, etc.)
        /// Returns the new offset after placing this element.
        /// </summary>
        float LayoutNode(
            ElementRenderNode node,
            float parentX,
            float parentY,
            float containerWidth,
            float offsetYSoFar,
            LayoutEngine engine);
    }

    public sealed class LayoutEngine
    {
        private readonly List<ILayoutStrategy> _strategies;

        public LayoutEngine()
        {
            // Initialize your known strategies:
            _strategies = new List<ILayoutStrategy>
            {
                // new BlockLayoutStrategy(),
                // new FlexLayoutStrategy()
            };
        }

        /// <summary>
        /// The top-level call, e.g. from your RenderDocument().
        /// The user supplies a rootNode (often the html or body), plus a viewport width/height.
        /// </summary>
        public void LayoutDocument(IRenderNode? rootNode, float viewportWidth, float viewportHeight)
        {
            if (rootNode == null) return;

            // Start layout at origin (0,0)
            ComputeLayoutForNode(rootNode, 0f, 0f, viewportWidth, 0f);
        }

        /// <summary>
        /// A recursive method that dispatches to the correct layout strategy or
        /// handles text nodes directly.
        /// </summary>
        public float ComputeLayoutForNode(
            IRenderNode node,
            float parentX,
            float parentY,
            float containerWidth,
            float offsetYSoFar)
        {
            // If it's an element node, pick the right strategy
            if (node is ElementRenderNode elemNode)
            {
                // Attempt to find a matching layout strategy
                var strategy = _strategies.FirstOrDefault(s => s.CanHandle(elemNode));
                if (strategy != null)
                {
                    return strategy.LayoutNode(elemNode, parentX, parentY, containerWidth, offsetYSoFar, this);
                }
                else
                {
                    // If no strategy found, treat as block fallback
                    return _strategies[0].LayoutNode(elemNode, parentX, parentY, containerWidth, offsetYSoFar, this);
                }
            }
            else if (node is TextRenderNode textNode)
            {
                return LayoutTextNode(textNode, parentX, parentY, containerWidth, offsetYSoFar);
            }

            // Unknown node => do nothing
            return offsetYSoFar;
        }

        /// <summary>
        /// Very naive text layout (single line, no wrap).
        /// </summary>
        private float LayoutTextNode(
            TextRenderNode textNode,
            float parentX,
            float parentY,
            float containerWidth,
            float offsetYSoFar)
        {
            var textContent = textNode.Ref.TextContent ?? string.Empty;
            if (string.IsNullOrWhiteSpace(textContent))
            {
                textNode.Layout = null;
                return offsetYSoFar;
            }

            // A naive assumption: 7px per character, line height = 16px
            float approxCharWidth = 7f;
            float lineHeight = 16f;
            float textWidth = textContent.Length * approxCharWidth;

            if (textWidth > containerWidth)
            {
                // No wrapping => might overflow.
                // Real approach: you'd split lines or clamp. We'll just let it overflow.
            }

            float x = parentX;
            float y = parentY + offsetYSoFar;

            textNode.Layout = new LayoutBox(x, y, textWidth, lineHeight);

            // Increase offset so next sibling is below this line
            return offsetYSoFar + lineHeight;
        }
    }
}
