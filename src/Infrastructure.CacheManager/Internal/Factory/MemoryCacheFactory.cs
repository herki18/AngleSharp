using System;
using Microsoft.Extensions.Caching.Memory;
using Infrastructure.CacheManager.API.Caching;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Factory;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.Internal.Caches;

namespace Infrastructure.CacheManager.Internal.Factory;

/// <summary>
/// Concrete implementation of ICacheFactory that creates memory-based caches
/// </summary>
public class MemoryCacheFactory : ICacheFactory
{
    private readonly ICacheManager _cacheManager;

    public MemoryCacheFactory(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
    }

    /// <inheritdoc />
    public ICache<TKey, TValue> CreateCache<TKey, TValue>(CacheOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.Name)) throw new ArgumentException("Cache name cannot be empty", nameof(options));

        var cache = new MemoryCacheBase<TKey, TValue>(
            options.Name,
            options.Priority,
            options.CleanupInterval,
            options.MemoryCacheOptions);

        if (options.AutoRegister)
        {
            _cacheManager.RegisterCache(options.Name, cache);
        }

        return cache;
    }

    /// <inheritdoc />
    public IPrioritizedCache<TKey, TValue> CreatePrioritizedCache<TKey, TValue>(CacheOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.Name)) throw new ArgumentException("Cache name cannot be empty", nameof(options));

        var cache = new PrioritizedMemoryCache<TKey, TValue>(
            options.Name,
            options.Priority,
            options.CleanupInterval,
            options.MemoryCacheOptions);

        if (options.AutoRegister)
        {
            _cacheManager.RegisterCache(options.Name, cache);
        }

        return cache;
    }

    /// <inheritdoc />
    public IDependencyTrackingCache<TKey, TValue> CreateDependencyTrackingCache<TKey, TValue>(CacheOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.Name)) throw new ArgumentException("Cache name cannot be empty", nameof(options));

        var cache = new DependencyTrackingMemoryCache<TKey, TValue>(
            options.Name,
            options.Priority,
            options.CleanupInterval,
            options.MemoryCacheOptions);

        if (options.AutoRegister)
        {
            _cacheManager.RegisterCache(options.Name, cache);
        }

        return cache;
    }

    /// <inheritdoc />
    public IAdvancedCache<TKey, TValue> CreateAdvancedCache<TKey, TValue>(CacheOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.Name)) throw new ArgumentException("Cache name cannot be empty", nameof(options));

        var cache = new PrioritizedDependencyTrackingMemoryCache<TKey, TValue>(
            options.Name,
            options.Priority,
            options.CleanupInterval,
            options.MemoryCacheOptions);

        if (options.AutoRegister)
        {
            _cacheManager.RegisterCache(options.Name, cache);
        }

        return cache;
    }
}