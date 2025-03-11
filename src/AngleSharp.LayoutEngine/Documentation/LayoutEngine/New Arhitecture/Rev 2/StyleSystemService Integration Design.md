# StyleSystemService Integration Design

## Overview

The `StyleSystemService` is the primary integration point between AngleSharp and the StyleSystem. It follows AngleSharp's service architecture pattern, allowing the StyleSystem to be registered with the `IBrowsingContext` and properly initialized during page loading.

## Purpose and Responsibilities

- Register the StyleSystem with AngleSharp's service container
- Initialize and coordinate all StyleSystem components
- Manage lifecycle of style-related services
- Provide a clean public API for StyleSystem functionality
- Handle integration with AngleSharp's existing CSS components

## Component Structure

```csharp
public class StyleSystemService : IStyleSystemService
{
    private readonly StyleEngine _styleEngine;
    private readonly DomMutationTracker _mutationTracker;
    private readonly StyleInvalidationTracker _invalidationTracker;
    private readonly StyleRecalcScheduler _recalcScheduler;
    private readonly DocumentLifecycleCoordinator _lifecycleCoordinator;
    private readonly StyleSystemOptions _options;
    
    public StyleSystemService(StyleSystemOptions? options = null)
    {
        _options = options ?? new StyleSystemOptions();
        
        // Components will be initialized when attached to a context
    }
    
    public void Initialize(IBrowsingContext context)
    {
        // Create all components in the correct order
        _invalidationTracker = new StyleInvalidationTracker();
        
        var renderDevice = context.GetService<IRenderDevice>() ?? new DefaultRenderDevice();
        _styleEngine = new StyleEngine(context);
        
        _mutationTracker = new DomMutationTracker(context, _invalidationTracker);
        _recalcScheduler = new StyleRecalcScheduler(
            _styleEngine,
            context,
            new MainThreadStyleWork(_styleEngine),
            _options.EnableThreading ? new WorkerThreadStylePool(_styleEngine, _options.ThreadCount) : null
        );
        
        _lifecycleCoordinator = new DocumentLifecycleCoordinator(context, _styleEngine);
        
        // Connect the components
        _invalidationTracker.StyleRecalcScheduler = _recalcScheduler;
        
        // Register for document lifecycle events
        if (context.Active != null)
        {
            _lifecycleCoordinator.AttachToDocument(context.Active);
        }
    }
    
    public IComputedStyle GetComputedStyle(IElement element, string? pseudoElement = null)
    {
        return _styleEngine.ComputeElementStyle(element, pseudoElement);
    }
    
    public void UpdateStyles(IElement root)
    {
        _styleEngine.UpdateStyles(root);
    }
    
    public void NotifyViewportChanged(int width, int height)
    {
        _styleEngine.NotifyViewportChanged(width, height);
    }
    
    public void Dispose()
    {
        _mutationTracker?.Dispose();
        _recalcScheduler?.Dispose();
        _styleEngine?.Dispose();
    }
}
```

## Service Options

```csharp
public class StyleSystemOptions
{
    /// <summary>
    /// Gets or sets whether to enable multi-threaded style computation.
    /// </summary>
    public bool EnableThreading { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the number of worker threads to use. If 0, uses Environment.ProcessorCount - 1.
    /// </summary>
    public int ThreadCount { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets whether to load user agent stylesheets.
    /// </summary>
    public bool LoadUserAgentStylesheets { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to enable style memory optimization.
    /// </summary>
    public bool EnableOptimization { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to collect optimization metrics.
    /// </summary>
    public bool CollectMetrics { get; set; } = false;
}
```

## AngleSharp Integration

### Service Interface

```csharp
/// <summary>
/// Represents a service that provides style computation functionality.
/// </summary>
public interface IStyleSystemService : IDisposable
{
    /// <summary>
    /// Gets the computed style for an element.
    /// </summary>
    /// <param name="element">The element to compute styles for.</param>
    /// <param name="pseudoElement">Optional pseudo-element selector.</param>
    /// <returns>The computed style.</returns>
    IComputedStyle GetComputedStyle(IElement element, string? pseudoElement = null);
    
    /// <summary>
    /// Updates styles for a subtree.
    /// </summary>
    /// <param name="root">The root element of the subtree.</param>
    void UpdateStyles(IElement root);
    
    /// <summary>
    /// Notifies the style system of viewport size changes.
    /// </summary>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    void NotifyViewportChanged(int width, int height);
    
    /// <summary>
    /// Initializes the style system with the given browsing context.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    void Initialize(IBrowsingContext context);
}
```

### Extension Methods

```csharp
public static class StyleSystemExtensions
{
    /// <summary>
    /// Adds the StyleSystem to the given browsing context.
    /// </summary>
    /// <param name="context">The browsing context to extend.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The configured browsing context.</returns>
    public static IBrowsingContext WithStyleSystem(this IBrowsingContext context, StyleSystemOptions? options = null)
    {
        var service = new StyleSystemService(options);
        context.Services.Register<IStyleSystemService>(service);
        service.Initialize(context);
        return context;
    }
    
    /// <summary>
    /// Gets the computed style for an element.
    /// </summary>
    /// <param name="element">The element to get computed styles for.</param>
    /// <param name="pseudoElement">Optional pseudo-element selector.</param>
    /// <returns>The computed style.</returns>
    public static IComputedStyle GetComputedStyle(this IElement element, string? pseudoElement = null)
    {
        var context = element.Owner?.Context;
        var service = context?.GetService<IStyleSystemService>();
        
        if (service == null)
            throw new InvalidOperationException("StyleSystem is not registered with this browsing context.");
            
        return service.GetComputedStyle(element, pseudoElement);
    }
}
```

## Usage Example

```csharp
// Create a browsing context with the StyleSystem
var config = Configuration.Default.WithStyleSystem(new StyleSystemOptions {
    EnableThreading = true,
    EnableOptimization = true
});

var context = BrowsingContext.New(config);

// Load a document
var document = await context.OpenAsync("https://example.com");

// Get computed styles for an element
var element = document.QuerySelector("div.content");
var computedStyle = element.GetComputedStyle();

// Access style properties
var color = computedStyle.GetPropertyValue("color");
var display = computedStyle.Display;
var fontSize = computedStyle.FontSize;
```

## Integration with AngleSharp Document Lifecycle

The StyleSystemService integrates with AngleSharp's document lifecycle events through the DocumentLifecycleCoordinator:

```csharp
public void Initialize(IBrowsingContext context)
{
    // ... other initialization code
    
    // Listen for document changes
    context.Active = async (_, e) => 
    {
        if (e.OldValue != null)
            _lifecycleCoordinator.DetachFromDocument(e.OldValue);
        
        if (e.NewValue != null)
            _lifecycleCoordinator.AttachToDocument(e.NewValue);
    };
    
    // Initial document
    if (context.Active != null)
    {
        _lifecycleCoordinator.AttachToDocument(context.Active);
    }
}
```

## Style Recalculation Process

The StyleSystemService coordinates the entire style recalculation process, following the Blink architecture:

1. DOM changes are detected by DomMutationTracker
2. DomMutationTracker notifies StyleInvalidationTracker of affected elements
3. StyleInvalidationTracker schedules recalculation through StyleRecalcScheduler
4. StyleRecalcScheduler coordinates recalculation timing and threading
5. StyleEngine performs the actual style computation

```
DOM Changes → DomMutationTracker → StyleInvalidationTracker → StyleRecalcScheduler → StyleEngine
```

This follows Blink's architecture while integrating cleanly with AngleSharp's service model.

## Configuration Options

The StyleSystemService can be configured through StyleSystemOptions to adapt to different usage scenarios:

- **Development and Debugging**:
    
    ```csharp
    var options = new StyleSystemOptions {
        EnableThreading = false,
        CollectMetrics = true,
        EnableOptimization = false
    };
    ```
    
- **High Performance**:
    
    ```csharp
    var options = new StyleSystemOptions {
        EnableThreading = true,
        ThreadCount = Environment.ProcessorCount,
        EnableOptimization = true
    };
    ```
    
- **Memory Efficient**:
    
    ```csharp
    var options = new StyleSystemOptions {
        EnableThreading = false,
        EnableOptimization = true
    };
    ```
    

## Implementation Considerations

1. **Thread Safety**: The service must be thread-safe as it may be accessed from different threads.
    
2. **Performance**: Initialization should be lazy where possible to minimize startup time.
    
3. **Error Handling**: Robust error handling for service registration and initialization failures.
    
4. **Resource Management**: Proper disposal of all created resources.
    
5. **Service Dependencies**: Clear documentation of dependencies on other AngleSharp services.
    

This design allows the StyleSystem to integrate seamlessly with AngleSharp while maintaining the Blink-style architecture for style computation.