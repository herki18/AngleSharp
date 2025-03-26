using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using LayoutEngine.Contracts.LayoutSystem;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.StyleSystem;
using Microsoft.Extensions.Logging;

namespace LayoutEngine.LayoutSystem;

/// <summary>
/// Mock implementation of ILayoutBox to provide dummy layout data.
/// </summary>
public class LayoutBox : ILayoutBox
{
    private readonly List<ILayoutBox> _children = new List<ILayoutBox>();

    public IElement? Element { get; }
    public BoxType BoxType { get; }
    public IComputedStyle? Style { get; }
    public ILayoutBox? Parent { get; set; }
    public IReadOnlyList<ILayoutBox> Children => _children;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public BoxEdges Margin { get; set; }
    public BoxEdges Border { get; set; }
    public BoxEdges Padding { get; set; }

    public Rect ContentRect => new Rect(X, Y, Width, Height);

    public LayoutBox(IElement? element, BoxType boxType = BoxType.Block, IComputedStyle? style = null)
    {
        Element = element;
        BoxType = boxType;
        Style = style;

        // Default dimensions
        X = 0;
        Y = 0;
        Width = 100;
        Height = 20;

        // Default edges
        Margin = new BoxEdges(0);
        Border = new BoxEdges(0);
        Padding = new BoxEdges(0);

        // Adjust based on tag name if available
        if (element != null)
        {
            var tagName = element.TagName.ToLowerInvariant();

            switch (tagName)
            {
                case "body":
                    Width = 800;
                    Height = 600;
                    break;
                case "div":
                    Width = 780;
                    Height = 100;
                    Margin = new BoxEdges(5);
                    break;
                case "h1":
                    Height = 32;
                    Margin = new BoxEdges(10, 0, 10, 0);
                    break;
                case "h2":
                    Height = 24;
                    Margin = new BoxEdges(8, 0, 8, 0);
                    break;
                case "p":
                    Height = 20;
                    Margin = new BoxEdges(5, 0, 5, 0);
                    break;
                case "a":
                    BoxType = BoxType.Inline;
                    break;
                case "span":
                    BoxType = BoxType.Inline;
                    break;
                case "img":
                    Width = 200;
                    Height = 150;
                    break;
            }
        }
    }

    public Rect GetAbsoluteRect()
    {
        var absX = X;
        var absY = Y;

        // Traverse parent chain to calculate absolute position
        var currentParent = Parent;
        while (currentParent != null)
        {
            absX += currentParent.X;
            absY += currentParent.Y;
            currentParent = currentParent.Parent;
        }

        return new Rect(absX, absY, Width, Height);
    }

    public bool ContainsPoint(float x, float y)
    {
        var absoluteRect = GetAbsoluteRect();
        return x >= absoluteRect.X && x <= absoluteRect.X + absoluteRect.Width &&
               y >= absoluteRect.Y && y <= absoluteRect.Y + absoluteRect.Height;
    }

    public void AddChild(LayoutBox child)
    {
        child.Parent = this;
        _children.Add(child);
    }
}

/// <summary>
/// Mock implementation of ILayoutEngine that returns dummy layout information.
/// </summary>
public class LayoutEngine : Contracts.LayoutSystem.ILayoutEngine, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<LayoutEngine>? _logger;
    private readonly Dictionary<IElement, ILayoutBox> _cachedBoxes = new Dictionary<IElement, ILayoutBox>();
    private readonly List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();

    private LayoutBox _rootBox = new LayoutBox(null);
    private IDocument? _document;
    private bool _isInitialized;
    private bool _isDisposed;
    private DocumentLifecyclePhase _currentPhase = DocumentLifecyclePhase.Inactive;
    private Rect _viewport = new Rect(0, 0, 800, 600);

    public LayoutEngine(
        IEventAggregator eventAggregator,
        ILogger<LayoutEngine>? logger = null)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger;

        // Subscribe to events
        _subscriptions.Add(_eventAggregator.Subscribe<LayoutInvalidatedEvent>(OnLayoutInvalidated));
        _subscriptions.Add(_eventAggregator.Subscribe<PhaseChangedEvent>(OnPhaseChanged));
        _subscriptions.Add(_eventAggregator.Subscribe<StyleComputedEvent>(OnStyleComputed));
    }

    public DocumentLifecyclePhase CurrentPhase => _currentPhase;

    public bool HasPendingUpdates => false; // Mock always returns false

    public Rect Viewport => _viewport;

    public Task InitializeAsync(IDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isInitialized = true;
        _logger?.LogInformation("LayoutEngine initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _document = null;
        _cachedBoxes.Clear();
        _isInitialized = false;
        return Task.CompletedTask;
    }

    public Task ProcessUpdatesAsync()
    {
        EnsureInitialized();

        if (_document?.Body == null)
            return Task.CompletedTask;

        // Create a root box for the document
        var bodyBox = GetOrCreateLayoutBox(_document.Body);
        _rootBox = (LayoutBox)bodyBox;

        // Build a simple layout tree with the body's direct children
        foreach (var child in _document.Body.Children)
        {
            var childBox = GetOrCreateLayoutBox(child);
            if (childBox is LayoutBox layoutBox)
            {
                ((LayoutBox)bodyBox).AddChild(layoutBox);

                // Position child boxes with y-offset
                layoutBox.Y = bodyBox.Children.Count * 20; // Simple stacking
            }
        }

        // Create the updated boxes dictionary
        var updatedBoxes = new Dictionary<IElement, ILayoutBox>();
        updatedBoxes[_document.Body] = bodyBox;

        foreach (var child in _document.Body.Children)
        {
            var childBox = GetOrCreateLayoutBox(child);
            updatedBoxes[child] = childBox;
        }

        // Create list of elements
        var elements = updatedBoxes.Keys.ToList();

        // Publish the layout updated event
        _eventAggregator.Publish(new LayoutUpdatedEvent(elements, _rootBox, updatedBoxes));

        // Also publish a fragment tree updated event for the render system
        _eventAggregator.Publish(new FragmentTreeUpdatedEvent(_rootBox));

        return Task.CompletedTask;
    }

    public Task<ILayoutBox> ComputeLayoutAsync(IElement element)
    {
        EnsureInitialized();

        var layoutBox = GetOrCreateLayoutBox(element);
        return Task.FromResult(layoutBox);
    }

    public void InvalidateLayout(IReadOnlyList<IElement> elements)
    {
        EnsureInitialized();

        // Remove elements from cache
        foreach (var element in elements)
        {
            _cachedBoxes.Remove(element);
        }
    }

    public void InvalidateAllLayout()
    {
        EnsureInitialized();

        // Clear all cached layout boxes
        _cachedBoxes.Clear();
    }

    public ILayoutBox? GetCachedLayout(IElement element)
    {
        return _cachedBoxes.TryGetValue(element, out var box) ? box : null;
    }

    public ILayoutBox GetLayoutTree()
    {
        return _rootBox;
    }

    public void SetViewportSize(float width, float height)
    {
        _viewport = new Rect(0, 0, width, height);

        // If we have the document, adjust the body box size too
        if (_document?.Body != null && _cachedBoxes.TryGetValue(_document.Body, out var bodyBox))
        {
            if (bodyBox is LayoutBox layoutBox)
            {
                layoutBox.Width = width;
                layoutBox.Height = height;
            }
        }

        // Invalidate layout since viewport has changed
        InvalidateAllLayout();
    }

    public IElement? ElementFromPoint(float x, float y)
    {
        // Simple hit testing - iterate through all boxes and check if the point is contained
        foreach (var boxEntry in _cachedBoxes)
        {
            if (boxEntry.Value.ContainsPoint(x, y))
            {
                return boxEntry.Key;
            }
        }

        return null;
    }

    private ILayoutBox GetOrCreateLayoutBox(IElement element)
    {
        if (!_cachedBoxes.TryGetValue(element, out var box))
        {
            // Determine box type based on element style (in a real implementation)
            // For the mock, we'll just use some simple rules based on tag name
            var tagName = element.TagName.ToLowerInvariant();
            var boxType = tagName switch
            {
                "span" or "a" or "strong" or "em" or "b" or "i" => BoxType.Inline,
                "div" or "p" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" => BoxType.Block,
                _ => BoxType.Block
            };

            box = new LayoutBox(element, boxType);
            _cachedBoxes[element] = box;
        }

        return box;
    }

    private void OnLayoutInvalidated(LayoutInvalidatedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        // Invalidate layout for the elements
        InvalidateLayout(e.Elements);
    }

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        // Update current phase
        _currentPhase = e.Phase;

        if (e.Phase == DocumentLifecyclePhase.InLayout && e.ChangeType == PhaseChangeType.Enter)
        {
            // Process layout
            ProcessUpdatesAsync().ContinueWith(_ =>
            {
                // The event is published inside ProcessUpdatesAsync
            });
        }
    }

    private void OnStyleComputed(StyleComputedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        // When styles change, invalidate layout for those elements
        InvalidateLayout(e.Elements);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized || _document == null)
        {
            throw new InvalidOperationException("LayoutEngine is not initialized. Call InitializeAsync first.");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Unsubscribe from events
        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }
        _subscriptions.Clear();

        // Clear resources
        _cachedBoxes.Clear();
        _document = null;
    }
}