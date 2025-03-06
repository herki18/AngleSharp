# CacheSystem Integration with StyleSystem Pipeline

The CacheSystem integrates closely with the StyleSystem to optimize performance by avoiding redundant style calculations. This document explains how the two systems work together.

## Integration Architecture

The StyleSystem and CacheSystem are designed to work together with clear interfaces:

```csharp
// StyleSystem interfaces for cache integration
public interface IStyleEngine
{
    // Main style computation method - internally uses cache
    ICssStyleDeclaration ComputeElementStyle(
        IElement element,
        ICssStyleDeclaration parentStyle = null,
        string pseudoElement = null);
        
    // Cache-aware computation
    ICssStyleDeclaration ComputeElementStyleWithCache(
        IElement element,
        ICssStyleDeclaration parentStyle = null,
        string pseudoElement = null,
        IStyleCache cache = null);
}

// CacheSystem interfaces for style integration
public interface IStyleCache
{
    bool TryGetValue(StyleCacheKey key, out ICssStyleDeclaration style);
    void AddOrUpdate(StyleCacheKey key, ICssStyleDeclaration style);
    ICssStyleDeclaration GetOrAdd(StyleCacheKey key, Func<StyleCacheKey, ICssStyleDeclaration> factory);
    void InvalidateElement(IElement element);
    void Clear();
}
```

## Cache Lookup Process

When the StyleEngine computes styles, it follows this process:

1. Create a cache key for the element and pseudo-element
2. Check if the style is already in the cache
3. If found, return the cached style
4. If not found, compute the style and add it to the cache

```csharp
// Example implementation (simplified)
public ICssStyleDeclaration ComputeElementStyle(IElement element, ICssStyleDeclaration parentStyle = null, string pseudoElement = null)
{
    // Create cache key
    var key = new StyleCacheKey(element, pseudoElement);
    
    // Check cache
    if (_cacheManager?.StyleCache?.TryGetValue(key, out var cachedStyle) == true)
    {
        return cachedStyle;
    }
    
    // Compute style if not cached
    var computedStyle = ComputeStyleInternal(element, parentStyle, pseudoElement);
    
    // Cache the result
    _cacheManager?.StyleCache?.AddOrUpdate(key, computedStyle);
    
    return computedStyle;
}
```

## Cache Invalidation

Cache invalidation happens through different channels:

1. **DOM Mutations**: When the DOM changes, the LifecycleSystem notifies the CacheSystem
2. **Stylesheet Changes**: When stylesheets change, the StyleSystem triggers invalidation
3. **Manual Invalidation**: Developers can manually invalidate the cache when needed

```csharp
// Invalidation workflow
public void OnDomMutation(IElement element)
{
    // Invalidate the element
    _cacheManager.InvalidateElement(element);
    
    // Invalidate elements that depend on this element
    var dependents = _dependencyTracker.GetDependents(element);
    foreach (var dependent in dependents)
    {
        _cacheManager.InvalidateElement(dependent);
    }
}
```

## Performance Optimization Strategies

The integration employs several strategies to maximize performance:

1. **Hierarchical Caching**: Parent styles are cached before child styles
2. **Dependency-Aware Invalidation**: Only invalidate what's affected by changes
3. **Partial Computation Reuse**: Reuse partial results when possible
4. **Lazy Dependency Tracking**: Track dependencies only when needed

```csharp
// Example of dependency-aware invalidation
public void InvalidateStylesheet(ICssStyleSheet stylesheet)
{
    // Get all selectors in the stylesheet
    var selectors = stylesheet.GetAllSelectors();
    
    // Find elements affected by each selector
    foreach (var selector in selectors)
    {
        var affectedElements = _dependencyTracker.GetElementsMatchingSelector(selector);
        foreach (var element in affectedElements)
        {
            _cacheManager.InvalidateElement(element);
        }
    }
}
```

## Memory Management

The CacheSystem uses strategies to manage memory efficiently:

1. **Weak References**: Use weak references to prevent memory leaks
2. **Cache Size Limits**: Set maximum size for caches
3. **Priority-Based Eviction**: Remove less important entries first
4. **Element Disposal Tracking**: Clear cache entries when elements are removed

```csharp
// Example of cache size management
public void TrimCache(int targetSize)
{
    if (_cache.Count <= targetSize) return;
    
    // Sort entries by importance (access frequency, etc.)
    var entries = _cache.OrderByImportance();
    
    // Remove least important entries until we reach target size
    int removeCount = _cache.Count - targetSize;
    for (int i = 0; i < removeCount; i++)
    {
        _cache.Remove(entries[i].Key);
    }
}
```

## Integration Diagram

```
┌─────────────────────┐       ┌────────────────────┐
│                     │       │                    │
│     StyleSystem     │◄──────┤    LifecycleSystem │
│                     │       │                    │
└───────────┬─────────┘       └────────────────────┘
            │                           ▲
            │                           │
            ▼                           │
┌─────────────────────┐       ┌────────────────────┐
│                     │       │                    │
│     CacheSystem     │◄──────┤     LayoutSystem   │
│                     │       │                    │
└─────────────────────┘       └────────────────────┘
```

## Best Practices for Integration

1. **Minimize Direct Cache Access**: Let the StyleEngine handle cache operations when possible
2. **Use Cache Keys Consistently**: Ensure cache keys are created consistently across the application
3. **Consider Cache Lifetime**: Clear caches when appropriate (document changes, navigation)
4. **Monitor Cache Performance**: Track cache hit rates and size growth
5. **Balance Memory and Speed**: Adjust cache size and eviction policies based on application needs