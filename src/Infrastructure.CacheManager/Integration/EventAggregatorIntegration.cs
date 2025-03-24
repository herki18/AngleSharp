// using System;
// using System.Collections.Generic;
// using Infrastructure.CacheManager.API.Invalidation;
// using Infrastructure.CacheManager.API.Management;
// using Infrastructure.CacheManager.API.Monitoring;
// using Infrastructure.CacheManager.Internal.Invalidation;
//
// namespace Infrastructure.CacheManager.Integration
// {
//     /// <summary>
//     /// Integration between the CacheManager and EventAggregator systems.
//     /// Provides event-based communication for cache operations.
//     /// </summary>
//     public static class EventAggregatorIntegration
//     {
//         /// <summary>
//         /// Integrates the cache manager with the event aggregator for event-based cache management.
//         /// </summary>
//         public static ISubscriptionToken IntegrateWithEventAggregator(
//             this ICacheManager cacheManager,
//             IEventAggregator eventAggregator,
//             CacheEventIntegrationOptions? options = null)
//         {
//             if (cacheManager == null)
//                 throw new ArgumentNullException(nameof(cacheManager));
//
//             if (eventAggregator == null)
//                 throw new ArgumentNullException(nameof(eventAggregator));
//
//             options = options ?? new CacheEventIntegrationOptions();
//             var integrationHandler = new CacheEventIntegrationHandler(cacheManager, eventAggregator, options);
//             return integrationHandler;
//         }
//     }
//
//     /// <summary>
//     /// Options for configuring the integration between cache manager and event aggregator.
//     /// </summary>
//     public class CacheEventIntegrationOptions
//     {
//         /// <summary>
//         /// Minimum priority level for events published by the integration.
//         /// </summary>
//         public EventPriority MinimumEventPriority { get; set; } = EventPriority.Normal;
//
//         /// <summary>
//         /// Whether to subscribe to DOM mutation events for cache invalidation.
//         /// </summary>
//         public bool SubscribeToDomMutations { get; set; } = true;
//
//         /// <summary>
//         /// Whether to publish events when cache entries are invalidated.
//         /// </summary>
//         public bool PublishInvalidationEvents { get; set; } = true;
//
//         /// <summary>
//         /// Whether to publish events when caches are trimmed.
//         /// </summary>
//         public bool PublishTrimmingEvents { get; set; } = true;
//
//         /// <summary>
//         /// Whether to publish cache metrics events.
//         /// </summary>
//         public bool PublishMetricsEvents { get; set; } = true;
//     }
//
//     /// <summary>
//     /// Handler for the integration between cache manager and event aggregator.
//     /// </summary>
//     internal class CacheEventIntegrationHandler : ISubscriptionToken, IDisposable
//     {
//         private readonly ICacheManager _cacheManager;
//         private readonly IEventAggregator _eventAggregator;
//         private readonly CacheEventIntegrationOptions _options;
//         private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();
//         private readonly CacheInvalidator _cacheInvalidator;
//         private bool _isDisposed;
//
//         public Type EventType => typeof(IEvent);
//
//         public CacheEventIntegrationHandler(
//             ICacheManager cacheManager,
//             IEventAggregator eventAggregator,
//             CacheEventIntegrationOptions options)
//         {
//             _cacheManager = cacheManager;
//             _eventAggregator = eventAggregator;
//             _options = options;
//             _cacheInvalidator = new CacheInvalidator(cacheManager);
//
//             SubscribeToEvents();
//         }
//
//         private void SubscribeToEvents()
//         {
//             // Subscribe to memory pressure events with critical priority
//             _subscriptionTokens.Add(_eventAggregator.SubscribeWithPriority<MemoryPressureEvent>(
//                 HandleMemoryPressureEvent, EventPriority.Critical));
//
//             // Subscribe to DOM mutation events if enabled
//             if (_options.SubscribeToDomMutations)
//             {
//                 _subscriptionTokens.Add(_eventAggregator.Subscribe<DomAttributeChangedEvent>(
//                     HandleDomAttributeChangedEvent));
//
//                 _subscriptionTokens.Add(_eventAggregator.Subscribe<DomStructureChangedEvent>(
//                     HandleDomStructureChangedEvent));
//             }
//
//             // Subscribe to cache invalidation requests
//             _subscriptionTokens.Add(_eventAggregator.Subscribe<CacheInvalidationRequestEvent>(
//                 HandleCacheInvalidationRequestEvent));
//         }
//
//         private void HandleMemoryPressureEvent(MemoryPressureEvent e)
//         {
//             var pressureSeverity = ConvertToMemoryPressureSeverity(e.Severity);
//
//             // Apply different trimming strategies based on pressure severity
//             switch (pressureSeverity)
//             {
//                 case PressureSeverity.Low:
//                     _cacheManager.TrimByPriority(CachePriority.Low, 50);
//                     break;
//
//                 case PressureSeverity.Medium:
//                     _cacheManager.TrimByPriority(CachePriority.Low, 100);
//                     _cacheManager.TrimByPriority(CachePriority.Normal, 50);
//                     break;
//
//                 case PressureSeverity.High:
//                     _cacheManager.TrimByPriority(CachePriority.Low, 100);
//                     _cacheManager.TrimByPriority(CachePriority.Normal, 100);
//                     _cacheManager.TrimByPriority(CachePriority.High, 50);
//                     break;
//
//                 case PressureSeverity.Critical:
//                     _cacheManager.TrimByPriority(CachePriority.High, 100);
//                     _cacheManager.TrimByPriority(CachePriority.Critical, 50);
//                     GC.Collect(); // Force garbage collection in critical situations
//                     break;
//             }
//
//             // Publish trimming event if enabled
//             if (_options.PublishTrimmingEvents)
//             {
//                 PublishCacheTrimmingEvent(pressureSeverity);
//             }
//         }
//
//         private void HandleDomAttributeChangedEvent(DomAttributeChangedEvent e)
//         {
//             // Handle style-related attribute changes
//             if (IsStyleRelatedAttribute(e.AttributeName))
//             {
//                 _cacheInvalidator.InvalidateByScope(InvalidationScope.Style);
//
//                 if (!string.IsNullOrEmpty(e.ElementId))
//                 {
//                     _cacheInvalidator.Invalidate("StyleCache", e.ElementId);
//                     _cacheInvalidator.InvalidateWithDependents("LayoutCache", e.ElementId);
//                 }
//             }
//             // Handle layout-related attribute changes
//             else if (IsLayoutRelatedAttribute(e.AttributeName))
//             {
//                 _cacheInvalidator.InvalidateByScope(InvalidationScope.Layout);
//
//                 if (!string.IsNullOrEmpty(e.ElementId))
//                 {
//                     _cacheInvalidator.Invalidate("LayoutCache", e.ElementId);
//                     _cacheInvalidator.InvalidateWithDependents("RenderCache", e.ElementId);
//                 }
//             }
//
//             // Publish invalidation event if enabled
//             if (_options.PublishInvalidationEvents)
//             {
//                 PublishCacheInvalidationEvent(e.ElementId, e.AttributeName);
//             }
//         }
//
//         private void HandleDomStructureChangedEvent(DomStructureChangedEvent e)
//         {
//             // Structure changes require more aggressive invalidation
//             _cacheInvalidator.InvalidateByScope(InvalidationScope.Layout);
//             _cacheInvalidator.InvalidateByScope(InvalidationScope.Render);
//
//             if (!string.IsNullOrEmpty(e.ParentElementId))
//             {
//                 _cacheInvalidator.InvalidateWithDependents("LayoutCache", e.ParentElementId);
//                 _cacheInvalidator.InvalidateWithDependents("RenderCache", e.ParentElementId);
//             }
//
//             // Publish invalidation event if enabled
//             if (_options.PublishInvalidationEvents)
//             {
//                 PublishCacheInvalidationEvent(e.ParentElementId, "structure");
//             }
//         }
//
//         private void HandleCacheInvalidationRequestEvent(CacheInvalidationRequestEvent e)
//         {
//             if (string.IsNullOrEmpty(e.CacheName))
//             {
//                 if (e.InvalidateAll)
//                 {
//                     _cacheManager.ClearAllCaches();
//                 }
//                 else
//                 {
//                     _cacheInvalidator.InvalidateByScope(e.Scope);
//                 }
//             }
//             else
//             {
//                 if (e.InvalidateAll)
//                 {
//                     try
//                     {
//                         var cache = _cacheManager.GetCache<ITrimableCache>(e.CacheName);
//                         cache.Clear();
//                     }
//                     catch (Exception)
//                     {
//                         // Swallow exceptions if cache not found
//                     }
//                 }
//                 else if (e.Key != null)
//                 {
//                     _cacheInvalidator.Invalidate(e.CacheName, e.Key);
//                 }
//             }
//
//             // Publish invalidation event if enabled
//             if (_options.PublishInvalidationEvents)
//             {
//                 PublishCacheInvalidationEvent(e.Key?.ToString(), e.CacheName);
//             }
//         }
//
//         private bool IsStyleRelatedAttribute(string attributeName)
//         {
//             if (string.IsNullOrEmpty(attributeName))
//                 return false;
//
//             return attributeName.Equals("class", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.Equals("style", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.StartsWith("data-style-", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.Equals("id", StringComparison.OrdinalIgnoreCase);
//         }
//
//         private bool IsLayoutRelatedAttribute(string attributeName)
//         {
//             if (string.IsNullOrEmpty(attributeName))
//                 return false;
//
//             return attributeName.Equals("width", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.Equals("height", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.Equals("align", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.Equals("valign", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.Equals("margin", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.Equals("padding", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.Equals("hidden", StringComparison.OrdinalIgnoreCase) ||
//                    attributeName.StartsWith("data-layout-", StringComparison.OrdinalIgnoreCase);
//         }
//
//         private void PublishCacheInvalidationEvent(string? elementId, string? reason)
//         {
//             var invalidationEvent = new CacheEntriesInvalidatedEvent(
//                 elementId ?? string.Empty,
//                 reason ?? "unknown",
//                 DateTime.UtcNow);
//
//             _eventAggregator.Publish(invalidationEvent, _options.MinimumEventPriority);
//         }
//
//         private void PublishCacheTrimmingEvent(PressureSeverity pressureSeverity)
//         {
//             var trimmingEvent = new CacheTrimmedEvent(
//                 _cacheManager.GetTotalCacheSize(),
//                 _cacheManager.GetTotalEntryCount(),
//                 pressureSeverity.ToString(),
//                 DateTime.UtcNow);
//
//             _eventAggregator.Publish(trimmingEvent, _options.MinimumEventPriority);
//         }
//
//         private PressureSeverity ConvertToMemoryPressureSeverity(EventPriority eventPriority)
//         {
//             switch (eventPriority)
//             {
//                 case EventPriority.Critical:
//                     return PressureSeverity.Critical;
//                 case EventPriority.High:
//                     return PressureSeverity.High;
//                 case EventPriority.Normal:
//                     return PressureSeverity.Medium;
//                 default:
//                     return PressureSeverity.Low;
//             }
//         }
//
//         public void Dispose()
//         {
//             if (_isDisposed)
//                 return;
//
//             _isDisposed = true;
//
//             foreach (var token in _subscriptionTokens)
//             {
//                 _eventAggregator.Unsubscribe(token);
//             }
//
//             _subscriptionTokens.Clear();
//         }
//     }
//
//     #region Event Types
//     public interface IEvent
//     {
//         Guid Id { get; }
//         DateTime Timestamp { get; }
//     }
//
//     public interface IEventAggregator
//     {
//         void Publish<TEvent>(TEvent eventData) where TEvent : class, IEvent;
//         void Publish<TEvent>(TEvent eventData, EventPriority priority) where TEvent : class, IEvent;
//         IObservable<TEvent> GetObservable<TEvent>() where TEvent : class, IEvent;
//         ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent;
//         ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler) where TEvent : class, IEvent;
//         ISubscriptionToken SubscribeWithPriority<TEvent>(Action<TEvent> handler, EventPriority minimumPriority)
//             where TEvent : class, IEvent;
//         void Unsubscribe(ISubscriptionToken token);
//     }
//
//     public interface ISubscriptionToken : IDisposable
//     {
//         Type EventType { get; }
//     }
//
//     public enum EventPriority
//     {
//         Low = 0,
//         Normal = 10,
//         High = 20,
//         Critical = 30
//     }
//
//     public class MemoryPressureEvent : IEvent
//     {
//         public Guid Id { get; } = Guid.NewGuid();
//         public DateTime Timestamp { get; } = DateTime.UtcNow;
//         public EventPriority Severity { get; }
//         public long CurrentMemoryUsage { get; }
//         public double MemoryUsagePercentage { get; }
//
//         public MemoryPressureEvent(
//             EventPriority severity,
//             long currentMemoryUsage,
//             double memoryUsagePercentage)
//         {
//             Severity = severity;
//             CurrentMemoryUsage = currentMemoryUsage;
//             MemoryUsagePercentage = memoryUsagePercentage;
//         }
//     }
//
//     public class DomAttributeChangedEvent : IEvent
//     {
//         public Guid Id { get; } = Guid.NewGuid();
//         public DateTime Timestamp { get; } = DateTime.UtcNow;
//         public string ElementId { get; }
//         public string AttributeName { get; }
//         public string OldValue { get; }
//         public string NewValue { get; }
//
//         public DomAttributeChangedEvent(
//             string elementId,
//             string attributeName,
//             string oldValue,
//             string newValue)
//         {
//             ElementId = elementId;
//             AttributeName = attributeName;
//             OldValue = oldValue;
//             NewValue = newValue;
//         }
//     }
//
//     public class DomStructureChangedEvent : IEvent
//     {
//         public Guid Id { get; } = Guid.NewGuid();
//         public DateTime Timestamp { get; } = DateTime.UtcNow;
//         public string ParentElementId { get; }
//         public DomChangeType ChangeType { get; }
//         public string? TargetElementId { get; }
//
//         public DomStructureChangedEvent(
//             string parentElementId,
//             DomChangeType changeType,
//             string? targetElementId = null)
//         {
//             ParentElementId = parentElementId;
//             ChangeType = changeType;
//             TargetElementId = targetElementId;
//         }
//     }
//
//     public enum DomChangeType
//     {
//         NodeAdded,
//         NodeRemoved,
//         NodeMoved,
//         MultipleChanges
//     }
//
//     public class CacheInvalidationRequestEvent : IEvent
//     {
//         public Guid Id { get; } = Guid.NewGuid();
//         public DateTime Timestamp { get; } = DateTime.UtcNow;
//         public string? CacheName { get; }
//         public object? Key { get; }
//         public bool InvalidateAll { get; }
//         public InvalidationScope Scope { get; }
//
//         public CacheInvalidationRequestEvent(
//             string? cacheName = null,
//             object? key = null,
//             bool invalidateAll = false,
//             InvalidationScope scope = InvalidationScope.Entry)
//         {
//             CacheName = cacheName;
//             Key = key;
//             InvalidateAll = invalidateAll;
//             Scope = scope;
//         }
//     }
//
//     public class CacheEntriesInvalidatedEvent : IEvent
//     {
//         public Guid Id { get; } = Guid.NewGuid();
//         public DateTime Timestamp { get; }
//         public string ElementId { get; }
//         public string Reason { get; }
//
//         public CacheEntriesInvalidatedEvent(
//             string elementId,
//             string reason,
//             DateTime timestamp)
//         {
//             ElementId = elementId;
//             Reason = reason;
//             Timestamp = timestamp;
//         }
//     }
//
//     public class CacheTrimmedEvent : IEvent
//     {
//         public Guid Id { get; } = Guid.NewGuid();
//         public DateTime Timestamp { get; }
//         public long TotalCacheSize { get; }
//         public int TotalEntryCount { get; }
//         public string Reason { get; }
//
//         public CacheTrimmedEvent(
//             long totalCacheSize,
//             int totalEntryCount,
//             string reason,
//             DateTime timestamp)
//         {
//             TotalCacheSize = totalCacheSize;
//             TotalEntryCount = totalEntryCount;
//             Reason = reason;
//             Timestamp = timestamp;
//         }
//     }
//     #endregion
// }