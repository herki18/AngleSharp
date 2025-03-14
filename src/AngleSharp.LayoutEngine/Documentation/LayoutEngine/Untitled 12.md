# AngleSharp.StyleSystem Dependency Injection Guide

## Overview

AngleSharp.StyleSystem now uses Microsoft.Extensions.DependencyInjection for component management, simplifying integration and customization. This document provides guidance for using and configuring the style system with dependency injection.

## Key Components Registered in DI

|Component|Interface|Description|Lifetime|
|---|---|---|---|
|`StyleEngine`|`IStyleEngine`|Main orchestrator for style computation that coordinates all style-related operations|Singleton|
|`StyleSheetManager`|`IStyleSheetManager`|Manages CSS stylesheets by origin (user agent, user, author)|Singleton|
|`PropertyTreeManager`|`IPropertyTreeManager`|Manages efficient storage and sharing of style properties|Singleton|
|`StyleCache`|`IStyleCache`|Caches computed styles for elements to improve performance|Singleton|
|`StyleInvalidationTracker`|`IStyleInvalidationTracker`|Tracks elements that need style recalculation|Singleton|
|`RuleCollector`|`IRuleCollector`|Collects and matches CSS rules to elements|Singleton|
|`CascadeResolver`|`ICascadeResolver`|Resolves conflicts between CSS rules based on specificity|Singleton|
|`InheritanceProcessor`|`IInheritanceProcessor`|Handles property inheritance between elements|Singleton|
|`VariableResolver`|`IVariableResolver`|Resolves CSS custom properties (variables)|Singleton|
|`ValueCalculator`|`IValueCalculator`|Computes CSS values (units, calc expressions, etc.)|Singleton|
|`StylePropertyMapper`|`IStylePropertyMapper`|Maps logical properties to physical properties based on writing mode|Singleton|
|`ComputedStyleBuilder`|`IComputedStyleBuilder`|Builds computed style objects from CSS declarations|Singleton|
|`ComputedStyleFactory`|`IComputedStyleFactory`|Creates and manages computed style objects|Singleton|
|`StyleTreeResolver`|`IStyleTreeResolver`|Resolves styles for element subtrees|Singleton|
|`DocumentLifecycleCoordinator`|-|Coordinates style updates with document lifecycle|Singleton|
|`DomMutationTracker`|-|Tracks DOM changes to trigger style invalidation|Singleton|
|`StyleTaskScheduler`|`IStyleTaskScheduler`|Schedules style-related tasks|Singleton|
|`MainThreadStyleWork`|`IMainThreadStyleWork`|Processes high-priority style work on the main thread|Singleton|
|`StyleRecalcScheduler`|`IStyleRecalcScheduler`|Schedules style recalculation with prioritization|Singleton|
|`WorkerThreadStylePool`|`IWorkerThreadStylePool`|Processes style work in background threads|Singleton|
|`StyleSystemService`|-|Main entry point that ties all components together|Singleton|

## Configuration Options

The `StyleSystemOptions` class provides customization options:

```csharp
public class StyleSystemOptions
{
    // Enable property tree optimization
    public bool EnableOptimization { get; set; } = true;
    
    // Collect metrics about optimization effectiveness
    public bool CollectMetrics { get; set; } = false;
    
    // Apply style updates immediately upon changes
    public bool UpdateStylesImmediately { get; set; } = true;
    
    // Number of worker threads for parallel style computation (0 = disabled)
    public int MaxWorkerThreads { get; set; } = 0;
    
    // Include default browser stylesheets 
    public bool LoadUserAgentStylesheets { get; set; } = true;
    
    // Minimum interval between style updates (milliseconds)
    public int ThrottleIntervalMs { get; set; } = 16;
    
    // Maximum number of elements to process in one batch
    public int BatchSize { get; set; } = 100;
}
```

## Registration with AngleSharp

### Method 1: Using the Configuration Extensions

```csharp
// Basic registration with default options
var context = BrowsingContext.New(Configuration.Default
    .WithStyleSystem());
    
// With custom options
var context = BrowsingContext.New(Configuration.Default
    .WithStyleSystem(options => {
        options.MaxWorkerThreads = 2;
        options.EnableOptimization = true;
        options.ThrottleIntervalMs = 32;
    }));
```

### Method 2: Using Dependency Injection Services

```csharp
// Setup services
var services = new ServiceCollection();

// Add AngleSharp services
services.AddSingleton<IBrowsingContext>(provider => 
    BrowsingContext.New(Configuration.Default));

// Add StyleSystem with custom options
services.AddStyleSystem(options => {
    options.MaxWorkerThreads = 2;
});

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get the browsing context
var context = serviceProvider.GetRequiredService<IBrowsingContext>();
```

## Migration from Manual Instantiation

### Before DI

```csharp
// Manual creation of StyleEngine and dependencies
var context = BrowsingContext.New(Configuration.Default);
var renderDevice = new DefaultRenderDevice();
var propertyTreeManager = new PropertyTreeManager();
var styleCache = new StyleCache();
var stylesheetManager = new StyleSheetManager(context);
var invalidationTracker = new StyleInvalidationTracker();
var ruleCollector = new RuleCollector(context, stylesheetManager);
var cascadeResolver = new CascadeResolver(context);
var inheritanceProcessor = new InheritanceProcessor(context);
var variableResolver = new VariableResolver(context);
var valueCalculator = new ValueCalculator(context, renderDevice);
var stylePropertyMapper = new StylePropertyMapper();

var styleEngine = new StyleEngine(
    context,
    renderDevice,
    propertyTreeManager,
    styleCache,
    stylesheetManager,
    invalidationTracker,
    ruleCollector,
    cascadeResolver,
    inheritanceProcessor,
    variableResolver,
    valueCalculator,
    /* other dependencies */
);

// Using the StyleEngine
var computedStyle = styleEngine.ComputeElementStyle(element);
```

### After DI

```csharp
// Configuration with DI
var context = BrowsingContext.New(Configuration.Default
    .WithStyleSystem());

// Using the StyleEngine
var styleEngine = context.GetStyleSystemService<IStyleEngine>();
var computedStyle = styleEngine.ComputeElementStyle(element);

// Or even simpler, using extension methods
var computedStyle = element.GetComputedStyle();
```

## Using StyleSystem Services

### Access via Context

```csharp
// Get specific services
var styleEngine = context.GetStyleSystemService<IStyleEngine>();
var styleCache = context.GetStyleSystemService<IStyleCache>();
var propertyTreeManager = context.GetStyleSystemService<IPropertyTreeManager>();

// Get the main service that wraps everything
var styleSystemService = context.GetStyleSystem();
```

### Component-Specific Extensions

```csharp
// Compute style for an element
var style = element.GetComputedStyle();

// With pseudo-element
var beforeStyle = element.GetComputedStyle("::before");

// Force style recalculation
context.RecalculateStyles();

// Get optimization metrics
var metrics = context.GetStyleOptimizationMetrics();
```

## Advanced Customization

### Custom Render Device

```csharp
// Create a custom render device implementation
public class MyRenderDevice : IRenderDevice
{
    // Implementation...
}

// Register with the service collection
services.AddSingleton<IRenderDevice, MyRenderDevice>();

// Or replace the default in options
services.AddStyleSystem(options => {
    // Your custom options
}).AddSingleton<IRenderDevice, MyRenderDevice>();
```

### Custom Style Application Strategy

```csharp
// Create a custom strategy
public class MyStyleApplicationStrategy : IStyleApplicationStrategy
{
    // Implementation...
}

// Register with the service collection
services.AddSingleton<IStyleApplicationStrategy, MyStyleApplicationStrategy>();
```

## Testing with DI

```csharp
// Setup for testing
public class StyleSystemTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IBrowsingContext _context;
    
    public StyleSystemTests()
    {
        var services = new ServiceCollection();
        
        // Add mocks or test implementations
        services.AddSingleton<IBrowsingContext>(MockBrowsingContext());
        services.AddSingleton<IRenderDevice>(MockRenderDevice());
        
        // Add the style system with test options
        services.AddStyleSystem(options => {
            options.MaxWorkerThreads = 0;  // Disable threading in tests
            options.LoadUserAgentStylesheets = false;  // Simplify test environment
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<IBrowsingContext>();
    }
    
    [Fact]
    public void StyleEngine_ComputesStyles_Correctly()
    {
        // Get the style engine from DI
        var styleEngine = _serviceProvider.GetRequiredService<IStyleEngine>();
        
        // Test implementation...
    }
}
```

## Best Practices

1. **Use the Configuration Extension**: The `WithStyleSystem()` extension method provides the simplest integration.
    
2. **Access via Context**: Use `context.GetStyleSystemService<T>()` to access specific services.
    
3. **Use Element Extensions**: Prefer `element.GetComputedStyle()` over direct service access for common operations.
    
4. **Configuration Options**: Customize behavior through `StyleSystemOptions` rather than creating custom implementations.
    
5. **Lifecycle Management**: Services are registered as singletons - they will be disposed when the context is disposed.
    
6. **Performance Tuning**:
    
    - Adjust `BatchSize` and `ThrottleIntervalMs` based on document complexity
    - Enable `MaxWorkerThreads` for large documents
    - Use `EnableOptimization` for memory-constrained environments
7. **Monitoring**: Use `CollectMetrics` and `GetStyleOptimizationMetrics()` to monitor performance.