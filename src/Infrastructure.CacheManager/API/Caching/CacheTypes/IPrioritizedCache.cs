namespace Infrastructure.CacheManager.API.Caching.CacheTypes;

using System;
using Models;

/// <summary>
/// Represents a cache that supports priority-based entry eviction.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
public interface IPrioritizedCache<TKey, TValue> : ICache<TKey, TValue>
{
    /// <summary>
    /// Gets or creates a cache entry with the specified priority.
    /// </summary>
    /// <param name="key">The key to look up or create.</param>
    /// <param name="valueFactory">The factory function to create the value if not found.</param>
    /// <param name="priority">The priority to assign to the cache entry.</param>
    /// <returns>The cached or created value.</returns>
    TValue GetOrCreate(TKey key, Func<TKey, TValue> valueFactory, CacheEntryPriority priority);

    /// <summary>
    /// Sets a value in the cache with the specified priority.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="priority">The priority to assign to the cache entry.</param>
    /// <param name="options">Optional cache entry options.</param>
    void Set(TKey key, TValue value, CacheEntryPriority priority, CacheEntryOptions? options = null);

    /// <summary>
    /// Trims cache entries with priority lower than or equal to the specified minimum priority.
    /// </summary>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 100.</param>
    /// <param name="minimumPriority">The minimum priority level to consider for trimming.</param>
    /// <returns>The number of entries removed.</returns>
    int TrimByPriority(double percentage, CacheEntryPriority minimumPriority);
}