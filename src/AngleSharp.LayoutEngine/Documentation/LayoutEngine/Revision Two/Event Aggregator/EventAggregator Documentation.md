# EventAggregator

## Overview

The EventAggregator is a lightweight, high-performance event bus implementation that enables loosely coupled communication between components. It allows components to publish events and subscribe to them without direct references to each other, promoting a cleaner architecture with better separation of concerns.

## Features

- **Generic event types** - Support for any event type with strong typing
- **Thread-safe operation** - Concurrent publishing and subscription management
- **Subscription filtering** - Conditional event handling based on predicates
- **Memory leak prevention** - Options for weak references to subscribers
- **Synchronous and asynchronous dispatch** - Support for different execution models
- **Performance optimized** - Designed for high-frequency event scenarios
- **Simple API** - Easy to understand and use

## Core Interfaces

```csharp
/// <summary>
/// Represents the fundamental event bus for publishing messages and managing subscriptions
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers of the specified event type
    /// </summary>
    void Publish<TEvent>(TEvent eventData) where TEvent : class;
    
    /// <summary>
    /// Subscribes to events of the specified type
    /// </summary>
    /// <returns>A subscription token that can be used to unsubscribe</returns>
    ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    
    /// <summary>
    /// Subscribes to events of the specified type with a filter predicate
    /// </summary>
    /// <returns>A subscription token that can be used to unsubscribe</returns>
    ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent> filter, Action<TEvent> handler) where TEvent : class;
    
    /// <summary>
    /// Unsubscribes using a previously obtained subscription token
    /// </summary>
    void Unsubscribe(ISubscriptionToken token);
    
    /// <summary>
    /// Removes all subscriptions for the specified event type
    /// </summary>
    void ClearSubscriptions<TEvent>() where TEvent : class;
    
    /// <summary>
    /// Removes all subscriptions across all event types
    /// </summary>
    void ClearAllSubscriptions();
}

/// <summary>
/// Represents a subscription to an event
/// </summary>
public interface ISubscriptionToken : IDisposable
{
    /// <summary>
    /// Gets the type of event this subscription is for
    /// </summary>
    Type EventType { get; }
    
    /// <summary>
    /// Gets whether this subscription token has been disposed
    /// </summary>
    bool IsDisposed { get; }
}
```

## Usage Examples

### Basic Publishing and Subscribing

```csharp
// Create the event aggregator
var eventAggregator = new EventAggregator();

// Define an event class
public class UserLoggedInEvent
{
    public string Username { get; set; }
    public DateTime LoginTime { get; set; }
}

// Subscribe to the event
var token = eventAggregator.Subscribe<UserLoggedInEvent>(e => {
    Console.WriteLine($"User {e.Username} logged in at {e.LoginTime}");
});

// Publish an event
eventAggregator.Publish(new UserLoggedInEvent { 
    Username = "johndoe", 
    LoginTime = DateTime.UtcNow 
});

// Unsubscribe when no longer needed
token.Dispose();
```

### Filtered Subscriptions

```csharp
// Subscribe only to events for a specific user
eventAggregator.Subscribe<UserLoggedInEvent>(
    e => e.Username == "admin",
    e => {
        Console.WriteLine($"Admin logged in at {e.LoginTime}");
    });
```

### Component Integration

```csharp
public class UserService
{
    private readonly IEventAggregator _eventAggregator;
    
    public UserService(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }
    
    public void Login(string username, string password)
    {
        // Authenticate user...
        
        // Publish event
        _eventAggregator.Publish(new UserLoggedInEvent {
            Username = username,
            LoginTime = DateTime.UtcNow
        });
    }
}

public class AuditLogger : IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();
    
    public AuditLogger(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        
        // Subscribe to events
        _subscriptions.Add(
            _eventAggregator.Subscribe<UserLoggedInEvent>(LogUserLogin)
        );
    }
    
    private void LogUserLogin(UserLoggedInEvent evt)
    {
        // Log the event
    }
    
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
    }
}
```

## Best Practices

1. **Keep event classes simple and immutable**
   - Make them plain data carriers
   - Avoid complex logic in event classes

2. **Use fine-grained events**
   - Specific events are easier to manage than general ones
   - Helps to keep subscribers focused

3. **Avoid event loops**
   - Be careful with subscribers that publish events
   - Can cause infinite recursion or stack overflow

4. **Consider thread synchronization**
   - Events may be published from different threads
   - Make event handlers thread-safe

5. **Manage subscriptions properly**
   - Always unsubscribe when no longer needed
   - Consider using weak references for long-lived components

6. **Performance considerations**
   - Events should be lightweight
   - Avoid expensive operations in event handlers
   - Consider asynchronous handlers for lengthy operations