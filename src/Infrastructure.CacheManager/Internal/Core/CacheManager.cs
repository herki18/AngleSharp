namespace Infrastructure.CacheManager.Internal.Core;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using API.Management;
using API.Monitoring;

/// <summary>
/// Implementation of ICacheManager that provides centralized cache management.
/// </summary>
internal class CacheManager : ICacheManager, IDisposable
{
    private readonly ConcurrentDictionary<string, ITrimableCache> _caches;
    private readonly ReaderWriterLockSlim _cacheLock;
    private readonly IMemoryPressureMonitor _memoryPressureMonitor;
    private readonly Timer _statisticsTimer;
    private readonly TimeSpan _statisticsInterval;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheManager"/> class.
    /// </summary>
    /// <param name="memoryPressureMonitor">The memory pressure monitor to use.</param>
    /// <param name="statisticsInterval">The interval at which to collect cache statistics.</param>
    public CacheManager(IMemoryPressureMonitor memoryPressureMonitor, TimeSpan? statisticsInterval = null)
    {
        _caches = new ConcurrentDictionary<string, ITrimableCache>(StringComparer.OrdinalIgnoreCase);
        _cacheLock = new ReaderWriterLockSlim();
        _memoryPressureMonitor = memoryPressureMonitor ?? throw new ArgumentNullException(nameof(memoryPressureMonitor));
        _statisticsInterval = statisticsInterval ?? TimeSpan.FromMinutes(5);

        // Subscribe to memory pressure events
        _memoryPressureMonitor.MemoryPressureDetected += OnMemoryPressureDetected;

        // Start statistics collection timer
        _statisticsTimer = new Timer(CollectStatistics, null, _statisticsInterval, _statisticsInterval);

        // Start memory pressure monitoring
        _memoryPressureMonitor.StartMonitoring();
    }

    /// <summary>
    /// Registers a cache with the cache manager.
    /// </summary>
    /// <param name="cacheName">A unique name for the cache.</param>
    /// <param name="cache">The cache instance to register.</param>
    public void RegisterCache(string cacheName, ITrimableCache cache)
    {
        if (string.IsNullOrWhiteSpace(cacheName))
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(cacheName));

        if (cache == null)
            throw new ArgumentNullException(nameof(cache));

        _cacheLock.EnterWriteLock();
        try
        {
            if (_caches.ContainsKey(cacheName))
                throw new ArgumentException($"A cache with the name '{cacheName}' is already registered.", nameof(cacheName));

            _caches[cacheName] = cache;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets a registered cache by name.
    /// </summary>
    /// <typeparam name="T">The expected type of the cache.</typeparam>
    /// <param name="cacheName">The name of the cache to retrieve.</param>
    /// <returns>The requested cache instance.</returns>
    public T GetCache<T>(string cacheName) where T : ITrimableCache
    {
        if (string.IsNullOrWhiteSpace(cacheName))
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(cacheName));

        _cacheLock.EnterReadLock();
        try
        {
            if (_caches.TryGetValue(cacheName, out var cache))
            {
                if (cache is T typedCache)
                    return typedCache;

                throw new InvalidCastException($"Cache '{cacheName}' is of type '{cache.GetType().Name}' which cannot be cast to '{typeof(T).Name}'.");
            }

            throw new KeyNotFoundException($"No cache with the name '{cacheName}' is registered.");
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets all registered caches.
    /// </summary>
    /// <returns>A collection of all registered cache instances.</returns>
    public IReadOnlyCollection<ITrimableCache> GetAllCaches()
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _caches.Values.ToList().AsReadOnly();
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Trims all registered caches by the specified percentage.
    /// </summary>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 100.</param>
    /// <returns>The total number of entries removed across all caches.</returns>
    public int TrimAllCaches(double percentage)
    {
        if (percentage <= 0)
            return 0;

        if (percentage > 100)
            percentage = 100;

        int totalTrimmed = 0;

        _cacheLock.EnterReadLock();
        try
        {
            // Group caches by priority for optimal trimming
            var cachesByPriority = _caches.Values
                .GroupBy(c => c.Priority)
                .OrderBy(g => (int)g.Key)
                .ToList();

            foreach (var group in cachesByPriority)
            {
                foreach (var cache in group)
                {
                    totalTrimmed += cache.Trim(percentage);
                }
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        return totalTrimmed;
    }

    /// <summary>
    /// Trims caches with priority lower than or equal to the specified minimum priority.
    /// </summary>
    /// <param name="minimumPriority">The minimum priority level to consider for trimming.</param>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 100.</param>
    /// <returns>The total number of entries removed across all affected caches.</returns>
    public int TrimByPriority(CachePriority minimumPriority, double percentage = 100)
    {
        if (percentage <= 0)
            return 0;

        if (percentage > 100)
            percentage = 100;

        int totalTrimmed = 0;

        _cacheLock.EnterReadLock();
        try
        {
            // Select caches with priority <= minimumPriority
            var cachesToTrim = _caches.Values
                .Where(c => (int)c.Priority <= (int)minimumPriority)
                .OrderBy(c => (int)c.Priority)
                .ToList();

            foreach (var cache in cachesToTrim)
            {
                totalTrimmed += cache.Trim(percentage);
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        return totalTrimmed;
    }

    /// <summary>
    /// Clears all entries from all registered caches.
    /// </summary>
    public void ClearAllCaches()
    {
        _cacheLock.EnterReadLock();
        try
        {
            foreach (var cache in _caches.Values)
            {
                cache.Clear();
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Unregisters a cache from the cache manager.
    /// </summary>
    /// <param name="cacheName">The name of the cache to unregister.</param>
    /// <returns>True if the cache was found and unregistered, false otherwise.</returns>
    public bool UnregisterCache(string cacheName)
    {
        if (string.IsNullOrWhiteSpace(cacheName))
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(cacheName));

        _cacheLock.EnterWriteLock();
        try
        {
            return _caches.TryRemove(cacheName, out _);
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the total estimated size of all registered caches.
    /// </summary>
    /// <returns>The total size in bytes.</returns>
    public long GetTotalCacheSize()
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _caches.Values.Sum(c => c.EstimatedSize);
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the total number of entries across all registered caches.
    /// </summary>
    /// <returns>The total entry count.</returns>
    public int GetTotalEntryCount()
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _caches.Values.Sum(c => c.Count);
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Handles memory pressure events.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnMemoryPressureDetected(object sender, MemoryPressureEventArgs e)
    {
        // Implement memory pressure response strategy based on severity
        switch (e.Severity)
        {
            case PressureSeverity.Low:
                // Trim low-priority caches
                TrimByPriority(CachePriority.Low, 50);
                break;

            case PressureSeverity.Medium:
                // Clear low-priority and trim normal priority
                TrimByPriority(CachePriority.Low, 100);
                TrimByPriority(CachePriority.Normal, 50);
                break;

            case PressureSeverity.High:
                // Clear low and normal, trim high
                TrimByPriority(CachePriority.Low, 100);
                TrimByPriority(CachePriority.Normal, 100);
                TrimByPriority(CachePriority.High, 50);
                break;

            case PressureSeverity.Critical:
                // Clear everything except a portion of critical caches
                TrimByPriority(CachePriority.High, 100);
                TrimByPriority(CachePriority.Critical, 50);
                GC.Collect(); // Request garbage collection
                break;
        }

        // TODO: Publish event via EventAggregator to notify other components
    }

    /// <summary>
    /// Collects statistics about the cache system.
    /// </summary>
    /// <param name="state">The timer state.</param>
    private void CollectStatistics(object state)
    {
        try
        {
            long totalSize = GetTotalCacheSize();
            int totalEntries = GetTotalEntryCount();

            // TODO: Log statistics or publish via EventAggregator

            // Check if we're approaching memory pressure
            if (totalSize > _memoryPressureMonitor.MemoryThreshold * 0.8)
            {
                // Proactively trim low-priority caches
                TrimByPriority(CachePriority.Low, 25);
            }
        }
        catch (Exception)
        {
            // Swallow exceptions in timer callback
        }
    }

    /// <summary>
    /// Disposes resources used by the cache manager.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _statisticsTimer?.Dispose();

        // Unsubscribe from events
        if (_memoryPressureMonitor != null)
        {
            _memoryPressureMonitor.MemoryPressureDetected -= OnMemoryPressureDetected;
            _memoryPressureMonitor.Dispose();
        }

        // Dispose all caches
        _cacheLock.EnterWriteLock();
        try
        {
            foreach (var cache in _caches.Values)
            {
                if (cache is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _caches.Clear();
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        _cacheLock?.Dispose();
    }
}