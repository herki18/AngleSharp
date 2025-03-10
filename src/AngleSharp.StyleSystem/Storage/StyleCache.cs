namespace AngleSharp.StyleSystem.Storage;

using System.Collections.Generic;
using AngleSharp.StyleSystem.Models;
using Interfaces;

/// <summary>
/// Provides caching for computed styles.
/// </summary>
public class StyleCache
{
    private readonly Dictionary<StyleCacheKey, IComputedStyle> _cache = new Dictionary<StyleCacheKey, IComputedStyle>();
    private readonly int _maxSize = 10000;

    /// <summary>
    /// Tries to get a cached style.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="style">The output style if found.</param>
    /// <returns>True if the style was found; otherwise, false.</returns>
    public bool TryGetValue(StyleCacheKey key, out IComputedStyle style)
    {
        return _cache.TryGetValue(key, out style);
    }

    /// <summary>
    /// Stores a style in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="style">The style to store.</param>
    public void Store(StyleCacheKey key, IComputedStyle style)
    {
        if (_cache.Count >= _maxSize)
        {
            _cache.Clear();
        }
        _cache[key] = style;
    }

    /// <summary>
    /// Removes a style from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void Remove(StyleCacheKey key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets all cached styles.
    /// </summary>
    /// <returns>All cached styles.</returns>
    public IEnumerable<IComputedStyle> GetAllStyles()
    {
        return _cache.Values;
    }

    /// <summary>
    /// Gets the number of cached styles.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets all cache keys.
    /// </summary>
    /// <returns>All cache keys.</returns>
    public IEnumerable<StyleCacheKey> GetAllKeys()
    {
        return _cache.Keys;
    }
}