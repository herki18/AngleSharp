namespace Infrastructure.CacheManager.API.Caching;

using System;
using Models;

/// <summary>
/// Represents a single cache entry with metadata and value access.
/// </summary>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
public interface ICacheEntry<TValue> : IDisposable
{
    /// <summary>
    /// Gets the key associated with this cache entry.
    /// </summary>
    object Key { get; }

    /// <summary>
    /// Gets or sets the value stored in this cache entry.
    /// </summary>
    TValue Value { get; set; }

    /// <summary>
    /// Gets the options associated with this cache entry.
    /// </summary>
    CacheEntryOptions Options { get; }

    /// <summary>
    /// Gets the UTC time when this entry was created.
    /// </summary>
    DateTime CreationTime { get; }

    /// <summary>
    /// Gets the UTC time when this entry was last accessed.
    /// </summary>
    DateTime LastAccessTime { get; }

    /// <summary>
    /// Gets the UTC time when this entry will expire, if applicable.
    /// </summary>
    DateTime? ExpirationTime { get; }

    /// <summary>
    /// Gets or sets the priority of this individual cache entry.
    /// This is used for determining which entries to trim first within a cache.
    /// </summary>
    CacheEntryPriority Priority { get; set; }

    /// <summary>
    /// Refreshes the expiration time of this entry based on its options.
    /// </summary>
    void RefreshExpiration();

    /// <summary>
    /// Checks whether this entry has expired.
    /// </summary>
    /// <returns>True if the entry has expired, false otherwise.</returns>
    bool HasExpired();
}