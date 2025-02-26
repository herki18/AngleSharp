#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace AngleSharp.Renderer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using AngleSharp.Css.Dom;
    using Css.Values;
    using Dom;
    using Css;
    using Html.Dom;

    public class RenderTreeBuilder
    {
        private readonly IBrowsingContext _context;
        private readonly IWindow _window;
        private readonly IEnumerable<ICssStyleSheet> _defaultSheets;
        private readonly IRenderDevice _device;
        private readonly StyleResolver _styleResolver;

        public RenderTreeBuilder(IWindow window, IRenderDevice? device = null)
        {
            var ctx = window.Document.Context;
            var defaultStyleSheetProvider = ctx.GetServices<ICssDefaultStyleSheetProvider>();
            _context = ctx;
            _device = device ?? ctx.GetService<IRenderDevice>() ?? throw new ArgumentNullException(nameof(device));
            _defaultSheets = defaultStyleSheetProvider.Select(m => m.Default).Where(m => m is not null);
            _window = window;
            _styleResolver = new StyleResolver(ctx, _device);
        }

        public IRenderNode? RenderDocument()
        {
            var document = _window.Document;
            var currentSheets = document.GetStyleSheets().OfType<ICssStyleSheet>();
            var stylesheets = _defaultSheets.Concat(currentSheets).ToList();
            var collection = new StyleCollection(stylesheets, _device);

            // 1. Compute root style
            var rootStyle = collection.ComputeCascadedStyle(document.DocumentElement);
            var rootFontSize = ((CssLengthValue?)rootStyle.GetProperty(PropertyNames.FontSize)?.RawValue)?.Value ?? 16;

            // 2. Build the render tree
            var rootNode = RenderElement(rootFontSize, document.DocumentElement, collection);

            return rootNode;
        }

        private IRenderNode? RenderElement(
            Double rootFontSize,
            IElement reference,
            StyleCollection collection,
            ICssStyleDeclaration? parentComputedStyles = null)
        {
            bool isNonRenderable = IsNonRenderableElement(reference);

            if (isNonRenderable)
            {
                return HandleNonRenderableElement(rootFontSize, reference, collection, parentComputedStyles);
            }
            else
            {
                return HandleRenderableElement(rootFontSize, reference, collection, parentComputedStyles);
            }
        }

        private IRenderNode? HandleNonRenderableElement(
            double rootFontSize,
            IElement element,
            StyleCollection collection,
            ICssStyleDeclaration? parentComputedStyles)
        {
            // Convert ALL children to NonRenderableNode (no recursion needed)
            var children = element.ChildNodes
                .Select<INode, IRenderNode?>(child =>
                    new NonRenderableNode(child, Enumerable.Empty<IRenderNode?>()!))
                .ToList();

            var node = new NonRenderableNode(element, children!);

            // Set parent references
            foreach (var child in children)
            {
                if (child is NonRenderableNode nonRenderableChild)
                {
                    nonRenderableChild.Parent = node;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return node;
        }

        private ElementNode? HandleRenderableElement(
            double rootFontSize,
            IElement reference,
            StyleCollection collection,
            ICssStyleDeclaration? parentComputedStyles)
        {
            // Merge all stylesheets, including default stylesheets
            var style = _styleResolver.ComputeCascadedStyle(reference, collection);
            var computedStyle = _styleResolver.ComputeComputedStyle(style, parentComputedStyles, rootFontSize);

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

            var node = new ElementNode(reference, children!, style, computedStyle);

            foreach (var child in children)
            {
                if (child is ElementNode elementChild)
                {
                    elementChild.Parent = node;
                }
                else if (child is TextNode textChild)
                {
                    textChild.Parent = node;
                }
                else if (child is NonRenderableNode nonRenderableChild)
                {
                    nonRenderableChild.Parent = node;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return node;
        }

        private static bool IsNonRenderableElement(IElement element)
        {
            if (element is IHtmlHeadElement or
                IHtmlScriptElement or
                IHtmlStyleElement)
            {
                return true;
            }

            return false;
        }

        private IRenderNode? RenderText(IText text)
        {
            var content = text.TextContent;

            // Treat empty/whitespace text as NonRenderableNode
            return string.IsNullOrWhiteSpace(content)
                ? new NonRenderableNode(text, Enumerable.Empty<IRenderNode?>()!)
                : new TextNode(text);
        }
    }

    public sealed class StyleResolver
    {
        private readonly IBrowsingContext _context;
        private readonly IRenderDevice _device;
        private readonly FontEngine _fontEngine;

        public StyleResolver(IBrowsingContext context, IRenderDevice device)
        {
            _context = context;
            _device = device;
            _fontEngine = new FontEngine(context, device);
        }

        public ICssStyleDeclaration ComputeCascadedStyle(IElement element, StyleCollection collection)
        {
            return collection.ComputeCascadedStyle(element);
        }

        public CssStyleDeclaration? ComputeComputedStyle(
            ICssStyleDeclaration specifiedStyle,
            ICssStyleDeclaration? parentComputedStyle,
            Double rootFontSize)
        {
            var computedStyle = new CssStyleDeclaration(_context);
            var fontSize = ComputeFontSize(rootFontSize, specifiedStyle, parentComputedStyle);

            foreach (var initialValue in InitialValues)
            {
                specifiedStyle.SetDefaultProperty(initialValue.Key, initialValue.Value);
            }

            var declarations = specifiedStyle.OfType<CssProperty>().Select(property =>
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
                        Debug.Write("Works");
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

            if (parentComputedStyle != null)
            {
                computedStyle?.UpdateDeclarations(parentComputedStyle);
            }

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

        private static readonly Dictionary<string, string> InitialValues = new()
        {
            // Layout
            { PropertyNames.Display, CssKeywords.Inline },
            { PropertyNames.Position, CssKeywords.Static },
            { PropertyNames.Width, CssKeywords.Auto },
            { PropertyNames.Height, CssKeywords.Auto },
            { PropertyNames.MarginTop, "0px" },
            { PropertyNames.MarginRight, "0px" },
            // ... other margin/padding properties

            // Typography
            { PropertyNames.FontSize, "16px" },
            { PropertyNames.LineHeight, CssKeywords.Normal },
            // { PropertyNames.Color, Color.Black },

            // Borders
            { PropertyNames.BorderTopWidth, "px" },
            { PropertyNames.BorderTopStyle, CssKeywords.None },
            // ... other border properties

            // Add all other properties here...
        };

        private Double GetFontSizeInPixels(ICssValue value, Double rootFontSize, Double parentFontSize, ICssStyleDeclaration? parentStyle)
        {
            return value switch
            {
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.XxSmall } => 9D / 16 * rootFontSize,
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.XSmall } => 10D / 16 * rootFontSize,
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.Small } => 13D / 16 * rootFontSize,
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.Medium } => 16D / 16 * rootFontSize,
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.Large } => 18D / 16 * rootFontSize,
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.XLarge } => 24D / 16 * rootFontSize,
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.XxLarge } => 32D / 16 * rootFontSize,
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.XxxLarge } => 48D / 16 * rootFontSize,
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.Smaller } constLength =>
                    ComputeRelativeFontSize(constLength, rootFontSize, parentStyle),
                CssConstantValue<CssLengthValue> { CssText: CssKeywords.Larger } constLength => ComputeRelativeFontSize(constLength, rootFontSize, parentStyle),
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
                ICssSpecialValue { CssText: CssKeywords.Initial } => rootFontSize,
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

    public sealed class FontEngine
    {
        private readonly IRenderDevice _device;
        private readonly IBrowsingContext _context;

        public FontEngine(IBrowsingContext context, IRenderDevice device)
        {
            _context = context;
            _device = device;
        }

        public Double ComputeFontSize(
            ICssStyleDeclaration style,
            ICssStyleDeclaration? parentStyle,
            Double rootFontSize)
        {
            if (style.GetProperty(PropertyNames.FontSize)?.RawValue is not ICssValue value)
                return parentStyle?.GetProperty(PropertyNames.FontSize)?.RawValue switch
                {
                    CssLengthValue parentLength => parentLength.Value,
                    _ => rootFontSize
                };

            return value switch
            {
                CssLengthValue length => length.ToPixel(_device),
                _ => GetKeywordFontSize(value.CssText, rootFontSize)
            };
        }

        public ICssValue? ConvertUnits(
            ICssValue value,
            double currentFontSize,
            double rootFontSize,
            ICssStyleDeclaration? parentStyle)
        {
            // Handle unit conversions (em, rem, %, etc.)
            return null;
        }

        private Double GetKeywordFontSize(String keyword, Double rootFontSize)
        {
            return keyword switch
            {
                CssKeywords.XxSmall => 9,
                CssKeywords.XSmall => 10,
                CssKeywords.Small => 13,
                CssKeywords.Medium => 16,
                CssKeywords.Large => 18,
                CssKeywords.XLarge => 24,
                CssKeywords.XxLarge => 32,
                CssKeywords.XxxLarge => 48,
                _ => rootFontSize
            };
        }
    }

    public interface ILayoutStrategy
    {
        /// <summary>
        /// Return true if this strategy can handle the given element node's display type, etc.
        /// </summary>
        bool CanHandle(ElementNode node);

        /// <summary>
        /// Perform the layout for this element node.
        /// - parentX, parentY: the top-left of the containing block
        /// - containerWidth: the width available for this node
        /// - offsetYSoFar: how far we've already stacked in the parent (for block flow)
        /// - engine: reference back to the layout engine (for child recursion, text layout, etc.)
        /// Returns the new offset after placing this element.
        /// </summary>
        float LayoutNode(
            ElementNode node,
            float offsetYSoFar,
            FormattingContext context,
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
                new BlockLayoutStrategy(),
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
            ComputeLayoutForNode(rootNode, 0f, new FormattingContext() { ParentY = 0f, ParentX = 0f, AvailableWidth = viewportWidth });
        }

        /// <summary>
        /// A recursive method that dispatches to the correct layout strategy or
        /// handles text nodes directly.
        /// </summary>
        public Single ComputeLayoutForNode(
            IRenderNode node,
            Single offsetYSoFar,
            FormattingContext context)
        {
            if (node is ElementNode elementNode && elementNode.Ref is IHtmlHeadElement)
            {
                elementNode.Layout = null;
                return offsetYSoFar;
            }

            // If it's an element node, pick the right strategy
            if (node is ElementNode elemNode)
            {
                // Attempt to find a matching layout strategy
                var strategy = _strategies.FirstOrDefault(s => s.CanHandle(elemNode));
                if (strategy != null)
                {
                    return strategy.LayoutNode(elemNode, offsetYSoFar, context, this);
                }
                else
                {
                    // If no strategy found, treat as block fallback
                    return _strategies[0].LayoutNode(elemNode, offsetYSoFar, context, this);
                }
            }
            else if (node is TextNode textNode)
            {
                return LayoutTextNode(textNode, offsetYSoFar, context);
            }

            // Unknown node => do nothing
            return offsetYSoFar;
        }

        /// <summary>
        /// Very naive text layout (single line, no wrap).
        /// </summary>
        private Single LayoutTextNode(
            TextNode textNode,
            Single offsetYSoFar,
            FormattingContext context)
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

            if (textWidth > context.AvailableWidth)
            {
                // No wrapping => might overflow.
                // Real approach: you'd split lines or clamp. We'll just let it overflow.
            }

            float x = context.ParentX;
            float y = context.ParentY + offsetYSoFar;

            textNode.Layout = new LayoutBox(x, y, textWidth, lineHeight);

            // Increase offset so next sibling is below this line
            return offsetYSoFar + lineHeight;
        }
    }

    public sealed class BlockLayoutStrategy : ILayoutStrategy
    {
        public bool CanHandle(ElementNode node)
        {
            if (node.ComputedStyle is null)
                return false;

            var display = node.ComputedStyle.GetPropertyValue("display");

            return display == "block"
                   || display == "list-item"
                   || string.IsNullOrEmpty(display);
        }

        /// <summary>
        /// Lay out the "node" within the given formatting context,
        /// returning how much vertical space we've consumed (offsetYSoFar + element's total height).
        /// </summary>
        public float LayoutNode(
            ElementNode node,
            float offsetYSoFar,
            FormattingContext context,
            LayoutEngine engine)
        {
            if (node.ComputedStyle is null)
            {
                node.Layout = null;
                return offsetYSoFar;
            }

            // 1️⃣ Compute Box Model using the available width from the context.
            var box = new BoxModelCalculator(node.ComputedStyle, context.AvailableWidth);

            // 2️⃣ Resolve constraints.
            var constraints = new LayoutConstraints(node.ComputedStyle);
            float finalContentWidth = constraints.ApplyWidth(box.ContentWidth);

            // 3️⃣ Collapse margins.
            float collapsedMargin = MarginCollapser.Collapse(context.PreviousMarginBottom, box.MarginTop);

            // 4️⃣ Determine positioning.
            (float posX, float posY) = PositioningResolver.CalculateElementPosition(
                node.ComputedStyle, context.ParentX, context.ParentY, context.AvailableWidth, finalContentWidth);
            posY += collapsedMargin;

            // 5️⃣ Layout children within this element's content area.
            float childOffsetY = 0f;
            var childContext = new FormattingContext
            {
                ParentX = posX,
                ParentY = posY + box.BorderTop + box.PaddingTop,
                AvailableWidth = finalContentWidth,
                PreviousMarginBottom = 0f
            };

            foreach (var child in node.Children)
            {
                if (child != null)
                {
                    childOffsetY = engine.ComputeLayoutForNode(child, childOffsetY, childContext);
                }
            }

            // 6️⃣ Determine content height:
            // If the computed height is "auto" (NaN), use the total height from children.
            float contentHeight = float.IsNaN(box.ContentHeight) ? childOffsetY : box.ContentHeight;
            float finalContentHeight = constraints.ApplyHeight(contentHeight);

            // 7️⃣ Compute overall element dimensions.
            // Choose the final width based on box-sizing.
            // For border-box, the computed finalContentWidth already represents the total outer width.
            // For content-box, use box.BoxWidth, which adds borders (and padding) to the content width.
            string boxSizing = node.ComputedStyle.GetPropertyValue("box-sizing");
            if (string.IsNullOrEmpty(boxSizing))
            {
                boxSizing = "content-box"; // Default per CSS spec.
            }

            float totalWidth = (boxSizing == "border-box") ? finalContentWidth : box.BoxWidth;
            float totalHeight = finalContentHeight;

            // 8️⃣ Create and assign a new LayoutBox with all the details.
            var layoutBox = new LayoutBox(posX, posY, totalWidth, totalHeight)
            {
                // Save box model metrics for later use (e.g. hit testing, alignment, etc.)
                MarginTop = box.MarginTop,
                MarginRight = box.MarginRight,
                MarginBottom = box.MarginBottom,
                MarginLeft = box.MarginLeft,
                BorderTop = box.BorderTop,
                BorderRight = box.BorderRight,
                BorderBottom = box.BorderBottom,
                BorderLeft = box.BorderLeft,
                PaddingTop = box.PaddingTop,
                PaddingRight = box.PaddingRight,
                PaddingBottom = box.PaddingBottom,
                PaddingLeft = box.PaddingLeft,
                // Relative positions (relative to the parent's coordinate system)
                RelativeX = posX - context.ParentX,
                RelativeY = posY - context.ParentY
            };

            node.Layout = layoutBox;

            // 9️⃣ Return the updated vertical offset.
            float totalElementHeight = box.MarginTop + totalHeight + box.MarginBottom;
            return offsetYSoFar + totalElementHeight;
        }
    }

    /// <summary>
    /// Contains the layout info that a parent passes down to its child.
    /// In a block layout, we primarily need:
    /// - Where to place the child (ParentX, ParentY)
    /// - How wide the parent's content box is (AvailableWidth)
    /// </summary>
    public struct FormattingContext
    {
        public float ParentX;
        public float ParentY;
        public float AvailableWidth;

        public float PreviousMarginBottom;
    }
}