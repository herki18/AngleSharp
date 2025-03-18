namespace Infrastructure.CacheManager.Integration;

using System;
using System.Collections.Generic;
using API.Invalidation;
using API.Management;
using API.Monitoring;
using Internal.Invalidation;

/// <summary>
/// Provides integration between the CacheManager and EventAggregator systems.
/// </summary>
public static class EventAggregatorIntegration
{
    /// <summary>
    /// Integrates the CacheManager with the EventAggregator.
    /// </summary>
    /// <param name="cacheManager">The cache manager to integrate.</param>
    /// <param name="eventAggregator">The event aggregator instance.</param>
    /// <param name="options">Optional integration options.</param>
    /// <returns>A disposable subscription token that can be used to unsubscribe.</returns>
    public static ISubscriptionToken IntegrateWithEventAggregator(
        this ICacheManager cacheManager,
        IEventAggregator eventAggregator,
        CacheEventIntegrationOptions? options = null)
    {
        if (cacheManager == null)
            throw new ArgumentNullException(nameof(cacheManager));

        if (eventAggregator == null)
            throw new ArgumentNullException(nameof(eventAggregator));

        options = options ?? new CacheEventIntegrationOptions();

        // Create an integration handler
        var integrationHandler = new CacheEventIntegrationHandler(cacheManager, eventAggregator, options);

        return integrationHandler;
    }
}

/// <summary>
/// Options for EventAggregator integration.
/// </summary>
public class CacheEventIntegrationOptions
{
    /// <summary>
    /// Gets or sets the minimum priority for cache events that will be published.
    /// </summary>
    public EventPriority MinimumEventPriority { get; set; } = EventPriority.Normal;

    /// <summary>
    /// Gets or sets a value indicating whether to subscribe to DOM mutation events.
    /// </summary>
    public bool SubscribeToDomMutations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to publish cache invalidation events.
    /// </summary>
    public bool PublishInvalidationEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to publish cache trimming events.
    /// </summary>
    public bool PublishTrimmingEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to publish cache metrics events.
    /// </summary>
    public bool PublishMetricsEvents { get; set; } = true;
}

/// <summary>
/// Handler for EventAggregator integration.
/// </summary>
internal class CacheEventIntegrationHandler : ISubscriptionToken, IDisposable
{
    private readonly ICacheManager _cacheManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly CacheEventIntegrationOptions _options;
    private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();
    private readonly CacheInvalidator _cacheInvalidator;
    private bool _isDisposed;

    /// <summary>
    /// Gets the type of event that this token is for.
    /// </summary>
    public Type EventType => typeof(IEvent);

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEventIntegrationHandler"/> class.
    /// </summary>
    /// <param name="cacheManager">The cache manager to integrate.</param>
    /// <param name="eventAggregator">The event aggregator instance.</param>
    /// <param name="options">Integration options.</param>
    public CacheEventIntegrationHandler(
        ICacheManager cacheManager,
        IEventAggregator eventAggregator,
        CacheEventIntegrationOptions options)
    {
        _cacheManager = cacheManager;
        _eventAggregator = eventAggregator;
        _options = options;
        _cacheInvalidator = new CacheInvalidator(cacheManager);

        // Subscribe to events
        SubscribeToEvents();
    }

    /// <summary>
    /// Subscribes to the relevant events from the EventAggregator.
    /// </summary>
    private void SubscribeToEvents()
    {
        // Subscribe to memory pressure events
        _subscriptionTokens.Add(_eventAggregator.SubscribeWithPriority<MemoryPressureEvent>(
            HandleMemoryPressureEvent, EventPriority.Critical));

        // Subscribe to DOM mutation events
        if (_options.SubscribeToDomMutations)
        {
            _subscriptionTokens.Add(_eventAggregator.Subscribe<DomAttributeChangedEvent>(
                HandleDomAttributeChangedEvent));

            _subscriptionTokens.Add(_eventAggregator.Subscribe<DomStructureChangedEvent>(
                HandleDomStructureChangedEvent));
        }

        // Subscribe to cache invalidation requests
        _subscriptionTokens.Add(_eventAggregator.Subscribe<CacheInvalidationRequestEvent>(
            HandleCacheInvalidationRequestEvent));
    }

    /// <summary>
    /// Handles memory pressure events from the system.
    /// </summary>
    /// <param name="e">The memory pressure event.</param>
    private void HandleMemoryPressureEvent(MemoryPressureEvent e)
    {
        // Convert EventAggregator event to CacheManager event
        var pressureSeverity = ConvertToMemoryPressureSeverity(e.Severity);

        // Determine how to respond based on severity
        switch (pressureSeverity)
        {
            case PressureSeverity.Low:
                _cacheManager.TrimByPriority(CachePriority.Low, 50);
                break;

            case PressureSeverity.Medium:
                _cacheManager.TrimByPriority(CachePriority.Low, 100);
                _cacheManager.TrimByPriority(CachePriority.Normal, 50);
                break;

            case PressureSeverity.High:
                _cacheManager.TrimByPriority(CachePriority.Low, 100);
                _cacheManager.TrimByPriority(CachePriority.Normal, 100);
                _cacheManager.TrimByPriority(CachePriority.High, 50);
                break;

            case PressureSeverity.Critical:
                _cacheManager.TrimByPriority(CachePriority.High, 100);
                _cacheManager.TrimByPriority(CachePriority.Critical, 50);

                // Request garbage collection
                GC.Collect();
                break;
        }

        // Publish a cache trimmed event if enabled
        if (_options.PublishTrimmingEvents)
        {
            PublishCacheTrimmingEvent(pressureSeverity);
        }
    }

    /// <summary>
    /// Handles DOM attribute changed events.
    /// </summary>
    /// <param name="e">The DOM attribute changed event.</param>
    private void HandleDomAttributeChangedEvent(DomAttributeChangedEvent e)
    {
        // Handle based on attribute type
        if (IsStyleRelatedAttribute(e.AttributeName))
        {
            // Invalidate style cache entries for this element
            _cacheInvalidator.InvalidateByScope(InvalidationScope.Style);

            // Use element ID as key for more targeted invalidation
            if (!string.IsNullOrEmpty(e.ElementId))
            {
                _cacheInvalidator.Invalidate("StyleCache", e.ElementId);

                // Invalidate dependents in layout cache
                _cacheInvalidator.InvalidateWithDependents("LayoutCache", e.ElementId);
            }
        }
        else if (IsLayoutRelatedAttribute(e.AttributeName))
        {
            // Invalidate layout cache entries for this element
            _cacheInvalidator.InvalidateByScope(InvalidationScope.Layout);

            // Use element ID as key for more targeted invalidation
            if (!string.IsNullOrEmpty(e.ElementId))
            {
                _cacheInvalidator.Invalidate("LayoutCache", e.ElementId);

                // Invalidate dependents in render cache
                _cacheInvalidator.InvalidateWithDependents("RenderCache", e.ElementId);
            }
        }

        // Publish invalidation event if enabled
        if (_options.PublishInvalidationEvents)
        {
            PublishCacheInvalidationEvent(e.ElementId, e.AttributeName);
        }
    }

    /// <summary>
    /// Handles DOM structure changed events.
    /// </summary>
    /// <param name="e">The DOM structure changed event.</param>
    private void HandleDomStructureChangedEvent(DomStructureChangedEvent e)
    {
        // Structure changes affect layout and rendering
        _cacheInvalidator.InvalidateByScope(InvalidationScope.Layout);
        _cacheInvalidator.InvalidateByScope(InvalidationScope.Render);

        // If a specific element is affected, perform targeted invalidation
        if (!string.IsNullOrEmpty(e.ParentElementId))
        {
            _cacheInvalidator.InvalidateWithDependents("LayoutCache", e.ParentElementId);
            _cacheInvalidator.InvalidateWithDependents("RenderCache", e.ParentElementId);
        }

        // Publish invalidation event if enabled
        if (_options.PublishInvalidationEvents)
        {
            PublishCacheInvalidationEvent(e.ParentElementId, "structure");
        }
    }

    /// <summary>
    /// Handles cache invalidation request events.
    /// </summary>
    /// <param name="e">The cache invalidation request event.</param>
    private void HandleCacheInvalidationRequestEvent(CacheInvalidationRequestEvent e)
    {
        if (string.IsNullOrEmpty(e.CacheName))
        {
            // Invalidate all caches
            if (e.InvalidateAll)
            {
                _cacheManager.ClearAllCaches();
            }
            else
            {
                // Invalidate by scope
                _cacheInvalidator.InvalidateByScope(e.Scope);
            }
        }
        else
        {
            // Invalidate specific cache
            if (e.InvalidateAll)
            {
                try
                {
                    var cache = _cacheManager.GetCache<ITrimableCache>(e.CacheName);
                    cache.Clear();
                }
                catch (Exception)
                {
                    // Cache doesn't exist or has invalid type
                }
            }
            else if (e.Key != null)
            {
                // Invalidate specific key
                _cacheInvalidator.Invalidate(e.CacheName, e.Key);
            }
        }

        // Publish invalidation event if enabled
        if (_options.PublishInvalidationEvents)
        {
            PublishCacheInvalidationEvent(e.Key?.ToString() ?? throw new InvalidOperationException(), e.CacheName ?? throw new InvalidOperationException());
        }
    }

    /// <summary>
    /// Determines if an attribute is related to styling.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>True if the attribute affects styling, false otherwise.</returns>
    private bool IsStyleRelatedAttribute(string attributeName)
    {
        if (string.IsNullOrEmpty(attributeName))
            return false;

        // Common style-related attributes
        return attributeName.Equals("class", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("style", StringComparison.OrdinalIgnoreCase) ||
               attributeName.StartsWith("data-style-", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("id", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if an attribute is related to layout.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>True if the attribute affects layout, false otherwise.</returns>
    private bool IsLayoutRelatedAttribute(string attributeName)
    {
        if (string.IsNullOrEmpty(attributeName))
            return false;

        // Common layout-related attributes
        return attributeName.Equals("width", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("height", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("align", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("valign", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("margin", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("padding", StringComparison.OrdinalIgnoreCase) ||
               attributeName.Equals("hidden", StringComparison.OrdinalIgnoreCase) ||
               attributeName.StartsWith("data-layout-", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Publishes a cache invalidation event.
    /// </summary>
    /// <param name="elementId">The ID of the element that triggered invalidation.</param>
    /// <param name="reason">The reason for invalidation.</param>
    private void PublishCacheInvalidationEvent(string elementId, string reason)
    {
        var invalidationEvent = new CacheEntriesInvalidatedEvent(
            elementId,
            reason,
            DateTime.UtcNow);

        _eventAggregator.Publish(invalidationEvent, _options.MinimumEventPriority);
    }

    /// <summary>
    /// Publishes a cache trimming event.
    /// </summary>
    /// <param name="pressureSeverity">The severity of the memory pressure that triggered trimming.</param>
    private void PublishCacheTrimmingEvent(PressureSeverity pressureSeverity)
    {
        var trimmingEvent = new CacheTrimmedEvent(
            _cacheManager.GetTotalCacheSize(),
            _cacheManager.GetTotalEntryCount(),
            pressureSeverity.ToString(),
            DateTime.UtcNow);

        _eventAggregator.Publish(trimmingEvent, _options.MinimumEventPriority);
    }

    /// <summary>
    /// Converts EventAggregator priority to cache memory pressure severity.
    /// </summary>
    /// <param name="eventPriority">The event priority.</param>
    /// <returns>The equivalent memory pressure severity.</returns>
    private PressureSeverity ConvertToMemoryPressureSeverity(EventPriority eventPriority)
    {
        switch (eventPriority)
        {
            case EventPriority.Critical:
                return PressureSeverity.Critical;
            case EventPriority.High:
                return PressureSeverity.High;
            case EventPriority.Normal:
                return PressureSeverity.Medium;
            default:
                return PressureSeverity.Low;
        }
    }

    /// <summary>
    /// Disposes resources used by the integration handler.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Unsubscribe from all events
        foreach (var token in _subscriptionTokens)
        {
            _eventAggregator.Unsubscribe(token);
        }

        _subscriptionTokens.Clear();
    }
}

#region Event Types

/// <summary>
/// Base interface for all events.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// Interface for the EventAggregator.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventData">The event data.</param>
    void Publish<TEvent>(TEvent eventData) where TEvent : class, IEvent;

    /// <summary>
    /// Publishes an event to all subscribers with the specified priority.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventData">The event data.</param>
    /// <param name="priority">The priority of the event.</param>
    void Publish<TEvent>(TEvent eventData, EventPriority priority) where TEvent : class, IEvent;

    /// <summary>
    /// Gets an observable for events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <returns>An observable for the event type.</returns>
    IObservable<TEvent> GetObservable<TEvent>() where TEvent : class, IEvent;

    /// <summary>
    /// Subscribes to events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The event handler.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent;

    /// <summary>
    /// Subscribes to events of the specified type with a filter.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="filter">A filter function to determine which events to handle.</param>
    /// <param name="handler">The event handler.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent> filter, Action<TEvent> handler) where TEvent : class, IEvent;

    /// <summary>
    /// Subscribes to events of the specified type with a minimum priority.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The event handler.</param>
    /// <param name="minimumPriority">The minimum priority of events to receive.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    ISubscriptionToken SubscribeWithPriority<TEvent>(Action<TEvent> handler, EventPriority minimumPriority)
        where TEvent : class, IEvent;

    /// <summary>
    /// Unsubscribes from events using the specified token.
    /// </summary>
    /// <param name="token">The subscription token.</param>
    void Unsubscribe(ISubscriptionToken token);
}

/// <summary>
/// Interface for subscription tokens.
/// </summary>
public interface ISubscriptionToken : IDisposable
{
    /// <summary>
    /// Gets the type of event that this token is for.
    /// </summary>
    Type EventType { get; }
}

/// <summary>
/// Enum defining priority levels for events.
/// </summary>
public enum EventPriority
{
    /// <summary>
    /// Low priority events.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority events.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority events.
    /// </summary>
    High = 20,

    /// <summary>
    /// Critical priority events.
    /// </summary>
    Critical = 30
}

/// <summary>
/// Event raised when memory pressure is detected.
/// </summary>
public class MemoryPressureEvent : IEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the severity of the memory pressure.
    /// </summary>
    public EventPriority Severity { get; }

    /// <summary>
    /// Gets the current memory usage in bytes.
    /// </summary>
    public long CurrentMemoryUsage { get; }

    /// <summary>
    /// Gets the memory usage percentage.
    /// </summary>
    public double MemoryUsagePercentage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPressureEvent"/> class.
    /// </summary>
    /// <param name="severity">The severity of the memory pressure.</param>
    /// <param name="currentMemoryUsage">The current memory usage in bytes.</param>
    /// <param name="memoryUsagePercentage">The memory usage percentage.</param>
    public MemoryPressureEvent(
        EventPriority severity,
        long currentMemoryUsage,
        double memoryUsagePercentage)
    {
        Severity = severity;
        CurrentMemoryUsage = currentMemoryUsage;
        MemoryUsagePercentage = memoryUsagePercentage;
    }
}

/// <summary>
/// Event raised when DOM attributes change.
/// </summary>
public class DomAttributeChangedEvent : IEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the ID of the element that changed.
    /// </summary>
    public string ElementId { get; }

    /// <summary>
    /// Gets the name of the attribute that changed.
    /// </summary>
    public string AttributeName { get; }

    /// <summary>
    /// Gets the old value of the attribute.
    /// </summary>
    public string OldValue { get; }

    /// <summary>
    /// Gets the new value of the attribute.
    /// </summary>
    public string NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomAttributeChangedEvent"/> class.
    /// </summary>
    /// <param name="elementId">The ID of the element that changed.</param>
    /// <param name="attributeName">The name of the attribute that changed.</param>
    /// <param name="oldValue">The old value of the attribute.</param>
    /// <param name="newValue">The new value of the attribute.</param>
    public DomAttributeChangedEvent(
        string elementId,
        string attributeName,
        string oldValue,
        string newValue)
    {
        ElementId = elementId;
        AttributeName = attributeName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Event raised when DOM structure changes.
/// </summary>
public class DomStructureChangedEvent : IEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the ID of the parent element where the structure changed.
    /// </summary>
    public string ParentElementId { get; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public DomChangeType ChangeType { get; }

    /// <summary>
    /// Gets the ID of the target element that was added or removed, if applicable.
    /// </summary>
    public string? TargetElementId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomStructureChangedEvent"/> class.
    /// </summary>
    /// <param name="parentElementId">The ID of the parent element where the structure changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <param name="targetElementId">The ID of the target element that was added or removed, if applicable.</param>
    public DomStructureChangedEvent(
        string parentElementId,
        DomChangeType changeType,
        string? targetElementId = null)
    {
        ParentElementId = parentElementId;
        ChangeType = changeType;
        TargetElementId = targetElementId;
    }
}

/// <summary>
/// Enum defining types of DOM structure changes.
/// </summary>
public enum DomChangeType
{
    /// <summary>
    /// An element was added.
    /// </summary>
    NodeAdded,

    /// <summary>
    /// An element was removed.
    /// </summary>
    NodeRemoved,

    /// <summary>
    /// An element was moved.
    /// </summary>
    NodeMoved,

    /// <summary>
    /// Multiple changes occurred.
    /// </summary>
    MultipleChanges
}

/// <summary>
/// Event requesting cache invalidation.
/// </summary>
public class CacheInvalidationRequestEvent : IEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the name of the cache to invalidate.
    /// </summary>
    public string? CacheName { get; }

    /// <summary>
    /// Gets the key to invalidate.
    /// </summary>
    public object? Key { get; }

    /// <summary>
    /// Gets a value indicating whether to invalidate all entries in the cache.
    /// </summary>
    public bool InvalidateAll { get; }

    /// <summary>
    /// Gets the invalidation scope.
    /// </summary>
    public InvalidationScope Scope { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheInvalidationRequestEvent"/> class.
    /// </summary>
    /// <param name="cacheName">The name of the cache to invalidate.</param>
    /// <param name="key">The key to invalidate.</param>
    /// <param name="invalidateAll">Whether to invalidate all entries in the cache.</param>
    /// <param name="scope">The invalidation scope.</param>
    public CacheInvalidationRequestEvent(
        string? cacheName = null,
        object? key = null,
        bool invalidateAll = false,
        InvalidationScope scope = InvalidationScope.Entry)
    {
        CacheName = cacheName;
        Key = key;
        InvalidateAll = invalidateAll;
        Scope = scope;
    }
}

/// <summary>
/// Event raised when cache entries are invalidated.
/// </summary>
public class CacheEntriesInvalidatedEvent : IEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the element ID associated with the invalidation, if any.
    /// </summary>
    public string ElementId { get; }

    /// <summary>
    /// Gets the reason for invalidation.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntriesInvalidatedEvent"/> class.
    /// </summary>
    /// <param name="elementId">The element ID associated with the invalidation.</param>
    /// <param name="reason">The reason for invalidation.</param>
    /// <param name="timestamp">The timestamp of the invalidation.</param>
    public CacheEntriesInvalidatedEvent(
        string elementId,
        string reason,
        DateTime timestamp)
    {
        ElementId = elementId;
        Reason = reason;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when caches are trimmed.
/// </summary>
public class CacheTrimmedEvent : IEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the total cache size after trimming.
    /// </summary>
    public long TotalCacheSize { get; }

    /// <summary>
    /// Gets the total entry count after trimming.
    /// </summary>
    public int TotalEntryCount { get; }

    /// <summary>
    /// Gets the reason for trimming.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheTrimmedEvent"/> class.
    /// </summary>
    /// <param name="totalCacheSize">The total cache size after trimming.</param>
    /// <param name="totalEntryCount">The total entry count after trimming.</param>
    /// <param name="reason">The reason for trimming.</param>
    /// <param name="timestamp">The timestamp of the trimming.</param>
    public CacheTrimmedEvent(
        long totalCacheSize,
        int totalEntryCount,
        string reason,
        DateTime timestamp)
    {
        TotalCacheSize = totalCacheSize;
        TotalEntryCount = totalEntryCount;
        Reason = reason;
        Timestamp = timestamp;
    }
}

#endregion