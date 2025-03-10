namespace AngleSharp.StyleSystem.Interfaces;

using System.Collections.Generic;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Defines a cache for storing and retrieving computed styles.
/// </summary>
public interface IStyleCache
{
    /// <summary>
    /// Tries to retrieve a cached style for the specified key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="style">The retrieved style if found.</param>
    /// <returns>True if the style was found; otherwise, false.</returns>
    bool TryGetValue(StyleCacheKey key, out IComputedStyle style);

    /// <summary>
    /// Stores a computed style in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="style">The computed style to cache.</param>
    void Store(StyleCacheKey key, IComputedStyle style);

    /// <summary>
    /// Removes a specific entry from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    void Remove(StyleCacheKey key);

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all computed styles currently in the cache.
    /// </summary>
    /// <returns>An enumerable of all cached styles.</returns>
    IEnumerable<IComputedStyle> GetAllStyles();

    /// <summary>
    /// Gets the number of items in the cache.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets all keys currently in the cache.
    /// </summary>
    /// <returns>An enumerable of all cache keys.</returns>
    IEnumerable<StyleCacheKey> GetAllKeys();
}