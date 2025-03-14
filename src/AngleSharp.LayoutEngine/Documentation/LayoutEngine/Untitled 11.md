# StyleSystem Dependency Injection Implementation Plan

## Overview

This plan outlines the steps to integrate Microsoft.Extensions.DependencyInjection with the AngleSharp.StyleSystem components. This will replace manual dependency management with a more maintainable and testable approach.

## Goals

- Simplify component creation and lifecycle management
- Improve testing by enabling easier mocking of dependencies
- Provide a standard configuration API for consumers
- Maintain compatibility with existing AngleSharp patterns
- Minimize changes to core component interfaces

## Implementation Phases

### Phase 1: Service Registration

1. **Install Required Package**
    
    - Add `Microsoft.Extensions.DependencyInjection` package to the project
    - Target version should be compatible with the current .NET target framework
2. **Create Extension Methods for Service Collection**
    
    - Implement `StyleSystemServiceCollectionExtensions` class
    - Create `AddStyleSystem()` extension method for `IServiceCollection`
    - Register all components with appropriate lifetimes
3. **Configuration Options**
    
    - Implement `StyleSystemOptions` class for customizing behavior
    - Add overload for `AddStyleSystem(Action<StyleSystemOptions>)` to accept configuration
    - Apply configuration to registered services

### Phase 2: Component Updates

1. **Update Core Components**
    
    - Refactor `StyleEngine` to use constructor injection
    - Update `StyleSheetManager`, `ComputedStyleBuilder`, and other key components
    - Ensure consistent dependency resolution patterns across components
2. **Service Lifetime Management**
    
    - Register singleton services for shared state (e.g., `PropertyTreeManager`)
    - Register scoped services for document-specific components
    - Handle disposal of services correctly
3. **Interface Changes**
    
    - Minimize changes to public interfaces
    - Add appropriate constructor parameters to concrete implementations
    - Update factory methods to use DI container when needed

### Phase 3: Integration with AngleSharp

1. **AngleSharp Configuration Integration**
    
    - Create extension method for `Configuration` class
    - Connect StyleSystem DI container with AngleSharp's service provider
    - Register StyleSystem service with AngleSharp
2. **Context and Service Bridge**
    
    - Ensure `IBrowsingContext` can access StyleSystem services
    - Create `GetRequiredService<T>` helper extensions for StyleSystem components
    - Maintain compatibility with existing `context.GetService<T>()` pattern
3. **Document Lifecycle Binding**
    
    - Update `DocumentLifecycleCoordinator` to use DI
    - Ensure proper scoping for document-specific services
    - Handle service resolution in document changed events

### Phase 4: Migration and Testing

1. **Gradual Migration Strategy**
    
    - Add fallback resolution for services not yet in the container
    - Support both DI and manual instantiation during transition
    - Document new instantiation patterns for consumers
2. **Unit Testing Improvements**
    
    - Create test helpers for service mocking
    - Update existing tests to use DI for component creation
    - Test integration with AngleSharp configuration
3. **Performance Verification**
    
    - Benchmark service resolution overhead
    - Compare memory usage before/after DI implementation
    - Optimize critical resolution paths if needed

## Code Structure

### Service Registration

```csharp
// StyleSystemServiceCollectionExtensions.cs
public static class StyleSystemServiceCollectionExtensions
{
    public static IServiceCollection AddStyleSystem(this IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<IStyleEngine, StyleEngine>();
        services.AddSingleton<IStyleSheetManager, StyleSheetManager>();
        services.AddSingleton<IPropertyTreeManager, PropertyTreeManager>();
        
        // Register computation services
        services.AddSingleton<IRuleCollector, RuleCollector>();
        services.AddSingleton<ICascadeResolver, CascadeResolver>();
        services.AddSingleton<IInheritanceProcessor, InheritanceProcessor>();
        services.AddSingleton<IVariableResolver, VariableResolver>();
        services.AddSingleton<IValueCalculator, ValueCalculator>();
        services.AddSingleton<IStylePropertyMapper, StylePropertyMapper>();
        services.AddSingleton<IComputedStyleBuilder, ComputedStyleBuilder>();
        services.AddSingleton<IComputedStyleFactory, ComputedStyleFactory>();
        
        // Register storage services
        services.AddSingleton<IStyleCache, StyleCache>();
        
        // Register integration services
        services.AddSingleton<DocumentLifecycleCoordinator>();
        services.AddSingleton<DomMutationTracker>();
        services.AddSingleton<IStyleInvalidationTracker, StyleInvalidationTracker>();
        
        // Register threading services
        services.AddSingleton<IMainThreadStyleWork, MainThreadStyleWork>();
        services.AddSingleton<IWorkerThreadStylePool, WorkerThreadStylePool>();
        services.AddSingleton<IStyleRecalcScheduler, StyleRecalcScheduler>();
        
        // Register the service that ties everything together
        services.AddSingleton<StyleSystemService>();
        
        return services;
    }
    
    public static IServiceCollection AddStyleSystem(
        this IServiceCollection services, 
        Action<StyleSystemOptions> configure)
    {
        var options = new StyleSystemOptions();
        configure(options);
        
        services.AddSingleton(options);
        
        // Add services with options
        services.AddStyleSystem();
        
        // Apply options to registered services
        if (options.MaxWorkerThreads > 0)
        {
            services.Configure<WorkerThreadStylePool>(pool => 
            {
                // Apply configuration
            });
        }
        
        return services;
    }
}
```

### Component Updates

```csharp
// StyleEngine with constructor injection
public class StyleEngine : IStyleEngine, IDisposable
{
    public StyleEngine(
        IBrowsingContext context,
        IPropertyTreeManager propertyTreeManager,
        IStyleCache styleCache,
        IStyleSheetManager stylesheetManager,
        IStyleInvalidationTracker invalidationTracker,
        IRuleCollector ruleCollector,
        ICascadeResolver cascadeResolver,
        IInheritanceProcessor inheritanceProcessor,
        IVariableResolver variableResolver,
        IStylePropertyMapper stylePropertyMapper,
        IValueCalculator valueCalculator,
        IComputedStyleFactory styleFactory)
    {
        Context = context;
        PropertyTreeManager = propertyTreeManager;
        _styleCache = styleCache;
        _stylesheetManager = stylesheetManager;
        InvalidationTracker = invalidationTracker;
        RuleCollector = ruleCollector;
        CascadeResolver = cascadeResolver;
        InheritanceProcessor = inheritanceProcessor;
        VariableResolver = variableResolver;
        StyleFactory = styleFactory;
        ComputedStyleBuilder = new ComputedStyleBuilder(
            context,
            this,
            variableResolver,
            valueCalculator,
            stylePropertyMapper,
            propertyTreeManager,
            _renderDevice);
            
        // Rest of initialization code
    }
    
    // Rest of class implementation
}
```

### AngleSharp Integration

```csharp
// StyleSystemExtensions.cs
public static class StyleSystemExtensions
{
    public static IConfiguration WithStyleSystem(this IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddStyleSystem();
        
        var provider = services.BuildServiceProvider();
        
        return configuration.WithServices(provider);
    }
    
    public static IConfiguration WithStyleSystem(
        this IConfiguration configuration, 
        Action<StyleSystemOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddStyleSystem(configure);
        
        var provider = services.BuildServiceProvider();
        
        return configuration.WithServices(provider);
    }
    
    // Other extension methods
}
```

## Compatibility Considerations

- Maintain existing constructor interfaces alongside DI for backward compatibility
- Keep the service registration in a separate namespace to avoid dependency on DI libraries for core functionality
- Support resolving dependencies from the container or through explicit parameters
- Document the preferred approach for consumers going forward

## Testing Strategy

- Create a base test fixture that sets up the DI container with test services
- Use DI to provide test doubles for dependencies in unit tests
- Test both the DI container setup and the individual components with DI
