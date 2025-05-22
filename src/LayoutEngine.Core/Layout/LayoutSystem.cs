namespace LayoutEngine.Core.Layout;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using Style;
using FragmentTreeUpdatedEvent = Events.FragmentTreeUpdatedEvent;
using LayoutInvalidatedEvent = Events.LayoutInvalidatedEvent;

public class LayoutSystem : ILayoutSystem
{
    private readonly IStyleSystem _styleSystem;
    private readonly IEventAggregator _eventAggregator;
    private ILayoutResult? _currentLayoutResult;

    public LayoutSystem(IStyleSystem styleSystem, IEventAggregator eventAggregator)
    {
        _styleSystem = styleSystem;
        _eventAggregator = eventAggregator;
    }

    public ILayoutResult PerformLayout(IDocument document)
    {
        if (MockLayoutData.UseMockData)
        {
            var mockResult = MockLayoutData.GetMockLayoutResult();
            _currentLayoutResult = mockResult;
            ClearLayoutFlags(document.DocumentElement);
            _eventAggregator.Publish(new FragmentTreeUpdatedEvent(mockResult));
            return mockResult;
        }

        var result = new LayoutResult();
        _styleSystem.ComputeDocumentStyles(document);

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

    private void ClearLayoutFlags(IElement? element)
    {
        if (element == null) return;

        element.ClearNeedsLayout();
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

            case DisplayType.None:
                break;
        }

        result.AddFragment(element, fragment);

        return fragment;
    }

    private void LayoutBlockElement(IElement element, IComputedStyle style, LayoutFragment fragment, LayoutContext context, LayoutResult result)
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

    private void LayoutFlexElement(IElement element, IComputedStyle style, LayoutFragment fragment, LayoutContext context, LayoutResult result)
    {
        // For future implementation
    }

    private void LayoutInlineElement(IElement element, IComputedStyle style, LayoutFragment fragment, LayoutContext context, LayoutResult result)
    {
        // For future implementation
    }

    private BoxModel CalculateBoxModel(IElement element, IComputedStyle style, LayoutContext context)
    {
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

        float width;
        var widthValue = style.GetValue("width");
        if (!string.IsNullOrEmpty(widthValue))
        {
            width = ParseLength(widthValue, context.ContainerWidth, context.ContainerWidth);
        }
        else
        {
            if (style.Display == DisplayType.Block)
            {
                width = context.ContainerWidth - marginLeft - marginRight - borderLeft - borderRight - paddingLeft - paddingRight;
            }
            else
            {
                width = 100;
            }
        }

        float height;
        var heightValue = style.GetValue("height");
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

    public bool NeedsLayout(IElement element)
    {
        return element.NeedsLayout();
    }

    public void InvalidateLayout(IElement element, bool recursive = true)
    {
        element.SetNeedsLayout();

        if (recursive)
        {
            foreach (var child in element.Children.OfType<IElement>())
            {
                InvalidateLayout(child, true);
            }
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
