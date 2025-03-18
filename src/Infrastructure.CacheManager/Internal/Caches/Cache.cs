namespace Infrastructure.CacheManager.Internal.Caches;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using API.Caching;
using API.Management;
using API.Models;

/// <summary>
/// Default implementation of ICache with thread-safe operations.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
internal class Cache<TKey, TValue> : ICache<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _entries;
    private readonly ReaderWriterLockSlim _trimLock = new ReaderWriterLockSlim();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cleanupInterval;
    private long _estimatedSize;

    /// <summary>
    /// Gets the name of the cache.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the priority of the cache.
    /// </summary>
    public CachePriority Priority { get; }

    /// <summary>
    /// Gets the number of items in the cache.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Gets the estimated total size of all entries in the cache in bytes.
    /// </summary>
    public long EstimatedSize => Interlocked.Read(ref _estimatedSize);

    /// <summary>
    /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="name">The name of the cache.</param>
    /// <param name="priority">The priority of the cache.</param>
    /// <param name="cleanupInterval">The interval at which to clean up expired entries.</param>
    public Cache(string name, CachePriority priority = CachePriority.Normal, TimeSpan? cleanupInterval = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Priority = priority;
        _entries = new ConcurrentDictionary<TKey, CacheEntry<TValue>>();
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);

        // Start cleanup timer
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);
    }

    /// <summary>
    /// Attempts to get a value from the cache.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The retrieved value if found, default otherwise.</param>
    /// <returns>True if the key was found, false otherwise.</returns>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.HasExpired())
            {
                // Entry has expired, remove it
                Remove(key);
                value = default;
                return false;
            }

            // Update last access time
            entry.RefreshExpiration();
            value = entry.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets a value from the cache if it exists, otherwise creates it using the factory.
    /// </summary>
    /// <param name="key">The key to look up or create.</param>
    /// <param name="valueFactory">The factory function to create the value if not found.</param>
    /// <returns>The cached or created value.</returns>
    public TValue GetOrCreate(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (TryGetValue(key, out var value))
        {
            if (value == null)
            {
                throw new InvalidOperationException("Cache entry value cannot be null");
            }

            return value;
        }

        // Create the value
        value = valueFactory(key);

        // Add to cache
        Set(key, value);

        return value;
    }

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="options">Optional cache entry options.</param>
    public virtual void Set(TKey key, TValue value, CacheEntryOptions? options = null)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // Create a new cache entry
        var entry = new CacheEntry<TValue>(key, value, options);

        // If the entry already exists, remove its size from the total
        if (_entries.TryGetValue(key, out var oldEntry))
        {
            Interlocked.Add(ref _estimatedSize, -oldEntry.Size);
        }

        // Add or update the entry
        _entries[key] = entry;

        // Update the estimated size
        Interlocked.Add(ref _estimatedSize, entry.Size);
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the key was found and removed, false otherwise.</returns>
    public virtual bool Remove(TKey key)
    {
        if (_entries.TryRemove(key, out var entry))
        {
            Interlocked.Add(ref _estimatedSize, -entry.Size);
            entry.Dispose();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    public bool Contains(TKey key)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.HasExpired())
            {
                // Entry has expired, remove it
                Remove(key);
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Trims the cache by removing a percentage of the entries.
    /// </summary>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 100.</param>
    /// <returns>The number of entries removed.</returns>
    public int Trim(double percentage)
    {
        if (percentage <= 0)
            return 0;

        if (percentage >= 100)
        {
            Clear();
            return 0; // Clear() already disposed all entries
        }

        int count = _entries.Count;
        int removeCount = (int)Math.Ceiling(count * percentage / 100);

        if (removeCount <= 0)
            return 0;

        _trimLock.EnterWriteLock();
        try
        {
            // Get entries ordered by last access time (oldest first)
            var entriesToRemove = _entries
                .OrderBy(e => e.Value.LastAccessTime)
                .Take(removeCount)
                .ToList();

            int removedCount = 0;
            foreach (var entry in entriesToRemove)
            {
                if (Remove(entry.Key))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }
        finally
        {
            _trimLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public virtual void Clear()
    {
        _trimLock.EnterWriteLock();
        try
        {
            // Dispose all entries
            foreach (var entry in _entries.Values)
            {
                entry.Dispose();
            }

            _entries.Clear();
            Interlocked.Exchange(ref _estimatedSize, 0);
        }
        finally
        {
            _trimLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Cleans up expired entries periodically.
    /// </summary>
    private void CleanupExpiredEntries(object state)
    {
        // Don't acquire a write lock if there are no entries
        if (_entries.Count == 0)
            return;

        // Try to acquire a write lock without blocking
        if (_trimLock.TryEnterWriteLock(100))
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredEntries = _entries.Where(e => e.Value.HasExpired()).ToList();

                foreach (var expired in expiredEntries)
                {
                    Remove(expired.Key);
                }
            }
            finally
            {
                _trimLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Disposes resources used by the cache.
    /// </summary>
    public virtual void Dispose()
    {
        _cleanupTimer?.Dispose();
        _trimLock?.Dispose();

        Clear();
    }
}