using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Style;

namespace LayoutEngine.Core.Viewport;

using Style.Public;

public class ViewportManager
{
    private readonly IStyleSystem _styleSystem;
    private readonly Dictionary<string, Viewport> _viewportsById = new();
    private readonly Dictionary<IElement, string> _elementToViewportId = new();
    private readonly Dictionary<string, List<string>> _fragmentsInViewport = new();
    private readonly Dictionary<IElement, Point> _savedScrollPositions = new();

    public Viewport RootViewport { get; private set; }

    public ViewportManager(IStyleSystem styleSystem)
    {
        _styleSystem = styleSystem ?? throw new ArgumentNullException(nameof(styleSystem));
        RootViewport = new Viewport("root", null);
        _viewportsById["root"] = RootViewport;
    }

    public Viewport BuildViewportTree(IFragmentTree fragmentTree)
    {
        _viewportsById.Clear();
        _elementToViewportId.Clear();
        _fragmentsInViewport.Clear();
        RootViewport = new Viewport("root", null);
        _viewportsById["root"] = RootViewport;
        ProcessFragment(fragmentTree.RootFragment, RootViewport);
        RestoreScrollPositions();
        return RootViewport;
    }

    private void ProcessFragment(ILayoutFragment fragment, Viewport parentViewport)
    {
        string fragmentId = fragment.GetHashCode().ToString();
        if (!_fragmentsInViewport.TryGetValue(parentViewport.Id, out var fragments))
        {
            fragments = new List<string>();
            _fragmentsInViewport[parentViewport.Id] = fragments;
        }
        fragments.Add(fragmentId);

        if (fragment.Element != null)
        {
            var style = _styleSystem.GetComputedStyle(fragment.Element);
            if (IsScrollContainer(style))
            {
                var viewportId = Guid.NewGuid().ToString();
                var viewport = new Viewport(viewportId, fragment.Element);
                viewport.ViewportRect = fragment.Bounds;
                viewport.Parent = parentViewport;
                var overflowX = style.GetValue("overflow-x");
                var overflowY = style.GetValue("overflow-y");
                viewport.CanScrollHorizontally = overflowX == "auto" || overflowX == "scroll";
                viewport.CanScrollVertically = overflowY == "auto" || overflowY == "scroll";
                var scrollBehavior = style.GetValue("scroll-behavior");
                viewport.UseSmoothScrolling = scrollBehavior == "smooth";
                _viewportsById[viewportId] = viewport;
                _elementToViewportId[fragment.Element] = viewportId;
                parentViewport.Children.Add(viewport);
                ProcessFragmentChildren(fragment, viewport);
                CalculateContentSize(viewport, fragment);
                return;
            }
        }
        foreach (var child in fragment.Children)
        {
            ProcessFragment(child, parentViewport);
        }
    }

    private void ProcessFragmentChildren(ILayoutFragment parentFragment, Viewport viewport)
    {
        foreach (var child in parentFragment.Children)
        {
            ProcessFragment(child, viewport);
        }
    }

    private void CalculateContentSize(Viewport viewport, ILayoutFragment fragment)
    {
        float maxRight = 0;
        float maxBottom = 0;
        foreach (var child in fragment.Children)
        {
            maxRight = Math.Max(maxRight, child.Bounds.X + child.Bounds.Width);
            maxBottom = Math.Max(maxBottom, child.Bounds.Y + child.Bounds.Height);
        }
        viewport.ContentSize = new Size(
            Math.Max(maxRight, viewport.ViewportRect.Width),
            Math.Max(maxBottom, viewport.ViewportRect.Height)
        );
    }

    private bool IsScrollContainer(IComputedStyle style)
    {
        var overflow = style.GetValue("overflow");
        var overflowX = style.GetValue("overflow-x");
        var overflowY = style.GetValue("overflow-y");
        return overflow == "auto" || overflow == "scroll" ||
               overflowX == "auto" || overflowX == "scroll" ||
               overflowY == "auto" || overflowY == "scroll";
    }

    public void SaveScrollPosition(IElement element)
    {
        if (_elementToViewportId.TryGetValue(element, out var viewportId) &&
            _viewportsById.TryGetValue(viewportId, out var viewport))
        {
            _savedScrollPositions[element] = viewport.ScrollOffset;
        }
    }

    public void SaveAllScrollPositions()
    {
        foreach (var kvp in _elementToViewportId)
        {
            var element = kvp.Key;
            var viewportId = kvp.Value;
            if (_viewportsById.TryGetValue(viewportId, out var viewport))
            {
                _savedScrollPositions[element] = viewport.ScrollOffset;
            }
        }
    }

    private void RestoreScrollPositions()
    {
        foreach (var kvp in _savedScrollPositions)
        {
            var element = kvp.Key;
            var scrollPos = kvp.Value;
            if (_elementToViewportId.TryGetValue(element, out var viewportId) &&
                _viewportsById.TryGetValue(viewportId, out var viewport))
            {
                viewport.ScrollOffset = scrollPos;
            }
        }
    }

    public Viewport? GetViewport(string id)
    {
        _viewportsById.TryGetValue(id, out var viewport);
        return viewport;
    }

    public Viewport? GetViewportForElement(IElement? element)
    {
        if (element == null) return null;
        if (_elementToViewportId.TryGetValue(element, out var viewportId))
        {
            _viewportsById.TryGetValue(viewportId, out var viewport);
            return viewport;
        }
        return null;
    }
}