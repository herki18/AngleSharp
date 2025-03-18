using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.CacheManager.API.Caching;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Invalidation;
using Infrastructure.CacheManager.API.Management;

namespace Infrastructure.CacheManager.Internal.Invalidation
{
    /// <summary>
    /// Implementation of ICacheInvalidator for the MemoryCache-based system.
    /// Provides centralized invalidation capabilities for different cache types.
    /// </summary>
    internal sealed class CacheInvalidator : ICacheInvalidator
    {
        private readonly ICacheManager _cacheManager;
        private readonly InvalidationTracker _invalidationTracker;

        public CacheInvalidator(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _invalidationTracker = new InvalidationTracker();
        }

        public bool Invalidate<TKey>(string cacheName, TKey key)
        {
            try
            {
                // Get the cache from the cache manager
                var cache = _cacheManager.GetCache<ICache<TKey, object>>(cacheName);
                return cache.Remove(key);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        public int InvalidateWithDependents<TKey>(string cacheName, TKey key)
        {
            try
            {
                // Try to get the cache as a dependency tracking cache
                var cache = _cacheManager.GetCache<IDependencyTrackingCache<TKey, object>>(cacheName);
                return cache.InvalidateWithDependents(key);
            }
            catch (KeyNotFoundException)
            {
                return 0;
            }
            catch (InvalidCastException)
            {
                // Fall back to simple invalidation if the cache doesn't support dependencies
                if (Invalidate(cacheName, key))
                {
                    return 1;
                }
                return 0;
            }
        }

        public int InvalidateMultiple<TKey>(string cacheName, IEnumerable<TKey> keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            try
            {
                var cache = _cacheManager.GetCache<ICache<TKey, object>>(cacheName);
                int count = 0;

                // Invalidate each key individually
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
                return 0;
            }
            catch (InvalidCastException)
            {
                return 0;
            }
        }

        public int InvalidateByScope(InvalidationScope scope)
        {
            switch (scope)
            {
                case InvalidationScope.AllCaches:
                    _cacheManager.ClearAllCaches();
                    return -1; // Special value indicating all caches cleared

                case InvalidationScope.Style:
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
                    return 0;
            }
        }

        public void TrackDependency<TKey>(string cacheName, TKey dependentKey, TKey dependencyKey)
        {
            try
            {
                // Try to get the cache and add the dependency
                var cache = _cacheManager.GetCache<IDependencyTrackingCache<TKey, object>>(cacheName);
                cache.AddDependencies(dependentKey, new[] { dependencyKey });
            }
            catch (Exception)
            {
                // Fall back to the internal tracker if the cache doesn't support dependencies
                _invalidationTracker.TrackDependency(cacheName, dependentKey, dependencyKey);
            }
        }

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
                // Fall back to the internal tracker
                foreach (var dependencyKey in dependencyKeys)
                {
                    _invalidationTracker.TrackDependency(cacheName, dependentKey, dependencyKey);
                }
            }
        }
    }

    /// <summary>
    /// Internal tracker for dependencies when the cache doesn't support dependency tracking directly.
    /// </summary>
    internal sealed class InvalidationTracker
    {
        private readonly Dictionary<string, Dictionary<object, HashSet<object>>> _dependencyMap =
            new Dictionary<string, Dictionary<object, HashSet<object>>>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lockObj = new object();

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
                if (!_dependencyMap.TryGetValue(cacheName, out var cacheMap))
                {
                    cacheMap = new Dictionary<object, HashSet<object>>();
                    _dependencyMap[cacheName] = cacheMap;
                }

                if (!cacheMap.TryGetValue(dependencyKey, out var dependents))
                {
                    dependents = new HashSet<object>();
                    cacheMap[dependencyKey] = dependents;
                }

                dependents.Add(dependentKey);
            }
        }

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

        public void Clear()
        {
            lock (_lockObj)
            {
                _dependencyMap.Clear();
            }
        }
    }
}