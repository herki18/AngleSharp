namespace AngleSharp.LayoutEngine.Caching;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

/// <summary>
/// Generic interface for a computation cache.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
public interface IComputationCache<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Gets or computes a value from the cache.
    /// </summary>
    TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

    /// <summary>
    /// Tries to get a value from the cache.
    /// </summary>
    bool TryGetValue(TKey key, out TValue value);

    /// <summary>
    /// Adds or updates a value in the cache.
    /// </summary>
    void AddOrUpdate(TKey key, TValue value);

    /// <summary>
    /// Removes a specific item from the cache.
    /// </summary>
    bool Remove(TKey key);

    /// <summary>
    /// Clears all items from the cache.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the current cache size.
    /// </summary>
    int Count { get; }
}

/// <summary>
/// Cache entry dependency tracking options.
/// </summary>
[Flags]
public enum CacheDependencyOptions
{
    None = 0,
    TrackElementDependencies = 1,
    TrackStyleDependencies = 2,
    TrackDocumentDependencies = 4,
    TrackLayoutDependencies = 8,
    All = TrackElementDependencies | TrackStyleDependencies | TrackDocumentDependencies | TrackLayoutDependencies
}

/// <summary>
/// Base implementation of a versioned computation cache.
/// </summary>
public class LayoutEngineCache<TKey, TValue> : IComputationCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<object, TValue> _cache = new();
    private readonly Func<TKey, object> _keyTransformer;
    private long _version = 0;

    /// <summary>
    /// Initializes a new instance of the LayoutEngineCache.
    /// </summary>
    /// <param name="keyTransformer">Optional function to transform keys for versioning.</param>
    public LayoutEngineCache(Func<TKey, object>? keyTransformer = null)
    {
        _keyTransformer = keyTransformer ?? (key => new VersionedKey<TKey>(key, _version));
    }

    /// <summary>
    /// Gets the current cache version.
    /// </summary>
    public long Version => _version;

    /// <summary>
    /// Gets the current number of items in the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets or computes a value from the cache.
    /// </summary>
    public virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        var cacheKey = _keyTransformer(key);
        return _cache.GetOrAdd(cacheKey, _ => valueFactory(key));
    }

    /// <summary>
    /// Tries to get a value from the cache.
    /// </summary>
    public virtual bool TryGetValue(TKey key, out TValue value)
    {
        var cacheKey = _keyTransformer(key);
        return _cache.TryGetValue(cacheKey, out value!);
    }

    /// <summary>
    /// Adds or updates a value in the cache.
    /// </summary>
    public virtual void AddOrUpdate(TKey key, TValue value)
    {
        var cacheKey = _keyTransformer(key);
        _cache.AddOrUpdate(cacheKey, value, (_, _) => value);
    }

    /// <summary>
    /// Removes a specific item from the cache.
    /// </summary>
    public virtual bool Remove(TKey key)
    {
        var cacheKey = _keyTransformer(key);
        return _cache.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Clears all items from the cache.
    /// </summary>
    public virtual void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Invalidates the cache by incrementing the version and optionally clearing all items.
    /// </summary>
    /// <param name="clearItems">Whether to remove all items after invalidation.</param>
    public virtual void Invalidate(bool clearItems = true)
    {
        Interlocked.Increment(ref _version);
        if (clearItems)
        {
            Clear();
        }
    }
}

/// <summary>
/// A versioned key wrapper for cache entries.
/// </summary>
internal readonly struct VersionedKey<T> : IEquatable<VersionedKey<T>> where T : notnull
{
    public readonly T Key;
    public readonly long Version;

    public VersionedKey(T key, long version)
    {
        Key = key;
        Version = version;
    }

    public bool Equals(VersionedKey<T> other) =>
        EqualityComparer<T>.Default.Equals(Key, other.Key) &&
        Version == other.Version;

    public override bool Equals(object? obj) =>
        obj is VersionedKey<T> key && Equals(key);

    public override int GetHashCode() =>
        HashCode.Combine(Key, Version);
}

/// <summary>
/// Dependency tracker for cached values in the layout engine.
/// </summary>
public class CacheDependencyTracker
{
    // Element dependencies (parent-child, ancestor-descendant)
    private readonly Dictionary<IElement, HashSet<IElement>> _elementDependencies = new();

    // Style dependencies (element depends on another element's style)
    private readonly Dictionary<IElement, HashSet<IElement>> _styleDependencies = new();

    // Document dependencies (element to document)
    private readonly Dictionary<IDocument, HashSet<IElement>> _documentDependencies = new();

    // Layout dependencies (elements depending on others for positioning)
    private readonly Dictionary<IElement, HashSet<IElement>> _layoutDependencies = new();

    /// <summary>
    /// Tracks a dependency between elements.
    /// </summary>
    public void TrackDependency(
        IElement dependentElement,
        IElement sourceElement,
        CacheDependencyOptions options)
    {
        if ((options & CacheDependencyOptions.TrackElementDependencies) != 0)
        {
            AddDependency(_elementDependencies, sourceElement, dependentElement);
        }

        if ((options & CacheDependencyOptions.TrackStyleDependencies) != 0)
        {
            AddDependency(_styleDependencies, sourceElement, dependentElement);
        }

        if ((options & CacheDependencyOptions.TrackLayoutDependencies) != 0)
        {
            AddDependency(_layoutDependencies, sourceElement, dependentElement);
        }
    }

    /// <summary>
    /// Tracks a dependency between an element and its document.
    /// </summary>
    public void TrackDocumentDependency(IElement element, IDocument document)
    {
        AddDependency(_documentDependencies, document, element);
    }

    /// <summary>
    /// Gets elements that depend on the specified element for styling.
    /// </summary>
    public IEnumerable<IElement> GetStyleDependents(IElement element)
    {
        return GetDependents(_styleDependencies, element);
    }

    /// <summary>
    /// Gets elements that depend on the specified element for layout.
    /// </summary>
    public IEnumerable<IElement> GetLayoutDependents(IElement element)
    {
        return GetDependents(_layoutDependencies, element);
    }

    /// <summary>
    /// Gets all elements that depend on the specified element in any way.
    /// </summary>
    public IEnumerable<IElement> GetAllDependents(IElement element)
    {
        var result = new HashSet<IElement>();

        if (_elementDependencies.TryGetValue(element, out var elementDeps))
            result.UnionWith(elementDeps);

        if (_styleDependencies.TryGetValue(element, out var styleDeps))
            result.UnionWith(styleDeps);

        if (_layoutDependencies.TryGetValue(element, out var layoutDeps))
            result.UnionWith(layoutDeps);

        return result;
    }

    /// <summary>
    /// Gets all elements that depend on the specified document.
    /// </summary>
    public IEnumerable<IElement> GetDocumentDependents(IDocument document)
    {
        return GetDependents(_documentDependencies, document);
    }

    private void AddDependency<TSource, TDependent>(
        Dictionary<TSource, HashSet<TDependent>> dependencies,
        TSource source,
        TDependent dependent)
        where TSource : class
        where TDependent : class
    {
        if (!dependencies.TryGetValue(source, out var dependents))
        {
            dependents = new HashSet<TDependent>();
            dependencies[source] = dependents;
        }

        dependents.Add(dependent);
    }

    private IEnumerable<TDependent> GetDependents<TSource, TDependent>(
        Dictionary<TSource, HashSet<TDependent>> dependencies,
        TSource source)
        where TSource : class
        where TDependent : class
    {
        if (dependencies.TryGetValue(source, out var dependents))
        {
            return dependents;
        }

        return Array.Empty<TDependent>();
    }

    /// <summary>
    /// Clears all tracked dependencies.
    /// </summary>
    public void Clear()
    {
        _elementDependencies.Clear();
        _styleDependencies.Clear();
        _documentDependencies.Clear();
        _layoutDependencies.Clear();
    }
}

/// <summary>
/// Cache key for style computations.
/// </summary>
public readonly struct StyleCacheKey : IEquatable<StyleCacheKey>
{
    public readonly IElement Element;
    public readonly string? PseudoElement;

    public StyleCacheKey(IElement element, string? pseudoElement = null)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        PseudoElement = pseudoElement;
    }

    public bool Equals(StyleCacheKey other) =>
        ReferenceEquals(Element, other.Element) &&
        PseudoElement == other.PseudoElement;

    public override bool Equals(object? obj) =>
        obj is StyleCacheKey key && Equals(key);

    public override int GetHashCode() =>
        HashCode.Combine(RuntimeHelpers.GetHashCode(Element), PseudoElement);
}

/// <summary>
/// Cache key for element layout computations.
/// </summary>
public readonly struct LayoutCacheKey : IEquatable<LayoutCacheKey>
{
    public readonly IElement Element;
    public readonly double ContainerWidth;
    public readonly double ContainerHeight;

    public LayoutCacheKey(IElement element, double containerWidth, double containerHeight)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        ContainerWidth = containerWidth;
        ContainerHeight = containerHeight;
    }

    public bool Equals(LayoutCacheKey other) =>
        ReferenceEquals(Element, other.Element) &&
        ContainerWidth == other.ContainerWidth &&
        ContainerHeight == other.ContainerHeight;

    public override bool Equals(object? obj) =>
        obj is LayoutCacheKey key && Equals(key);

    public override int GetHashCode() =>
        HashCode.Combine(
            RuntimeHelpers.GetHashCode(Element),
            ContainerWidth,
            ContainerHeight);
}

/// <summary>
/// Cache for style computations.
/// </summary>
public class StyleCache : IComputationCache<StyleCacheKey, ICssStyleDeclaration>
{
    private readonly LayoutEngineCache<StyleCacheKey, ICssStyleDeclaration> _cache = new();
    private readonly CacheDependencyTracker _dependencyTracker;

    public StyleCache(CacheDependencyTracker? dependencyTracker = null)
    {
        _dependencyTracker = dependencyTracker ?? new CacheDependencyTracker();
    }

    public int Count => _cache.Count;

    public void AddOrUpdate(StyleCacheKey key, ICssStyleDeclaration value)
    {
        _cache.AddOrUpdate(key, value);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public ICssStyleDeclaration GetOrAdd(StyleCacheKey key, Func<StyleCacheKey, ICssStyleDeclaration> valueFactory)
    {
        // Track relationship with parent for invalidation
        var parent = key.Element.ParentElement;
        if (parent != null)
        {
            _dependencyTracker.TrackDependency(
                key.Element,
                parent,
                CacheDependencyOptions.TrackElementDependencies |
                CacheDependencyOptions.TrackStyleDependencies);
        }

        // Track document dependency
        var document = key.Element.OwnerDocument;
        if (document != null)
        {
            _dependencyTracker.TrackDocumentDependency(key.Element, document);
        }

        return _cache.GetOrAdd(key, valueFactory);
    }

    public bool Remove(StyleCacheKey key)
    {
        return _cache.Remove(key);
    }

    public bool TryGetValue(StyleCacheKey key, out ICssStyleDeclaration value)
    {
        return _cache.TryGetValue(key, out value!);
    }

    /// <summary>
    /// Invalidates the entire style cache.
    /// </summary>
    public void Invalidate()
    {
        _cache.Invalidate();
    }

    /// <summary>
    /// Invalidates cache entries for a specific element and its dependents.
    /// </summary>
    public void InvalidateElement(IElement element)
    {
        // Remove direct entry
        _cache.Remove(new StyleCacheKey(element));

        // Also remove pseudo-element entries
        _cache.Remove(new StyleCacheKey(element, "::before"));
        _cache.Remove(new StyleCacheKey(element, "::after"));

        // Invalidate style dependents
        foreach (var dependent in _dependencyTracker.GetStyleDependents(element))
        {
            _cache.Remove(new StyleCacheKey(dependent));
        }
    }

    /// <summary>
    /// Invalidates cache entries for elements in a document.
    /// </summary>
    public void InvalidateDocument(IDocument document)
    {
        foreach (var element in _dependencyTracker.GetDocumentDependents(document))
        {
            InvalidateElement(element);
        }
    }
}

/// <summary>
/// Cache for element layout calculations.
/// </summary>
public class LayoutBoxCache<TLayoutData> : IComputationCache<LayoutCacheKey, TLayoutData>
{
    private readonly LayoutEngineCache<LayoutCacheKey, TLayoutData> _cache = new();
    private readonly CacheDependencyTracker _dependencyTracker;

    public LayoutBoxCache(CacheDependencyTracker? dependencyTracker = null)
    {
        _dependencyTracker = dependencyTracker ?? new CacheDependencyTracker();
    }

    public int Count => _cache.Count;

    public void AddOrUpdate(LayoutCacheKey key, TLayoutData value)
    {
        _cache.AddOrUpdate(key, value);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public TLayoutData GetOrAdd(LayoutCacheKey key, Func<LayoutCacheKey, TLayoutData> valueFactory)
    {
        // Track relationship with parent for layout invalidation
        var parent = key.Element.ParentElement;
        if (parent != null)
        {
            _dependencyTracker.TrackDependency(
                key.Element,
                parent,
                CacheDependencyOptions.TrackElementDependencies |
                CacheDependencyOptions.TrackLayoutDependencies);
        }

        return _cache.GetOrAdd(key, valueFactory);
    }

    public bool Remove(LayoutCacheKey key)
    {
        return _cache.Remove(key);
    }

    public bool TryGetValue(LayoutCacheKey key, out TLayoutData value)
    {
        return _cache.TryGetValue(key, out value!);
    }

    /// <summary>
    /// Invalidates the entire layout cache.
    /// </summary>
    public void Invalidate()
    {
        _cache.Invalidate();
    }

    /// <summary>
    /// Invalidates layout cache entries for a specific element and its dependents.
    /// </summary>
    public void InvalidateElement(IElement element)
    {
        // Invalidate all sizes for this element
        // In a real implementation, we might be more selective based on container sizes
        var keysToRemove = new List<LayoutCacheKey>();

        // Invalidate layouts that depend on this element
        foreach (var dependent in _dependencyTracker.GetLayoutDependents(element))
        {
            // Invalidate all sizes for dependent elements
            // In a real implementation, this would be more selective
            keysToRemove.Add(new LayoutCacheKey(dependent, 0, 0));
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
    }
}

/// <summary>
/// Central cache manager for the layout engine.
/// </summary>
public class LayoutEngineCacheManager
{
    private readonly CacheDependencyTracker _dependencyTracker = new();
    private readonly StyleCache _styleCache;
    private readonly Dictionary<Type, object> _layoutCaches = new();

    public LayoutEngineCacheManager()
    {
        _styleCache = new StyleCache(_dependencyTracker);
    }

    /// <summary>
    /// Gets the style cache.
    /// </summary>
    public StyleCache StyleCache => _styleCache;

    /// <summary>
    /// Gets a layout cache for the specified layout data type.
    /// </summary>
    /// <typeparam name="TLayoutData">The type of layout data to cache.</typeparam>
    /// <returns>The layout cache for the specified type.</returns>
    public LayoutBoxCache<TLayoutData> GetLayoutCache<TLayoutData>()
    {
        var type = typeof(TLayoutData);
        if (!_layoutCaches.TryGetValue(type, out var cache))
        {
            cache = new LayoutBoxCache<TLayoutData>(_dependencyTracker);
            _layoutCaches[type] = cache;
        }

        return (LayoutBoxCache<TLayoutData>)cache;
    }

    /// <summary>
    /// Invalidates all caches.
    /// </summary>
    public void InvalidateAll()
    {
        _styleCache.Invalidate();

        foreach (var cache in _layoutCaches.Values)
        {
            if (cache is IComputationCache<LayoutCacheKey, object> layoutCache)
            {
                layoutCache.Clear();
            }
        }

        _dependencyTracker.Clear();
    }

    /// <summary>
    /// Invalidates caches for a specific element.
    /// </summary>
    public void InvalidateElement(IElement element)
    {
        _styleCache.InvalidateElement(element);

        foreach (var cache in _layoutCaches.Values)
        {
            if (cache is LayoutBoxCache<object> layoutCache)
            {
                layoutCache.InvalidateElement(element);
            }
        }
    }

    /// <summary>
    /// Invalidates styles for a document.
    /// </summary>
    public void InvalidateDocument(IDocument document)
    {
        _styleCache.InvalidateDocument(document);
    }
}