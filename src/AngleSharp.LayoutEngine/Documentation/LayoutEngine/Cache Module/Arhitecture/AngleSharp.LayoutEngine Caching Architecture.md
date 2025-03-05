# AngleSharp.LayoutEngine Caching Architecture

## Overview

The caching system in AngleSharp.LayoutEngine is designed to optimize style and layout computation performance by avoiding redundant calculations. It implements a sophisticated dependency tracking mechanism that ensures proper cache invalidation while maintaining memory efficiency.

The system provides specialized caches for style declarations and layout boxes, with a versioning mechanism to efficiently invalidate entries when dependencies change. It maintains proper separation of concerns by having distinct cache strategies for different computation types while sharing a common dependency tracking infrastructure.

## Core Components

### Cache Interfaces

- **IComputationCache<TKey, TValue>**: The foundational generic interface for all computation caches
    - Defines methods for adding, retrieving, and invalidating cache entries
    - Provides a consistent API regardless of what's being cached

### Base Implementation

- **LayoutEngineCache<TKey, TValue>**: Base implementation for all cache types
    - Implements versioning for efficient mass invalidation
    - Handles key transformation for specialized caching needs
    - Provides thread-safe operations using ConcurrentDictionary

### Specialized Cache Types

- **StyleCache**: Caches computed style declarations for elements
    
    - Keys based on element and optional pseudo-element
    - Integrates with dependency tracking for style relationships
- **LayoutBoxCache<TLayoutData>**: Caches layout calculations
    
    - Keys based on element and container dimensions
    - Handles dependencies between layout computations

### Dependency Tracking

- **CacheDependencyTracker**: Maintains relationships between cached items
    - Tracks element hierarchical dependencies
    - Tracks style dependencies
    - Tracks document dependencies
    - Tracks layout dependencies
    - Enables targeted invalidation of dependent entries

### Keys and Versioning

- **StyleCacheKey**: Identifies style computations (element + pseudo-element)
- **LayoutCacheKey**: Identifies layout computations (element + container dimensions)
- **VersionedKey<T>**: Wraps keys with version information for invalidation

### Central Management

- **LayoutEngineCacheManager**: Coordinates all caches in the system
    - Provides access to specialized caches
    - Orchestrates invalidation across multiple cache types
    - Maintains a shared dependency tracker

## Caching Strategies

The caching system employs multiple strategies to balance performance and correctness:

### 1. Hierarchical Dependency Tracking

Style and layout computations often depend on parent elements. The system tracks these relationships to ensure that changes to parent elements correctly invalidate cached values for their descendants.

```csharp
// When caching a style for an element, track its parent dependency
var parent = key.Element.ParentElement;
if (parent != null)
{
    _dependencyTracker.TrackDependency(
        key.Element,
        parent,
        CacheDependencyOptions.TrackElementDependencies |
        CacheDependencyOptions.TrackStyleDependencies);
}
```

### 2. Versioned Cache Entries

Rather than removing all entries when a significant change occurs, the system can increment a version counter and effectively invalidate all entries at once.

```csharp
// Invalidate by incrementing version
public virtual void Invalidate(bool clearItems = true)
{
    Interlocked.Increment(ref _version);
    if (clearItems)
    {
        Clear();
    }
}
```

### 3. Selective Invalidation

For targeted updates, the system can selectively invalidate only the affected elements and their dependents.

```csharp
// Invalidate a specific element and its dependents
public void InvalidateElement(IElement element)
{
    // Remove direct entry
    _cache.Remove(new StyleCacheKey(element));
    
    // Also invalidate dependents
    foreach (var dependent in _dependencyTracker.GetStyleDependents(element))
    {
        _cache.Remove(new StyleCacheKey(dependent));
    }
}
```

### 4. Document-Level Invalidation

When a document changes significantly, all elements within that document can be invalidated.

```csharp
// Invalidate all elements in a document
public void InvalidateDocument(IDocument document)
{
    foreach (var element in _dependencyTracker.GetDocumentDependents(document))
    {
        InvalidateElement(element);
    }
}
```

## Integration with Style Computation

The caching system integrates directly with the `StyleComputationEngine` to provide transparent caching of computed styles:

```csharp
public ICssStyleDeclaration ComputeElementStyle(IElement element,
    ICssStyleDeclaration? parentStyle = null,
    string? pseudoElement = null)
{
    // Create a cache key
    var cacheKey = new StyleCacheKey(element, pseudoElement);

    // Try to get from cache first
    if (_cacheManager.StyleCache.TryGetValue(cacheKey, out var cachedStyle))
    {
        return cachedStyle;
    }

    // If not in cache, compute and store for future use
    return _cacheManager.StyleCache.GetOrAdd(cacheKey, _ => {
        // Compute style...
        return computedStyle;
    });
}
```

## Performance Considerations

The caching system is designed with performance in mind:

1. **Memory Efficiency**:
    
    - Uses key versioning to avoid keeping multiple copies of the same data
    - Allows for selective clearing of caches
2. **CPU Efficiency**:
    
    - Minimizes redundant style and layout computations
    - Targets invalidations to affected elements only
    - Uses thread-safe concurrent collections for multi-threaded scenarios
3. **Scalability**:
    
    - Supports large DOM trees through targeted invalidation
    - Handles complex dependency chains efficiently
    - Maintains separate caches for different computation types