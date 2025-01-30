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

        private ElementNode? RenderElement(
            Double rootFontSize,
            IElement reference,
            StyleCollection collection,
            ICssStyleDeclaration? parentComputedStyles = null)
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
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return node;
        }

        private IRenderNode? RenderText(IText text) => new TextNode(text);
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
            // 1) Check we have computed style for the current element
            var style = node.ComputedStyle;
            if (style is null)
            {
                node.Layout = null;
                return offsetYSoFar;
            }

            // 2) Parse the current element's margins, borders, padding
            bool marginLeftIsAuto = (style.GetPropertyValue("margin-left") == "auto");
            bool marginRightIsAuto = (style.GetPropertyValue("margin-right") == "auto");

            float marginLeftVal = ParsePx(style, "margin-left");
            float marginRightVal = ParsePx(style, "margin-right");
            float marginTopVal = ParsePx(style, "margin-top");
            float marginBottomVal = ParsePx(style, "margin-bottom");

            float borderLeft = ParsePx(style, "border-left-width");
            float borderRight = ParsePx(style, "border-right-width");
            float borderTop = ParsePx(style, "border-top-width");
            float borderBottom = ParsePx(style, "border-bottom-width");

            float paddingLeft = ParsePx(style, "padding-left");
            float paddingRight = ParsePx(style, "padding-right");
            float paddingTop = ParsePx(style, "padding-top");
            float paddingBottom = ParsePx(style, "padding-bottom");

            // (Optional) parse min-width / max-width if supporting them
            float minWidthVal     = ParsePx(style, "min-width");
            float maxWidthVal     = ParsePx(style, "max-width");

            // 3) Determine this element's content width
            float specifiedWidth = ParsePx(style, "width");

            // Sum of the element's margins/borders/padding on L+R sides
            float totalNonContent = marginLeftVal + borderLeft + paddingLeft
                                    + paddingRight + borderRight + marginRightVal;

            // Decide how wide the current element's content area should be
            float rawContentWidth;
            if (specifiedWidth > 0)
            {
                // The user specified an explicit width (e.g. "width:160px")
                rawContentWidth = specifiedWidth;
            }
            else
            {
                // "width: auto" => fill leftover from parent's available width
                rawContentWidth = context.AvailableWidth - totalNonContent;
            }

            // (Optional) clamp to min-width, max-width
            if (minWidthVal > 0 && rawContentWidth < minWidthVal)
            {
                rawContentWidth = minWidthVal;
            }
            if (maxWidthVal > 0 && rawContentWidth > maxWidthVal)
            {
                rawContentWidth = maxWidthVal;
            }
            if (rawContentWidth < 0) rawContentWidth = 0; // clamp to 0 if negative
            float contentWidth = rawContentWidth;

            // 4) Resolve auto margins, if any
            (float resolvedLeftMargin, float resolvedRightMargin) = ResolveAutoMargins(
                parentAvailableWidth: context.AvailableWidth,
                elementContentWidth: contentWidth,
                marginLeftVal,
                marginRightVal,
                marginLeftIsAuto,
                marginRightIsAuto
            );
            // 5) Compute the final X,Y of the current element
        //    We offset from the parent's coordinate plus our own margin/border/padding
        float elementX = context.ParentX
                         + borderLeft
                         + paddingLeft
                         + resolvedLeftMargin;

        float elementY = context.ParentY
                         + offsetYSoFar
                         + marginTopVal;  // vertical offset includes the top margin

        // 6) Layout this element's children (vertical stacking)
        //    We'll give them a new FormattingContext representing
        //    our content box as their parent.
        float childOffsetY = 0f;

        var childContext = new FormattingContext
        {
            // The left coordinate of our content box
            ParentX = elementX,
            // The top coordinate (already accounted for border/padding in elementY,
            // but if you want to offset inside more, you can add borderTop/paddingTop here).
            ParentY = elementY + borderTop + paddingTop,
            // The horizontal space available for children is our contentWidth
            AvailableWidth = contentWidth
        };

        // Recurse into our child nodes
        foreach (var child in node.Children)
        {
            if (child is null or TextNode)
                continue;

            childOffsetY = engine.ComputeLayoutForNode(child, childOffsetY, childContext);
        }

        // childOffsetY is how tall the children collectively are
        float contentHeight = childOffsetY;

        // 7) The final height of the current element includes its own border/padding
        float finalHeight = borderTop + paddingTop + contentHeight + paddingBottom + borderBottom;

        // 8) Store the layout box for this element
        node.Layout = new LayoutBox(elementX, elementY, contentWidth, finalHeight);

        // 9) Return the updated vertical offset so the next sibling is placed below
        float totalElementHeight = marginTopVal + finalHeight + marginBottomVal;
        return offsetYSoFar + totalElementHeight;
        }

        /// <summary>
        /// Distribute leftover space among margin-left/margin-right if they are "auto",
        /// in order to center or push the element left/right within the parent's content box.
        /// </summary>
        private static (float marginLeft, float marginRight) ResolveAutoMargins(
            float parentAvailableWidth,
            float elementContentWidth,
            float marginLeftVal,
            float marginRightVal,
            bool marginLeftIsAuto,
            bool marginRightIsAuto)
        {
            // The non-auto space used up so far: marginLeft + element's content + marginRight
            float usedNonAuto = marginLeftVal + elementContentWidth + marginRightVal;

            // leftoverSpace = how much is left in the parent's content box
            float leftoverSpace = parentAvailableWidth - usedNonAuto;
            if (leftoverSpace < 0) leftoverSpace = 0; // clamp if negative

            if (marginLeftIsAuto && marginRightIsAuto)
            {
                // Center horizontally
                marginLeftVal  = leftoverSpace / 2f;
                marginRightVal = leftoverSpace / 2f;
            }
            else if (marginLeftIsAuto)
            {
                // Push to the right
                marginLeftVal = leftoverSpace;
            }
            else if (marginRightIsAuto)
            {
                // Push to the left
                marginRightVal = leftoverSpace;
            }

            return (marginLeftVal, marginRightVal);
        }


        private float ParsePx(ICssStyleDeclaration style, string propName)
        {
            var raw = style.GetProperty(propName)?.RawValue;
            if (raw is CssLengthValue lv && lv.Type == CssLengthValue.Unit.Px)
            {
                return (float)lv.Value;
            }

            // If not found or "auto", return 0.
            return 0f;
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

        // You can add more fields if needed, e.g. min/max widths, parent's baseline, etc.
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
}