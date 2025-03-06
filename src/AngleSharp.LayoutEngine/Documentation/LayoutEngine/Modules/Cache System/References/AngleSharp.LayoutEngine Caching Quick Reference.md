# AngleSharp.LayoutEngine CacheSystem Quick Reference

## Getting Started

The caching system in AngleSharp.LayoutEngine improves performance by reusing computed styles and layouts. This reference provides common usage patterns without requiring detailed knowledge of the implementation.

## Basic Usage

### Setting Up the Cache Manager

```csharp
// Create a new cache manager
var cacheManager = new LayoutEngineCacheManager();

// Use it with the style engine
var engine = new StyleEngine(
    renderDevice, 
    context, 
    document, 
    cacheManager);
```

### Retrieving Cached Styles

```csharp
// The engine automatically uses the cache
var style = engine.ComputeElementStyle(element);

// Style is retrieved from cache if available, or computed and cached otherwise
```

## Direct Cache Access

### Style Cache

```csharp
// Get the style cache from the manager
var styleCache = cacheManager.StyleCache;

// Create a style cache key
var key = new StyleCacheKey(element, "::before"); // Optional pseudo-element

// Check if a style is cached
if (styleCache.TryGetValue(key, out var cachedStyle))
{
    // Use cached style
}

// Add or update a cached style
styleCache.AddOrUpdate(key, computedStyle);

// Get or compute a style
var style = styleCache.GetOrAdd(key, _ => {
    // Compute style if not in cache
    return computedStyle;
});

// Remove a specific cache entry
styleCache.Remove(key);
```

### Layout Cache

```csharp
// Get a layout cache for a specific layout data type
var layoutCache = cacheManager.GetLayoutCache<MyLayoutData>();

// Create a layout cache key with container dimensions
var key = new LayoutCacheKey(element, containerWidth, containerHeight);

// Use the layout cache (same pattern as style cache)
var layout = layoutCache.GetOrAdd(key, _ => {
    // Compute layout if not in cache
    return computedLayout;
});
```

## Cache Invalidation

### Invalidating Single Elements

```csharp
// Invalidate cache entries for a specific element
cacheManager.InvalidateElement(element);

// This invalidates:
// 1. The element's style and layout
// 2. Pseudo-element styles (::before, ::after)
// 3. Dependent elements' styles and layouts
```

### Invalidating Documents

```csharp
// Invalidate all cached entries for elements in a document
cacheManager.InvalidateDocument(document);
```

### Complete Cache Clearing

```csharp
// Clear all caches
cacheManager.InvalidateAll();
```

### Targeted Invalidation

```csharp
// Invalidate just styles for an element
styleCache.InvalidateElement(element);

// Invalidate just layouts for an element
layoutCache.InvalidateElement(element);
```

## Dependency Tracking (Advanced)

The cache automatically tracks dependencies, but you can manually specify them if needed:

```csharp
// Get the dependency tracker
var tracker = new CacheDependencyTracker();

// Track a dependency between elements
tracker.TrackDependency(
    dependentElement,
    sourceElement,
    CacheDependencyOptions.TrackStyleDependencies);

// Track a document dependency
tracker.TrackDocumentDependency(element, document);

// Get elements that depend on an element for styling
var dependents = tracker.GetStyleDependents(element);

// Get elements that depend on an element for layout
var layoutDependents = tracker.GetLayoutDependents(element);
```

## Best Practices

1. **Let the Engine Handle Caching**
    
    - In most cases, just use `engine.ComputeElementStyle()` and let it handle caching
    - Direct cache manipulation is only needed for advanced scenarios
2. **Invalidate at the Right Level**
    
    - Use `InvalidateElement()` for changes to a single element
    - Use `InvalidateDocument()` for major document changes
    - Use `InvalidateAll()` sparingly, as it clears all caches
3. **Memory Management**
    
    - The cache can grow with large documents
    - Consider clearing the cache when switching documents
    - For long-lived applications, periodically call `InvalidateAll()`
4. **Performance Monitoring**
    
    - Check `styleCache.Count` to see how many entries are cached
    - Large cache sizes may indicate need for invalidation

## Common Scenarios

### Styling After DOM Changes

```csharp
// When elements are modified
element.ClassName = "new-class";
cacheManager.InvalidateElement(element);

// When adding new content
parent.AppendChild(newElement);
cacheManager.InvalidateElement(parent);

// After major document changes
document.Body.InnerHtml = newContent;
cacheManager.InvalidateDocument(document);
```

### Working with Pseudo-elements

```csharp
// Compute style for a pseudo-element
var beforeStyle = engine.ComputeElementStyle(element, null, "::before");
var afterStyle = engine.ComputeElementStyle(element, null, "::after");
```

### Custom Layout Caching

```csharp
// Define custom layout data
public class BoxLayoutData
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

// Get a specialized cache
var boxCache = cacheManager.GetLayoutCache<BoxLayoutData>();

// Use the cache
var key = new LayoutCacheKey(element, containerWidth, containerHeight);
var layout = boxCache.GetOrAdd(key, _ => CalculateBoxLayout(element));
```

## Common Pitfalls

1. **Stale Data**: Forgetting to invalidate after changes
    
    - Always invalidate when making DOM changes
    - Consider which elements are affected by changes
2. **Excessive Invalidation**: Invalidating too broadly
    
    - Prefer targeted invalidation over clearing all caches
    - Consider which dependencies actually changed
3. **Memory Leaks**: Keeping references to removed elements
    
    - Invalidate cache entries for elements before removing them
    - Periodically check cache size for unexpected growth
4. **Cache Thrashing**: Computing and caching too frequently
    
    - Batch your DOM changes before invalidating
    - Consider whether caching is beneficial for one-time operations