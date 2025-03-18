namespace Infrastructure.CacheManager.Internal.Invalidation;

using System;
using System.Collections.Generic;
using System.Linq;
using API.Caching;
using API.Caching.CacheTypes;
using API.Invalidation;
using API.Management;

/// <summary>
/// Implementation of ICacheInvalidator that provides mechanisms for invalidating caches.
/// </summary>
internal class CacheInvalidator : ICacheInvalidator
{
    private readonly ICacheManager _cacheManager;
    private readonly InvalidationTracker _invalidationTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheInvalidator"/> class.
    /// </summary>
    /// <param name="cacheManager">The cache manager.</param>
    public CacheInvalidator(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _invalidationTracker = new InvalidationTracker();
    }

    /// <summary>
    /// Invalidates a specific key in a cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>True if the key was found and invalidated, false otherwise.</returns>
    public bool Invalidate<TKey>(string cacheName, TKey key)
    {
        // Try to get the cache
        try
        {
            var cache = _cacheManager.GetCache<ICache<TKey, object>>(cacheName);
            return cache.Remove(key);
        }
        catch (KeyNotFoundException)
        {
            // Cache doesn't exist
            return false;
        }
        catch (InvalidCastException)
        {
            // Cache exists but has incompatible key/value types
            return false;
        }
    }

    /// <summary>
    /// Invalidates a specific key and all its dependents in a cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>The number of entries invalidated.</returns>
    public int InvalidateWithDependents<TKey>(string cacheName, TKey key)
    {
        // Try to get the cache
        try
        {
            var cache = _cacheManager.GetCache<IDependencyTrackingCache<TKey, object>>(cacheName);
            return cache.InvalidateWithDependents(key);
        }
        catch (KeyNotFoundException)
        {
            // Cache doesn't exist
            return 0;
        }
        catch (InvalidCastException)
        {
            // Cache exists but is not a dependency tracking cache or has incompatible key/value types
            // Try to invalidate just the single key
            if (Invalidate(cacheName, key))
            {
                return 1;
            }
            return 0;
        }
    }

    /// <summary>
    /// Invalidates multiple keys in a cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="keys">The keys to invalidate.</param>
    /// <returns>The number of entries invalidated.</returns>
    public int InvalidateMultiple<TKey>(string cacheName, IEnumerable<TKey> keys)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        // Try to get the cache
        try
        {
            var cache = _cacheManager.GetCache<ICache<TKey, object>>(cacheName);
            int count = 0;

            foreach (var key in keys)
            {
                if (cache.Remove(key))
                {
                    count++;
                }
            }

            return count;
        }
        catch (KeyNotFoundException)
        {
            // Cache doesn't exist
            return 0;
        }
        catch (InvalidCastException)
        {
            // Cache exists but has incompatible key/value types
            return 0;
        }
    }

    /// <summary>
    /// Invalidates caches by scope.
    /// </summary>
    /// <param name="scope">The invalidation scope.</param>
    /// <returns>The number of entries invalidated.</returns>
    public int InvalidateByScope(InvalidationScope scope)
    {
        switch (scope)
        {
            case InvalidationScope.AllCaches:
                _cacheManager.ClearAllCaches();
                return -1; // -1 indicates all entries were cleared

            case InvalidationScope.Style:
                // Clear style-related caches
                int styleCount = 0;
                foreach (var cache in _cacheManager.GetAllCaches())
                {
                    if (cache.Name.Contains("Style", StringComparison.OrdinalIgnoreCase))
                    {
                        cache.Clear();
                        styleCount += cache.Count;
                    }
                }
                return styleCount;

            case InvalidationScope.Layout:
                // Clear layout-related caches
                int layoutCount = 0;
                foreach (var cache in _cacheManager.GetAllCaches())
                {
                    if (cache.Name.Contains("Layout", StringComparison.OrdinalIgnoreCase))
                    {
                        cache.Clear();
                        layoutCount += cache.Count;
                    }
                }
                return layoutCount;

            case InvalidationScope.Render:
                // Clear render-related caches
                int renderCount = 0;
                foreach (var cache in _cacheManager.GetAllCaches())
                {
                    if (cache.Name.Contains("Render", StringComparison.OrdinalIgnoreCase))
                    {
                        cache.Clear();
                        renderCount += cache.Count;
                    }
                }
                return renderCount;

            case InvalidationScope.Resource:
                // Clear resource-related caches
                int resourceCount = 0;
                foreach (var cache in _cacheManager.GetAllCaches())
                {
                    if (cache.Name.Contains("Resource", StringComparison.OrdinalIgnoreCase))
                    {
                        cache.Clear();
                        resourceCount += cache.Count;
                    }
                }
                return resourceCount;

            default:
                // Other scopes require element-specific information, which is not provided in this API
                // They should be handled by specialized invalidators
                return 0;
        }
    }

    /// <summary>
    /// Tracks a dependency relationship between objects.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="dependentKey">The key of the dependent object.</param>
    /// <param name="dependencyKey">The key of the dependency object.</param>
    public void TrackDependency<TKey>(string cacheName, TKey dependentKey, TKey dependencyKey)
    {
        try
        {
            var cache = _cacheManager.GetCache<IDependencyTrackingCache<TKey, object>>(cacheName);
            cache.AddDependencies(dependentKey, new[] { dependencyKey });
        }
        catch (Exception)
        {
            // If the cache doesn't exist or isn't a dependency tracking cache, track it in our own tracker
            _invalidationTracker.TrackDependency(cacheName, dependentKey, dependencyKey);
        }
    }

    /// <summary>
    /// Tracks multiple dependency relationships.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="dependentKey">The key of the dependent object.</param>
    /// <param name="dependencyKeys">The keys of the dependency objects.</param>
    public void TrackDependencies<TKey>(string cacheName, TKey dependentKey, IEnumerable<TKey> dependencyKeys)
    {
        if (dependencyKeys == null)
            throw new ArgumentNullException(nameof(dependencyKeys));

        try
        {
            var cache = _cacheManager.GetCache<IDependencyTrackingCache<TKey, object>>(cacheName);
            cache.AddDependencies(dependentKey, dependencyKeys);
        }
        catch (Exception)
        {
            // If the cache doesn't exist or isn't a dependency tracking cache, track it in our own tracker
            foreach (var dependencyKey in dependencyKeys)
            {
                _invalidationTracker.TrackDependency(cacheName, dependentKey, dependencyKey);
            }
        }
    }
}

/// <summary>
/// Tracks dependencies between objects that aren't in dependency tracking caches.
/// </summary>
internal class InvalidationTracker
{
    // Data structure: Dictionary<CacheName, Dictionary<DependencyKey, HashSet<DependentKey>>>
    private readonly Dictionary<string, Dictionary<object, HashSet<object>>> _dependencyMap =
        new Dictionary<string, Dictionary<object, HashSet<object>>>(StringComparer.OrdinalIgnoreCase);

    private readonly object _lockObj = new object();

    /// <summary>
    /// Tracks a dependency relationship.
    /// </summary>
    public void TrackDependency<TKey>(string cacheName, TKey dependentKey, TKey dependencyKey)
    {
        if (dependencyKey == null)
        {
            throw new ArgumentNullException(nameof(dependencyKey), "Dependency key cannot be null");
        }

        if (dependentKey == null)
        {
            throw new ArgumentNullException(nameof(dependentKey), "Dependent key cannot be null");
        }

        lock (_lockObj)
        {
            // Get or create the dependency map for this cache
            if (!_dependencyMap.TryGetValue(cacheName, out var cacheMap))
            {
                cacheMap = new Dictionary<object, HashSet<object>>();
                _dependencyMap[cacheName] = cacheMap;
            }

            // Get or create the dependents set for this dependency
            if (!cacheMap.TryGetValue(dependencyKey, out var dependents))
            {
                dependents = new HashSet<object>();
                cacheMap[dependencyKey] = dependents;
            }

            // Add the dependent
            dependents.Add(dependentKey);
        }
    }

    /// <summary>
    /// Gets the keys of all objects that depend on the specified object.
    /// </summary>
    public IReadOnlyCollection<object> GetDependents(string cacheName, object dependencyKey)
    {
        lock (_lockObj)
        {
            if (_dependencyMap.TryGetValue(cacheName, out var cacheMap) &&
                cacheMap.TryGetValue(dependencyKey, out var dependents))
            {
                return dependents.ToList().AsReadOnly();
            }

            return Array.Empty<object>();
        }
    }

    /// <summary>
    /// Clears all tracked dependencies.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _dependencyMap.Clear();
        }
    }
}