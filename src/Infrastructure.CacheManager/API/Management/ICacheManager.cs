namespace Infrastructure.CacheManager.API.Management;

using System;
using System.Collections.Generic;

/// <summary>
/// Main interface for managing caches in the system.
/// Provides centralized control over cache registration, lookup, and memory management.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Registers a cache with the cache manager.
    /// </summary>
    /// <param name="cacheName">A unique name for the cache.</param>
    /// <param name="cache">The cache instance to register.</param>
    void RegisterCache(string cacheName, ITrimableCache cache);

    /// <summary>
    /// Gets a registered cache by name.
    /// </summary>
    /// <typeparam name="T">The expected type of the cache.</typeparam>
    /// <param name="cacheName">The name of the cache to retrieve.</param>
    /// <returns>The requested cache instance.</returns>
    T GetCache<T>(string cacheName) where T : ITrimableCache;

    /// <summary>
    /// Gets all registered caches.
    /// </summary>
    /// <returns>A collection of all registered cache instances.</returns>
    IReadOnlyCollection<ITrimableCache> GetAllCaches();

    /// <summary>
    /// Trims all registered caches by the specified percentage.
    /// </summary>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 100.</param>
    /// <returns>The total number of entries removed across all caches.</returns>
    int TrimAllCaches(double percentage);

    /// <summary>
    /// Trims caches with priority lower than or equal to the specified minimum priority.
    /// </summary>
    /// <param name="minimumPriority">The minimum priority level to consider for trimming.</param>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 100.</param>
    /// <returns>The total number of entries removed across all affected caches.</returns>
    int TrimByPriority(CachePriority minimumPriority, double percentage = 100);

    /// <summary>
    /// Clears all entries from all registered caches.
    /// </summary>
    void ClearAllCaches();

    /// <summary>
    /// Unregisters a cache from the cache manager.
    /// </summary>
    /// <param name="cacheName">The name of the cache to unregister.</param>
    /// <returns>True if the cache was found and unregistered, false otherwise.</returns>
    bool UnregisterCache(string cacheName);

    /// <summary>
    /// Gets the total estimated size of all registered caches.
    /// </summary>
    /// <returns>The total size in bytes.</returns>
    long GetTotalCacheSize();

    /// <summary>
    /// Gets the total number of entries across all registered caches.
    /// </summary>
    /// <returns>The total entry count.</returns>
    int GetTotalEntryCount();
}