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
    /// MemoryCache implementation that tracks dependencies between cache entries,
    /// allowing for cascading invalidation when a dependency changes.
    /// </summary>
    internal class DependencyTrackingMemoryCache<TKey, TValue> : MemoryCacheBase<TKey, TValue>, IDependencyTrackingCache<TKey, TValue>
    {
        // Forward index: key -> dependencies it depends on
        private readonly ConcurrentDictionary<TKey, ConcurrentHashSet<TKey>> _dependencyIndex;

        // Reverse index: key -> dependents that depend on it
        private readonly ConcurrentDictionary<TKey, ConcurrentHashSet<TKey>> _dependentIndex;

        // Lock for dependency operations
        private readonly ReaderWriterLockSlim _dependencyLock = new ReaderWriterLockSlim();

        public DependencyTrackingMemoryCache(string name, CachePriority priority = CachePriority.Normal,
            TimeSpan? cleanupInterval = null, MemoryCacheOptions? options = null)
            : base(name, priority, cleanupInterval, options)
        {
            _dependencyIndex = new ConcurrentDictionary<TKey, ConcurrentHashSet<TKey>>();
            _dependentIndex = new ConcurrentDictionary<TKey, ConcurrentHashSet<TKey>>();
        }

        public void SetWithDependencies(TKey key, TValue value, IEnumerable<TKey> dependencies)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (dependencies == null)
                throw new ArgumentNullException(nameof(dependencies));

            // First set the value in the cache
            base.Set(key, value);

            // Then add dependencies
            AddDependencies(key, dependencies);
        }

        public bool AddDependencies(TKey key, IEnumerable<TKey> dependencies)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (dependencies == null)
                throw new ArgumentNullException(nameof(dependencies));

            // Check if the entry exists before adding dependencies
            if (!base.Contains(key))
                return false;

            _dependencyLock.EnterWriteLock();
            try
            {
                // Get or create dependency set for this key
                var dependencySet = _dependencyIndex.GetOrAdd(key, _ => new ConcurrentHashSet<TKey>());

                foreach (var dependency in dependencies)
                {
                    // Skip if dependency is the same as the key (avoid self-dependency)
                    if (EqualityComparer<TKey>.Default.Equals(key, dependency))
                        continue;

                    // Add dependency to key's dependencies
                    dependencySet.Add(dependency);

                    // Add key to dependency's dependents
                    var dependentSet = _dependentIndex.GetOrAdd(dependency, _ => new ConcurrentHashSet<TKey>());
                    dependentSet.Add(key);
                }

                return true;
            }
            finally
            {
                _dependencyLock.ExitWriteLock();
            }
        }

        public IReadOnlyCollection<TKey> GetDependents(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _dependencyLock.EnterReadLock();
            try
            {
                if (_dependentIndex.TryGetValue(key, out var dependents))
                {
                    return new List<TKey>(dependents).AsReadOnly();
                }

                return Array.Empty<TKey>();
            }
            finally
            {
                _dependencyLock.ExitReadLock();
            }
        }

        public IReadOnlyCollection<TKey> GetDependencies(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _dependencyLock.EnterReadLock();
            try
            {
                if (_dependencyIndex.TryGetValue(key, out var dependencies))
                {
                    return new List<TKey>(dependencies).AsReadOnly();
                }

                return Array.Empty<TKey>();
            }
            finally
            {
                _dependencyLock.ExitReadLock();
            }
        }

        public int InvalidateWithDependents(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            // Use the non-recursive collection method
            var keysToInvalidate = CollectAllDependentKeys(key);

            int invalidatedCount = 0;
            foreach (var k in keysToInvalidate)
            {
                if (base.Remove(k))
                {
                    invalidatedCount++;
                }
            }

            _dependencyLock.EnterWriteLock();
            try
            {
                foreach (var k in keysToInvalidate)
                {
                    CleanupDependencies(k);
                }
            }
            finally
            {
                _dependencyLock.ExitWriteLock();
            }

            return invalidatedCount;
        }

        public int InvalidateWithDependents(IEnumerable<TKey> keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            // Collect all dependent keys for all specified keys
            var keysToInvalidate = new HashSet<TKey>();
            foreach (var key in keys.Where(k => k != null))
            {
                var dependentKeys = CollectAllDependentKeys(key);
                keysToInvalidate.UnionWith(dependentKeys);
            }

            int invalidatedCount = 0;
            foreach (var k in keysToInvalidate)
            {
                if (base.Remove(k))
                {
                    invalidatedCount++;
                }
            }

            _dependencyLock.EnterWriteLock();
            try
            {
                foreach (var k in keysToInvalidate)
                {
                    CleanupDependencies(k);
                }
            }
            finally
            {
                _dependencyLock.ExitWriteLock();
            }

            return invalidatedCount;
        }

        public override bool Remove(TKey key)
        {
            if (key == null)
                return false;

            bool removed = base.Remove(key);

            if (removed)
            {
                // Clean up dependency tracking
                _dependencyLock.EnterWriteLock();
                try
                {
                    CleanupDependencies(key);
                }
                finally
                {
                    _dependencyLock.ExitWriteLock();
                }
            }

            return removed;
        }

        public override void Clear()
        {
            _dependencyLock.EnterWriteLock();
            try
            {
                // Clear dependency tracking
                _dependencyIndex.Clear();
                _dependentIndex.Clear();

                // Clear the base cache
                base.Clear();
            }
            finally
            {
                _dependencyLock.ExitWriteLock();
            }
        }

        private void CollectDependentKeys(TKey key, HashSet<TKey> collected)
        {
            _dependencyLock.EnterReadLock();
            try
            {
                if (_dependentIndex.TryGetValue(key, out var dependents))
                {
                    foreach (var dependent in dependents)
                    {
                        // Add dependent and process recursively if it's new
                        if (collected.Add(dependent))
                        {
                            CollectDependentKeys(dependent, collected);
                        }
                    }
                }
            }
            finally
            {
                _dependencyLock.ExitReadLock();
            }
        }

        private HashSet<TKey> CollectAllDependentKeys(TKey rootKey)
        {
            var result = new HashSet<TKey> { rootKey };
            var toProcess = new Queue<TKey>();
            toProcess.Enqueue(rootKey);

            _dependencyLock.EnterReadLock();
            try
            {
                while (toProcess.Count > 0)
                {
                    var currentKey = toProcess.Dequeue();

                    if (_dependentIndex.TryGetValue(currentKey, out var dependents))
                    {
                        foreach (var dependent in dependents)
                        {
                            if (result.Add(dependent))
                            {
                                toProcess.Enqueue(dependent);
                            }
                        }
                    }
                }
            }
            finally
            {
                _dependencyLock.ExitReadLock();
            }

            return result;
        }

        private void CleanupDependencies(TKey key)
        {
            ConcurrentHashSet<TKey>? dependents;
            // Remove key from dependency index and update reverse dependencies
            if (_dependencyIndex.TryRemove(key, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (_dependentIndex.TryGetValue(dependency, out dependents))
                    {
                        dependents.TryRemove(key);

                        // Remove empty dependency entries
                        if (dependents.Count == 0)
                        {
                            _dependentIndex.TryRemove(dependency, out _);
                        }
                    }
                }
            }

            // Remove key from dependent index and update forward dependencies
            if (_dependentIndex.TryRemove(key, out dependents))
            {
                foreach (var dependent in dependents)
                {
                    if (_dependencyIndex.TryGetValue(dependent, out var dependentDependencies))
                    {
                        dependentDependencies.TryRemove(key);

                        // Remove empty dependency entries
                        if (dependentDependencies.Count == 0)
                        {
                            _dependencyIndex.TryRemove(dependent, out _);
                        }
                    }
                }
            }
        }

        public override void Dispose()
        {
            _dependencyLock?.Dispose();
            base.Dispose();
        }
    }
}