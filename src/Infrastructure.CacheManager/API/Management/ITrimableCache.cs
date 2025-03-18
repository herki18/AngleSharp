namespace Infrastructure.CacheManager.API.Management;

/// <summary>
/// Represents a cache that can be trimmed to reduce memory consumption.
/// This is a core interface used by the cache manager to manage memory pressure.
/// </summary>
public interface ITrimableCache
{
    /// <summary>
    /// Gets the name of the cache.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the estimated total size of all entries in the cache in bytes.
    /// </summary>
    long EstimatedSize { get; }

    /// <summary>
    /// Gets the number of items in the cache.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the cache priority which determines the order in which caches are trimmed.
    /// </summary>
    CachePriority Priority { get; }

    /// <summary>
    /// Trims the cache by removing a percentage of the entries.
    /// </summary>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 100.</param>
    /// <returns>The number of entries removed.</returns>
    int Trim(double percentage);

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    void Clear();
}