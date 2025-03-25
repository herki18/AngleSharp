using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Infrastructure.CacheManager.API.Caching;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.API.Models;

namespace Infrastructure.CacheManager.Internal.Caches
{
    using System.Linq;

    /// <summary>
    /// Base implementation of ICache using Microsoft's MemoryCache.
    /// Provides core caching functionality and implements ITrimableCache for memory management.
    /// </summary>
    internal class MemoryCacheBase<TKey, TValue> : ICache<TKey, TValue>, ITrimableCache, IDisposable
    {
        private MemoryCache _memoryCache;
        private readonly ConcurrentDictionary<TKey, CacheEntryMetadata> _entryMetadata;
        private readonly Timer _cleanupTimer;
        private readonly TimeSpan _cleanupInterval;
        private long _estimatedSize;
        private int _count;
        private readonly object _trimLock = new object();
        private readonly MemoryCacheOptions _cacheOptions;
        private bool _isDisposed;

        public string Name { get; }
        public CachePriority Priority { get; }

        public int Count => _count;
        public long EstimatedSize => Interlocked.Read(ref _estimatedSize);

        public MemoryCacheBase(string name, CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null, MemoryCacheOptions? options = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Priority = priority;
            _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);
            _cacheOptions = options ?? new MemoryCacheOptions();
            _memoryCache = new MemoryCache(Options.Create(_cacheOptions));
            _entryMetadata = new ConcurrentDictionary<TKey, CacheEntryMetadata>();
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);
        }

        public bool TryGetValue(TKey key, out TValue? value)
        {
            if (key == null)
            {
                value = default;
                return false;
            }

            if (_memoryCache.TryGetValue(CreateCacheKey(key), out var cachedValue))
            {
                // Update last access time in metadata
                if (_entryMetadata.TryGetValue(key, out var metadata))
                {
                    metadata.LastAccessTime = DateTime.UtcNow;
                }

                value = (TValue)cachedValue!;
                return true;
            }

            value = default;
            return false;
        }

        public TValue GetOrCreate(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            var cacheKey = CreateCacheKey(key);

            // Fast path: check if already exists
            if (_memoryCache.TryGetValue(cacheKey, out var existingValue))
            {
                if (_entryMetadata.TryGetValue(key, out var metadata))
                {
                    metadata.LastAccessTime = DateTime.UtcNow;
                }
                return (TValue)existingValue!;
            }

            // Create value if not exists
            var value = valueFactory(key);
            Set(key, value);
            return value;
        }

        public virtual void Set(TKey key, TValue value, CacheEntryOptions? options = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var cacheKey = CreateCacheKey(key);
            var effectiveOptions = options?.Clone() ?? new CacheEntryOptions();
            var existingSize = 0L;

            if (_entryMetadata.TryGetValue(key, out var existingMetadata))
            {
                existingSize = existingMetadata.Size;
                _memoryCache.Remove(cacheKey);
            }
            else
            {
                Interlocked.Increment(ref _count);
            }

            long size = effectiveOptions.Size ?? EstimateSize(value);
            Interlocked.Add(ref _estimatedSize, -existingSize);
            Interlocked.Add(ref _estimatedSize, size);

            var metadata = new CacheEntryMetadata(size, effectiveOptions);
            _entryMetadata[key] = metadata;

            var cacheEntryOptions = new MemoryCacheEntryOptions();

            // Set size on the MemoryCacheEntryOptions - THIS IS THE MISSING PART
            if (effectiveOptions.Size.HasValue || _cacheOptions.SizeLimit.HasValue)
            {
                cacheEntryOptions.Size = size;
            }

            if (effectiveOptions.AbsoluteExpiration.HasValue)
            {
                cacheEntryOptions.SetAbsoluteExpiration(effectiveOptions.AbsoluteExpiration.Value);
            }
            if (effectiveOptions.SlidingExpiration.HasValue)
            {
                cacheEntryOptions.SetSlidingExpiration(effectiveOptions.SlidingExpiration.Value);
            }
            cacheEntryOptions.RegisterPostEvictionCallback(OnEntryEvicted, key);

            _memoryCache.Set(cacheKey, value, cacheEntryOptions);
        }

        public virtual bool Remove(TKey key)
        {
            if (key == null)
                return false;

            var cacheKey = CreateCacheKey(key);

            if (_entryMetadata.TryRemove(key, out var metadata))
            {
                _memoryCache.Remove(cacheKey);
                Interlocked.Add(ref _estimatedSize, -metadata.Size);
                Interlocked.Decrement(ref _count);

                if (metadata.Options.PostEvictionCallback != null)
                {
                    try
                    {
                        metadata.Options.PostEvictionCallback.Invoke(key);
                    }
                    catch (Exception)
                    {
                        // Swallow exceptions from callbacks
                    }
                }

                return true;
            }

            return false;
        }

        public bool Contains(TKey key)
        {
            if (key == null)
                return false;

            var cacheKey = CreateCacheKey(key);
            return _memoryCache.TryGetValue(cacheKey, out _);
        }

        public int Trim(double percentage)
        {
            if (percentage <= 0)
                return 0;

            if (percentage >= 100)
            {
                int count = _count;
                Clear();
                return count;
            }

            lock (_trimLock)
            {
                int currentCount = _count;
                int removeCount = (int)Math.Ceiling(currentCount * percentage / 100);

                if (removeCount <= 0)
                    return 0;

                // Fix: Make sure we're getting a snapshot of the keys to avoid collection
                // modification issues during iteration
                var entriesToRemove = _entryMetadata
                    .OrderBy(e => e.Value.LastAccessTime)
                    .Take(removeCount)
                    .Select(e => e.Key)
                    .ToList();

                int removedCount = 0;
                foreach (var key in entriesToRemove)
                {
                    if (Remove(key))
                    {
                        removedCount++;
                    }
                }

                return removedCount;
            }
        }

        public virtual void Clear()
        {
            lock (_trimLock)
            {
                // Clear the MemoryCache by disposing it and creating a new one
                var oldCache = _memoryCache;

                try
                {
                    // Create a new MemoryCache
                    var newCache = new MemoryCache(Options.Create(_cacheOptions));

                    // Replace the reference
                    Interlocked.Exchange(ref Unsafe.As<MemoryCache, object>(ref _memoryCache), newCache);
                }
                finally
                {
                    // Dispose the old cache
                    oldCache?.Dispose();
                }

                // Clear metadata
                _entryMetadata.Clear();
                Interlocked.Exchange(ref _estimatedSize, 0);
                Interlocked.Exchange(ref _count, 0);
            }
        }

        protected virtual string CreateCacheKey(TKey key)
        {
            // Prefix with cache name to avoid collisions across different caches
            return $"{Name}:{key}";
        }

        protected virtual long EstimateSize(TValue? value)
        {
            if (value == null)
                return 0;

            // Basic size estimation - improved from original implementation
            // For production, consider using a more accurate size estimator
            Type type = value.GetType();

            if (type.IsValueType)
            {
                // For simple value types, use approximate size
                return Math.Max(8, System.Runtime.InteropServices.Marshal.SizeOf(type));
            }

            if (value is string str)
            {
                return 24 + (str.Length * 2); // Base overhead + 2 bytes per char
            }

            if (value is System.Collections.ICollection collection)
            {
                return 24 + (collection.Count * 8); // Base overhead + reference size per item
            }

            // Default size for objects
            return 64;
        }

        private void OnEntryEvicted(object key, object? value, EvictionReason reason, object? state)
        {
            if (state is TKey typedKey && _entryMetadata.TryRemove(typedKey, out var metadata))
            {
                Interlocked.Add(ref _estimatedSize, -metadata.Size);
                Interlocked.Decrement(ref _count);

                try
                {
                    metadata.Options.PostEvictionCallback?.Invoke(value!);
                }
                catch
                {
                    // Swallow exceptions from callbacks
                }
            }
        }

        private void CleanupExpiredEntries(object? state)
        {
            if (_isDisposed || _count == 0)
                return;

            // No explicit cleanup needed - MemoryCache handles expirations internally
            // This method is kept for monitoring or future enhancements
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _cleanupTimer?.Dispose();
            _memoryCache?.Dispose();

            _entryMetadata.Clear();
        }

        /// <summary>
        /// Internal struct for tracking cache entry metadata to minimize GC pressure
        /// </summary>
        protected struct CacheEntryMetadata
        {
            public long Size { get; }
            public DateTime CreationTime { get; }
            public DateTime LastAccessTime { get; set; }
            public CacheEntryOptions Options { get; }

            public CacheEntryMetadata(long size, CacheEntryOptions options)
            {
                Size = size;
                CreationTime = DateTime.UtcNow;
                LastAccessTime = CreationTime;
                Options = options;
            }
        }
    }

    /// <summary>
    /// Unsafe helper class to perform reference exchanges
    /// </summary>
    internal static class Unsafe
    {
        /// <summary>
        /// Reference casting helper
        /// </summary>
        internal static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            return ref System.Runtime.CompilerServices.Unsafe.As<TFrom, TTo>(ref source);
        }
    }
}