using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Infrastructure.CacheManager.API.Management;

namespace Infrastructure.CacheManager.Internal.Core;

internal sealed class MemoryCacheManager : ICacheManager, IDisposable
{
    private readonly ConcurrentDictionary<string, CacheRegistration> _caches;
    private readonly ReaderWriterLockSlim _cacheLock;
    private readonly Timer? _statisticsTimer;
    private readonly TimeSpan _statisticsInterval;
    private bool _isDisposed;

    public MemoryCacheManager(TimeSpan? statisticsInterval = null)
    {
        _caches = new ConcurrentDictionary<string, CacheRegistration>(StringComparer.OrdinalIgnoreCase);
        _cacheLock = new ReaderWriterLockSlim();
        _statisticsInterval = statisticsInterval ?? TimeSpan.FromMinutes(5);
        _statisticsTimer = new Timer(CollectStatistics, null, _statisticsInterval, _statisticsInterval);
    }

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

            var registration = new CacheRegistration(cache);
            _caches[cacheName] = registration;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    public T GetCache<T>(string cacheName) where T : ITrimableCache
    {
        if (string.IsNullOrWhiteSpace(cacheName))
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(cacheName));

        _cacheLock.EnterReadLock();
        try
        {
            if (_caches.TryGetValue(cacheName, out var registration))
            {
                if (registration.Cache is T typedCache)
                    return typedCache;

                throw new InvalidCastException($"Cache '{cacheName}' is of type '{registration.Cache.GetType().Name}' which cannot be cast to '{typeof(T).Name}'.");
            }
            throw new KeyNotFoundException($"No cache with the name '{cacheName}' is registered.");
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public IReadOnlyCollection<ITrimableCache> GetAllCaches()
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _caches.Values.Select(r => r.Cache).ToList().AsReadOnly();
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

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
            var cachesByPriority = _caches.Values
                .GroupBy(r => r.Cache.Priority)
                .OrderBy(g => (int)g.Key)
                .ToList();

            foreach (var group in cachesByPriority)
            {
                foreach (var registration in group)
                {
                    totalTrimmed += registration.Cache.Trim(percentage);
                }
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        return totalTrimmed;
    }

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
            var cachesToTrim = _caches.Values
                .Where(r => (int)r.Cache.Priority <= (int)minimumPriority)
                .OrderBy(r => (int)r.Cache.Priority)
                .ToList();

            foreach (var registration in cachesToTrim)
            {
                totalTrimmed += registration.Cache.Trim(percentage);
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        return totalTrimmed;
    }

    public void ClearAllCaches()
    {
        _cacheLock.EnterReadLock();
        try
        {
            foreach (var registration in _caches.Values)
            {
                registration.Cache.Clear();
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

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

    public long GetTotalCacheSize()
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _caches.Values.Sum(r => r.Cache.EstimatedSize);
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public int GetTotalEntryCount()
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _caches.Values.Sum(r => r.Cache.Count);
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    private void CollectStatistics(object? state)
    {
        try
        {
            // Just collect cache statistics without automatic trimming
            long totalSize = GetTotalCacheSize();
            int totalEntries = GetTotalEntryCount();

            // Future enhancement: Implement size threshold-based trimming here if needed
        }
        catch (Exception)
        {
            // Silently handle exceptions during statistics collection
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _statisticsTimer?.Dispose();

        _cacheLock.EnterWriteLock();
        try
        {
            foreach (var registration in _caches.Values)
            {
                if (registration.Cache is IDisposable disposable)
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

    private readonly struct CacheRegistration
    {
        public ITrimableCache Cache { get; }

        public CacheRegistration(ITrimableCache cache)
        {
            Cache = cache;
        }
    }
}