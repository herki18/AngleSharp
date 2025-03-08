namespace AngleSharp.StyleSystem.Core;

using System.Collections.Generic;
using AngleSharp.StyleSystem.Core.Interfaces;

/// <summary>
/// Caches computed styles to avoid redundant computation.
/// </summary>
public class StyleCache
{
    private readonly Dictionary<StyleCacheKey, IComputedStyle> _cache = new Dictionary<StyleCacheKey, IComputedStyle>();
    private readonly int _maxSize = 10000; // Arbitrary limit to prevent unbounded growth

    /// <summary>
    /// Tries to get a cached style.
    /// </summary>
    public bool TryGetValue(StyleCacheKey key, out IComputedStyle style)
    {
        return _cache.TryGetValue(key, out style);
    }

    /// <summary>
    /// Stores a computed style in the cache.
    /// </summary>
    public void Store(StyleCacheKey key, IComputedStyle style)
    {
        if (_cache.Count >= _maxSize)
        {
            // In a real implementation, we'd use a more sophisticated eviction strategy
            _cache.Clear();
        }

        _cache[key] = style;
    }

    /// <summary>
    /// Removes a style from the cache.
    /// </summary>
    public void Remove(StyleCacheKey key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Clears the entire cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }
}