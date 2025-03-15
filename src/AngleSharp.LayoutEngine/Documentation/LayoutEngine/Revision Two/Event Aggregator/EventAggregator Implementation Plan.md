# EventAggregator Implementation Plan

## Overview

This document outlines the implementation plan for integrating the EventAggregator pattern into the AngleSharp.StyleSystem. The plan focuses on direct integration without adapter components, creating a generic solution that can be used broadly throughout the system.

## Phase 1: Core Implementation

### 1.1 Create Base Interfaces and Classes

- Create `IEventAggregator` interface defining core functionality:
  - `Publish<TEvent>(TEvent eventData)`
  - `Subscribe<TEvent>(Action<TEvent> handler)`
  - `Subscribe<TEvent>(Predicate<TEvent> filter, Action<TEvent> handler)`
  - `Unsubscribe(ISubscriptionToken token)`
  - `ClearSubscriptions<TEvent>()`
  - `ClearAllSubscriptions()`

- Create `ISubscriptionToken` interface with:
  - `EventType` property
  - `IsDisposed` property
  - `Dispose()` method (from IDisposable)

- Implement `EventAggregator` and `SubscriptionToken` concrete classes:
  - Thread-safe implementation with reader-writer locks
  - Efficient handling of concurrent subscriptions and publications
  - Exception handling in event processing

### 1.2 Create Unit Tests

- Base functionality tests:
  - Subscription and unsubscription
  - Publishing events to subscribers
  - Filtered subscriptions
  - Exception handling in subscribers
  - Concurrent operation safety

- Performance tests:
  - Measure throughput for high-frequency events
  - Memory allocation and GC pressure
  - Scaling with number of subscribers

### 1.3 Integration with Dependency Injection

- Add extension methods for registering with service collection:
  ```csharp
  services.AddEventAggregator();
  ```

- Configure as singleton in DI container

## Phase 2: StyleSystem Event Types

### 2.1 Define Event Classes

Create a hierarchy of event classes for style system events:

- Base class: `StyleSystemEvent`
  - Properties: Timestamp, Source

- Style Computation Events:
  - `ElementStyleComputed` - When an element's style is computed
  - `SubtreeStylesUpdated` - When a subtree's styles are updated
  - `StyleCacheHit` - When a style is retrieved from cache
  - `StyleCacheMiss` - When a style is not found in cache

- Style Invalidation Events:
  - `ElementInvalidated` - When an element's style is invalidated
  - `PropertyInvalidated` - When specific properties are invalidated
  - `SubtreeInvalidated` - When a subtree is invalidated
  - `DeviceDependentStylesInvalidated` - When device-dependent styles are invalidated

- Lifecycle Events:
  - `DocumentAttached` - When a document is attached
  - `DocumentDetached` - When a document is detached
  - `DocumentReadyStateChanged` - When a document's ready state changes

### 2.2 Create Unit Tests for Events

- Test each event type:
  - Creation and property validation
  - Publishing and subscription
  - Event inheritance hierarchy

## Phase 3: Direct Integration with StyleSystem Components

### 3.1 Integration with StyleEngine

- Add `IEventAggregator` as dependency to StyleEngine
- Modify StyleEngine to publish events at key points:
  - After computing an element's style
  - After updating a subtree's styles
  - On cache hits and misses

```csharp
public class StyleEngine : IStyleEngine
{
    private readonly IEventAggregator _eventAggregator;
    
    public StyleEngine(/* existing dependencies */, IEventAggregator eventAggregator)
    {
        // Initialize
        _eventAggregator = eventAggregator;
    }
    
    public IComputedStyle ComputeElementStyle(IElement element, string? pseudoElement = null)
    {
        // Existing implementation
        var style = /* compute style */;
        
        // Publish event
        _eventAggregator.Publish(new ElementStyleComputed(this, element, style));
        
        return style;
    }
    
    // Other methods with similar integration
}
```

### 3.2 Integration with StyleInvalidationTracker

- Add `IEventAggregator` as dependency to StyleInvalidationTracker
- Modify methods to publish events:
  - When an element is invalidated
  - When properties are invalidated
  - When a subtree is invalidated
  - When device-dependent styles are invalidated

### 3.3 Integration with DocumentLifecycleCoordinator

- Add `IEventAggregator` as dependency to DocumentLifecycleCoordinator
- Publish events for document lifecycle events:
  - Document attached/detached
  - DOM updates
  - Ready state changes

### 3.4 Integration Tests

- Create integration tests to verify events are published correctly
- Test event flow through the system

## Phase 4: Performance Monitoring and Diagnostics

### 4.1 Style Performance Monitor

Create a StylePerformanceMonitor that subscribes to events and tracks performance metrics:

```csharp
public class StylePerformanceMonitor : IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private readonly Dictionary<string, Stopwatch> _timers = new();
    private readonly Dictionary<string, List<TimeSpan>> _measurements = new();
    
    public StylePerformanceMonitor(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        
        // Subscribe to relevant events
        _subscriptions.Add(_eventAggregator.Subscribe<ElementStyleComputed>(OnElementStyleComputed));
        _subscriptions.Add(_eventAggregator.Subscribe<SubtreeStylesUpdated>(OnSubtreeStylesUpdated));
        // More subscriptions...
    }
    
    // Event handlers, metrics collection, reporting methods...
    
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
    }
}
```

### 4.2 Style Logger

Create a StyleLogger for diagnostic logging:

```csharp
public class StyleLogger : IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly List<ISubscriptionToken> _subscriptions = new();
    
    public StyleLogger(IEventAggregator eventAggregator, LogLevel minimumLevel = LogLevel.Information)
    {
        _eventAggregator = eventAggregator;
        
        // Subscribe to all style events with appropriate filtering
        _subscriptions.Add(_eventAggregator.Subscribe<StyleSystemEvent>(evt => LogEvent(evt, minimumLevel)));
    }
    
    // Logging implementation...
    
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
    }
}
```

## Phase 5: Dependency Injection Extensions

### 5.1 Integration with Microsoft.Extensions.DependencyInjection

```csharp
public static class EventAggregatorServiceCollectionExtensions
{
    public static IServiceCollection AddEventAggregator(this IServiceCollection services)
    {
        services.AddSingleton<IEventAggregator, EventAggregator>();
        return services;
    }
    
    public static IServiceCollection AddStylePerformanceMonitor(this IServiceCollection services)
    {
        services.AddSingleton<StylePerformanceMonitor>();
        return services;
    }
    
    public static IServiceCollection AddStyleLogger(this IServiceCollection services, LogLevel minimumLevel = LogLevel.Information)
    {
        services.AddSingleton<StyleLogger>(sp => 
            new StyleLogger(sp.GetRequiredService<IEventAggregator>(), minimumLevel));
        return services;
    }
}
```

### 5.2 Browsing Context Extensions

```csharp
public static class EventAggregatorBrowsingContextExtensions
{
    public static IBrowsingContext UseEventAggregator(this IBrowsingContext context)
    {
        // Ensure EventAggregator is registered and available
        var eventAggregator = context.GetService<IEventAggregator>();
        if (eventAggregator == null)
        {
            throw new InvalidOperationException(
                "EventAggregator service is not registered. Call services.AddEventAggregator() during service registration.");
        }
        
        return context;
    }
    
    public static IBrowsingContext UseStylePerformanceMonitor(this IBrowsingContext context)
    {
        // Register and initialize StylePerformanceMonitor
        var monitor = context.GetService<StylePerformanceMonitor>();
        if (monitor == null)
        {
            throw new InvalidOperationException(
                "StylePerformanceMonitor service is not registered. Call services.AddStylePerformanceMonitor() during service registration.");
        }
        
        return context;
    }
}
```

## Phase 6: Documentation and Examples

### 6.1 API Documentation

- Complete XML comments for all public APIs
- Create comprehensive documentation with examples
- Document best practices and performance considerations

### 6.2 Usage Examples

Create example projects demonstrating:

- Basic usage
- Performance monitoring
- Diagnostics and logging
- Custom event handling
- Integration with StyleSystem components

## Testing Strategy

### Unit Testing

- Test each component in isolation
- Use mocks for dependencies
- Cover edge cases and error handling

### Integration Testing

- Test components working together
- Verify event flow through the system
- Test with different configuration options

### Performance Testing

- Benchmark performance against baseline
- Measure memory usage and allocation
- Test with different event frequencies and subscriber counts

### Stress Testing

- Test with high concurrency
- Verify behavior under load
- Test with large DOM structures

## Rollout Strategy

1. **Development Environment**
   - Initial implementation and testing
   - Performance benchmarking and optimization

2. **Internal Testing**
   - Integration with existing CodeBase
   - Gather feedback from team members

3. **Beta Release**
   - Limited external availability
   - Documentation and examples

4. **Full Release**
   - Complete documentation
   - Performance guidelines
   - Migration guides

## Conclusion

This implementation plan provides a roadmap for integrating a generic EventAggregator into the AngleSharp.StyleSystem. By directly integrating with core components, we eliminate the need for adapter classes while still achieving loose coupling and improved diagnostics capabilities.

The phased approach allows for incremental development and testing, with each phase building on the previous ones. The result will be a flexible, high-performance event system that enhances the StyleSystem's capabilities while maintaining compatibility with existing code.