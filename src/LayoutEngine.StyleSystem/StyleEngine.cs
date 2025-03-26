using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Contracts.StyleSystem;
using Infrastructure.EventAggregator.API.Aggregation;
using Microsoft.Extensions.Logging;

namespace LayoutEngine.StyleSystem;

/// <summary>
/// Simple implementation of IComputedStyle that returns mocked style data.
/// </summary>
public class ComputedStyle : IComputedStyle
{
    private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IElement Element { get; }

    public ComputedStyle(IElement element)
    {
        Element = element;

        // Basic dummy style values based on element tag
        _properties["color"] = "rgba(0, 0, 0, 1)";
        _properties["background-color"] = "rgba(255, 255, 255, 1)";
        _properties["font-family"] = "Arial, sans-serif";
        _properties["font-size"] = "16px";
        _properties["display"] = "block";
        _properties["margin"] = "0px";
        _properties["padding"] = "0px";

        // Add tag-specific dummy styles
        var tagName = element.TagName.ToLowerInvariant();
        if (tagName == "h1" || tagName == "h2")
        {
            _properties["font-weight"] = "bold";
            _properties["margin-bottom"] = "16px";
            _properties["font-size"] = tagName == "h1" ? "32px" : "24px";
        }
        else if (tagName == "p")
        {
            _properties["margin-bottom"] = "16px";
        }
        else if (tagName == "a")
        {
            _properties["color"] = "rgba(0, 0, 255, 1)";
            _properties["text-decoration"] = "underline";
        }
        else if (tagName == "span")
        {
            _properties["display"] = "inline";
        }
        else if (tagName == "body")
        {
            _properties["margin"] = "8px";
        }
        else if (tagName == "div")
        {
            _properties["margin-bottom"] = "8px";
        }
    }

    public string GetValue(string propertyName)
    {
        return _properties.TryGetValue(propertyName, out var value) ? value : string.Empty;
    }

    public IReadOnlyDictionary<string, string> Properties => _properties;

    public bool HasProperty(string propertyName)
    {
        return _properties.ContainsKey(propertyName);
    }
}

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

        if (_document?.Body == null)
            return Task.CompletedTask;

        // Just compute styles for body and its direct children in our mock
        var elements = new List<IElement>();
        var computedStyles = new Dictionary<IElement, IComputedStyle>();

        elements.Add(_document.Body);
        computedStyles[_document.Body] = GetOrCreateComputedStyle(_document.Body);

        foreach (var child in _document.Body.Children)
        {
            elements.Add(child);
            computedStyles[child] = GetOrCreateComputedStyle(child);
        }

        // Publish computed styles event
        if (elements.Count > 0)
        {
            _eventAggregator.Publish(new StyleComputedEvent(elements, computedStyles));
        }

        return Task.CompletedTask;
    }

    public Task<IComputedStyle> ComputeStyleAsync(IElement element)
    {
        EnsureInitialized();

        var style = GetOrCreateComputedStyle(element);

        // Publish single element style computed event
        var elements = new List<IElement> { element };
        var computedStyles = new Dictionary<IElement, IComputedStyle> { [element] = style };
        _eventAggregator.Publish(new StyleComputedEvent(elements, computedStyles));

        return Task.FromResult(style);
    }

    public void InvalidateStyles(IReadOnlyList<IElement> elements)
    {
        EnsureInitialized();

        // Remove elements from cache
        foreach (var element in elements)
        {
            _cachedStyles.Remove(element);
        }
    }

    public void InvalidateAllStyles()
    {
        EnsureInitialized();

        // Clear all cached styles
        _cachedStyles.Clear();
    }

    public IComputedStyle? GetCachedStyle(IElement element)
    {
        return _cachedStyles.TryGetValue(element, out var style) ? style : null;
    }

    public Task<string> AddStyleSheetAsync(string styleSheet, StyleSheetOrigin origin, string? mediaQuery = null)
    {
        EnsureInitialized();

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

    private IComputedStyle GetOrCreateComputedStyle(IElement element)
    {
        if (!_cachedStyles.TryGetValue(element, out var style))
        {
            style = new ComputedStyle(element);
            _cachedStyles[element] = style;
        }
        return style;
    }

    private void OnStyleInvalidated(StyleInvalidatedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        // Invalidate the styles for the elements
        foreach (var element in e.Elements)
        {
            _cachedStyles.Remove(element);
        }
    }

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (_isDisposed || !_isInitialized)
            return;

        // Update current phase
        _currentPhase = e.Phase;

        if (e.Phase == DocumentLifecyclePhase.InStyleRecalc && e.ChangeType == PhaseChangeType.Enter)
        {
            // Process styles
            ProcessUpdatesAsync().ContinueWith(_ =>
            {
                // No direct calls to lifecycle coordinator - publish an event instead
                _eventAggregator.Publish(new StyleComputedEvent(
                    new List<IElement>(),
                    new Dictionary<IElement, IComputedStyle>())
                );
            });
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