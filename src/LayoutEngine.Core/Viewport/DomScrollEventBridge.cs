namespace LayoutEngine.Core.Viewport;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using Layout;
using Layout.Internal;

public class DomScrollEventBridge : IDisposable
{
    private readonly ViewportManager _viewportManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly Dictionary<IElement?, ScrollEventListener> _scrollListeners = new();
    private bool _isDisposed;

    public DomScrollEventBridge(ViewportManager viewportManager, IEventAggregator eventAggregator)
    {
        _viewportManager = viewportManager ?? throw new ArgumentNullException(nameof(viewportManager));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        // Subscribe to scroll events from Unity
        _eventAggregator.Subscribe<UnityScrollEvent>(OnUnityScroll);
    }

    // Hook up DOM event listeners for all viewports
    public void AttachDomListeners()
    {
        // First, detach any existing listeners
        DetachAllDomListeners();

        // Recursively attach to all viewports
        AttachDomListenersRecursive(_viewportManager.RootViewport);
    }

    private void AttachDomListenersRecursive(Viewport viewport)
    {
        if (viewport.DomElement != null && !_scrollListeners.ContainsKey(viewport.DomElement))
        {
            var listener = new ScrollEventListener(viewport.DomElement, OnDomElementScrolled);
            _scrollListeners[viewport.DomElement] = listener;
        }

        foreach (var child in viewport.Children)
        {
            AttachDomListenersRecursive(child);
        }
    }

    // Called when a DOM element is scrolled
    private void OnDomElementScrolled(IElement element, double scrollX, double scrollY)
    {
        var viewport = _viewportManager.GetViewportForElement(element);
        if (viewport != null)
        {
            // Update viewport scroll position
            var oldOffset = viewport.ScrollOffset;
            viewport.ScrollOffset = new Point((float)scrollX, (float)scrollY);

            // Publish event for Unity to update its scroll views
            _eventAggregator.Publish(new DomScrollEvent(viewport.Id, viewport.ScrollOffset, oldOffset));
        }
    }

    // Called when a Unity scroll view is scrolled
    private void OnUnityScroll(UnityScrollEvent evt)
    {
        if (_viewportManager.GetViewport(evt.ViewportId) is Viewport viewport &&
            viewport.DomElement != null)
        {
            // Update the DOM element's scroll position
            viewport.ScrollOffset = evt.NewScrollOffset;
            UpdateDomElementScroll(viewport.DomElement, evt.NewScrollOffset);
        }
    }

    // Update a DOM element's scroll position using JavaScript
    private void UpdateDomElementScroll(IElement? element, Point scrollOffset)
    {
        // In a real implementation, this would use some JavaScript interop
        // to update the element.scrollLeft and element.scrollTop properties

        // Pseudo-code (would depend on your JS bridge):
        // var js = $"document.querySelector('{selector}').scrollLeft = {scrollOffset.X};" +
        //          $"document.querySelector('{selector}').scrollTop = {scrollOffset.Y};";
        // ExecuteJavaScript(js);

        Console.WriteLine($"DOM scroll position updated: Element={element?.TagName}, ScrollX={scrollOffset.X}, ScrollY={scrollOffset.Y}");
    }

    // Detach all DOM listeners
    private void DetachAllDomListeners()
    {
        foreach (var listener in _scrollListeners.Values)
        {
            listener.Dispose();
        }
        _scrollListeners.Clear();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        DetachAllDomListeners();
    }

    // Helper class to listen for DOM scroll events
    private class ScrollEventListener : IDisposable
    {
        private readonly IElement _element;
        private readonly Action<IElement, double, double> _callback;
        private bool _isDisposed;

        public ScrollEventListener(IElement? element, Action<IElement, double, double> callback)
        {
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            // In a real implementation, this would attach JavaScript event listeners
            // to the element's scroll event
            AttachJsEventListener();
        }

        private void AttachJsEventListener()
        {
            // Pseudo-code (would depend on your JS bridge):
            // var js = $@"
            //     document.querySelector('{selector}').addEventListener('scroll', function() {{
            //         window.unityInstance.SendMessage('ScrollBridge', 'OnDomScroll',
            //             JSON.stringify({{
            //                 elementId: '{elementId}',
            //                 scrollX: this.scrollLeft,
            //                 scrollY: this.scrollTop
            //             }}));
            //     }});";
            // ExecuteJavaScript(js);

            Console.WriteLine($"Attached scroll listener to element: {_element.TagName}");
        }

        private void DetachJsEventListener()
        {
            // Remove the JavaScript event listener when disposed
            // Similar pseudo-code as above, but with removeEventListener
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            DetachJsEventListener();
        }
    }
}