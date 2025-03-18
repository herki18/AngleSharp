namespace Infrastructure.CacheManager.API.Caching.CacheTypes;

using System.Collections.Generic;

/// <summary>
/// Represents a cache that tracks dependencies between entries for intelligent invalidation.
/// When a dependency is invalidated, all dependent entries are also invalidated.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
public interface IDependencyTrackingCache<TKey, TValue> : ICache<TKey, TValue>
{
    /// <summary>
    /// Sets a value in the cache with dependencies.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="dependencies">The keys of other cache entries this entry depends on.</param>
    void SetWithDependencies(TKey key, TValue value, IEnumerable<TKey> dependencies);

    /// <summary>
    /// Adds dependencies to an existing cache entry.
    /// </summary>
    /// <param name="key">The key of the cache entry.</param>
    /// <param name="dependencies">The keys of other cache entries this entry depends on.</param>
    /// <returns>True if the key was found and dependencies added, false otherwise.</returns>
    bool AddDependencies(TKey key, IEnumerable<TKey> dependencies);

    /// <summary>
    /// Gets the keys of all cache entries that depend on the specified key.
    /// </summary>
    /// <param name="key">The dependency key to look up.</param>
    /// <returns>A collection of dependent keys.</returns>
    IReadOnlyCollection<TKey> GetDependents(TKey key);

    /// <summary>
    /// Invalidates the specified key and all entries that depend on it, directly or indirectly.
    /// </summary>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>The total number of entries invalidated.</returns>
    int InvalidateWithDependents(TKey key);

    /// <summary>
    /// Invalidates multiple keys and all entries that depend on them, directly or indirectly.
    /// </summary>
    /// <param name="keys">The keys to invalidate.</param>
    /// <returns>The total number of entries invalidated.</returns>
    int InvalidateWithDependents(IEnumerable<TKey> keys);

    /// <summary>
    /// Gets the keys of all cache entries that this entry depends on.
    /// </summary>
    /// <param name="key">The key to look up dependencies for.</param>
    /// <returns>A collection of dependency keys.</returns>
    IReadOnlyCollection<TKey> GetDependencies(TKey key);
}