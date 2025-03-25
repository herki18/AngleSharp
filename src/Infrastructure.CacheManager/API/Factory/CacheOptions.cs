using System;
using Microsoft.Extensions.Caching.Memory;
using Infrastructure.CacheManager.API.Management;

namespace Infrastructure.CacheManager.API.Factory;

/// <summary>
/// Configuration options for cache creation
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// The name of the cache
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The priority of the cache
    /// </summary>
    public CachePriority Priority { get; set; } = CachePriority.Normal;

    /// <summary>
    /// The interval at which expired entries should be cleaned up
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Options for the underlying memory cache
    /// </summary>
    public MemoryCacheOptions MemoryCacheOptions { get; set; } = new MemoryCacheOptions();

    /// <summary>
    /// Maximum size limit for the cache
    /// </summary>
    public long? SizeLimit
    {
        get => MemoryCacheOptions.SizeLimit;
        set => MemoryCacheOptions.SizeLimit = value;
    }

    /// <summary>
    /// Whether caches created with these options should be automatically registered with the CacheManager
    /// </summary>
    public bool AutoRegister { get; set; } = true;

    /// <summary>
    /// Creates a new instance of cache options with default values
    /// </summary>
    public CacheOptions()
    {
    }

    /// <summary>
    /// Creates a new instance of cache options with the specified name
    /// </summary>
    public CacheOptions(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Creates a copy of these options
    /// </summary>
    public CacheOptions Clone()
    {
        return new CacheOptions
        {
            Name = this.Name,
            Priority = this.Priority,
            CleanupInterval = this.CleanupInterval,
            MemoryCacheOptions = new MemoryCacheOptions
            {
                SizeLimit = this.MemoryCacheOptions.SizeLimit,
                ExpirationScanFrequency = this.MemoryCacheOptions.ExpirationScanFrequency,
                CompactionPercentage = this.MemoryCacheOptions.CompactionPercentage
            },
            AutoRegister = this.AutoRegister
        };
    }
}