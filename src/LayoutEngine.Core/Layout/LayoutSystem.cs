namespace LayoutEngine.Core.Layout;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Contracts.Platform.Events;
using Infrastructure.EventAggregator.API.Aggregation;
using Style;
using FragmentTreeUpdatedEvent = Events.FragmentTreeUpdatedEvent;
using LayoutInvalidatedEvent = Events.LayoutInvalidatedEvent;

public class LayoutSystem : ILayoutSystem
{
    private readonly IStyleSystem _styleSystem;
    private readonly IEventAggregator _eventAggregator;
    private readonly HashSet<IElement> _elementsNeedingLayout = new();
    private ILayoutResult? _currentLayoutResult;

    public LayoutSystem(IStyleSystem styleSystem, IEventAggregator eventAggregator)
    {
        _styleSystem = styleSystem;
        _eventAggregator = eventAggregator;
    }

    public ILayoutResult PerformLayout(IDocument document)
    {
        // Create a new layout result
        var result = new LayoutResult();

        // Ensure styles are computed
        _styleSystem.ComputeDocumentStyles(document);

        if (document.DocumentElement != null)
        {
            // Determine viewport size
            var viewportWidth = 800f; // Default, could be passed in
            var viewportHeight = 600f; // Default, could be passed in

            // Create layout context
            var context = new LayoutContext(viewportWidth, viewportHeight);

            // Layout the document recursively
            var rootFragment = LayoutElement(document.DocumentElement, context, result);
            result.SetRootFragment(rootFragment);
        }

        // Store result
        _currentLayoutResult = result;

        // Clear pending layouts
        _elementsNeedingLayout.Clear();

        // Notify that fragment tree has been updated
        _eventAggregator.Publish(new FragmentTreeUpdatedEvent(result));

        return result;
    }

    private ILayoutFragment LayoutElement(IElement element, LayoutContext context, LayoutResult result)
    {
        // Get computed style
        var style = _styleSystem.GetComputedStyle(element);
        if (style == null)
        {
            style = _styleSystem.ComputeStyle(element);
        }

        // Create a new fragment for this element
        var fragment = new LayoutFragment
        {
            Element = element,
            VisualProperties = ExtractVisualProperties(style)
        };

        // Handle layout based on display type
        switch (style.Display)
        {
            case DisplayType.Block:
                LayoutBlockElement(element, style, fragment, context, result);
                break;

            case DisplayType.Flex:
                LayoutFlexElement(element, style, fragment, context, result);
                break;

            case DisplayType.Inline:
            case DisplayType.InlineBlock:
                LayoutInlineElement(element, style, fragment, context, result);
                break;

            // Other display types...

            case DisplayType.None:
                // Don't layout elements with display: none
                break;
        }

        // Register this fragment with the layout result
        result.AddFragment(element, fragment);

        return fragment;
    }

    private void LayoutBlockElement(IElement element, IComputedStyle style, LayoutFragment fragment, LayoutContext context, LayoutResult result)
    {
        // Calculate element box model
        var boxModel = CalculateBoxModel(element, style, context);

        // Set fragment bounds based on box model
        fragment.Bounds = boxModel.ContentRect;

        // Store layout info in result
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

        // Create child context
        var childContext = new LayoutContext(
            boxModel.ContentRect.Width,
            boxModel.ContentRect.Height,
            boxModel.ContentRect.X,
            boxModel.ContentRect.Y
        );

        // Process children
        var children = new List<ILayoutFragment>();
        foreach (var child in element.Children.OfType<IElement>())
        {
            var childFragment = LayoutElement(child, childContext, result);
            children.Add(childFragment);
        }
        fragment.Children = children;
    }

    private void LayoutFlexElement(IElement element, IComputedStyle style, LayoutFragment fragment, LayoutContext context, LayoutResult result)
    {
        // Similar to LayoutBlockElement, but with flex layout logic

        // For future implementation
        // Flexbox is complex and would need its own dedicated algorithm
    }

    private void LayoutInlineElement(IElement element, IComputedStyle style, LayoutFragment fragment, LayoutContext context, LayoutResult result)
    {
        // Similar to LayoutBlockElement, but with inline layout logic

        // For future implementation
        // Inline layout requires line breaking algorithms
    }

    private BoxModel CalculateBoxModel(IElement element, IComputedStyle style, LayoutContext context)
    {
        // Extract margin, border, padding from style
        var marginTop = ParseLength(style.GetValue("margin-top"), context.ContainerWidth, 0);
        var marginRight = ParseLength(style.GetValue("margin-right"), context.ContainerWidth, 0);
        var marginBottom = ParseLength(style.GetValue("margin-bottom"), context.ContainerWidth, 0);
        var marginLeft = ParseLength(style.GetValue("margin-left"), context.ContainerWidth, 0);

        var borderTop = ParseLength(style.GetValue("border-top-width"), context.ContainerWidth, 0);
        var borderRight = ParseLength(style.GetValue("border-right-width"), context.ContainerWidth, 0);
        var borderBottom = ParseLength(style.GetValue("border-bottom-width"), context.ContainerWidth, 0);
        var borderLeft = ParseLength(style.GetValue("border-left-width"), context.ContainerWidth, 0);

        var paddingTop = ParseLength(style.GetValue("padding-top"), context.ContainerWidth, 0);
        var paddingRight = ParseLength(style.GetValue("padding-right"), context.ContainerWidth, 0);
        var paddingBottom = ParseLength(style.GetValue("padding-bottom"), context.ContainerWidth, 0);
        var paddingLeft = ParseLength(style.GetValue("padding-left"), context.ContainerWidth, 0);

        // Calculate element width
        float width;
        var widthValue = style.GetValue("width");
        if (!string.IsNullOrEmpty(widthValue))
        {
            width = ParseLength(widthValue, context.ContainerWidth, context.ContainerWidth);
        }
        else
        {
            // Auto width depends on display type
            if (style.Display == DisplayType.Block)
            {
                width = context.ContainerWidth - marginLeft - marginRight - borderLeft - borderRight - paddingLeft - paddingRight;
            }
            else
            {
                // For inline elements, width depends on content
                width = 100; // Placeholder, would need text measurement
            }
        }

        // Calculate element height
        float height;
        var heightValue = style.GetValue("height");
        if (!string.IsNullOrEmpty(heightValue))
        {
            height = ParseLength(heightValue, context.ContainerHeight, context.ContainerHeight);
        }
        else
        {
            // Auto height depends on content
            height = 50; // Placeholder, would be calculated based on children
        }

        // Calculate positions
        var x = context.X + marginLeft + borderLeft + paddingLeft;
        var y = context.Y + marginTop + borderTop + paddingTop;

        // Create box model
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

        // Parse numeric value and unit
        // This is a simplified version
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
            BackgroundColor = style.GetValue("background-color"),
            BorderTopWidth = float.TryParse(style.GetValue("border-top-width"), out var btw) ? btw : 0,
            BorderRightWidth = float.TryParse(style.GetValue("border-right-width"), out var brw) ? brw : 0,
            BorderBottomWidth = float.TryParse(style.GetValue("border-bottom-width"), out var bbw) ? bbw : 0,
            BorderLeftWidth = float.TryParse(style.GetValue("border-left-width"), out var blw) ? blw : 0,
            BorderTopColor = style.GetValue("border-top-color"),
            BorderRightColor = style.GetValue("border-right-color"),
            BorderBottomColor = style.GetValue("border-bottom-color"),
            BorderLeftColor = style.GetValue("border-left-color"),
            Color = style.GetValue("color"),
            FontFamily = style.GetValue("font-family"),
            FontSize = float.TryParse(style.GetValue("font-size"), out var fs) ? fs : 16,
            FontWeight = style.GetValue("font-weight")
        };
    }

    // ILayoutSystem interface methods
    public bool NeedsLayout(IElement element)
    {
        return _elementsNeedingLayout.Contains(element);
    }

    public void InvalidateLayout(IElement element, bool recursive = true)
    {
        var affectedElements = new List<IElement>();

        // Add the element to the dirty list
        _elementsNeedingLayout.Add(element);
        affectedElements.Add(element);

        if (recursive)
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                InvalidateLayoutRecursive(child, affectedElements);
            }
        }

        // Publish layout invalidation event
        _eventAggregator.Publish(new LayoutInvalidatedEvent(affectedElements));
    }

    private void InvalidateLayoutRecursive(IElement element, List<IElement> affectedElements)
    {
        _elementsNeedingLayout.Add(element);
        affectedElements.Add(element);

        foreach (var child in element.Children.OfType<IElement>())
        {
            InvalidateLayoutRecursive(child, affectedElements);
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

    // Helper classes for layout
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