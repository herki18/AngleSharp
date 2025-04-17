using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.StyleSystem;
using Infrastructure.EventAggregator.API.Aggregation;
using Microsoft.Extensions.Logging;

namespace LayoutEngine.StyleSystem;

using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Mock implementation of IStyleEngine that returns dummy style data.
/// Follows the correct event flow and architecture patterns.
/// </summary>
public class StyleEngine : IStyleEngine, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleEngine> _logger;
    private readonly Dictionary<string, string> _styleSheets = new Dictionary<string, string>();
    private readonly Dictionary<IElement, IComputedStyle> _cachedStyles = new Dictionary<IElement, IComputedStyle>();
    private readonly List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();

    private HashSet<IElement> _dirtyElements = new HashSet<IElement>();
    private bool _isEverythingDirty = false;

    private IDocument? _document;
    private bool _isInitialized;
    private bool _isDisposed;
    private int _styleSheetCounter = 0;
    private DocumentLifecyclePhase _currentPhase = DocumentLifecyclePhase.Inactive;

    public StyleEngine(
        IEventAggregator eventAggregator,
        ILogger<StyleEngine>? logger = null)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? NullLogger<StyleEngine>.Instance;

        // Subscribe to events
        _subscriptions.Add(_eventAggregator.Subscribe<StyleInvalidatedEvent>(OnStyleInvalidated));
        _subscriptions.Add(_eventAggregator.Subscribe<PhaseChangedEvent>(OnPhaseChanged));
    }

    // Track current phase by subscribing to PhaseChangedEvent
    public DocumentLifecyclePhase CurrentPhase => _currentPhase;

    public bool HasPendingUpdates => _isEverythingDirty || _dirtyElements.Count > 0;

    public Task InitializeAsync(IDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isInitialized = true;
        _logger?.LogInformation("StyleEngine initialized");

        InvalidateAllStyles();

        // Pre-compute styles for all document elements to ensure they're ready
        // if (document.DocumentElement != null)
        // {
        //     _logger?.LogDebug("Pre-computing styles for document elements");
        //     PrecomputeStylesForElement(document.DocumentElement);
        // }

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _document = null;
        _cachedStyles.Clear();
        _styleSheets.Clear();
        _dirtyElements.Clear(); // Clear dirty state
        _isEverythingDirty = false;
        _isInitialized = false;
        return Task.CompletedTask;
    }

    public Task ProcessUpdatesAsync()
    {
        EnsureInitialized();
        if (!HasPendingUpdates)
        {
            _logger.LogTrace("No pending style updates to process.");
            // Publish an empty event if nothing was dirty, to signal completion
             _eventAggregator.Publish(new StyleComputedEvent(
                 new List<IElement>(),
                 new Dictionary<IElement, IComputedStyle>())
             );
            return Task.CompletedTask;
        }

        _logger.LogDebug("Processing {Count} dirty style elements (or all)", _dirtyElements.Count);

        var elementsToProcess = new List<IElement>();
        var computedStyles = new Dictionary<IElement, IComputedStyle>();

        if (_isEverythingDirty)
        {
            // If everything is dirty, process all elements in the document
            if (_document?.DocumentElement != null)
            {
                CollectAllElements(_document.DocumentElement, elementsToProcess);
            }
            _dirtyElements.Clear(); // Clear specific dirty elements as we process all
            _isEverythingDirty = false; // Reset the flag
        }
        else
        {
            // Process only the explicitly marked dirty elements
            elementsToProcess.AddRange(_dirtyElements);
            _dirtyElements.Clear(); // Clear the dirty set
        }


        // Compute styles for the collected elements
        foreach (var element in elementsToProcess)
        {
            // Use the internal method to create/get style (mock logic)
            var style = GetOrCreateComputedStyle(element);
            computedStyles[element] = style;
            _cachedStyles[element] = style; // Ensure cache is updated
        }


        // Publish computed styles event
        if (elementsToProcess.Count > 0)
        {
            _logger.LogDebug("Publishing StyleComputedEvent for {count} elements", elementsToProcess.Count);
            _eventAggregator.Publish(new StyleComputedEvent(elementsToProcess, computedStyles));
        }
        else
        {
            // Publish empty event if nothing was processed but flag was set
             _eventAggregator.Publish(new StyleComputedEvent(
                 new List<IElement>(),
                 new Dictionary<IElement, IComputedStyle>())
             );
        }

        return Task.CompletedTask;
    }

    public Task<IComputedStyle> ComputeStyleAsync(IElement element)
    {
        EnsureInitialized();
        _logger.LogDebug("ComputeStyleAsync called for element {element}", element.TagName);

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // Check cache first
        if (_cachedStyles.TryGetValue(element, out var style))
        {
            _logger.LogTrace("Returning cached style for {element}", element.TagName);
            return Task.FromResult(style);
        }

        // If not cached, it means it needs computation (or is dirty)
        // Mark as dirty and rely on ProcessUpdatesAsync to handle it
        _dirtyElements.Add(element);
        _logger.LogTrace("Element {element} not cached or is dirty, marked for update.", element.TagName);

        // In a real engine, we might schedule an update here if not already processing
        // For the mock, ProcessUpdatesAsync will pick it up.
        // We return a *placeholder* or throw, as the actual computation is deferred.
        // Let's return a newly created one for now, but acknowledge it might be stale
        // until ProcessUpdatesAsync runs.
        var placeholderStyle = GetOrCreateComputedStyle(element);
        _cachedStyles[element] = placeholderStyle; // Cache immediately for subsequent calls before processing

        // Publish event immediately for simplicity in mock, acknowledging this differs from real engine
        _eventAggregator.Publish(new StyleComputedEvent(
            new List<IElement>{ element },
            new Dictionary<IElement, IComputedStyle>{ { element, placeholderStyle } })
        );

        return Task.FromResult(placeholderStyle);
    }

    public void InvalidateStyles(IReadOnlyList<IElement> elements)
    {
        EnsureInitialized();
        _logger?.LogDebug("InvalidateStyles called for {count} elements", elements.Count);

        // Remove elements from cache
        foreach (var element in elements)
        {
            _cachedStyles.Remove(element); // Remove from cache
            _dirtyElements.Add(element);   // Mark as dirty
        }
        //
        // // IMMEDIATELY recompute and publish the styles
        // var computedStyles = new Dictionary<IElement, IComputedStyle>();
        // foreach (var element in elements)
        // {
        //     var style = GetOrCreateComputedStyle(element);
        //     computedStyles[element] = style;
        // }
        //
        // // Publish the computed styles event
        // if (elements.Count > 0)
        // {
        //     _logger?.LogDebug("Publishing StyleComputedEvent after invalidation for {count} elements", elements.Count);
        //     _eventAggregator.Publish(new StyleComputedEvent(elements.ToList(), computedStyles));
        // }
    }

    public void InvalidateAllStyles()
    {
        EnsureInitialized();
        _logger.LogDebug("InvalidateAllStyles called");

        _cachedStyles.Clear();      // Clear cache
        _dirtyElements.Clear();     // Clear specific dirty elements
        _isEverythingDirty = true; // Mark everything as dirty

        // IMMEDIATELY recompute styles for all previously cached elements
        // if (elements.Count > 0)
        // {
        //     var computedStyles = new Dictionary<IElement, IComputedStyle>();
        //     foreach (var element in elements)
        //     {
        //         var style = GetOrCreateComputedStyle(element);
        //         computedStyles[element] = style;
        //     }
        //
        //     _logger?.LogDebug("Publishing StyleComputedEvent after full invalidation for {count} elements", elements.Count);
        //     _eventAggregator.Publish(new StyleComputedEvent(elements, computedStyles));
        // }
    }

    public IComputedStyle? GetCachedStyle(IElement element)
    {
        if (element == null)
            return null;

        // Only return if truly cached and not dirty
        if (!_isEverythingDirty && !_dirtyElements.Contains(element) && _cachedStyles.TryGetValue(element, out var style))
        {
            _logger.LogTrace("Found valid cached style for element {element}", element.TagName);
            return style;
        }

        _logger.LogTrace("No valid cached style found for element {element}", element.TagName);
        return null;
    }

    public Task<string> AddStyleSheetAsync(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null)
    {
        EnsureInitialized();
        _logger.LogDebug("AddStyleSheetAsync called");

        var styleSheetId = $"style_{++_styleSheetCounter}";
        _styleSheets[styleSheetId] = styleSheet;

        // Stylesheet change invalidates everything
        InvalidateAllStyles(); // This now just marks dirty

        _eventAggregator.Publish(new StyleSheetChangedEvent(StyleSheetChangeType.Added, styleSheetId));
        return Task.FromResult(styleSheetId);
    }

    public bool RemoveStyleSheet(string styleSheetId)
    {
        EnsureInitialized();
        _logger.LogDebug("RemoveStyleSheet called for id {id}", styleSheetId);

        var removed = _styleSheets.Remove(styleSheetId);

        if (removed)
        {
            // Stylesheet change invalidates everything
            InvalidateAllStyles(); // This now just marks dirty

            _eventAggregator.Publish(new StyleSheetChangedEvent(StyleSheetChangeType.Removed, styleSheetId));
        }

        return removed;
    }

    // Precompute styles for element and all descendants
    private void PrecomputeStylesForElement(IElement element)
    {
        // This method is no longer used for precomputation, but kept for potential future use
        GetOrCreateComputedStyle(element);
        foreach (var child in element.Children)
        {
            PrecomputeStylesForElement(child);
        }
    }

    private IComputedStyle GetOrCreateComputedStyle(IElement element)
    {
        // Internal helper, still creates the mock style on demand
        // but doesn't imply it's the *final* computed style until ProcessUpdatesAsync runs.
        // Check cache first in case ComputeStyleAsync cached a placeholder
        if (_cachedStyles.TryGetValue(element, out var style))
        {
            return style;
        }

        _logger.LogTrace("Creating new mock computed style for element {element}", element.TagName);
        style = new ComputedStyle(element);
        _cachedStyles[element] = style; // Cache it
        return style;
    }

    private void CollectAllElements(IElement element, List<IElement> list)
    {
        list.Add(element);
        foreach (var child in element.Children)
        {
            CollectAllElements(child, list);
        }
    }

    private void ProcessElementAndDescendants(IElement element, List<IElement> elements, Dictionary<IElement, IComputedStyle> computedStyles)
    {
        // Process the element itself
        elements.Add(element);
        computedStyles[element] = GetOrCreateComputedStyle(element);

        // Process all descendants
        foreach (var child in element.Children)
        {
            ProcessElementAndDescendants(child, elements, computedStyles);
        }
    }

    private void OnStyleInvalidated(StyleInvalidatedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _logger.LogDebug("Received StyleInvalidatedEvent for {count} elements", e.Elements.Count);

        // Mark elements as dirty
        foreach (var element in e.Elements)
        {
            _cachedStyles.Remove(element); // Remove from cache
            _dirtyElements.Add(element);   // Mark as dirty
        }

        // // IMMEDIATELY recompute and publish the styles
        // var computedStyles = new Dictionary<IElement, IComputedStyle>();
        // foreach (var element in e.Elements)
        // {
        //     computedStyles[element] = GetOrCreateComputedStyle(element);
        // }
        //
        // // Publish the computed styles event
        // if (e.Elements.Count > 0)
        // {
        //     _logger?.LogDebug("Publishing StyleComputedEvent from OnStyleInvalidated for {count} elements", e.Elements.Count);
        //     _eventAggregator.Publish(new StyleComputedEvent(e.Elements.ToList(), computedStyles));
        // }
    }

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        _currentPhase = e.Phase;
        _logger.LogDebug("Phase changed to {phase}, {changeType}", e.Phase, e.ChangeType);

        // if (e.Phase == DocumentLifecyclePhase.InStyleRecalc && e.ChangeType == PhaseChangeType.Enter)
        // {
        //     _logger?.LogDebug("Entered style recalc phase, processing updates");
        //
        //     // Process styles immediately and synchronously
        //     try
        //     {
        //         var elements = new List<IElement>();
        //         var computedStyles = new Dictionary<IElement, IComputedStyle>();
        //
        //         if (_document?.Body != null)
        //         {
        //             ProcessElementAndDescendants(_document.DocumentElement ?? _document.Body, elements, computedStyles);
        //         }
        //
        //         // Publish computed styles event with all elements
        //         if (elements.Count > 0)
        //         {
        //             _logger?.LogDebug("Publishing StyleComputedEvent from OnPhaseChanged for {count} elements", elements.Count);
        //             _eventAggregator.Publish(new StyleComputedEvent(elements, computedStyles));
        //         }
        //         else
        //         {
        //             // Even if empty, publish an event to signal completion
        //             _logger?.LogDebug("Publishing empty StyleComputedEvent from OnPhaseChanged");
        //             _eventAggregator.Publish(new StyleComputedEvent(
        //                 new List<IElement>(),
        //                 new Dictionary<IElement, IComputedStyle>())
        //             );
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger?.LogError(ex, "Error processing styles in InStyleRecalc phase");
        //     }
        // }
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized || _document == null)
        {
            throw new InvalidOperationException("StyleEngine is not initialized. Call InitializeAsync first.");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _logger.LogInformation("StyleEngine disposing");

        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }
        _subscriptions.Clear();

        _cachedStyles.Clear();
        _styleSheets.Clear();
        _dirtyElements.Clear();
        _document = null;
    }
}