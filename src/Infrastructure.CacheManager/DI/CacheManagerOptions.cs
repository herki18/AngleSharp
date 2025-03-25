namespace Infrastructure.CacheManager.DI;

using System;

/// <summary>
/// Configuration options for the cache manager.
/// </summary>
public class CacheManagerOptions
{
    /// <summary>
    /// Interval at which cache statistics are collected.
    /// </summary>
    public TimeSpan StatisticsCollectionInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to enable automatic cleanup of expired cache entries.
    /// </summary>
    public bool EnableAutomaticCleanup { get; set; } = true;

    /// <summary>
    /// Interval at which expired cache entries are cleaned up.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(10);
}