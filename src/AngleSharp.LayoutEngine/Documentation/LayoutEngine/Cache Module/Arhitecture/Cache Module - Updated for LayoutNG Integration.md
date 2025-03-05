# Cache Module - Updated for LayoutNG Integration

## Overview

The Cache Module in AngleSharp.LayoutEngine optimizes performance by avoiding redundant style and layout computations. This updated architecture incorporates enhancements to support the LayoutNG-inspired approach, particularly the caching of immutable fragments and intrinsic sizes as separate concerns.

The system provides specialized caches for style declarations, layout fragments, and intrinsic sizes, with a sophisticated dependency tracking mechanism that ensures proper cache invalidation while maintaining memory efficiency.

## Core Components

### 1. LayoutEngineCacheManager

The central coordinator for all caches in the system. It has been enhanced to support fragment and intrinsic size caching.

```csharp
public class LayoutEngineCacheManager
{
    // Access to specialized caches
    public IStyleCache StyleCache { get; }
    public IFragmentCache FragmentCache { get; } // NEW: For layout fragments
    public IIntrinsicSizeCache IntrinsicSizeCache { get; } // NEW: For intrinsic sizes
    public ILayoutBoxCache<TLayoutData> GetLayoutCache<TLayoutData>();
    
    // Invalidation methods
    public void InvalidateAll();
    public void InvalidateElement(IElement element);
    public void InvalidateDocument(IDocument document);
    
    // Enhanced invalidation for LayoutNG
    public void InvalidateIntrinsicSizes(IElement element); // NEW
    public void InvalidateFragments(IElement element); // NEW
    public void InvalidateByWritingMode(IElement element, WritingMode oldMode, WritingMode newMode); // NEW
    
    // Cache metrics
    public CacheMetrics GetMetrics();
    
    // Memory management
    public void TrimExcessMemory();
    public void SetMemoryLimit(long bytes);
}
```

### 2. StyleCache

Caches computed style declarations for elements. The implementation remains largely the same.

```csharp
public class StyleCache : LayoutEngineCache<StyleCacheKey, ICssStyleDeclaration>, IStyleCache
{
    // Key structure for style cache
    public StyleCacheKey CreateKey(IElement element, string pseudoElement = null);
    
    // Specialized invalidation methods
    public void InvalidateElementStyle(IElement element);
    public void InvalidateBySelector(string selector);
    public void InvalidateByProperty(string propertyName);
    public void InvalidateByVariable(string variableName);
}

// Key for style cache
public class StyleCacheKey : IEquatable<StyleCacheKey>
{
    public IElement Element { get; }
    public string PseudoElement { get; }
    
    // Equality and hashing implementation
}
```

### 3. FragmentCache (NEW)

New cache specifically for layout fragments, which are the immutable results of layout operations in the LayoutNG approach.

```csharp
public class FragmentCache : LayoutEngineCache<FragmentCacheKey, LayoutFragment>, IFragmentCache
{
    // Key creation with constraint space
    public FragmentCacheKey CreateKey(IElement element, IConstraintSpace constraintSpace);
    
    // Specialized invalidation methods
    public void InvalidateElement(IElement element);
    public void InvalidateSubtree(IElement root);
    public void InvalidateByConstraintChange(IElement element);
    public void InvalidateByGeometryChange(IElement element);
    
    // Fragment lookup (specialized)
    public bool TryGetFragmentWithSimilarConstraints(
        IElement element, 
        IConstraintSpace constraintSpace, 
        out LayoutFragment fragment);
}

// Key for fragment cache including constraints
public class FragmentCacheKey : IEquatable<FragmentCacheKey>
{
    public IElement Element { get; }
    public IConstraintSpace ConstraintSpace { get; }
    
    // Constraint hash code for quick comparison
    public int ConstraintHashCode { get; }
    
    // Equality and hashing implementation
    
    // Constraint comparison
    public bool AreConstraintsSimilar(IConstraintSpace other, float tolerance = 0.01f);
}
```

### 4. IntrinsicSizeCache (NEW)

New cache specifically for intrinsic sizes (min/max content sizes), which are calculated separately from layout in the LayoutNG approach.

```csharp
public class IntrinsicSizeCache : LayoutEngineCache<IntrinsicSizeCacheKey, MinMaxSizes>, IIntrinsicSizeCache
{
    // Key creation
    public IntrinsicSizeCacheKey CreateKey(IElement element, WritingMode writingMode = null);
    
    // Specialized invalidation methods
    public void InvalidateElement(IElement element);
    public void InvalidateByWritingMode(IElement element);
    public void InvalidateByStyleChange(IElement element, string propertyName);
}

// Key for intrinsic size cache
public class IntrinsicSizeCacheKey : IEquatable<IntrinsicSizeCacheKey>
{
    public IElement Element { get; }
    public WritingMode WritingMode { get; }
    
    // Equality and hashing implementation
}
```

### 5. LayoutBoxCache

Maintains backward compatibility with the original layout box approach while adapting to work with the new fragment-based system.

```csharp
public class LayoutBoxCache<TLayoutData> : LayoutEngineCache<LayoutCacheKey, TLayoutData>, ILayoutBoxCache<TLayoutData>
{
    // Integration with fragment cache
    public void ImportFromFragment(LayoutFragment fragment, Func<LayoutFragment, TLayoutData> converter);
    
    // Adapt to constraint-based approach
    public LayoutCacheKey CreateKeyFromConstraints(IElement element, IConstraintSpace constraintSpace);
}
```

### 6. CacheDependencyTracker

Tracks relationships between cached items to enable targeted invalidation. Enhanced to support fragment and intrinsic size dependencies.

```csharp
public class CacheDependencyTracker
{
    // Element dependency tracking
    public void TrackDependency(IElement dependent, IElement source, DependencyType type);
    public void TrackDocumentDependency(IElement element, IDocument document);
    
    // Enhanced tracking for LayoutNG
    public void TrackConstraintDependency(IElement element, IConstraintSpace constraintSpace);
    public void TrackSizeDependency(IElement dependent, IElement source);
    public void TrackWritingModeDependency(IElement element, WritingMode writingMode);
    
    // Dependency queries
    public IEnumerable<IElement> GetDependents(IElement element, DependencyType type);
    public IEnumerable<IElement> GetStyleDependents(IElement element);
    public IEnumerable<IElement> GetFragmentDependents(IElement element);
    public IEnumerable<IElement> GetSizeDependents(IElement element);
    
    // Specialized dependency queries
    public IEnumerable<IElement> GetWritingModeDependents(WritingMode writingMode);
    public IEnumerable<IElement> GetConstraintDependents(IConstraintSpace constraintSpace);
}

// Types of dependencies
[Flags]
public enum DependencyType
{
    None = 0,
    Style = 1,
    IntrinsicSize = 2, // NEW
    Fragment = 4, // NEW
    Layout = 8,
    All = Style | IntrinsicSize | Fragment | Layout
}
```

### 7. EnhancedDependencyTracker

Enhanced version of the dependency tracker with more sophisticated tracking mechanisms.

```csharp
public class EnhancedDependencyTracker : CacheDependencyTracker
{
    // Selector-based dependency tracking
    public void TrackSelectorDependency(string selector, IElement element);
    public IEnumerable<IElement> GetElementsMatchingSelector(string selector);
    
    // Property-based dependency tracking
    public void TrackPropertyDependency(string property, IElement element);
    public IEnumerable<IElement> GetElementsUsingProperty(string property);
    
    // Variable-based dependency tracking
    public void TrackVariableDependency(string variableName, IElement element);
    public IEnumerable<IElement> GetElementsUsingVariable(string variableName);
    
    // Formatting context dependency tracking
    public void TrackFormattingContextDependency(FormattingContextType type, IElement element);
    public IEnumerable<IElement> GetElementsInFormattingContext(FormattingContextType type);
}

// Types of formatting contexts
public enum FormattingContextType
{
    Block,
    Inline,
    Flex,
    Grid,
    Table
}
```

### 8. VersionedKey

Wrapper for cache keys that includes version information for efficient invalidation.

```csharp
public class VersionedKey<T> where T : class
{
    public T Key { get; }
    public int Version { get; }
    
    // Version comparison
    public bool IsValid(int currentVersion);
}
```

### 9. CacheValueConverter (NEW)

Utilities for converting between different cache value types.

```csharp
public static class CacheValueConverter
{
    // Convert layout fragment to layout box
    public static TLayoutBox ToLayoutBox<TLayoutBox>(LayoutFragment fragment) 
        where TLayoutBox : ILayoutBox, new();
    
    // Convert layout box to fragment builder
    public static FragmentBuilder ToFragmentBuilder<TLayoutBox>(TLayoutBox layoutBox) 
        where TLayoutBox : ILayoutBox;
    
    // Convert intrinsic sizes from logical to physical
    public static MinMaxSizes ConvertSizes(MinMaxSizes sizes, WritingMode writingMode);
}
```

## Caching Strategies

The caching system employs multiple strategies to balance performance and correctness:

### 1. Constraint-Based Fragment Caching

Fragment caching now takes into account the constraint space under which the fragment was created:

```csharp
// Example of constraint-based caching
public LayoutFragment GetOrCreateFragment(IElement element, IConstraintSpace constraintSpace)
{
    var key = new FragmentCacheKey(element, constraintSpace);
    
    return _fragmentCache.GetOrAdd(key, _ => {
        // Perform layout with constraints
        var fragment = _layoutEngine.LayoutWithConstraints(element, constraintSpace);
        
        // Track dependencies for this fragment
        _dependencyTracker.TrackConstraintDependency(element, constraintSpace);
        
        foreach (var child in element.Children.OfType<IElement>())
        {
            _dependencyTracker.TrackDependency(element, child, DependencyType.Fragment);
        }
        
        return fragment;
    });
}
```

### 2. Similar Constraint Optimization

The system can reuse fragments even when constraints aren't exactly the same but are similar enough:

```csharp
// Example of similar constraint optimization
public bool TryGetFragmentWithSimilarConstraints(
    IElement element, 
    IConstraintSpace constraintSpace, 
    out LayoutFragment fragment)
{
    fragment = null;
    
    // Try exact match first
    var exactKey = new FragmentCacheKey(element, constraintSpace);
    if (_cache.TryGetValue(exactKey, out fragment))
    {
        return true;
    }
    
    // Look for similar constraints
    foreach (var entry in _cache)
    {
        var key = entry.Key.Key; // Unwrap versioned key
        
        if (key.Element == element && key.AreConstraintsSimilar(constraintSpace))
        {
            fragment = entry.Value;
            return true;
        }
    }
    
    return false;
}
```

### 3. Writing Mode-Specific Caching

The system caches size information separately for different writing modes:

```csharp
// Example of writing mode-specific caching
public MinMaxSizes GetOrCreateIntrinsicSizes(IElement element, WritingMode writingMode)
{
    var key = new IntrinsicSizeCacheKey(element, writingMode);
    
    return _intrinsicSizeCache.GetOrAdd(key, _ => {
        // Calculate intrinsic sizes for this writing mode
        var sizes = _sizesCalculator.ComputeMinMaxSizes(element, writingMode);
        
        // Track dependencies
        _dependencyTracker.TrackWritingModeDependency(element, writingMode);
        
        foreach (var child in element.Children.OfType<IElement>())
        {
            _dependencyTracker.TrackDependency(element, child, DependencyType.IntrinsicSize);
        }
        
        return sizes;
    });
}
```

### 4. Hierarchical Invalidation

Advanced invalidation that respects formatting context boundaries and containment:

```csharp
// Example of hierarchical invalidation
public void InvalidateFragmentsWithContainment(IElement element)
{
    // Check for containment
    var style = _styleEngine.ComputeElementStyle(element);
    var containmentType = _containmentDetector.GetContainmentType(style);
    
    if ((containmentType & ContainmentType.Layout) != 0)
    {
        // Only invalidate within containment boundary
        _fragmentCache.InvalidateSubtree(element);
    }
    else
    {
        // Find all dependent elements
        var dependents = _dependencyTracker.GetFragmentDependents(element);
        
        // Also invalidate ancestors that might be affected
        var ancestor = element.ParentElement;
        while (ancestor != null)
        {
            _fragmentCache.InvalidateElement(ancestor);
            
            // Stop at containment boundary
            var ancestorStyle = _styleEngine.ComputeElementStyle(ancestor);
            if (_containmentDetector.HasLayoutContainment(ancestorStyle))
            {
                break;
            }
            
            ancestor = ancestor.ParentElement;
        }
        
        // Invalidate all dependents
        foreach (var dependent in dependents)
        {
            _fragmentCache.InvalidateElement(dependent);
        }
    }
}
```

### 5. Selective Property Invalidation

Optimize invalidation based on which properties actually changed:

```csharp
// Example of property-aware invalidation
public void InvalidateByPropertyChange(IElement element, string propertyName)
{
    // Always invalidate style cache
    _styleCache.InvalidateElementStyle(element);
    
    // Check if property affects intrinsic sizes
    if (_propertyAnalyzer.AffectsIntrinsicSizes(propertyName))
    {
        _intrinsicSizeCache.InvalidateElement(element);
        
        // Sizes affect fragments
        _fragmentCache.InvalidateElement(element);
    }
    // Check if property only affects geometry without changing sizes
    else if (_propertyAnalyzer.AffectsGeometryOnly(propertyName))
    {
        // Skip intrinsic size invalidation, just invalidate fragments
        _fragmentCache.InvalidateElement(element);
    }
    
    // Check if property affects writing mode
    if (_propertyAnalyzer.AffectsWritingMode(propertyName))
    {
        _intrinsicSizeCache.InvalidateByWritingMode(element);
        _fragmentCache.InvalidateByConstraintChange(element);
    }
}
```

## LayoutNG-Specific Cache Features

### 1. Fragment Builder Caching

Accelerate fragment creation by caching intermediate builder state:

```csharp
public class FragmentBuilderCache : LayoutEngineCache<FragmentBuilderCacheKey, FragmentBuilder>
{
    // Store partial fragment builders to speed up construction
    public void StorePartialBuilder(IElement element, FragmentBuilder builder);
    
    // Retrieve and complete a builder
    public FragmentBuilder GetOrCreateBuilder(IElement element, IConstraintSpace constraintSpace);
    
    // Invalidate builders
    public void InvalidateBuilders(IElement element);
}
```

### 2. Constraint Space Caching

Optimize creation of frequently used constraint spaces:

```csharp
public class ConstraintSpaceCache : LayoutEngineCache<ConstraintSpaceCacheKey, IConstraintSpace>
{
    // Create a constraint space or get from cache
    public IConstraintSpace GetOrCreateConstraintSpace(
        IElement element, 
        Size availableSize, 
        WritingMode writingMode);
    
    // Create a child constraint space
    public IConstraintSpace GetOrCreateChildConstraintSpace(
        IConstraintSpace parentSpace, 
        IElement childElement);
}

// Key for constraint space cache
public class ConstraintSpaceCacheKey : IEquatable<ConstraintSpaceCacheKey>
{
    public IElement TargetElement { get; }
    public Size AvailableSize { get; }
    public WritingMode WritingMode { get; }
    public bool IsNewFormattingContext { get; }
    
    // Equality and hashing implementation
}
```

### 3. Layout Result Caching

Cache complete layout results including fragments and intrinsic sizes:

```csharp
public class LayoutResultCache : LayoutEngineCache<LayoutResultCacheKey, LayoutResult>
{
    // Get or create a complete layout result
    public LayoutResult GetOrCreateResult(
        IElement element, 
        IConstraintSpace constraintSpace, 
        LayoutOptions options);
    
    // Try to find a usable layout result
    public bool TryGetUsableResult(
        IElement element, 
        IConstraintSpace constraintSpace, 
        out LayoutResult result);
}

// Key for layout result cache
public class LayoutResultCacheKey : IEquatable<LayoutResultCacheKey>
{
    public IElement Element { get; }
    public IConstraintSpace ConstraintSpace { get; }
    public LayoutOptions Options { get; }
    
    // Equality and hashing implementation
}

// Options for layout operation
public class LayoutOptions
{
    public bool IncludeIntrinsicSizes { get; set; }
    public bool IncludeOverflow { get; set; }
    public bool AllowFragmentation { get; set; }
}
```

## Memory Management for Caching

### Efficient Memory Usage

The system includes mechanisms to manage memory efficiently:

```csharp
public class CacheMemoryManager
{
    // Monitor memory usage
    public long GetTotalMemoryUsage();
    public long GetCacheTypeMemoryUsage<T>();
    
    // Memory management policies
    public void SetCacheSizeLimit(Type cacheType, long maxBytes);
    public void SetGlobalSizeLimit(long maxBytes);
    
    // Trim methods
    public void TrimToSize(Type cacheType, long targetBytes);
    public void TrimAll(float percentToKeep = 0.7f);
    
    // Eviction strategies
    public void SetEvictionStrategy(CacheEvictionStrategy strategy);
    
    // Memory pressure handling
    public void RegisterForMemoryPressureNotifications();
    public void HandleMemoryPressure(MemoryPressureLevel level);
}

// Eviction strategies
public enum CacheEvictionStrategy
{
    LeastRecentlyUsed,
    MostMemoryConsuming,
    LeastValuable // Based on computation cost vs memory usage
}

// Memory pressure levels
public enum MemoryPressureLevel
{
    Low,
    Medium,
    High,
    Critical
}
```

### Fragment Pooling

Reduce allocation pressure by reusing fragment data structures:

```csharp
public class FragmentPool
{
    // Get a fragment from the pool or create new
    public LayoutFragment GetFragment();
    
    // Return a fragment to the pool
    public void ReturnFragment(LayoutFragment fragment);
    
    // Create a new fragment from builder with pooling
    public LayoutFragment CreateFromBuilder(FragmentBuilder builder);
    
    // Pool statistics
    public int PoolSize { get; }
    public int ActiveFragments { get; }
}
```

## Integration with Other Modules

### Integration with Style Computation Module

The Cache Module integrates with the Style Computation Module through selective invalidation:

```csharp
// Style computation integration
public interface IStyleCacheIntegration
{
    // Invalidate based on selectors
    void InvalidateBySelector(string selector);
    
    // Invalidate based on specific properties
    void InvalidateByProperty(string propertyName);
    
    // CSS variable handling
    void InvalidateByVariable(string variableName);
    void TrackVariableDependency(string variableName, IElement element);
}
```

### Integration with Layout Engine Module

The Cache Module integrates with the Layout Engine Module through constraint awareness:

```csharp
// Layout engine integration
public interface ILayoutCacheIntegration
{
    // Fragment caching with constraints
    LayoutFragment GetCachedFragment(IElement element, IConstraintSpace constraintSpace);
    void StoreFragment(IElement element, IConstraintSpace constraintSpace, LayoutFragment fragment);
    
    // Intrinsic size caching
    MinMaxSizes GetCachedIntrinsicSizes(IElement element, WritingMode writingMode);
    void StoreIntrinsicSizes(IElement element, WritingMode writingMode, MinMaxSizes sizes);
    
    // Constraint tracking
    void TrackConstraintDependency(IElement element, IConstraintSpace constraintSpace);
}
```

### Integration with Document Lifecycle Module

The Cache Module integrates with the Document Lifecycle Module through invalidation handling:

```csharp
// Document lifecycle integration
public interface ILifecycleCacheIntegration
{
    // Handle various invalidation triggers
    void HandleStyleInvalidation(IElement element);
    void HandleIntrinsicSizeInvalidation(IElement element);
    void HandleFragmentInvalidation(IElement element);
    
    // Lifecycle state changes
    void HandleStateTransition(LifecycleState oldState, LifecycleState newState);
    
    // Batch invalidation
    void BeginInvalidationBatch();
    void EndInvalidationBatch();
}
```

## Conclusion

The enhanced Cache Module provides comprehensive support for the LayoutNG-inspired layout engine. The additions include:

1. Fragment-based caching with constraint awareness
2. Separate intrinsic size caching
3. Writing mode-specific caching
4. More sophisticated dependency tracking
5. Memory management optimizations
6. Constraint space caching
7. Fragment pooling for memory efficiency

These enhancements ensure that the Cache Module can efficiently store and manage the immutable fragments and constraint-based layout results produced by the LayoutNG approach, while maintaining optimal performance and memory usage.