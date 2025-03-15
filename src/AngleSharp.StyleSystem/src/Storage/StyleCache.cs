using System.Collections.Generic;
using System.Linq;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AngleSharp.StyleSystem.Storage;

public class StyleCache : IStyleCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly HashSet<StyleCacheKey> _keys = new HashSet<StyleCacheKey>();

    public StyleCache(IMemoryCache? memoryCache = null)
    {
        // Use provided cache or create new one
        _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 10000
        });

        // Set up default options for cache entries
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(1)  // Each entry counts as 1 unit for size limiting
            .SetPriority(CacheItemPriority.Normal);
    }

    public bool TryGetValue(StyleCacheKey key, out IComputedStyle style)
    {
        if (_memoryCache.TryGetValue(key, out object? cachedStyle) && cachedStyle is IComputedStyle computedStyle)
        {
            style = computedStyle;
            return true;
        }
        style = null!; // Using null! as required by out parameter
        return false;
    }

    public void Store(StyleCacheKey key, IComputedStyle style)
    {
        _memoryCache.Set(key, style, _cacheOptions);
        _keys.Add(key);
    }

    public void Remove(StyleCacheKey key)
    {
        _memoryCache.Remove(key);
        _keys.Remove(key);
    }

    public void Clear()
    {
        foreach (var key in _keys.ToList())
        {
            _memoryCache.Remove(key);
        }
        _keys.Clear();
    }

    public IEnumerable<IComputedStyle> GetAllStyles()
    {
        var styles = new List<IComputedStyle>();
        foreach (var key in _keys)
        {
            if (_memoryCache.TryGetValue(key, out object? cachedItem) &&
                cachedItem is IComputedStyle style)
            {
                styles.Add(style);
            }
        }
        return styles;
    }

    public int Count => _keys.Count;

    public IEnumerable<StyleCacheKey> GetAllKeys() => _keys;
}