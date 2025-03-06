# AngleSharp.LayoutEngine CacheSystem Architecture

## 1. Architecture Overview

The caching architecture for AngleSharp.LayoutEngine is designed to support both the existing layout approach and the new LayoutNG-inspired architecture. This unified caching system optimizes performance by avoiding redundant calculations while supporting immutable fragments, constraint-based layout, and phase separation.

### 1.1 Core Design Principles

- **Immutable Fragment Support**: Cache immutable layout fragments rather than mutable box models
- **Phase-Specific Caching**: Separate caches for intrinsic sizes, layout fragments, and positioning
- **Constraint-Based Keys**: Cache keys that incorporate layout constraints for precise invalidation
- **Hierarchical Dependency Tracking**: Enhanced tracking for formatting contexts and logical layout
- **Memory Efficiency**: Optimized storage for immutable objects with versioning

### 1.2 System Components

The enhanced caching system extends the existing architecture with new components to support LayoutNG:

#### Core Interfaces (From Existing System)

- **IComputationCache<TKey, TValue>**: Foundational generic interface for all computation caches
- **LayoutEngineCache<TKey, TValue>**: Base implementation with versioning support

#### Enhanced Components for LayoutNG

- **FragmentCache**: Specialized cache for immutable layout fragments
- **IntrinsicSizeCache**: Dedicated cache for min/max content sizes
- **ConstraintSpaceCache**: Optional cache for frequently reused constraint spaces

#### Enhanced Key Types

- **FragmentCacheKey**: Identifies fragment computations (element + constraint space + writing mode)
- **IntrinsicSizesCacheKey**: Identifies intrinsic size computations (element + writing mode)
- **VersionedKey<T>**: Enhanced with support for constraint-sensitive versioning

#### Dependency Tracking

- **EnhancedDependencyTracker**: Extended from existing CacheDependencyTracker with:
    - Formatting context dependency tracking
    - Logical relationship tracking (writing-mode aware)
    - Constraint-based dependency tracking
    - Fragment assembly dependencies

#### Central Management

- **LayoutEngineCacheManager**: Enhanced to coordinate all cache types, including new LayoutNG caches

## 2. Key Components in Detail

### 2.1 FragmentCache

Specialized for caching immutable layout fragments produced by the LayoutNG system:

- **Purpose**: Store and retrieve immutable layout fragments
- **Key Structure**: Element + ConstraintSpace + Optional PseudoElement
- **Dependencies**: Tracks relationships between elements, fragments, and formatting contexts
- **Integration**: Works directly with the LayoutEngine to cache layout results
- **Versioning**: Uses the versioning system for efficient invalidation
- **Memory Management**: Implements strategies for efficient storage of immutable fragments

### 2.2 IntrinsicSizeCache

Dedicated to caching intrinsic size calculations (min/max content sizes):

- **Purpose**: Store and retrieve min/max content size calculations
- **Key Structure**: Element + WritingMode + Optional PseudoElement
- **Dependencies**: Tracks content and writing mode dependencies
- **Integration**: Works with the intrinsic size calculation phase of layout
- **Performance**: Optimized for frequent access during layout calculations
- **Invalidation**: Selectively invalidated when content or style changes affect sizing

### 2.3 Enhanced Cache Keys

Keys for the LayoutNG caching system that capture the necessary context:

- **FragmentCacheKey**: Captures the full context for layout fragment generation
    - Element reference
    - Constraint space (available size, percentage resolution base, etc.)
    - Optional pseudo-element identifier
    - Writing mode and direction information
- **IntrinsicSizesCacheKey**: Captures the context for intrinsic size calculation
    - Element reference
    - Writing mode information
    - Optional pseudo-element identifier

### 2.4 EnhancedDependencyTracker

Extended dependency tracking for LayoutNG requirements:

- **Formatting Context Dependencies**: Track which elements depend on specific formatting contexts
- **Constraint Dependencies**: Track relationships between elements and constraint spaces
- **Intrinsic Size Dependencies**: Track dependencies for size calculations across writing modes
- **Fragment Assembly Dependencies**: Track parent-child relationships in fragment trees
- **Logical Layout Awareness**: Account for writing mode in dependency relationships
- **Query Capabilities**: Retrieve elements affected by specific context changes
- **Invalidation Assistance**: Provide information for targeted invalidation

### 2.5 Enhanced LayoutEngineCacheManager

Extended to support LayoutNG caching requirements:

- **Unified Management**: Coordinates all cache types in the system
- **Cache Access**: Provides access to specialized caches (style, fragment, intrinsic sizes)
- **Invalidation Coordination**: Orchestrates invalidation across multiple cache types
- **Dependency Integration**: Works with the EnhancedDependencyTracker
- **Backward Compatibility**: Maintains support for existing cache types
- **Memory Management**: Coordinates memory usage across cache types

## 3. LayoutNG Integration Strategies

### 3.1 Fragment-Based Caching

Unlike the current box-model caching, fragment-based caching stores immutable layout fragments:

- **Immutable Results**: Store complete, immutable layout results
- **Hierarchical Structure**: Cache fragments with their hierarchical structure intact
- **Box Properties**: Store all box properties (margins, borders, padding) within fragments
- **Coordinate Systems**: Support both logical and physical coordinate systems
- **Fragment Identity**: Maintain identity for fragment lookup and dependency tracking

### 3.2 Phase-Separated Caching

The LayoutNG architecture separates intrinsic size calculation from layout:

- **Separate Phase Caches**: Distinct caches for different layout phases
- **Intrinsic Size Phase**: Cache min/max content sizes independently
- **Layout Phase**: Cache layout fragments with their full structure
- **Positioning Phase**: Potentially separate cache for positioned elements
- **Inter-Phase Dependencies**: Track relationships between phases
- **Phase-Specific Invalidation**: Invalidate only affected phases when possible

### 3.3 Constraint-Aware Invalidation

Fragment invalidation must consider constraint spaces:

- **Constraint-Sensitive Keys**: Cache keys that incorporate constraint information
- **Partial Invalidation**: Invalidate only entries affected by specific constraint changes
- **Constraint Propagation**: Track how constraint changes affect descendant elements
- **Writing Mode Awareness**: Consider writing mode in invalidation decisions
- **Formatting Context Boundaries**: Respect formatting context boundaries during invalidation
- **Style-Constraint Relationships**: Track how style changes affect constraints

### 3.4 Logical Layout Compatibility

The enhanced caching system handles logical layout:

- **Writing Mode Support**: Cache entries specific to writing modes
- **Direction Awareness**: Account for text direction in caching and invalidation
- **Logical Properties**: Support logical property dependencies
- **Physical Conversion**: Handle conversion between logical and physical coordinates
- **Directional Invalidation**: Invalidate appropriately when writing mode or direction changes

## 4. Backward Compatibility

### 4.1 Legacy API Support

The enhanced system maintains compatibility with existing code:

- **Existing Interfaces**: Maintain support for IStyleCache and ILayoutBoxCache
- **Adapter Pattern**: Use adapters to bridge between old and new systems
- **Data Conversion**: Convert between mutable boxes and immutable fragments as needed
- **Dual Operation**: Allow both systems to operate concurrently during transition
- **Compatible Methods**: Provide equivalent methods for common operations

### 4.2 Transition Strategy

A gradual transition allows incremental adoption:

1. **Phase 1**: Keep both systems side-by-side
    - Create adapters between old and new cache types
    - Allow clients to choose which system to use
2. **Phase 2**: Gradually migrate to LayoutNG
    - Convert most common layout scenarios first
    - Use adapters for edge cases
3. **Phase 3**: Complete transition
    - Full migration to fragment-based caching
    - Legacy API maintained through adapters

## 5. Optimizations and Performance

### 5.1 Memory Efficiency

The immutable nature of fragments requires memory optimizations:

- **Fragment Pooling**: Reuse fragment objects for similar layouts
- **Structural Sharing**: Common parts of fragments can be shared
- **Adaptive Caching**: Cache size adjusts based on available memory
- **Memory Monitoring**: Track fragment cache memory usage

### 5.2 Computation Efficiency

Optimize the cache for performance:

- **Key Hashing Optimization**: Efficient hash codes for cache keys
- **Two-Level Caching**: In-memory and serialized caching for large documents
- **Lazy Fragment Assembly**: Only compute detailed fragments when needed
- **Partial Recalculation**: Update only affected parts of fragments

### 5.3 Concurrent Access

Support for multi-threaded scenarios:

- **Thread-Safe Cache Operations**: All cache operations are thread-safe
- **Concurrent Computation**: Layout computations can happen concurrently
- **Read/Write Splitting**: Multiple reads with coordinated writes

## 6. Integration with LifecycleSystem

The caching system integrates with the LifecycleSystem:

- **Mutation Observation**: Integration with mutation detection system
- **Invalidation Coordination**: Coordinate with InvalidationManager
- **Lifecycle State Awareness**: Respect document lifecycle states
- **Update Scheduling**: Work with SchedulingService for optimal update timing
- **Batch Processing**: Support batch invalidation and recalculation
- **Priority-Based Updates**: Prioritize updates for visible content

## 7. Component Relationships

### 7.1 Component Diagram

```
LayoutEngine
    ├── StyleSystem
    │   └── StyleCache
    ├── LayoutSystem
    │   ├── FragmentCache
    │   ├── IntrinsicSizeCache
    │   └── ConstraintSpaceCache
    ├── CacheSystem
    │   ├── LayoutEngineCacheManager
    │   └── EnhancedDependencyTracker
    └── LifecycleSystem
        ├── InvalidationManager
        ├── StyleInvalidationTracker
        └── LayoutInvalidationTracker
```

### 7.2 Data Flow

1. **Layout Request**:
    
    - Client requests layout for element
    - LayoutEngine checks FragmentCache for cached fragment
    - If cache miss, compute fragment using appropriate formatting context
    - Store result in FragmentCache
    - Return fragment to client
2. **Intrinsic Size Calculation**:
    
    - Client requests intrinsic sizes
    - Check IntrinsicSizeCache for cached sizes
    - If cache miss, compute sizes
    - Store result in IntrinsicSizeCache
    - Return sizes to client
3. **DOM Mutation**:
    
    - MutationObserver detects change
    - InvalidationManager determines affected elements
    - StyleInvalidationTracker and LayoutInvalidationTracker mark elements as invalid
    - CacheManager invalidates affected cache entries
    - LifecycleManager schedules recalculation
4. **Invalidation Chain**:
    
    - Element style changes
    - StyleCache entry invalidated
    - EnhancedDependencyTracker identifies dependent fragments
    - FragmentCache entries invalidated
    - Dependent elements' fragments invalidated recursively

### 7.3 Dependency Relationships

- **StyleCache → FragmentCache**: Fragment calculations depend on computed styles
- **IntrinsicSizeCache → FragmentCache**: Fragment layout may depend on intrinsic sizes
- **FragmentCache → FragmentCache**: Parent fragments depend on child fragments
- **ConstraintSpace → FragmentCache**: Fragments depend on their constraint spaces
- **EnhancedDependencyTracker → All Caches**: Provides dependency information for invalidation
- **MutationObserver → InvalidationManager → CacheManager**: Mutation flow for invalidation

## 8. Future Extensibility

The architecture is designed for future extensions:

- **New Layout Models**: Support for additional CSS layout models
- **Custom Formatting Contexts**: Extensible for new formatting context types
- **Alternative Rendering Models**: Support for print, pagination, or variable containers
- **Advanced Caching Strategies**: Framework for plugging in new caching algorithms
- **Specialized Optimizations**: Extension points for specific layout scenarios
- **Rendering Integration**: Future connection points for rendering systems