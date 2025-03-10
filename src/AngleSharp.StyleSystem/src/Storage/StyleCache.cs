namespace AngleSharp.StyleSystem.Storage;

using System.Collections.Generic;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Implements a cache for computed styles to improve performance by reusing style objects.
/// </summary>
public class StyleCache : IStyleCache
{
    private readonly Dictionary<StyleCacheKey, IComputedStyle> _cache = new Dictionary<StyleCacheKey, IComputedStyle>();
    private readonly int _maxSize = 10000;

    /// <summary>
    /// Tries to retrieve a cached style for the specified key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="style">The retrieved style if found.</param>
    /// <returns>True if the style was found; otherwise, false.</returns>
    public bool TryGetValue(StyleCacheKey key, out IComputedStyle style)
    {
        return _cache.TryGetValue(key, out style);
    }

    /// <summary>
    /// Stores a computed style in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="style">The computed style to cache.</param>
    public void Store(StyleCacheKey key, IComputedStyle style)
    {
        if (_cache.Count >= _maxSize)
        {
            _cache.Clear();
        }
        _cache[key] = style;
    }

    /// <summary>
    /// Removes a specific entry from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    public void Remove(StyleCacheKey key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets all computed styles currently in the cache.
    /// </summary>
    /// <returns>An enumerable of all cached styles.</returns>
    public IEnumerable<IComputedStyle> GetAllStyles()
    {
        return _cache.Values;
    }

    /// <summary>
    /// Gets the number of items in the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets all keys currently in the cache.
    /// </summary>
    /// <returns>An enumerable of all cache keys.</returns>
    public IEnumerable<StyleCacheKey> GetAllKeys()
    {
        return _cache.Keys;
    }
}