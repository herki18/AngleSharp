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
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Mock implementation of ILayoutEngine that returns dummy layout information.
/// </summary>
public class LayoutEngine : Contracts.LayoutSystem.ILayoutEngine, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<LayoutEngine> _logger;
    private readonly Dictionary<IElement, ILayoutBox> _cachedBoxes = new Dictionary<IElement, ILayoutBox>();
    private readonly List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();
    private readonly HashSet<IElement> _dirtyLayoutElements = new HashSet<IElement>();
    private bool _isEverythingDirty = false;

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
        _logger = logger ?? NullLogger<LayoutEngine>.Instance;

        // Subscribe to events
        _subscriptions.Add(_eventAggregator.Subscribe<LayoutInvalidatedEvent>(OnLayoutInvalidated));
        _subscriptions.Add(_eventAggregator.Subscribe<PhaseChangedEvent>(OnPhaseChanged));
        _subscriptions.Add(_eventAggregator.Subscribe<StyleComputedEvent>(OnStyleComputed));
    }

    public DocumentLifecyclePhase CurrentPhase => _currentPhase;

    public bool HasPendingUpdates => _isEverythingDirty || _dirtyLayoutElements.Count > 0;

    public Rect Viewport => _viewport;

    public Task InitializeAsync(IDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isInitialized = true;
        _logger.LogInformation("LayoutEngine initialized");

        // Mark layout as dirty initially
        InvalidateAllLayout();

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _document = null;
        _cachedBoxes.Clear();
        _dirtyLayoutElements.Clear(); // Clear dirty state
        _isEverythingDirty = false;
        _isInitialized = false;
        return Task.CompletedTask;
    }

    public Task ProcessUpdatesAsync()
    {
        EnsureInitialized();
        if (!HasPendingUpdates)
        {
            _logger.LogTrace("No pending layout updates to process.");
             // Publish an empty event if nothing was dirty, to signal completion
            _eventAggregator.Publish(new LayoutUpdatedEvent(
                new List<IElement>(), _rootBox, new Dictionary<IElement, ILayoutBox>())
            );
            _eventAggregator.Publish(new FragmentTreeUpdatedEvent(_rootBox)); // Also publish fragment tree
            return Task.CompletedTask;
        }

        _logger.LogDebug("Processing {Count} dirty layout elements (or all)", _dirtyLayoutElements.Count);

        // Create a root box for the document if needed or if everything is dirty
        if (_document?.Body != null && (_rootBox.Element == null || _isEverythingDirty))
        {
             _rootBox = (LayoutBox)GetOrCreateLayoutBox(_document.Body); // Update root if dirty
             _cachedBoxes[_document.Body] = _rootBox;
        }

        var processedElementsSet = new HashSet<IElement>();
        var updatedBoxes = new Dictionary<IElement, ILayoutBox>();
        List<IElement> elementsToProcess;

        if (_isEverythingDirty)
        {
            // If everything is dirty, process the whole document from body
            if (_document?.Body != null)
            {
                ProcessElementAndDescendants(_document.Body, 0, 0, processedElementsSet, updatedBoxes);
                elementsToProcess = updatedBoxes.Keys.ToList(); // Get keys from the processed elements
            }
            else
            {
                 elementsToProcess = new List<IElement>();
            }
            _dirtyLayoutElements.Clear(); // Clear specific dirty elements
            _isEverythingDirty = false;  // Reset the flag
        }
        else
        {
            // Process only the explicitly marked dirty elements and their descendants
            // Find the highest-level dirty elements to avoid redundant processing
            elementsToProcess = FindRootsOfDirtySubtrees(_dirtyLayoutElements);
            foreach (var rootElement in elementsToProcess)
            {
                // We need the parent's context for positioning, find the closest clean ancestor
                ILayoutBox? parentBox = FindNearestCachedParentBox(rootElement);
                float startX = parentBox?.X ?? 0;
                float startY = parentBox?.Y ?? 0;

                // Process this root and its descendants
                ProcessElementAndDescendants(rootElement, startX, startY, processedElementsSet, updatedBoxes);
            }
            _dirtyLayoutElements.Clear(); // Clear the dirty set
        }

        var finalUpdatedElements = updatedBoxes.Keys.ToList();


        // Publish the layout updated event
        if (finalUpdatedElements.Count > 0)
        {
            _logger.LogDebug("Publishing LayoutUpdatedEvent for {count} elements", finalUpdatedElements.Count);
            _eventAggregator.Publish(new LayoutUpdatedEvent(finalUpdatedElements, _rootBox, updatedBoxes));

            // Also publish a fragment tree updated event for the render system
            _eventAggregator.Publish(new FragmentTreeUpdatedEvent(_rootBox));
        }
        else
        {
             // Publish empty event if nothing was processed but flag was set
             _eventAggregator.Publish(new LayoutUpdatedEvent(
                 new List<IElement>(), _rootBox, new Dictionary<IElement, ILayoutBox>())
             );
             _eventAggregator.Publish(new FragmentTreeUpdatedEvent(_rootBox)); // Also publish fragment tree
        }


        return Task.CompletedTask;
    }

    public Task<ILayoutBox> ComputeLayoutAsync(IElement element)
    {
        EnsureInitialized();

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        _logger.LogDebug("ComputeLayoutAsync for element {element}", element.TagName);

        // Check cache first
        if (!_isEverythingDirty && !_dirtyLayoutElements.Contains(element) && _cachedBoxes.TryGetValue(element, out var box))
        {
            _logger.LogTrace("Returning cached layout for {element}", element.TagName);
            return Task.FromResult(box);
        }

        // Mark as dirty, return placeholder or throw
        _dirtyLayoutElements.Add(element);
        _logger.LogTrace("Element {element} layout not cached or is dirty, marked for update.", element.TagName);

        // Similar to StyleEngine, return a newly created one for now for the mock
        var placeholderBox = GetOrCreateLayoutBox(element);
        _cachedBoxes[element] = placeholderBox; // Cache placeholder

        // Publish event immediately for simplicity in mock
        _eventAggregator.Publish(new LayoutUpdatedEvent(
            new List<IElement>{ element }, _rootBox, new Dictionary<IElement, ILayoutBox>{ { element, placeholderBox } })
        );
        _eventAggregator.Publish(new FragmentTreeUpdatedEvent(_rootBox)); // Also publish fragment tree


        return Task.FromResult(placeholderBox);
    }

    public void InvalidateLayout(IReadOnlyList<IElement> elements)
    {
        EnsureInitialized();
        if (elements == null || elements.Count == 0) return;

        _logger.LogDebug("Invalidating layout for {count} elements", elements.Count);

        foreach (var element in elements)
        {
            InvalidateElementAndDescendants(element); // Removes from cache and marks descendants dirty
        }
    }

    public void InvalidateAllLayout()
    {
        EnsureInitialized();
        _logger.LogDebug("Invalidating all layout");

        _cachedBoxes.Clear();
        _dirtyLayoutElements.Clear();
        _isEverythingDirty = true; // Mark everything dirty

        // Recreate the root box conceptually, but don't compute yet
        _rootBox = new LayoutBox(null);
    }

    public ILayoutBox? GetCachedLayout(IElement element)
    {
        if (element == null)
            return null;

        // Check if valid cache entry exists
        if (!_isEverythingDirty && !_dirtyLayoutElements.Contains(element) && _cachedBoxes.TryGetValue(element, out var box))
        {
            _logger.LogTrace("Found valid cached layout for {element}", element.TagName);
            return box;
        }

        _logger.LogTrace("No valid cached layout found for {element}", element.TagName);
        return null;
    }

    public ILayoutBox GetLayoutTree()
    {
        // Ensure the tree is up-to-date if dirty before returning
        if (HasPendingUpdates)
        {
            _logger.LogWarning("GetLayoutTree called with pending updates. Processing synchronously.");
            ProcessUpdatesAsync().GetAwaiter().GetResult(); // Process synchronously for this call
        }
        return _rootBox;
    }

    public void SetViewportSize(float width, float height)
    {
        _logger.LogDebug("Setting viewport size to {width}x{height}", width, height);

        var oldViewport = _viewport;
        _viewport = new Rect(0, 0, width, height);

        // Viewport change invalidates all layout
        InvalidateAllLayout(); // This now just marks dirty

        // Publish viewport changed event immediately as it's a direct input change
        _eventAggregator.Publish(new ViewportChangedEvent(
            new Size((int)oldViewport.Width, (int)oldViewport.Height),
            new Size((int)width, (int)height)));
    }

    public IElement? ElementFromPoint(float x, float y)
    {
        // Ensure the tree is up-to-date if dirty before hit testing
        if (HasPendingUpdates)
        {
            _logger.LogWarning("ElementFromPoint called with pending updates. Processing synchronously.");
            ProcessUpdatesAsync().GetAwaiter().GetResult();
        }

        // Simple hit testing - find the topmost (last in tree traversal) element containing the point
        IElement? result = null;
        float smallestArea = float.MaxValue;

        // Need to traverse the *current* layout tree for hit testing
        Queue<ILayoutBox> queue = new Queue<ILayoutBox>();
        if (_rootBox != null) queue.Enqueue(_rootBox);

        while(queue.Count > 0)
        {
            var box = queue.Dequeue();
            if (box.Element != null && box.ContainsPoint(x, y))
            {
                var rect = box.GetAbsoluteRect();
                var area = rect.Width * rect.Height;

                // Prefer smaller boxes as they're likely more specific
                // Or if same area, prefer the one processed later (deeper in tree)
                if (area <= smallestArea)
                {
                    smallestArea = area;
                    result = box.Element;
                }
            }

            foreach (var child in box.Children)
            {
                queue.Enqueue(child);
            }
        }

        return result;
    }

    private ILayoutBox GetOrCreateLayoutBox(IElement element)
    {
        // Check cache first
        if (_cachedBoxes.TryGetValue(element, out var box))
        {
            return box;
        }

        // Mock logic for box creation
        var tagName = element.TagName.ToLowerInvariant();
        var boxType = tagName switch
        {
            "span" or "a" or "strong" or "em" or "b" or "i" => BoxType.Inline,
            _ => BoxType.Block
        };

        box = new LayoutBox(element, boxType); // Use the mock LayoutBox
        _cachedBoxes[element] = box; // Cache it

        return box;
    }

    private void ProcessElementAndDescendants(IElement element, float currentX, float currentY,
        HashSet<IElement> processedElements, Dictionary<IElement, ILayoutBox> updatedBoxes)
    {
        if (processedElements.Contains(element))
            return;

        processedElements.Add(element);

        var box = (LayoutBox)GetOrCreateLayoutBox(element); // Cast to mock LayoutBox
        updatedBoxes[element] = box;

        // Simple stacking layout for mock
        box.X = currentX + box.Margin.Left + box.Border.Left + box.Padding.Left;
        box.Y = currentY + box.Margin.Top + box.Border.Top + box.Padding.Top;

        float childY = box.Y + box.Padding.Top + box.Border.Top; // Start children inside padding/border
        float childX = box.X + box.Padding.Left + box.Border.Left;
        float currentLineY = childY;
        float currentLineX = childX;
        float maxLineHeight = 0;


        foreach (var child in element.Children)
        {
            var childBox = (LayoutBox)GetOrCreateLayoutBox(child); // Get or create child box

            if (childBox.BoxType == BoxType.Block)
            {
                 // If previous elements were inline, move to next line
                 if (currentLineX > childX)
                 {
                     currentLineY += maxLineHeight;
                     maxLineHeight = 0;
                 }
                 currentLineX = childX; // Reset X for block

                // Position block below previous content
                childBox.X = currentLineX;
                childBox.Y = currentLineY;
                ProcessElementAndDescendants(child, childBox.X, childBox.Y, processedElements, updatedBoxes);

                 // Update Y position for next block
                 currentLineY = childBox.Y + childBox.Height + childBox.Padding.Bottom + childBox.Border.Bottom + childBox.Margin.Bottom;

            }
            else // Inline
            {
                 // Simple inline layout: place to the right
                 childBox.X = currentLineX;
                 childBox.Y = currentLineY;

                 ProcessElementAndDescendants(child, childBox.X, childBox.Y, processedElements, updatedBoxes);

                 currentLineX = childBox.X + childBox.Width + childBox.Padding.Right + childBox.Border.Right + childBox.Margin.Right;
                 maxLineHeight = Math.Max(maxLineHeight, childBox.Height + childBox.Padding.Top + childBox.Padding.Bottom + childBox.Border.Top + childBox.Border.Bottom + childBox.Margin.Top + childBox.Margin.Bottom);

            }

             // Set parent relationship (if needed by mock)
             if (childBox is LayoutBox layoutChildBox) {
                 layoutChildBox.Parent = box;
                 box.AddChild(layoutChildBox); // Add to children if LayoutBox has such method
             }
        }
    }

    private void InvalidateElementAndDescendants(IElement element)
    {
        if (element == null)
            return;

        // Remove this element from cache
        _cachedBoxes.Remove(element);
        // Mark as dirty
        _dirtyLayoutElements.Add(element);

        // Recursively mark children dirty and remove from cache
        foreach (var child in element.Children)
        {
            InvalidateElementAndDescendants(child);
        }
    }

    private List<IElement> FindRootsOfDirtySubtrees(IEnumerable<IElement> dirtyElements)
    {
        var roots = new List<IElement>();
        var dirtySet = new HashSet<IElement>(dirtyElements);

        foreach (var element in dirtyElements)
        {
            // If the element's parent is also dirty, this element is not a root
            if (element.ParentElement != null && dirtySet.Contains(element.ParentElement))
            {
                continue;
            }
            // Otherwise, it's a root of a dirty subtree
            roots.Add(element);
        }
        return roots;
    }

    private ILayoutBox? FindNearestCachedParentBox(IElement element)
    {
        var parent = element.ParentElement;
        while(parent != null)
        {
            if(_cachedBoxes.TryGetValue(parent, out var parentBox))
            {
                // Ensure this parent isn't also dirty
                if(!_dirtyLayoutElements.Contains(parent))
                    return parentBox;
            }
            parent = parent.ParentElement;
        }
        // If no cached parent found, return the root box
        return _rootBox;
    }

    private void OnLayoutInvalidated(LayoutInvalidatedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger.LogDebug("Received LayoutInvalidatedEvent for {count} elements", e.Elements.Count);

        // Mark invalidated elements as dirty
        foreach (var element in e.Elements)
        {
            InvalidateElementAndDescendants(element); // Marks dirty and removes from cache
        }

        // DO NOT process updates immediately.
    }

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _currentPhase = e.Phase;
        _logger.LogDebug("Phase changed to {phase}, {changeType}", e.Phase, e.ChangeType);

        // --- REMOVED: No longer process updates directly on phase change ---
        // The UpdateScheduler should trigger processing when entering InLayout phase.
        // --- END REMOVED ---
    }

    private void OnStyleComputed(StyleComputedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger.LogDebug("Styles computed for {count} elements, invalidating layout.", e.Elements.Count);

        // When styles change, invalidate layout for those elements
        InvalidateLayout(e.Elements); // This now just marks things dirty

        // DO NOT process updates immediately.
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
        _logger.LogInformation("LayoutEngine disposing");

        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }
        _subscriptions.Clear();

        _cachedBoxes.Clear();
        _dirtyLayoutElements.Clear();
        _document = null;
    }
}