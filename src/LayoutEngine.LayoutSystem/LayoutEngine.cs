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

using Contracts.Platform.Dom;

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

        _logger?.LogDebug("Processing layout updates");

        // Create a root box for the document
        var bodyBox = GetOrCreateLayoutBox(_document.Body);
        _rootBox = (LayoutBox)bodyBox;

        // Dictionary to track processed elements
        var processedElements = new HashSet<IElement>();
        var updatedBoxes = new Dictionary<IElement, ILayoutBox>();

        // Process full document
        ProcessElementAndDescendants(_document.Body, 0, 0, processedElements, updatedBoxes);

        // Create list of elements that were updated
        var elements = updatedBoxes.Keys.ToList();

        // Publish the layout updated event
        if (elements.Count > 0)
        {
            _logger?.LogDebug("Publishing LayoutUpdatedEvent for {count} elements", elements.Count);
            _eventAggregator.Publish(new LayoutUpdatedEvent(elements, _rootBox, updatedBoxes));

            // Also publish a fragment tree updated event for the render system
            _eventAggregator.Publish(new FragmentTreeUpdatedEvent(_rootBox));
        }

        return Task.CompletedTask;
    }

    public Task<ILayoutBox> ComputeLayoutAsync(IElement element)
    {
        EnsureInitialized();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        _logger?.LogDebug("Computing layout for element {element}", element.TagName);

        // Get or create layout box for this element
        var layoutBox = GetOrCreateLayoutBox(element);

        // Start with the simplest case - single element update
        var elements = new List<IElement> { element };
        var updatedBoxes = new Dictionary<IElement, ILayoutBox> { [element] = layoutBox };

        // Publish layout update for this element
        _eventAggregator.Publish(new LayoutUpdatedEvent(elements, _rootBox, updatedBoxes));

        return Task.FromResult(layoutBox);
    }

    public void InvalidateLayout(IReadOnlyList<IElement> elements)
    {
        EnsureInitialized();

        _logger?.LogDebug("Invalidating layout for {count} elements", elements.Count);

        // Remove elements from cache
        foreach (var element in elements)
        {
            InvalidateElementAndDescendants(element);
        }

        // If this is called directly (not from an event handler),
        // publish the LayoutInvalidatedEvent
        if (elements.Count > 0)
        {
            _eventAggregator.Publish(new LayoutInvalidatedEvent(elements));
        }
    }

    public void InvalidateAllLayout()
    {
        EnsureInitialized();

        _logger?.LogDebug("Invalidating all layout");

        // Clear all cached layout boxes
        _cachedBoxes.Clear();

        // Recreate the root box
        _rootBox = new LayoutBox(null);

        // If we have a document, invalidate the whole document
        if (_document?.Body != null)
        {
            var elements = new List<IElement> { _document.Body };
            _eventAggregator.Publish(new LayoutInvalidatedEvent(elements));
        }
    }

    public ILayoutBox? GetCachedLayout(IElement element)
    {
        if (element == null)
            return null;

        return _cachedBoxes.TryGetValue(element, out var box) ? box : null;
    }

    public ILayoutBox GetLayoutTree()
    {
        return _rootBox;
    }

    public void SetViewportSize(float width, float height)
    {
        _logger?.LogDebug("Setting viewport size to {width}x{height}", width, height);

        var oldViewport = _viewport;
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

        // Invalidate all layout since viewport has changed
        InvalidateAllLayout();

        // Publish viewport changed event
        _eventAggregator.Publish(new ViewportChangedEvent(
            new Size((int)oldViewport.Width, (int)oldViewport.Height),
            new Size((int)width, (int)height)));
    }

    public IElement? ElementFromPoint(float x, float y)
    {
        // Simple hit testing - find the topmost (last in tree traversal) element containing the point
        IElement? result = null;
        float smallestArea = float.MaxValue;

        foreach (var kvp in _cachedBoxes)
        {
            var element = kvp.Key;
            var box = kvp.Value;

            if (box.ContainsPoint(x, y))
            {
                var rect = box.GetAbsoluteRect();
                var area = rect.Width * rect.Height;

                // Prefer smaller boxes as they're likely more specific
                if (area < smallestArea)
                {
                    smallestArea = area;
                    result = element;
                }
            }
        }

        return result;
    }

    private ILayoutBox GetOrCreateLayoutBox(IElement element)
    {
        if (_cachedBoxes.TryGetValue(element, out var box))
        {
            return box;
        }

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

        return box;
    }

    private void ProcessElementAndDescendants(IElement element, float parentX, float parentY,
        HashSet<IElement> processedElements, Dictionary<IElement, ILayoutBox> updatedBoxes)
    {
        if (processedElements.Contains(element))
            return;

        processedElements.Add(element);

        // Get or create layout box for this element
        var box = GetOrCreateLayoutBox(element);

        // Update position relative to parent
        if (box is LayoutBox layoutBox)
        {
            // Only update position if it's not already set
            if (layoutBox.X == 0 && layoutBox.Parent == null)
                layoutBox.X = parentX;

            if (layoutBox.Y == 0 && layoutBox.Parent == null)
                layoutBox.Y = parentY;
        }

        // Add to the updated boxes dictionary
        updatedBoxes[element] = box;

        // Calculate child position
        float childX = parentX;
        float childY = parentY;

        if (box.BoxType == BoxType.Block)
        {
            // Blocks create a new positioning context
            childX = box.X + box.Padding.Left + box.Border.Left;
            childY = box.Y + box.Padding.Top + box.Border.Top;
        }
        else
        {
            // Inline boxes flow with parent
            childX = parentX + box.Width;
            childY = parentY;
        }

        // Currently we're creating a very simplified layout
        // In a real implementation, we'd have a more complex layout algorithm
        float yOffset = 0;

        // Process children
        foreach (var child in element.Children)
        {
            // Create a layout box for this child
            var childBox = GetOrCreateLayoutBox(child);

            // If block, position below previous siblings
            if (childBox.BoxType == BoxType.Block)
            {
                if (childBox is LayoutBox layoutChildBox)
                {
                    layoutChildBox.X = childX;
                    layoutChildBox.Y = childY + yOffset;

                    // Set parent-child relationship
                    if (box is LayoutBox parentLayoutBox)
                    {
                        parentLayoutBox.AddChild(layoutChildBox);
                    }

                    // Move down for next block element
                    yOffset += childBox.Height + childBox.Margin.Top + childBox.Margin.Bottom;
                }
            }
            else
            {
                // For inline elements, position to the right of previous siblings
                if (childBox is LayoutBox layoutChildBox)
                {
                    layoutChildBox.X = childX;
                    layoutChildBox.Y = childY;

                    // Set parent-child relationship
                    if (box is LayoutBox parentLayoutBox)
                    {
                        parentLayoutBox.AddChild(layoutChildBox);
                    }

                    // Move right for next inline element
                    childX += childBox.Width + childBox.Margin.Left + childBox.Margin.Right;
                }
            }

            // Recursively process this child's descendants
            ProcessElementAndDescendants(child, childBox.X, childBox.Y, processedElements, updatedBoxes);
        }
    }

    private void InvalidateElementAndDescendants(IElement element)
    {
        if (element == null)
            return;

        // Remove this element from cache
        _cachedBoxes.Remove(element);

        // Recursively invalidate all children
        foreach (var child in element.Children)
        {
            InvalidateElementAndDescendants(child);
        }
    }

    private void OnLayoutInvalidated(LayoutInvalidatedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger?.LogDebug("Layout invalidated for {count} elements", e.Elements.Count);

        // Remove invalidated elements from cache
        foreach (var element in e.Elements)
        {
            InvalidateElementAndDescendants(element);
        }

        // If we're in the layout phase, process updates immediately
        if (_currentPhase == DocumentLifecyclePhase.InLayout ||
            _currentPhase == DocumentLifecyclePhase.LayoutDirty)
        {
            ProcessUpdatesAsync().ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger?.LogError(t.Exception, "Error processing layout updates");
                }
            });
        }
    }

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        // Update current phase
        _currentPhase = e.Phase;

        _logger?.LogDebug("Phase changed to {phase}, {changeType}", e.Phase, e.ChangeType);

        if (e.Phase == DocumentLifecyclePhase.InLayout && e.ChangeType == PhaseChangeType.Enter)
        {
            // Process layout updates when entering the layout phase
            ProcessUpdatesAsync().ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger?.LogError(t.Exception, "Error processing layout updates");
                }
            });
        }
    }

    private void OnStyleComputed(StyleComputedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger?.LogDebug("Styles computed for {count} elements", e.Elements.Count);

        // When styles change, invalidate layout for those elements
        InvalidateLayout(e.Elements);

        // If we're already in a layout phase, process updates immediately
        // This helps tests that are waiting for layout after a style change
        if (_currentPhase == DocumentLifecyclePhase.StyleClean ||
            _currentPhase == DocumentLifecyclePhase.LayoutClean ||
            _currentPhase == DocumentLifecyclePhase.InLayout)
        {
            ProcessUpdatesAsync().ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger?.LogError(t.Exception, "Error processing layout updates after style changes");
                }
            });
        }
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