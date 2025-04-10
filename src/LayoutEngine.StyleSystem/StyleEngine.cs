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

/// <summary>
/// Mock implementation of IStyleEngine that returns dummy style data.
/// Follows the correct event flow and architecture patterns.
/// </summary>
public class StyleEngine : IStyleEngine, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StyleEngine>? _logger;
    private readonly Dictionary<string, string> _styleSheets = new Dictionary<string, string>();
    private readonly Dictionary<IElement, IComputedStyle> _cachedStyles = new Dictionary<IElement, IComputedStyle>();
    private readonly List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();

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
        _logger = logger;

        // Subscribe to events
        _subscriptions.Add(_eventAggregator.Subscribe<StyleInvalidatedEvent>(OnStyleInvalidated));
        _subscriptions.Add(_eventAggregator.Subscribe<PhaseChangedEvent>(OnPhaseChanged));
    }

    // Track current phase by subscribing to PhaseChangedEvent
    public DocumentLifecyclePhase CurrentPhase => _currentPhase;

    public bool HasPendingUpdates => false; // Mock always returns false

    public Task InitializeAsync(IDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isInitialized = true;
        _logger?.LogInformation("StyleEngine initialized");

        // Pre-compute styles for all document elements to ensure they're ready
        if (document.DocumentElement != null)
        {
            _logger?.LogDebug("Pre-computing styles for document elements");
            PrecomputeStylesForElement(document.DocumentElement);
        }

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _document = null;
        _cachedStyles.Clear();
        _styleSheets.Clear();
        _isInitialized = false;
        return Task.CompletedTask;
    }

    public Task ProcessUpdatesAsync()
    {
        EnsureInitialized();
        _logger?.LogDebug("Processing style updates");

        if (_document?.Body == null)
            return Task.CompletedTask;

        // Compute styles for all elements in the document
        var elements = new List<IElement>();
        var computedStyles = new Dictionary<IElement, IComputedStyle>();

        // Process document element and all its descendants
        ProcessElementAndDescendants(_document.DocumentElement ?? _document.Body, elements, computedStyles);

        // Publish computed styles event
        if (elements.Count > 0)
        {
            _logger?.LogDebug("Publishing StyleComputedEvent for {count} elements", elements.Count);
            _eventAggregator.Publish(new StyleComputedEvent(elements, computedStyles));
        }

        return Task.CompletedTask;
    }

    public Task<IComputedStyle> ComputeStyleAsync(IElement element)
    {
        EnsureInitialized();
        _logger?.LogDebug("ComputeStyleAsync called for element {element}", element.TagName);

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // Get or create style immediately
        var style = GetOrCreateComputedStyle(element);

        // Publish single element style computed event IMMEDIATELY
        var elements = new List<IElement> { element };
        var computedStyles = new Dictionary<IElement, IComputedStyle> { [element] = style };

        _logger?.LogDebug("Publishing StyleComputedEvent for element {element}", element.TagName);
        _eventAggregator.Publish(new StyleComputedEvent(elements, computedStyles));

        // Cache the style
        _cachedStyles[element] = style;

        return Task.FromResult(style);
    }

    public void InvalidateStyles(IReadOnlyList<IElement> elements)
    {
        EnsureInitialized();
        _logger?.LogDebug("InvalidateStyles called for {count} elements", elements.Count);

        // Remove elements from cache
        foreach (var element in elements)
        {
            _cachedStyles.Remove(element);
        }

        // IMMEDIATELY recompute and publish the styles
        var computedStyles = new Dictionary<IElement, IComputedStyle>();
        foreach (var element in elements)
        {
            var style = GetOrCreateComputedStyle(element);
            computedStyles[element] = style;
        }

        // Publish the computed styles event
        if (elements.Count > 0)
        {
            _logger?.LogDebug("Publishing StyleComputedEvent after invalidation for {count} elements", elements.Count);
            _eventAggregator.Publish(new StyleComputedEvent(elements.ToList(), computedStyles));
        }
    }

    public void InvalidateAllStyles()
    {
        EnsureInitialized();
        _logger?.LogDebug("InvalidateAllStyles called");

        // Save all elements before clearing
        var elements = _cachedStyles.Keys.ToList();

        // Clear all cached styles
        _cachedStyles.Clear();

        // IMMEDIATELY recompute styles for all previously cached elements
        if (elements.Count > 0)
        {
            var computedStyles = new Dictionary<IElement, IComputedStyle>();
            foreach (var element in elements)
            {
                var style = GetOrCreateComputedStyle(element);
                computedStyles[element] = style;
            }

            _logger?.LogDebug("Publishing StyleComputedEvent after full invalidation for {count} elements", elements.Count);
            _eventAggregator.Publish(new StyleComputedEvent(elements, computedStyles));
        }
    }

    public IComputedStyle? GetCachedStyle(IElement element)
    {
        if (element == null)
            return null;

        if (_cachedStyles.TryGetValue(element, out var style))
        {
            _logger?.LogTrace("Found cached style for element {element}", element.TagName);
            return style;
        }

        // If not in cache, create it, cache it, and return it
        // This helps tests that directly access GetCachedStyle without going through ComputeStyleAsync
        style = new ComputedStyle(element);
        _cachedStyles[element] = style;
        _logger?.LogTrace("Created and cached style for element {element}", element.TagName);

        return style;
    }

    public Task<string> AddStyleSheetAsync(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null)
    {
        EnsureInitialized();
        _logger?.LogDebug("AddStyleSheetAsync called");

        // Generate a unique ID for the stylesheet
        var styleSheetId = $"style_{++_styleSheetCounter}";
        _styleSheets[styleSheetId] = styleSheet;

        // Invalidate all styles since a stylesheet was added
        InvalidateAllStyles();

        // Publish stylesheet added event
        _eventAggregator.Publish(new StyleSheetChangedEvent(StyleSheetChangeType.Added, styleSheetId));

        return Task.FromResult(styleSheetId);
    }

    public bool RemoveStyleSheet(string styleSheetId)
    {
        EnsureInitialized();
        _logger?.LogDebug("RemoveStyleSheet called for id {id}", styleSheetId);

        var removed = _styleSheets.Remove(styleSheetId);

        if (removed)
        {
            // Invalidate all styles since a stylesheet was removed
            InvalidateAllStyles();

            // Publish stylesheet removed event
            _eventAggregator.Publish(new StyleSheetChangedEvent(StyleSheetChangeType.Removed, styleSheetId));
        }

        return removed;
    }

    // Precompute styles for element and all descendants
    private void PrecomputeStylesForElement(IElement element)
    {
        // Create style for this element
        var style = new ComputedStyle(element);
        _cachedStyles[element] = style;

        // Process all children recursively
        foreach (var child in element.Children)
        {
            PrecomputeStylesForElement(child);
        }
    }

    private IComputedStyle GetOrCreateComputedStyle(IElement element)
    {
        if (!_cachedStyles.TryGetValue(element, out var style))
        {
            _logger?.LogTrace("Creating new computed style for element {element}", element.TagName);
            style = new ComputedStyle(element);
            _cachedStyles[element] = style;
        }
        return style;
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

        _logger?.LogDebug("OnStyleInvalidated for {count} elements", e.Elements.Count);

        // Invalidate the styles for the elements
        foreach (var element in e.Elements)
        {
            _cachedStyles.Remove(element);
        }

        // IMMEDIATELY recompute and publish the styles
        var computedStyles = new Dictionary<IElement, IComputedStyle>();
        foreach (var element in e.Elements)
        {
            computedStyles[element] = GetOrCreateComputedStyle(element);
        }

        // Publish the computed styles event
        if (e.Elements.Count > 0)
        {
            _logger?.LogDebug("Publishing StyleComputedEvent from OnStyleInvalidated for {count} elements", e.Elements.Count);
            _eventAggregator.Publish(new StyleComputedEvent(e.Elements.ToList(), computedStyles));
        }
    }

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        // Update current phase
        _currentPhase = e.Phase;
        _logger?.LogDebug("Phase changed to {phase}, {changeType}", e.Phase, e.ChangeType);

        if (e.Phase == DocumentLifecyclePhase.InStyleRecalc && e.ChangeType == PhaseChangeType.Enter)
        {
            _logger?.LogDebug("Entered style recalc phase, processing updates");

            // Process styles immediately and synchronously
            try
            {
                var elements = new List<IElement>();
                var computedStyles = new Dictionary<IElement, IComputedStyle>();

                if (_document?.Body != null)
                {
                    ProcessElementAndDescendants(_document.DocumentElement ?? _document.Body, elements, computedStyles);
                }

                // Publish computed styles event with all elements
                if (elements.Count > 0)
                {
                    _logger?.LogDebug("Publishing StyleComputedEvent from OnPhaseChanged for {count} elements", elements.Count);
                    _eventAggregator.Publish(new StyleComputedEvent(elements, computedStyles));
                }
                else
                {
                    // Even if empty, publish an event to signal completion
                    _logger?.LogDebug("Publishing empty StyleComputedEvent from OnPhaseChanged");
                    _eventAggregator.Publish(new StyleComputedEvent(
                        new List<IElement>(),
                        new Dictionary<IElement, IComputedStyle>())
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing styles in InStyleRecalc phase");
            }
        }
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
        _logger?.LogInformation("StyleEngine disposing");

        // Unsubscribe from events
        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }
        _subscriptions.Clear();

        // Clear resources
        _cachedStyles.Clear();
        _styleSheets.Clear();
        _document = null;
    }
}