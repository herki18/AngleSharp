# AngleSharp Layout Engine - Complete Architecture Overview

## 1. System Overview

The AngleSharp Layout Engine is a comprehensive system that extends AngleSharp with full styling, layout computation, and rendering capabilities. It is designed as a modular system with clear boundaries between components, allowing for independent development, testing, and maintenance.

The system consists of four primary systems:

1. **StyleSystem**: Computes CSS styles for DOM elements
2. **LifecycleSystem**: Tracks DOM mutations and manages invalidation
3. **LayoutSystem**: Computes element layout and positioning (future)
4. **CacheSystem**: Provides efficient caching with dependency tracking

These systems work together to provide a complete pipeline from DOM mutations to final rendering, with optimizations at each stage to ensure performance.

## 2. Core Architecture Principles

The architecture is guided by the following principles:

1. **Clear System Boundaries**: Each system has a well-defined responsibility and interface
2. **Reactive Updates**: Changes to the DOM trigger appropriate invalidations and recalculations
3. **Minimal Recomputation**: Only affected elements are recalculated
4. **Efficient Caching**: Results are cached with intelligent invalidation
5. **Browser-Like Architecture**: Follows patterns used in modern browser engines
6. **Performance Optimization**: Designed for efficient handling of complex documents

## 3. System Descriptions

### 3.1 StyleSystem

The StyleSystem is responsible for computing CSS styles for DOM elements. It processes style rules, matches them against elements, and computes final property values.

#### Key Components:

- **StyleEngine**: Main orchestrator for style computation
- **StyleSheetManager**: Manages stylesheets from different origins
- **SelectorMatcher**: Matches selectors against elements
- **CascadeResolver**: Resolves property conflicts
- **InheritanceProcessor**: Handles inheritance chains
- **ValueComputer**: Computes final property values

#### Primary Interfaces:

```csharp
// Main entry point for style computation
public interface IStyleEngine
{
    ICssStyleDeclaration ComputeElementStyle(IElement element, 
        ICssStyleDeclaration parentStyle = null, 
        string pseudoElement = null);
}

// Manages stylesheets from different origins
public interface IStyleSheetManager
{
    void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin);
    void UnregisterStylesheet(ICssStyleSheet stylesheet);
    void SetDocument(IDocument document);
    IEnumerable<StylesheetEntry> GetStylesheets();
}
```

### 3.2 LifecycleSystem

The LifecycleSystem is responsible for observing DOM mutations, determining what needs to be invalidated, and coordinating updates.

#### Key Components:

- **LifecycleManager**: Manages document state and transitions
- **MutationObserverAdapter**: Bridges to AngleSharp's MutationObserver
- **InvalidationManager**: Determines what needs invalidation
- **StyleInvalidationTracker**: Tracks elements needing style recalculation
- **LayoutInvalidationTracker**: Tracks elements needing layout recalculation
- **SchedulingService**: Controls when updates happen

#### Primary Interfaces:

```csharp
// Central coordinator for document lifecycle
public interface ILifecycleManager
{
    LifecycleState CurrentState { get; }
    void ScheduleStyleUpdate();
    void ScheduleLayoutUpdate();
    void ProcessPendingUpdates();
    event EventHandler<LifecycleStateChangedEventArgs> StateChanged;
}

// Manages invalidation across different aspects
public interface IInvalidationManager
{
    void ProcessMutation(IMutationRecord mutation);
    void InvalidateElement(IElement element, InvalidationFlags flags);
    void InvalidateStylesheet(ICssStyleSheet stylesheet);
}
```

### 3.3 LayoutSystem (Future)

The LayoutSystem will be responsible for computing element layout and positioning. It will calculate box dimensions, positions, and handle different layout algorithms.

#### Planned Components:

- **LayoutEngine**: Main orchestrator for layout computation
- **BoxModelComputer**: Computes box model dimensions
- **FlexLayoutComputer**: Handles flexbox layout
- **GridLayoutComputer**: Handles grid layout
- **PositioningComputer**: Handles element positioning
- **TextLayoutComputer**: Computes text layout

#### Primary Interfaces (Preliminary):

```csharp
// Main entry point for layout computation
public interface ILayoutEngine
{
    ILayoutBox ComputeLayout(IElement element, 
        ComputedStyle style, 
        LayoutConstraints constraints);
}

// Represents a computed layout box
public interface ILayoutBox
{
    float X { get; }
    float Y { get; }
    float Width { get; }
    float Height { get; }
    IBoxEdges Margin { get; }
    IBoxEdges Border { get; }
    IBoxEdges Padding { get; }
    ILayoutBoxCollection Children { get; }
}
```

### 3.4 CacheSystem

The CacheSystem provides efficient caching of computed styles and layouts with dependency tracking for intelligent invalidation.

#### Key Components:

- **LayoutEngineCacheManager**: Central cache coordinator
- **StyleCache**: Caches computed styles
- **LayoutBoxCache**: Caches computed layouts
- **CacheDependencyTracker**: Tracks dependencies for invalidation
- **EnhancedDependencyTracker**: Enhanced dependency tracking

#### Primary Interfaces:

```csharp
// Generic caching interface
public interface IComputationCache<TKey, TValue> where TKey : notnull
{
    TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);
    bool TryGetValue(TKey key, out TValue value);
    void AddOrUpdate(TKey key, TValue value);
    bool Remove(TKey key);
    void Clear();
    int Count { get; }
}

// Central cache manager
public interface ILayoutEngineCacheManager
{
    IStyleCache StyleCache { get; }
    ILayoutBoxCache<TLayoutData> GetLayoutCache<TLayoutData>();
    void InvalidateAll();
    void InvalidateElement(IElement element);
    void InvalidateDocument(IDocument document);
}
```

## 4. Cross-System Interactions

The systems interact in the following ways:

### 4.1 DOM Mutation → Lifecycle → Style → Layout Pipeline

1. **DOM Mutation Detection**:
    
    - `MutationObserver` detects DOM changes
    - `MutationObserverAdapter` processes mutation records
2. **Invalidation Analysis**:
    
    - `InvalidationManager` analyzes mutations
    - `DependencyTracker` identifies affected elements
    - Specialized trackers mark elements for different invalidation types
3. **Document Lifecycle Management**:
    
    - `LifecycleManager` updates document state
    - `SchedulingService` schedules updates
4. **Style Recalculation**:
    
    - `StyleEngine` recalculates styles for invalidated elements
    - `StyleCache` is updated with new computed styles
5. **Layout Recalculation (Future)**:
    
    - `LayoutEngine` recalculates layout for elements with updated styles
    - `LayoutBoxCache` is updated with new computed layouts

### 4.2 Style → Layout Dependency

- Computed styles serve as input for layout calculations
- Style invalidation triggers layout invalidation for affected elements
- Layout depends on style, but style does not depend on layout

### 4.3 Cache Invalidation Paths

1. **DOM Mutation → Cache Invalidation**:
    
    - Mutations trigger cache invalidation through the invalidation system
    - `InvalidationManager` determines which cache entries to invalidate
    - `CacheDependencyTracker` provides information about dependencies
2. **Style Update → Layout Cache Invalidation**:
    
    - Style changes invalidate related layout cache entries
    - `StyleInvalidationTracker` informs `LayoutInvalidationTracker`
3. **Stylesheet Change → Style Cache Invalidation**:
    
    - Stylesheet changes invalidate affected style cache entries
    - `InvalidationManager.InvalidateStylesheet` triggers appropriate invalidation

## 5. Data Flow

The flow of data through the system follows a clear pattern:

1. **Input**:
    
    - DOM structure and changes
    - CSS stylesheets and rules
    - Layout constraints
2. **Processing**:
    
    - Mutation analysis and invalidation
    - Style computation for invalidated elements
    - Layout computation based on computed styles
3. **Output**:
    
    - Computed styles for all elements
    - Computed layout boxes with positions and dimensions
    - Rendering information (future)

At each stage, results are cached and dependencies are tracked to optimize future updates.

## 6. System States and Transitions

The system maintains state through the `LifecycleManager`, which tracks the current state of the document and manages transitions between states:

### 6.1 Lifecycle States

- **Initial**: Document is in its initial state
- **StyleDirty**: Document needs style recalculation
- **StyleClean**: Styles are up to date
- **LayoutDirty**: Document needs layout recalculation
- **LayoutClean**: Layout is up to date
- **PaintDirty**: Document needs visual update (future)
- **PaintClean**: Visual representation is up to date (future)

### 6.2 Key State Transitions

1. **DOM Mutation → StyleDirty**: Mutations that affect styles
2. **StyleDirty → StyleClean**: Style recalculation completes
3. **StyleClean → LayoutDirty**: Style changes affect layout
4. **LayoutDirty → LayoutClean**: Layout recalculation completes
5. **LayoutClean → PaintDirty**: Layout changes affect visual representation
6. **PaintDirty → PaintClean**: Visual update completes

The system enforces valid state transitions to ensure consistent operation.

## 7. Performance Optimization Strategies

The architecture includes several performance optimization strategies:

### 7.1 Minimal Recalculation

- Only elements affected by changes are recalculated
- Dependency tracking identifies precisely what needs updating
- Containment boundaries limit the scope of changes

### 7.2 Efficient Caching

- Style and layout results are cached
- Intelligent invalidation based on detailed dependency tracking
- Different cache entries for different contexts (e.g., viewport sizes)

### 7.3 Batched Processing

- Related mutations are processed together
- Updates are scheduled in batches
- Non-critical updates can be deferred

### 7.4 Priority-Based Processing

- Visible elements are processed first
- Off-screen elements can be processed with lower priority
- Critical paths receive higher priority

## 8. Error Handling and Resilience

The system includes robust error handling:

### 8.1 Graceful Degradation

- If part of the system fails, it can fall back to simpler approaches
- Default styles and layouts are provided as fallbacks
- System continues to function even with partial failures

### 8.2 Error Recovery

- The system can recover from invalid states
- Timeouts prevent infinite loops or excessive computation
- Error boundaries contain failures to specific components

### 8.3 Logging and Diagnostics

- Comprehensive error logging helps diagnose issues
- Performance metrics identify bottlenecks
- Diagnostic tools help understand system behavior

## 9. Extension Points

The architecture includes several extension points:

### 9.1 Custom Invalidation Strategies

- Additional invalidation strategies can be added
- Custom dependency tracking mechanisms can be implemented
- Specialized trackers for different aspects can be created

### 9.2 Layout Algorithm Extensions

- Custom layout algorithms can be added
- Special rendering modes can be implemented
- Domain-specific optimizations can be integrated

### 9.3 Rendering Integration (Future)

- The system can be extended to integrate with different rendering backends
- Custom visualization can be implemented
- Export capabilities can be added

## 10. Implementation Considerations

When implementing the system, consider the following:

### 10.1 Threading Model

- The system primarily operates on a single thread
- Long-running operations could use background processing
- Thread safety is important for shared state

### 10.2 Memory Management

- Cached results should use appropriate memory management
- Weak references may be appropriate for some caching
- Disposal of unused resources is important

### 10.3 Performance Benchmarking

- Create benchmarks for key operations
- Compare with browser performance where possible
- Use performance data to guide optimization

### 10.4 Progressive Enhancement

- Implement core functionality first
- Add advanced features incrementally
- Ensure system works well with partial implementation

## 11. Integration with AngleSharp

The system integrates with AngleSharp's existing components:

### 11.1 DOM Integration

- Uses AngleSharp's DOM implementation
- Extends functionality without modifying core AngleSharp classes
- Provides extension methods for easy access

### 11.2 CSS Integration

- Uses AngleSharp's CSS parser and model
- Extends styling capabilities
- Integrates with existing style sheet handling

### 11.3 Configuration Integration

- Adds layout engine to AngleSharp configuration
- Provides configuration options for performance tuning
- Allows selective enabling of features

## 12. Next Steps

The recommended path forward for implementation:

1. **Complete StyleSystem**:
    
    - Finalize CSS variable resolution
    - Complete value computation
    - Add comprehensive error handling
2. **Implement LifecycleSystem**:
    
    - Create `LifecycleManager`
    - Develop `MutationObserverAdapter`
    - Implement invalidation system
3. **Enhance CacheSystem**:
    
    - Implement enhanced dependency tracking
    - Improve cache invalidation strategies
    - Add performance optimizations
4. **Prepare for LayoutSystem**:
    
    - Define interfaces and data structures
    - Plan integration with existing systems
    - Research layout algorithms

By following this path, a complete styling and layout engine can be built incrementally, with each stage providing value and building toward the final system.