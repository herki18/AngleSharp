namespace LayoutEngine.Core.Layout.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Core.Core;
using LayoutEngine.Core.Layout.Public;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;
using FragmentTreeUpdatedEvent = Events.FragmentTreeUpdatedEvent;
using LayoutInvalidatedEvent = Events.LayoutInvalidatedEvent;

public class LayoutSystem : ILayoutSystem
{
    private readonly IStyleSystem _styleSystem;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<LayoutSystem> _logger;
    private ILayoutResult? _currentLayoutResult;

    public LayoutSystem(
        IStyleSystem styleSystem,
        IEventAggregator eventAggregator,
        ILogger<LayoutSystem> logger)
    {
        _styleSystem = styleSystem ?? throw new ArgumentNullException(nameof(styleSystem));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ILayoutResult PerformLayout(IDocument document)
    {
        _logger.LogDebug("Starting layout for document");

        // Always compute document styles first
        _styleSystem.ComputeDocumentStyles(document);

        if (MockLayoutData.UseMockData)
        {
            _logger.LogDebug("Using mock layout data");
            var mockResult = CreateMockLayoutResult(document);
            _currentLayoutResult = mockResult;
            ClearLayoutFlags(document.DocumentElement);
            _eventAggregator.Publish(new FragmentTreeUpdatedEvent(mockResult));
            return mockResult;
        }

        var result = new LayoutResult();
        if (document.DocumentElement != null)
        {
            var viewportWidth = 800f;
            var viewportHeight = 600f;
            var context = new LayoutContext(viewportWidth, viewportHeight);
            var rootFragment = LayoutElement(document.DocumentElement, context, result);
            result.SetRootFragment(rootFragment);
        }

        _currentLayoutResult = result;
        ClearLayoutFlags(document.DocumentElement);
        _eventAggregator.Publish(new FragmentTreeUpdatedEvent(result));
        return result;
    }

    private ILayoutResult CreateMockLayoutResult(IDocument document)
    {
        var result = new LayoutResult();
        if (document.DocumentElement != null)
        {
            // Create layout fragments for the actual document structure
            var rootFragment = CreateMockFragment(document.DocumentElement, new Rect(0, 0, 800, 600), result);
            result.SetRootFragment(rootFragment);
        }
        return result;
    }

    private ILayoutFragment CreateMockFragment(IElement element, Rect bounds, LayoutResult result)
    {
        // Create visual properties based on element type and inline styles
        var visualProps = new VisualProperties
        {
            BackgroundColor = "white", // Default white background
            FontSize = 16,
            Color = "black"
        };

        // Parse inline styles if present
        var styleAttr = element.GetAttribute("style");
        if (!string.IsNullOrEmpty(styleAttr))
        {
            var stylePairs = styleAttr.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in stylePairs)
            {
                var parts = pair.Split(':', 2).Select(p => p.Trim()).ToArray();
                if (parts.Length == 2)
                {
                    var property = parts[0].ToLower();
                    var value = parts[1].Trim();

                    switch (property)
                    {
                        case "background-color":
                            visualProps.BackgroundColor = value;
                            _logger.LogDebug("Setting background-color: {Value} for {TagName}", value, element.TagName);
                            break;
                        case "color":
                            visualProps.Color = value;
                            _logger.LogDebug("Setting color: {Value} for {TagName}", value, element.TagName);
                            break;
                        case "font-size":
                            if (value.EndsWith("px") && float.TryParse(value.AsSpan(0, value.Length - 2), out var fontSize))
                            {
                                visualProps.FontSize = fontSize;
                                _logger.LogDebug("Setting font-size: {FontSize} for {TagName}", fontSize, element.TagName);
                            }
                            break;
                    }
                }
            }
        }

        var fragment = new LayoutFragment
        {
            Element = element,
            Bounds = bounds,
            VisualProperties = visualProps
        };

        _logger.LogDebug("Created fragment for {TagName} at {X},{Y} {Width}x{Height} with bg: {BackgroundColor}",
            element.TagName, bounds.X, bounds.Y, bounds.Width, bounds.Height, visualProps.BackgroundColor);

        // Create layout info for the element
        var layoutInfo = new LayoutInfo
        {
            ContentRect = bounds,
            PaddingRect = new Rect(bounds.X - 5, bounds.Y - 5, bounds.Width + 10, bounds.Height + 10),
            BorderRect = new Rect(bounds.X - 10, bounds.Y - 10, bounds.Width + 20, bounds.Height + 20),
            MarginRect = new Rect(bounds.X - 15, bounds.Y - 15, bounds.Width + 30, bounds.Height + 30),
            Position = new Point(bounds.X, bounds.Y)
        };
        layoutInfo.AddFragment(fragment);
        result.SetLayoutInfo(element, layoutInfo);
        result.AddFragment(element, fragment);

        // Process children
        var childFragments = new List<ILayoutFragment>();
        var childY = 20f;

        foreach (var child in element.Children.OfType<IElement>())
        {
            var childBounds = new Rect(20, childY, bounds.Width - 40, 50);
            var childFragment = CreateMockFragment(child, childBounds, result);
            childFragments.Add(childFragment);
            childY += 60;
        }

        // Also process text nodes
        foreach (var child in element.ChildNodes)
        {
            if (child.NodeType == (int)NodeType.Text && !string.IsNullOrWhiteSpace(child.TextContent))
            {
                var textBounds = new Rect(20, childY, bounds.Width - 40, 30);
                var textFragment = new LayoutFragment
                {
                    Element = null, // Text nodes don't have an element
                    Bounds = textBounds,
                    VisualProperties = new VisualProperties
                    {
                        Color = visualProps.Color,
                        FontSize = visualProps.FontSize,
                        BackgroundColor = "transparent"
                    }
                };

                // Store text content in a custom property for now
                // This is a limitation of the current design
                if (textFragment.VisualProperties is VisualProperties vp)
                {
                    // We need to pass text content through somehow
                    // For now, just log it
                    _logger.LogDebug("Created text fragment: '{TextContent}' at {X},{Y}",
                        child.TextContent, textBounds.X, textBounds.Y);
                }

                childFragments.Add(textFragment);
                childY += 40;
            }
        }

        fragment.Children = childFragments;
        return fragment;
    }

    private void ClearLayoutFlags(IElement? element)
    {
        if (element == null) return;

        element.ClearNeedsLayout();
        element.ClearChildNeedsLayout();

        foreach (var child in element.Children.OfType<IElement>())
        {
            ClearLayoutFlags(child);
        }
    }

    private ILayoutFragment LayoutElement(IElement element, LayoutContext context, LayoutResult result)
    {
        var style = _styleSystem.GetComputedStyle(element);
        if (style == null)
        {
            style = _styleSystem.ComputeStyle(element);
        }

        var fragment = new LayoutFragment
        {
            Element = element,
            VisualProperties = ExtractVisualProperties(style)
        };

        var displayValue = style.GetPropertyValue("display");
        switch (displayValue)
        {
            case "block":
                LayoutBlockElement(element, style, fragment, context, result);
                break;
            case "flex":
                LayoutFlexElement(element, style, fragment, context, result);
                break;
            case "inline":
            case "inline-block":
                LayoutInlineElement(element, style, fragment, context, result);
                break;
            case "none":
                break;
            default:
                // Default to block layout for unknown display types
                LayoutBlockElement(element, style, fragment, context, result);
                break;
        }

        result.AddFragment(element, fragment);
        return fragment;
    }

    private void LayoutBlockElement(IElement element, IComputedStyle style, LayoutFragment fragment,
        LayoutContext context, LayoutResult result)
    {
        var boxModel = CalculateBoxModel(element, style, context);
        fragment.Bounds = boxModel.ContentRect;

        var layoutInfo = new LayoutInfo
        {
            ContentRect = boxModel.ContentRect,
            PaddingRect = boxModel.PaddingRect,
            BorderRect = boxModel.BorderRect,
            MarginRect = boxModel.MarginRect,
            Position = new Point(boxModel.ContentRect.X, boxModel.ContentRect.Y)
        };
        layoutInfo.AddFragment(fragment);
        result.SetLayoutInfo(element, layoutInfo);

        var childContext = new LayoutContext(
            boxModel.ContentRect.Width,
            boxModel.ContentRect.Height,
            boxModel.ContentRect.X,
            boxModel.ContentRect.Y
        );

        var children = new List<ILayoutFragment>();
        foreach (var child in element.Children.OfType<IElement>())
        {
            var childFragment = LayoutElement(child, childContext, result);
            children.Add(childFragment);
        }

        fragment.Children = children;
    }

    private void LayoutFlexElement(IElement element, IComputedStyle style, LayoutFragment fragment,
        LayoutContext context, LayoutResult result)
    {
        // TODO: Implement flex layout
        _logger.LogDebug("Flex layout not implemented, falling back to block layout");
        LayoutBlockElement(element, style, fragment, context, result);
    }

    private void LayoutInlineElement(IElement element, IComputedStyle style, LayoutFragment fragment,
        LayoutContext context, LayoutResult result)
    {
        // TODO: Implement inline layout
        _logger.LogDebug("Inline layout not implemented, falling back to block layout");
        LayoutBlockElement(element, style, fragment, context, result);
    }

    private BoxModel CalculateBoxModel(IElement element, IComputedStyle style, LayoutContext context)
    {
        var marginTop = ParseLength(style.GetPropertyValue("margin-top"), context.ContainerWidth, 0);
        var marginRight = ParseLength(style.GetPropertyValue("margin-right"), context.ContainerWidth, 0);
        var marginBottom = ParseLength(style.GetPropertyValue("margin-bottom"), context.ContainerWidth, 0);
        var marginLeft = ParseLength(style.GetPropertyValue("margin-left"), context.ContainerWidth, 0);

        var borderTop = ParseLength(style.GetPropertyValue("border-top-width"), context.ContainerWidth, 0);
        var borderRight = ParseLength(style.GetPropertyValue("border-right-width"), context.ContainerWidth, 0);
        var borderBottom = ParseLength(style.GetPropertyValue("border-bottom-width"), context.ContainerWidth, 0);
        var borderLeft = ParseLength(style.GetPropertyValue("border-left-width"), context.ContainerWidth, 0);

        var paddingTop = ParseLength(style.GetPropertyValue("padding-top"), context.ContainerWidth, 0);
        var paddingRight = ParseLength(style.GetPropertyValue("padding-right"), context.ContainerWidth, 0);
        var paddingBottom = ParseLength(style.GetPropertyValue("padding-bottom"), context.ContainerWidth, 0);
        var paddingLeft = ParseLength(style.GetPropertyValue("padding-left"), context.ContainerWidth, 0);

        float width;
        var widthValue = style.GetPropertyValue("width");
        if (!string.IsNullOrEmpty(widthValue))
        {
            width = ParseLength(widthValue, context.ContainerWidth, context.ContainerWidth);
        }
        else
        {
            var displayValue = style.GetPropertyValue("display");
            if (displayValue == "block")
            {
                width = context.ContainerWidth - marginLeft - marginRight - borderLeft - borderRight - paddingLeft -
                        paddingRight;
            }
            else
            {
                width = 100;
            }
        }

        float height;
        var heightValue = style.GetPropertyValue("height");
        if (!string.IsNullOrEmpty(heightValue))
        {
            height = ParseLength(heightValue, context.ContainerHeight, context.ContainerHeight);
        }
        else
        {
            height = 50;
        }

        var x = context.X + marginLeft + borderLeft + paddingLeft;
        var y = context.Y + marginTop + borderTop + paddingTop;

        return new BoxModel
        {
            ContentRect = new Rect(x, y, width, height),
            PaddingRect = new Rect(
                x - paddingLeft,
                y - paddingTop,
                width + paddingLeft + paddingRight,
                height + paddingTop + paddingBottom
            ),
            BorderRect = new Rect(
                x - paddingLeft - borderLeft,
                y - paddingTop - borderTop,
                width + paddingLeft + paddingRight + borderLeft + borderRight,
                height + paddingTop + paddingBottom + borderTop + borderBottom
            ),
            MarginRect = new Rect(
                x - paddingLeft - borderLeft - marginLeft,
                y - paddingTop - borderTop - marginTop,
                width + paddingLeft + paddingRight + borderLeft + borderRight + marginLeft + marginRight,
                height + paddingTop + paddingBottom + borderTop + borderBottom + marginTop + marginBottom
            )
        };
    }

    private float ParseLength(string? value, float containerSize, float defaultValue)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;

        if (value.EndsWith("px"))
        {
            if (float.TryParse(value.AsSpan(0, value.Length - 2), out var px))
            {
                return px;
            }
        }
        else if (value.EndsWith("%"))
        {
            if (float.TryParse(value.AsSpan(0, value.Length - 1), out var percent))
            {
                return containerSize * percent / 100f;
            }
        }
        else if (value == "auto")
        {
            return defaultValue;
        }
        else if (float.TryParse(value, out var number))
        {
            return number;
        }

        return defaultValue;
    }

    private IVisualProperties ExtractVisualProperties(IComputedStyle style)
    {
        return new VisualProperties
        {
            BackgroundColor = style.GetPropertyValue("background-color"),
            BorderTopWidth = float.TryParse(style.GetPropertyValue("border-top-width"), out var btw) ? btw : 0,
            BorderRightWidth = float.TryParse(style.GetPropertyValue("border-right-width"), out var brw) ? brw : 0,
            BorderBottomWidth = float.TryParse(style.GetPropertyValue("border-bottom-width"), out var bbw) ? bbw : 0,
            BorderLeftWidth = float.TryParse(style.GetPropertyValue("border-left-width"), out var blw) ? blw : 0,
            BorderTopColor = style.GetPropertyValue("border-top-color"),
            BorderRightColor = style.GetPropertyValue("border-right-color"),
            BorderBottomColor = style.GetPropertyValue("border-bottom-color"),
            BorderLeftColor = style.GetPropertyValue("border-left-color"),
            Color = style.GetPropertyValue("color"),
            FontFamily = style.GetPropertyValue("font-family"),
            FontSize = float.TryParse(style.GetPropertyValue("font-size"), out var fs) ? fs : 16,
            FontWeight = style.GetPropertyValue("font-weight")
        };
    }

    public bool NeedsLayout(IElement element)
    {
        return element.NeedsLayout();
    }

    public void InvalidateLayout(IElement element, bool recursive = true)
    {
        var invalidatedElements = new List<IElement>();
        element.SetNeedsLayout();
        invalidatedElements.Add(element);

        if (recursive)
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                InvalidateLayoutRecursive(child, invalidatedElements);
            }
        }

        // Publish the invalidation event with all affected elements
        _eventAggregator.Publish(new LayoutInvalidatedEvent(invalidatedElements));
    }

    private void InvalidateLayoutRecursive(IElement element, List<IElement> invalidatedElements)
    {
        element.SetNeedsLayout();
        invalidatedElements.Add(element);

        foreach (var child in element.Children.OfType<IElement>())
        {
            InvalidateLayoutRecursive(child, invalidatedElements);
        }
    }

    public IFragmentTree GetFragmentTree()
    {
        if (_currentLayoutResult == null)
        {
            throw new InvalidOperationException("Layout has not been performed yet.");
        }

        return new FragmentTree(_currentLayoutResult);
    }

    public ILayoutInfo? GetLayoutInfo(IElement element)
    {
        return _currentLayoutResult?.GetLayoutInfo(element);
    }

    private class LayoutContext
    {
        public float ContainerWidth { get; }
        public float ContainerHeight { get; }
        public float X { get; }
        public float Y { get; }

        public LayoutContext(float containerWidth, float containerHeight, float x = 0, float y = 0)
        {
            ContainerWidth = containerWidth;
            ContainerHeight = containerHeight;
            X = x;
            Y = y;
        }
    }

    private class BoxModel
    {
        public Rect ContentRect { get; set; }
        public Rect PaddingRect { get; set; }
        public Rect BorderRect { get; set; }
        public Rect MarginRect { get; set; }
    }
}