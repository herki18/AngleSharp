using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.API.Models;

namespace Infrastructure.CacheManager.Internal.Caches
{
    using System.Linq;

    /// <summary>
    /// Priority-based implementation of MemoryCache that supports different eviction priorities.
    /// Extends MemoryCacheBase to add prioritization of cache entries.
    /// </summary>
    internal class PrioritizedMemoryCache<TKey, TValue> : MemoryCacheBase<TKey, TValue>, IPrioritizedCache<TKey, TValue>
    {
        // Priority indexes for fast retrieval by priority level
        private readonly ConcurrentDictionary<CacheEntryPriority, ConcurrentHashSet<TKey>> _priorityIndex;
        private readonly ReaderWriterLockSlim _priorityLock = new ReaderWriterLockSlim();

        public PrioritizedMemoryCache(string name, CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null, MemoryCacheOptions? options = null)
            : base(name, priority, cleanupInterval, options)
        {
            _priorityIndex = new ConcurrentDictionary<CacheEntryPriority, ConcurrentHashSet<TKey>>();

            // Initialize priority levels
            _priorityIndex[CacheEntryPriority.Low] = new ConcurrentHashSet<TKey>();
            _priorityIndex[CacheEntryPriority.Normal] = new ConcurrentHashSet<TKey>();
            _priorityIndex[CacheEntryPriority.High] = new ConcurrentHashSet<TKey>();
            _priorityIndex[CacheEntryPriority.Critical] = new ConcurrentHashSet<TKey>();
        }

        public TValue GetOrCreate(TKey key, Func<TKey, TValue> valueFactory, CacheEntryPriority priority)
        {
            if (TryGetValue(key, out var value))
            {
                if (value == null)
                {
                    throw new InvalidOperationException("Cached value cannot be null");
                }
                return value;
            }

            value = valueFactory(key);
            Set(key, value, priority);
            return value;
        }

        public void Set(TKey key, TValue value, CacheEntryPriority priority, CacheEntryOptions? options = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "Cache key cannot be null");
            }

            // Track the entry's priority before setting it in the cache
            TrackEntryPriority(key, priority);

            // Create prioritized options if not provided
            var effectiveOptions = options?.Clone() ?? new CacheEntryOptions();
            var metadata = new PrioritizedEntryMetadata(effectiveOptions.Size ?? EstimateSize(value), effectiveOptions, priority);

            // Use base class to set the value with the updated options
            base.Set(key, value, effectiveOptions);
        }

        public override void Set(TKey key, TValue value, CacheEntryOptions? options = null)
        {
            // Default to Normal priority when not specified
            Set(key, value, CacheEntryPriority.Normal, options);
        }

        public override bool Remove(TKey key)
        {
            if (key == null)
                return false;

            // Remove from priority tracking first
            RemoveFromPriorityIndex(key);

            // Then use base implementation to remove from cache
            return base.Remove(key);
        }

        public int TrimByPriority(double percentage, CacheEntryPriority minimumPriority)
        {
            if (percentage <= 0)
                return 0;

            if (percentage > 100)
                percentage = 100;

            _priorityLock.EnterWriteLock();
            try
            {
                // Get keys to trim based on priority levels
                var keysToTrim = new List<TKey>();

                foreach (var priorityLevel in GetPriorityLevelsToTrim(minimumPriority))
                {
                    if (_priorityIndex.TryGetValue(priorityLevel, out var keys))
                    {
                        keysToTrim.AddRange(keys);
                    }
                }

                if (keysToTrim.Count == 0)
                    return 0;

                // Calculate how many items to remove
                int removeCount = (int)Math.Ceiling(keysToTrim.Count * percentage / 100);
                if (removeCount <= 0)
                    return 0;

                // Randomize removal to avoid patterns
                var random = new Random();
                var shuffled = keysToTrim.OrderBy(x => random.Next()).Take(removeCount).ToList();

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

        public override void Clear()
        {
            _priorityLock.EnterWriteLock();
            try
            {
                // Clear priority indexes
                foreach (var prioritySet in _priorityIndex.Values)
                {
                    prioritySet.Clear();
                }

                // Clear the base cache
                base.Clear();
            }
            finally
            {
                _priorityLock.ExitWriteLock();
            }
        }

        private void TrackEntryPriority(TKey key, CacheEntryPriority priority)
        {
            _priorityLock.EnterWriteLock();
            try
            {
                // Remove from any existing priority index first
                RemoveFromPriorityIndex(key);

                // Add to the appropriate priority index
                if (!_priorityIndex.TryGetValue(priority, out var keys))
                {
                    keys = new ConcurrentHashSet<TKey>();
                    _priorityIndex[priority] = keys;
                }

                keys.Add(key);
            }
            finally
            {
                _priorityLock.ExitWriteLock();
            }
        }

        private void RemoveFromPriorityIndex(TKey key)
        {
            foreach (var prioritySet in _priorityIndex.Values)
            {
                prioritySet.TryRemove(key);
            }
        }

        private IEnumerable<CacheEntryPriority> GetPriorityLevelsToTrim(CacheEntryPriority minimumPriority)
        {
            // Return all priority levels up to and including the specified minimum
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

        public override void Dispose()
        {
            _priorityLock?.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// Extended metadata for prioritized cache entries
        /// </summary>
        protected struct PrioritizedEntryMetadata
        {
            public long Size { get; }
            public CacheEntryOptions Options { get; }
            public CacheEntryPriority Priority { get; }
            public DateTime CreationTime { get; }

            public PrioritizedEntryMetadata(long size, CacheEntryOptions options, CacheEntryPriority priority)
            {
                Size = size;
                Options = options;
                Priority = priority;
                CreationTime = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Thread-safe hash set implementation
    /// </summary>
    internal class ConcurrentHashSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, byte> _dictionary = new ConcurrentDictionary<T, byte>();

        public int Count => _dictionary.Count;

        public bool Add(T item)
        {
            return _dictionary.TryAdd(item, 0);
        }

        public bool TryRemove(T item)
        {
            return _dictionary.TryRemove(item, out _);
        }

        public bool Contains(T item)
        {
            return _dictionary.ContainsKey(item);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}