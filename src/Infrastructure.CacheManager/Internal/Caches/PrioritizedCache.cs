namespace Infrastructure.CacheManager.Internal.Caches;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using API.Caching.CacheTypes;
using API.Management;
using API.Models;

/// <summary>
/// A cache implementation that supports priority-based entry eviction.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
internal class PrioritizedCache<TKey, TValue> : Cache<TKey, TValue>, IPrioritizedCache<TKey, TValue>
{
    private readonly ConcurrentDictionary<CacheEntryPriority, ConcurrentBag<TKey>> _priorityIndex;
    private readonly ReaderWriterLockSlim _priorityLock = new ReaderWriterLockSlim();

    /// <summary>
    /// Initializes a new instance of the <see cref="PrioritizedCache{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="name">The name of the cache.</param>
    /// <param name="priority">The priority of the cache.</param>
    /// <param name="cleanupInterval">The interval at which to clean up expired entries.</param>
    public PrioritizedCache(string name, CachePriority priority = CachePriority.Normal, TimeSpan? cleanupInterval = null)
        : base(name, priority, cleanupInterval)
    {
        _priorityIndex = new ConcurrentDictionary<CacheEntryPriority, ConcurrentBag<TKey>>();

        // Initialize priority buckets
        _priorityIndex[CacheEntryPriority.Low] = new ConcurrentBag<TKey>();
        _priorityIndex[CacheEntryPriority.Normal] = new ConcurrentBag<TKey>();
        _priorityIndex[CacheEntryPriority.High] = new ConcurrentBag<TKey>();
        _priorityIndex[CacheEntryPriority.Critical] = new ConcurrentBag<TKey>();
    }

    /// <summary>
    /// Gets or creates a cache entry with the specified priority.
    /// </summary>
    /// <param name="key">The key to look up or create.</param>
    /// <param name="valueFactory">The factory function to create the value if not found.</param>
    /// <param name="priority">The priority to assign to the cache entry.</param>
    /// <returns>The cached or created value.</returns>
    public TValue GetOrCreate(TKey key, Func<TKey, TValue> valueFactory, CacheEntryPriority priority)
    {
        if (TryGetValue(key, out var value))
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return value;
        }

        // Create the value
        value = valueFactory(key);

        // Add to cache with priority
        Set(key, value, priority);

        return value;
    }

    /// <summary>
    /// Sets a value in the cache with the specified priority.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="priority">The priority to assign to the cache entry.</param>
    /// <param name="options">Optional cache entry options.</param>
    public void Set(TKey key, TValue value, CacheEntryPriority priority, CacheEntryOptions? options = null)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null");
        }

        // Create entry with priority
        var entry = new CacheEntry<TValue>(key, value, options, priority);

        // Track the entry with its priority
        TrackEntryPriority(key, priority);

        // Add to cache
        base.Set(key, value, options);
    }

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="options">Optional cache entry options.</param>
    public override void Set(TKey key, TValue value, CacheEntryOptions? options = null)
    {
        // Default to normal priority
        Set(key, value, CacheEntryPriority.Normal, options);
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the key was found and removed, false otherwise.</returns>
    public override bool Remove(TKey key)
    {
        // First remove from base cache
        bool removed = base.Remove(key);

        if (removed)
        {
            // We don't need to actually remove from priority bags as it's just a performance optimization
            // and the bags will be recreated during trimming if needed
        }

        return removed;
    }

    /// <summary>
    /// Trims cache entries with priority lower than or equal to the specified minimum priority.
    /// </summary>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 100.</param>
    /// <param name="minimumPriority">The minimum priority level to consider for trimming.</param>
    /// <returns>The number of entries removed.</returns>
    public int TrimByPriority(double percentage, CacheEntryPriority minimumPriority)
    {
        if (percentage <= 0)
            return 0;

        if (percentage > 100)
            percentage = 100;

        _priorityLock.EnterWriteLock();
        try
        {
            // Rebuild priority index to ensure it's accurate
            RebuildPriorityIndex();

            // Get all keys with priority less than or equal to the specified minimum
            var keysToTrim = new List<TKey>();

            // Add keys from each applicable priority level
            foreach (var priorityLevel in GetPriorityLevelsToTrim(minimumPriority))
            {
                if (_priorityIndex.TryGetValue(priorityLevel, out var keys))
                {
                    keysToTrim.AddRange(keys);
                }
            }

            if (keysToTrim.Count == 0)
                return 0;

            // Determine how many entries to remove
            int removeCount = (int)Math.Ceiling(keysToTrim.Count * percentage / 100);

            if (removeCount <= 0)
                return 0;

            // Randomize to avoid patterns of removal
            Random random = new Random();
            var shuffled = keysToTrim.OrderBy(x => random.Next()).Take(removeCount).ToList();

            // Remove entries
            int removedCount = 0;
            foreach (var key in shuffled)
            {
                if (base.Remove(key))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }
        finally
        {
            _priorityLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Tracks an entry's priority in the priority index.
    /// </summary>
    /// <param name="key">The key to track.</param>
    /// <param name="priority">The priority of the entry.</param>
    private void TrackEntryPriority(TKey key, CacheEntryPriority priority)
    {
        _priorityLock.EnterWriteLock();
        try
        {
            // Ensure the priority bucket exists
            if (!_priorityIndex.TryGetValue(priority, out var keys))
            {
                keys = new ConcurrentBag<TKey>();
                _priorityIndex[priority] = keys;
            }

            // Add the key to the appropriate priority bucket
            keys.Add(key);
        }
        finally
        {
            _priorityLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Rebuilds the priority index by examining all cache entries.
    /// </summary>
    private void RebuildPriorityIndex()
    {
        // Initialize empty priority buckets
        foreach (CacheEntryPriority priority in Enum.GetValues(typeof(CacheEntryPriority)))
        {
            _priorityIndex[priority] = new ConcurrentBag<TKey>();
        }

        // Scan all entries and re-index
        foreach (var entry in GetAllEntries())
        {
            if (!entry.HasExpired())
            {
                _priorityIndex[entry.Priority].Add((TKey)entry.Key);
            }
        }
    }

    /// <summary>
    /// Gets all priority levels that should be trimmed based on the specified minimum.
    /// </summary>
    /// <param name="minimumPriority">The minimum priority level to trim.</param>
    /// <returns>An enumerable of priority levels to trim.</returns>
    private IEnumerable<CacheEntryPriority> GetPriorityLevelsToTrim(CacheEntryPriority minimumPriority)
    {
        switch (minimumPriority)
        {
            case CacheEntryPriority.Low:
                yield return CacheEntryPriority.Low;
                break;

            case CacheEntryPriority.Normal:
                yield return CacheEntryPriority.Low;
                yield return CacheEntryPriority.Normal;
                break;

            case CacheEntryPriority.High:
                yield return CacheEntryPriority.Low;
                yield return CacheEntryPriority.Normal;
                yield return CacheEntryPriority.High;
                break;

            case CacheEntryPriority.Critical:
                yield return CacheEntryPriority.Low;
                yield return CacheEntryPriority.Normal;
                yield return CacheEntryPriority.High;
                yield return CacheEntryPriority.Critical;
                break;
        }
    }

    /// <summary>
    /// Gets all cache entries.
    /// </summary>
    /// <returns>An enumerable of all cache entries.</returns>
    private IEnumerable<CacheEntry<TValue>> GetAllEntries()
    {
        // This method needs to be implemented in the base Cache class
        return Enumerable.Empty<CacheEntry<TValue>>();
    }

    /// <summary>
    /// Disposes resources used by the cache.
    /// </summary>
    public override void Dispose()
    {
        _priorityLock?.Dispose();
        base.Dispose();
    }
}