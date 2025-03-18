namespace Infrastructure.CacheManager.Internal.Caches;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using API.Caching.CacheTypes;
using API.Management;

/// <summary>
/// A cache implementation that tracks dependencies between entries for intelligent invalidation.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
internal class DependencyTrackingCache<TKey, TValue> : Cache<TKey, TValue>, IDependencyTrackingCache<TKey, TValue>
{
    // For each key, track the keys of entries that depend on it
    private readonly ConcurrentDictionary<TKey, ConcurrentHashSet<TKey>> _dependentIndex;

    // For each key, track the keys it depends on
    private readonly ConcurrentDictionary<TKey, ConcurrentHashSet<TKey>> _dependencyIndex;

    private readonly ReaderWriterLockSlim _dependencyLock = new ReaderWriterLockSlim();

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyTrackingCache{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="name">The name of the cache.</param>
    /// <param name="priority">The priority of the cache.</param>
    /// <param name="cleanupInterval">The interval at which to clean up expired entries.</param>
    public DependencyTrackingCache(string name, CachePriority priority = CachePriority.Normal, TimeSpan? cleanupInterval = null)
        : base(name, priority, cleanupInterval)
    {
        _dependentIndex = new ConcurrentDictionary<TKey, ConcurrentHashSet<TKey>>();
        _dependencyIndex = new ConcurrentDictionary<TKey, ConcurrentHashSet<TKey>>();
    }

    /// <summary>
    /// Sets a value in the cache with dependencies.
    /// </summary>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="dependencies">The keys of other cache entries this entry depends on.</param>
    public void SetWithDependencies(TKey key, TValue value, IEnumerable<TKey> dependencies)
    {
        if (dependencies == null)
            throw new ArgumentNullException(nameof(dependencies));

        // First add the value to the cache
        base.Set(key, value);

        // Then add dependencies
        AddDependencies(key, dependencies);
    }

    /// <summary>
    /// Adds dependencies to an existing cache entry.
    /// </summary>
    /// <param name="key">The key of the cache entry.</param>
    /// <param name="dependencies">The keys of other cache entries this entry depends on.</param>
    /// <returns>True if the key was found and dependencies added, false otherwise.</returns>
    public bool AddDependencies(TKey key, IEnumerable<TKey> dependencies)
    {
        if (dependencies == null)
            throw new ArgumentNullException(nameof(dependencies));

        if (!base.Contains(key))
            return false;

        _dependencyLock.EnterWriteLock();
        try
        {
            // Get the set of dependencies for this key, creating if it doesn't exist
            var dependencySet = _dependencyIndex.GetOrAdd(key, _ => new ConcurrentHashSet<TKey>());

            foreach (var dependency in dependencies)
            {
                // Skip self-dependencies
                if (EqualityComparer<TKey>.Default.Equals(key, dependency))
                    continue;

                // Add to the dependency list for this key
                dependencySet.Add(dependency);

                // Add this key to the dependent list for the dependency
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

    /// <summary>
    /// Gets the keys of all cache entries that depend on the specified key.
    /// </summary>
    /// <param name="key">The dependency key to look up.</param>
    /// <returns>A collection of dependent keys.</returns>
    public IReadOnlyCollection<TKey> GetDependents(TKey key)
    {
        _dependencyLock.EnterReadLock();
        try
        {
            if (_dependentIndex.TryGetValue(key, out var dependents))
            {
                // Return a copy to avoid concurrent modification issues
                return dependents.ToList().AsReadOnly();
            }

            return Array.Empty<TKey>();
        }
        finally
        {
            _dependencyLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the keys of all cache entries that this entry depends on.
    /// </summary>
    /// <param name="key">The key to look up dependencies for.</param>
    /// <returns>A collection of dependency keys.</returns>
    public IReadOnlyCollection<TKey> GetDependencies(TKey key)
    {
        _dependencyLock.EnterReadLock();
        try
        {
            if (_dependencyIndex.TryGetValue(key, out var dependencies))
            {
                // Return a copy to avoid concurrent modification issues
                return dependencies.ToList().AsReadOnly();
            }

            return Array.Empty<TKey>();
        }
        finally
        {
            _dependencyLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Invalidates the specified key and all entries that depend on it, directly or indirectly.
    /// </summary>
    /// <param name="key">The key to invalidate.</param>
    /// <returns>The total number of entries invalidated.</returns>
    public int InvalidateWithDependents(TKey key)
    {
        HashSet<TKey> keysToInvalidate = new HashSet<TKey>();

        // Add the key itself
        keysToInvalidate.Add(key);

        // Collect all dependent keys recursively
        CollectDependentKeys(key, keysToInvalidate);

        // Invalidate all collected keys
        int invalidatedCount = 0;
        foreach (var k in keysToInvalidate)
        {
            if (base.Remove(k))
            {
                invalidatedCount++;
            }
        }

        // Clean up dependency tracking data
        _dependencyLock.EnterWriteLock();
        try
        {
            foreach (var k in keysToInvalidate)
            {
                _dependentIndex.TryRemove(k, out _);
                _dependencyIndex.TryRemove(k, out _);
            }
        }
        finally
        {
            _dependencyLock.ExitWriteLock();
        }

        return invalidatedCount;
    }

    /// <summary>
    /// Invalidates multiple keys and all entries that depend on them, directly or indirectly.
    /// </summary>
    /// <param name="keys">The keys to invalidate.</param>
    /// <returns>The total number of entries invalidated.</returns>
    public int InvalidateWithDependents(IEnumerable<TKey> keys)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        HashSet<TKey> keysToInvalidate = new HashSet<TKey>();

        // Collect all keys and their dependents
        foreach (var key in keys)
        {
            keysToInvalidate.Add(key);
            CollectDependentKeys(key, keysToInvalidate);
        }

        // Invalidate all collected keys
        int invalidatedCount = 0;
        foreach (var k in keysToInvalidate)
        {
            if (base.Remove(k))
            {
                invalidatedCount++;
            }
        }

        // Clean up dependency tracking data
        _dependencyLock.EnterWriteLock();
        try
        {
            foreach (var k in keysToInvalidate)
            {
                _dependentIndex.TryRemove(k, out _);
                _dependencyIndex.TryRemove(k, out _);
            }
        }
        finally
        {
            _dependencyLock.ExitWriteLock();
        }

        return invalidatedCount;
    }

    /// <summary>
    /// Recursively collects all keys that depend on the specified key.
    /// </summary>
    /// <param name="key">The key to collect dependents for.</param>
    /// <param name="collected">The set of keys that have been collected so far.</param>
    private void CollectDependentKeys(TKey key, HashSet<TKey> collected)
    {
        _dependencyLock.EnterReadLock();
        try
        {
            if (_dependentIndex.TryGetValue(key, out var dependents))
            {
                foreach (var dependent in dependents)
                {
                    if (collected.Add(dependent))
                    {
                        // Recursive call to find transitive dependencies
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

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the key was found and removed, false otherwise.</returns>
    public override bool Remove(TKey key)
    {
        bool removed = base.Remove(key);

        if (removed)
        {
            CleanupDependencies(key);
        }

        return removed;
    }

    /// <summary>
    /// Cleans up dependency tracking data for a removed key.
    /// </summary>
    /// <param name="key">The key that was removed.</param>
    private void CleanupDependencies(TKey key)
    {
        _dependencyLock.EnterWriteLock();
        try
        {
            ConcurrentHashSet<TKey>? dependents;
            // Remove from dependency index
            if (_dependencyIndex.TryRemove(key, out var dependencies))
            {
                // For each dependency, remove this key from its dependents
                foreach (var dependency in dependencies)
                {
                    if (_dependentIndex.TryGetValue(dependency, out dependents))
                    {
                        dependents.TryRemove(key);

                        // If there are no more dependents, remove the entry
                        if (dependents.Count == 0)
                        {
                            _dependentIndex.TryRemove(dependency, out _);
                        }
                    }
                }
            }

            // Remove from dependent index
            if (_dependentIndex.TryRemove(key, out dependents))
            {
                // For each dependent, remove this key from its dependencies
                foreach (var dependent in dependents)
                {
                    if (_dependencyIndex.TryGetValue(dependent, out var dependentDependencies))
                    {
                        dependentDependencies.TryRemove(key);

                        // If there are no more dependencies, remove the entry
                        if (dependentDependencies.Count == 0)
                        {
                            _dependencyIndex.TryRemove(dependent, out _);
                        }
                    }
                }
            }
        }
        finally
        {
            _dependencyLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public override void Clear()
    {
        base.Clear();

        _dependencyLock.EnterWriteLock();
        try
        {
            _dependentIndex.Clear();
            _dependencyIndex.Clear();
        }
        finally
        {
            _dependencyLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Disposes resources used by the cache.
    /// </summary>
    public override void Dispose()
    {
        _dependencyLock?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// A thread-safe hash set implementation.
/// </summary>
internal class ConcurrentHashSet<T> : IEnumerable<T>
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new ConcurrentDictionary<T, byte>();

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// Adds an element to the set.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <returns>True if the element was added, false if it already exists.</returns>
    public bool Add(T item)
    {
        return _dictionary.TryAdd(item, 0);
    }

    /// <summary>
    /// Attempts to remove an element from the set.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns>True if the element was removed, false if it was not found.</returns>
    public bool TryRemove(T item)
    {
        return _dictionary.TryRemove(item, out _);
    }

    /// <summary>
    /// Determines whether the set contains a specific element.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns>True if the set contains the element, false otherwise.</returns>
    public bool Contains(T item)
    {
        return _dictionary.ContainsKey(item);
    }

    /// <summary>
    /// Removes all elements from the set.
    /// </summary>
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the set.
    /// </summary>
    /// <returns>An enumerator for the set.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _dictionary.Keys.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the set.
    /// </summary>
    /// <returns>An enumerator for the set.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}