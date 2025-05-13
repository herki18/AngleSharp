namespace LayoutEngine.Core.Viewport;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Layout;
using Style;

public class ViewportManager
{
    private readonly Dictionary<string, Viewport> _viewportsById = new();
    private readonly Dictionary<IElement, string> _elementToViewportId = new();
    private readonly Dictionary<string, List<string>> _fragmentsInViewport = new();

    // Persistent scroll state (survives layout recalculation)
    private readonly Dictionary<IElement, Point> _savedScrollPositions = new();

    // Root viewport (represents the main window)
    public Viewport RootViewport { get; private set; }

    public ViewportManager()
    {
        // Create the root viewport
        RootViewport = new Viewport("root", null);
        _viewportsById["root"] = RootViewport;
    }

    // Build a viewport tree from a fragment tree
    public Viewport BuildViewportTree(IFragmentTree fragmentTree, IStyleSystem styleSystem)
    {
        // Clear previous mappings but keep scroll positions
        _viewportsById.Clear();
        _elementToViewportId.Clear();
        _fragmentsInViewport.Clear();

        // Always keep the root viewport
        RootViewport = new Viewport("root", null);
        _viewportsById["root"] = RootViewport;

        // Process the fragment tree to build viewports
        ProcessFragment(fragmentTree.RootFragment, RootViewport, styleSystem);

        // Restore saved scroll positions
        RestoreScrollPositions();

        return RootViewport;
    }

    private void ProcessFragment(
        ILayoutFragment fragment,
        Viewport parentViewport,
        IStyleSystem styleSystem)
    {
        string fragmentId = fragment.GetHashCode().ToString();

        // Track which fragments are in which viewports
        if (!_fragmentsInViewport.TryGetValue(parentViewport.Id, out var fragments))
        {
            fragments = new List<string>();
            _fragmentsInViewport[parentViewport.Id] = fragments;
        }
        fragments.Add(fragmentId);

        // If this fragment has an element, check if it's a scroll container
        if (fragment.Element != null)
        {
            var style = styleSystem.GetComputedStyle(fragment.Element);
            if (IsScrollContainer(style))
            {
                // Create a new viewport for this scroll container
                var viewportId = Guid.NewGuid().ToString();
                var viewport = new Viewport(viewportId, fragment.Element);

                // Set viewport properties
                viewport.ViewportRect = fragment.Bounds;
                viewport.Parent = parentViewport;

                // Determine scroll direction from CSS
                var overflowX = style.GetValue("overflow-x");
                var overflowY = style.GetValue("overflow-y");
                viewport.CanScrollHorizontally = overflowX == "auto" || overflowX == "scroll";
                viewport.CanScrollVertically = overflowY == "auto" || overflowY == "scroll";

                // Set smooth scrolling from CSS
                var scrollBehavior = style.GetValue("scroll-behavior");
                viewport.UseSmoothScrolling = scrollBehavior == "smooth";

                // Add to collections
                _viewportsById[viewportId] = viewport;
                _elementToViewportId[fragment.Element] = viewportId;
                parentViewport.Children.Add(viewport);

                // Process children within this viewport
                ProcessFragmentChildren(fragment, viewport, styleSystem);

                // Calculate total content size based on children
                CalculateContentSize(viewport, fragment);

                return; // Children are processed by ProcessFragmentChildren
            }
        }

        // For non-scroll containers, process children normally
        foreach (var child in fragment.Children)
        {
            ProcessFragment(child, parentViewport, styleSystem);
        }
    }

    private void ProcessFragmentChildren(
        ILayoutFragment parentFragment,
        Viewport viewport,
        IStyleSystem styleSystem)
    {
        foreach (var child in parentFragment.Children)
        {
            // Process child in the context of this viewport
            ProcessFragment(child, viewport, styleSystem);
        }
    }

    private void CalculateContentSize(Viewport viewport, ILayoutFragment fragment)
    {
        // Calculate the total content size based on all children
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

    // Save scroll position for an element
    public void SaveScrollPosition(IElement element)
    {
        if (_elementToViewportId.TryGetValue(element, out var viewportId) &&
            _viewportsById.TryGetValue(viewportId, out var viewport))
        {
            _savedScrollPositions[element] = viewport.ScrollOffset;
        }
    }

    // Save all current scroll positions
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

    // Restore saved scroll positions
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

    // Get viewport by ID
    public Viewport? GetViewport(string id)
    {
        _viewportsById.TryGetValue(id, out var viewport);
        return viewport;
    }

    // Get viewport for a DOM element
    public Viewport? GetViewportForElement(IElement element)
    {
        if (_elementToViewportId.TryGetValue(element, out var viewportId))
        {
            _viewportsById.TryGetValue(viewportId, out var viewport);
            return viewport;
        }
        return null;
    }

    // Check if a layout fragment is visible in any viewport
    public bool IsFragmentVisible(string fragmentId)
    {
        foreach (var viewportId in _fragmentsInViewport.Keys)
        {
            if (_fragmentsInViewport[viewportId].Contains(fragmentId) &&
                _viewportsById.TryGetValue(viewportId, out var viewport))
            {
                // Check if the fragment is within the viewport's visible area
                // This would need the actual fragment position relative to the viewport
                return true; // Simplified for now
            }
        }
        return false;
    }
}