# AngleSharp Layout Engine - Updated Architecture Overview

## 1. System Overview

The AngleSharp Layout Engine is a comprehensive system that extends AngleSharp with full styling, layout computation, and rendering capabilities. It is designed as a modular system with clear boundaries between components, allowing for independent development, testing, and maintenance.

The system consists of four primary modules:

1. **Style Computation Module**: Computes CSS styles for DOM elements
2. **Document Lifecycle Module**: Tracks DOM mutations and manages invalidation
3. **Layout Engine Module**: Computes element layout and positioning using a modern constraint-based approach
4. **Caching Module**: Provides efficient caching with dependency tracking

These modules work together to provide a complete pipeline from DOM mutations to final rendering, with optimizations at each stage to ensure performance.

## 2. Core Architecture Principles

The architecture is guided by the following principles:

1. **Clear Module Boundaries**: Each module has a well-defined responsibility and interface
2. **Reactive Updates**: Changes to the DOM trigger appropriate invalidations and recalculations
3. **Minimal Recomputation**: Only affected elements are recalculated
4. **Efficient Caching**: Results are cached with intelligent invalidation
5. **Browser-Like Architecture**: Follows patterns used in modern browser engines
6. **Performance Optimization**: Designed for efficient handling of complex documents
7. **Immutable Output**: Layout produces immutable fragments rather than modifying mutable boxes
8. **Constraint-Based Layout**: Layout is driven by explicitly defined constraints
9. **Phase Separation**: Clear separation between intrinsic size calculation, layout, and positioning

## 3. Module Descriptions

### 3.1 Style Computation Module

The Style Computation Module is responsible for computing CSS styles for DOM elements. It processes style rules, matches them against elements, and computes final property values.

#### Key Components:

- **StyleComputationEngine**: Main orchestrator for style computation
- **StyleSheetManager**: Manages stylesheets from different origins
- **SelectorMatcher**: Matches selectors against elements
- **CascadeResolver**: Resolves property conflicts
- **InheritanceProcessor**: Handles inheritance chains
- **ValueComputer**: Computes final property values
- **VariableResolver**: Resolves CSS custom property (variable) references
- **VariableRegistry**: Tracks and manages CSS variables

#### Primary Interfaces:

```csharp
// Main entry point for style computation
public interface IStyleComputationEngine
{
    ICssStyleDeclaration ComputeElementStyle(IElement element, 
        ICssStyleDeclaration parentStyle = null, 
        string pseudoElement = null);
        
    // New method to support layout-specific property access
    T GetComputedValue<T>(IElement element, string propertyName);
}

// Manages stylesheets from different origins
public interface IStyleSheetManager
{
    void RegisterStylesheet(ICssStyleSheet stylesheet, StylesheetOrigin origin);
    void UnregisterStylesheet(ICssStyleSheet stylesheet);
    void SetDocument(IDocument document);
    IEnumerable<StylesheetEntry> GetStylesheets();
}

// New interface for logical property resolution
public interface IStylePropertyResolver
{
    LogicalLength GetInlineSize(ICssStyleDeclaration style, WritingMode writingMode);
    LogicalLength GetBlockSize(ICssStyleDeclaration style, WritingMode writingMode);
    Edges GetLogicalMargins(ICssStyleDeclaration style, WritingMode writingMode);
    Edges GetLogicalPadding(ICssStyleDeclaration style, WritingMode writingMode);
    Edges GetLogicalBorders(ICssStyleDeclaration style, WritingMode writingMode);
}
```

### 3.2 Document Lifecycle Module

The Document Lifecycle Module is responsible for observing DOM mutations, determining what needs to be invalidated, and coordinating updates.

#### Key Components:

- **DocumentLifecycleManager**: Manages document state and transitions
- **MutationObserverAdapter**: Bridges to AngleSharp's MutationObserver
- **InvalidationManager**: Determines what needs invalidation
- **StyleInvalidationTracker**: Tracks elements needing style recalculation
- **LayoutInvalidationTracker**: Tracks elements needing layout recalculation
- **IntrinsicSizesInvalidationTracker**: New component for tracking intrinsic size invalidation
- **SchedulingService**: Controls when updates happen

#### Primary Interfaces:

```csharp
// Central coordinator for document lifecycle
public interface IDocumentLifecycleManager
{
    LifecycleState CurrentState { get; }
    void ScheduleStyleUpdate();
    void ScheduleIntrinsicSizesUpdate(); // New method for intrinsic size phase
    void ScheduleLayoutUpdate();
    void ProcessPendingUpdates();
    event EventHandler<LifecycleStateChangedEventArgs> StateChanged;
}

// Manages invalidation across different aspects
public interface IInvalidationManager
{
    void ProcessMutation(IMutationRecord mutation);
    void InvalidateElement(IElement element, InvalidationFlags flags);
    void InvalidateElementIntrinsicSizes(IElement element); // New method for size invalidation
    void InvalidateElementFragments(IElement element); // New method for fragment invalidation
    void InvalidateStylesheet(ICssStyleSheet stylesheet);
}

// Enhanced lifecycle states with intrinsic sizing phase
public enum LifecycleState
{
    Initial,
    StyleDirty,
    StyleClean,
    IntrinsicSizesDirty, // New state for intrinsic sizing phase
    IntrinsicSizesClean, // New state for completed intrinsic sizing
    LayoutDirty,
    LayoutClean,
    PaintDirty,
    PaintClean
}
```

### 3.3 Layout Engine Module (LayoutNG-Inspired)

The Layout Engine Module is responsible for computing element layout and positioning using a modern constraint-based approach inspired by browser engines like Blink's LayoutNG.

#### Key Components:

- **LayoutEngine**: Main orchestrator for layout computation
- **FormattingContextFactory**: Creates appropriate formatting contexts
- **LayoutFragmentTree**: Manages the tree of layout fragments
- **BoxGeometryResolver**: Computes box model dimensions
- **ConstraintSpace**: Encapsulates layout constraints and available space
- **MarginCollapsingEngine**: Handles margin collapsing per CSS spec
- **IntrinsicSizesCalculator**: Calculates min/max content sizes
- **LineBreaker**: Handles text wrapping and line creation

#### Formatting Contexts:

- **BlockFormattingContext**: Handles block layout
- **InlineFormattingContext**: Handles inline and text layout
- **FlexFormattingContext**: Handles flexbox layout
- **GridFormattingContext**: Handles grid layout

#### Data Structures:

- **LayoutFragment**: Immutable result of layout computation
- **FragmentBuilder**: Helper for constructing fragments
- **LayoutResult**: Container for complete layout results
- **MinMaxSizes**: Contains intrinsic size information

#### Primary Interfaces:

```csharp
// Main entry point for layout computation
public interface ILayoutEngine
{
    // Main layout method
    LayoutResult Layout(IElement element, ConstraintSpace constraintSpace = null);
    
    // Intrinsic size calculation (separate from full layout)
    MinMaxSizes ComputeMinMaxSizes(IElement element);
    
    // Create constraint space
    ConstraintSpace CreateConstraintSpace(IElement element, ICssStyleDeclaration style);
    
    // Specialized layout methods
    LayoutResult LayoutBlock(IElement element, BlockConstraintSpace constraintSpace);
    LayoutResult LayoutInline(IElement element, InlineConstraintSpace constraintSpace);
    LayoutResult LayoutFlex(IElement element, FlexConstraintSpace constraintSpace);
    LayoutResult LayoutGrid(IElement element, GridConstraintSpace constraintSpace);
}

// Constraint space for layout
public interface IConstraintSpace
{
    // Available space for layout
    Size AvailableSize { get; }
    
    // Size to resolve percentages against
    Size PercentageResolutionSize { get; }
    
    // Writing mode properties
    WritingMode WritingMode { get; }
    TextDirection Direction { get; }
    
    // Formatting context flags
    bool IsNewFormattingContext { get; }
    bool IsFloatContextEnabled { get; }
    
    // Margin collapsing state
    MarginStrut MarginStrut { get; }
    
    // For positioned elements
    Point BfcOffset { get; }
    
    // Create child constraint space
    IConstraintSpace CreateChildConstraintSpace(IElement child, ICssStyleDeclaration style);
}

// Formatting context interface
public interface IFormattingContext
{
    // Perform layout in this formatting context
    LayoutFragment Layout(IElement element, IConstraintSpace constraintSpace);
    
    // Calculate intrinsic sizes
    MinMaxSizes ComputeIntrinsicSizes(IElement element);
}

// Immutable layout result
public class LayoutFragment
{
    // Associated element
    public IElement Element { get; }
    
    // Box type
    public BoxType BoxType { get; }
    
    // Fragment geometry
    public LogicalSize LogicalSize { get; }
    public LogicalOffset LogicalOffset { get; }
    public PhysicalSize PhysicalSize { get; }
    public PhysicalOffset PhysicalOffset { get; }
    
    // Box model properties
    public Edges Margins { get; }
    public Edges Borders { get; }
    public Edges Paddings { get; }
    
    // Child fragments
    public IReadOnlyList<LayoutFragment> Children { get; }
    
    // For positioned elements
    public bool IsPositioned { get; }
    public PositionType PositionType { get; }
    
    // Create a new fragment with updated geometry
    public LayoutFragment CopyWithNewGeometry(LogicalSize newSize, LogicalOffset newOffset);
}
```

### 3.4 Caching Module

The Caching Module provides efficient caching of computed styles and layouts with dependency tracking for intelligent invalidation.

#### Key Components:

- **LayoutEngineCacheManager**: Central cache coordinator
- **StyleCache**: Caches computed styles
- **FragmentCache**: Caches layout fragments (NEW)
- **IntrinsicSizeCache**: Caches intrinsic sizes (NEW)
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

// Central cache manager with new fragment caching
public interface ILayoutEngineCacheManager
{
    IStyleCache StyleCache { get; }
    IFragmentCache FragmentCache { get; } // NEW: Fragment cache
    IIntrinsicSizeCache IntrinsicSizeCache { get; } // NEW: Intrinsic size cache
    ILayoutBoxCache<TLayoutData> GetLayoutCache<TLayoutData>();
    void InvalidateAll();
    void InvalidateElement(IElement element);
    void InvalidateDocument(IDocument document);
    void InvalidateFragments(IElement element); // NEW: Fragment invalidation
    void InvalidateIntrinsicSizes(IElement element); // NEW: Size invalidation
}

// NEW: Fragment cache key
public class FragmentCacheKey : IEquatable<FragmentCacheKey>
{
    public IElement Element { get; }
    public IConstraintSpace ConstraintSpace { get; }
    
    // Equality and hashing implementation
}

// NEW: Intrinsic size cache key
public class IntrinsicSizeCacheKey : IEquatable<IntrinsicSizeCacheKey>
{
    public IElement Element { get; }
    
    // Equality and hashing implementation
}
```

## 4. Cross-Module Interactions

The modules interact in the following ways:

### 4.1 DOM Mutation → Lifecycle → Style → Layout Pipeline

1. **DOM Mutation Detection**:
    - `MutationObserver` detects DOM changes
    - `MutationObserverAdapter` processes mutation records
2. **Invalidation Analysis**:
    - `InvalidationManager` analyzes mutations
    - `DependencyTracker` identifies affected elements
    - Specialized trackers mark elements for different invalidation types
3. **Document Lifecycle Management**:
    - `DocumentLifecycleManager` updates document state
    - `SchedulingService` schedules updates
4. **Style Recalculation**:
    - `StyleComputationEngine` recalculates styles for invalidated elements
    - `StyleCache` is updated with new computed styles
5. **Intrinsic Size Calculation** (NEW):
    - `IntrinsicSizesCalculator` calculates min/max sizes
    - Results are stored in `IntrinsicSizeCache`
6. **Layout Calculation**:
    - `LayoutEngine` creates constraint spaces
    - Appropriate `FormattingContext` runs layout algorithm
    - Produces `LayoutFragment` tree
    - Results stored in `FragmentCache`

### 4.2 Constraint Propagation and Fragment Assembly

The layout process now follows a two-phase approach:

1. **Top-down Constraint Propagation**:
    - Parent elements create constraint spaces for children
    - Constraints flow down the DOM tree
    - Each element receives an appropriate `ConstraintSpace`
2. **Bottom-up Fragment Assembly**:
    - Leaf nodes (elements without children) are laid out first
    - Child fragments are assembled into parent fragments
    - Final fragments represent the complete layout

### 4.3 Style → Layout Integration

The integration between Style Computation and Layout Engine is enhanced:

1. **Computed Style Access**:
    - Layout engine accesses computed style properties through efficient APIs
    - `StylePropertyResolver` provides logical property resolution based on writing mode
2. **CSS Variable Resolution**:
    - CSS variables resolved during style computation
    - Layout accesses fully resolved values
    - Changes to variables trigger appropriate invalidation
3. **Logical Properties**:
    - Writing-mode aware style resolution
    - Conversion between logical and physical coordinates
    - Support for all logical box model properties

### 4.4 Cache Invalidation Paths

1. **DOM Mutation → Cache Invalidation**:
    - Mutations trigger cache invalidation through the invalidation system
    - `InvalidationManager` determines which cache entries to invalidate
    - `CacheDependencyTracker` provides information about dependencies
2. **Style Update → Size and Layout Cache Invalidation**:
    - Style changes invalidate related size and layout cache entries
    - `StyleInvalidationTracker` informs `IntrinsicSizesInvalidationTracker` and `LayoutInvalidationTracker`
3. **Intrinsic Size Update → Layout Cache Invalidation**:
    - Size changes invalidate related layout cache entries
    - `IntrinsicSizesInvalidationTracker` informs `LayoutInvalidationTracker`
4. **Stylesheet Change → Style Cache Invalidation**:
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
    - Intrinsic size calculation
    - Constraint space creation
    - Formatting context layout algorithms
    - Fragment construction and assembly
3. **Output**:
    - Computed styles for all elements
    - Intrinsic sizes (min/max content sizes)
    - Layout fragments with positions and dimensions
    - Rendering information (future)

At each stage, results are cached and dependencies are tracked to optimize future updates.

## 6. System States and Transitions

The system maintains state through the `DocumentLifecycleManager`, which tracks the current state of the document and manages transitions between states:

### 6.1 Enhanced Lifecycle States

- **Initial**: Document is in its initial state
- **StyleDirty**: Document needs style recalculation
- **StyleClean**: Styles are up to date
- **IntrinsicSizesDirty**: Document needs intrinsic size recalculation
- **IntrinsicSizesClean**: Intrinsic sizes are up to date
- **LayoutDirty**: Document needs layout recalculation
- **LayoutClean**: Layout is up to date
- **PaintDirty**: Document needs visual update (future)
- **PaintClean**: Visual representation is up to date (future)

### 6.2 Key State Transitions

1. **DOM Mutation → StyleDirty**: Mutations that affect styles
2. **StyleDirty → StyleClean**: Style recalculation completes
3. **StyleClean → IntrinsicSizesDirty**: Style changes affect intrinsic sizes
4. **IntrinsicSizesDirty → IntrinsicSizesClean**: Size recalculation completes
5. **IntrinsicSizesClean → LayoutDirty**: Size changes affect layout
6. **LayoutDirty → LayoutClean**: Layout recalculation completes
7. **LayoutClean → PaintDirty**: Layout changes affect visual representation
8. **PaintDirty → PaintClean**: Visual update completes

The system enforces valid state transitions to ensure consistent operation.

## 7. Performance Optimization Strategies

The architecture includes several performance optimization strategies:

### 7.1 Minimal Recalculation

- Only elements affected by changes are recalculated
- Dependency tracking identifies precisely what needs updating
- Containment boundaries limit the scope of changes
- Separate intrinsic size and layout phases avoid unnecessary calculations

### 7.2 Efficient Caching

- Style, intrinsic size, and layout results are cached
- Constraint-based cache keys for precise invalidation
- Intelligent invalidation based on detailed dependency tracking
- Different cache entries for different contexts (e.g., viewport sizes)

### 7.3 Batched Processing

- Related mutations are processed together
- Updates are scheduled in batches
- Non-critical updates can be deferred

### 7.4 Formatting Context Optimizations

- Specialized algorithms for different layout types
- Fast paths for common layout scenarios
- Skip unnecessary phases when possible
- Reuse fragments when appropriate

### 7.5 Fragment Immutability Benefits

- Thread safety for concurrent operations
- Simpler memory management
- Easier debugging and testing
- Efficient change detection

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

### 9.1 Custom Formatting Contexts

- Additional formatting contexts can be implemented
- Specialized layout algorithms can be added
- Custom box types can be created

### 9.2 Layout Algorithm Extensions

- Custom layout algorithms can be added
- Special rendering modes can be implemented
- Domain-specific optimizations can be integrated

### 9.3 Constraint Space Extensions

- Custom constraints can be added
- Specialized constraint types for specific layouts
- Additional context information for algorithms

### 9.4 Rendering Integration (Future)

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
- Immutable fragments help with memory safety
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