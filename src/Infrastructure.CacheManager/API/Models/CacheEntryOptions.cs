namespace Infrastructure.CacheManager.API.Models;

using System;

/// <summary>
/// Options for cache entries to control their behavior and lifetime.
/// </summary>
public class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets the absolute expiration time relative to now.
    /// When provided, the cache entry will expire after this time period has elapsed.
    /// </summary>
    public TimeSpan? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration time.
    /// When provided, the cache entry will expire if it hasn't been accessed for this time period.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Gets or sets the estimated size of the cache entry in bytes.
    /// Used for memory management and determining when to trim the cache.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets a callback to be invoked after the cache entry is evicted.
    /// </summary>
    public Action<object>? PostEvictionCallback { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry should participate in priority-based eviction.
    /// If false, the entry is not considered for normal eviction, but may still expire due to time-based policies.
    /// </summary>
    public bool ParticipateInEviction { get; set; } = true;

    /// <summary>
    /// Creates a new instance of cache entry options with default values.
    /// </summary>
    public CacheEntryOptions()
    {
    }

    /// <summary>
    /// Creates a deep clone of the current options.
    /// </summary>
    /// <returns>A new options instance with the same values.</returns>
    public CacheEntryOptions Clone()
    {
        return new CacheEntryOptions
        {
            AbsoluteExpiration = this.AbsoluteExpiration,
            SlidingExpiration = this.SlidingExpiration,
            Size = this.Size,
            PostEvictionCallback = this.PostEvictionCallback,
            ParticipateInEviction = this.ParticipateInEviction
        };
    }
}