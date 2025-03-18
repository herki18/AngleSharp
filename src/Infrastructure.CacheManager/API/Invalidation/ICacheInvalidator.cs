namespace Infrastructure.CacheManager.API.Invalidation;

using System.Collections.Generic;

/// <summary>
/// Provides mechanisms for invalidating caches based on dependencies and events.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Invalidates a specific key in a cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>True if the key was found and invalidated, false otherwise.</returns>
    bool Invalidate<TKey>(string cacheName, TKey key);

    /// <summary>
    /// Invalidates a specific key and all its dependents in a cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>The number of entries invalidated.</returns>
    int InvalidateWithDependents<TKey>(string cacheName, TKey key);

    /// <summary>
    /// Invalidates multiple keys in a cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="keys">The keys to invalidate.</param>
    /// <returns>The number of entries invalidated.</returns>
    int InvalidateMultiple<TKey>(string cacheName, IEnumerable<TKey> keys);

    /// <summary>
    /// Invalidates caches by scope.
    /// </summary>
    /// <param name="scope">The invalidation scope.</param>
    /// <returns>The number of entries invalidated.</returns>
    int InvalidateByScope(InvalidationScope scope);

    /// <summary>
    /// Tracks a dependency relationship between objects.
    /// When the dependency is invalidated, all dependent objects will also be invalidated.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="dependentKey">The key of the dependent object.</param>
    /// <param name="dependencyKey">The key of the dependency object.</param>
    void TrackDependency<TKey>(string cacheName, TKey dependentKey, TKey dependencyKey);

    /// <summary>
    /// Tracks multiple dependency relationships.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="dependentKey">The key of the dependent object.</param>
    /// <param name="dependencyKeys">The keys of the dependency objects.</param>
    void TrackDependencies<TKey>(string cacheName, TKey dependentKey, IEnumerable<TKey> dependencyKeys);
}