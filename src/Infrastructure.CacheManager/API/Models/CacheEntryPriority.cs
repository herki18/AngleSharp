namespace Infrastructure.CacheManager.API.Models;

/// <summary>
/// Defines priority levels for individual cache entries to determine the order of eviction within a cache.
/// </summary>
public enum CacheEntryPriority
{
    /// <summary>
    /// Low priority entries are evicted first during cache trimming.
    /// Use for data that is easy to regenerate or non-critical.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority entries are evicted after low priority entries.
    /// This is the default priority for most cache entries.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority entries are evicted after normal priority entries.
    /// Use for data that is expensive to regenerate.
    /// </summary>
    High = 20,

    /// <summary>
    /// Critical priority entries are evicted only when absolutely necessary.
    /// Use sparingly for essential data that should be preserved if possible.
    /// </summary>
    Critical = 30
}