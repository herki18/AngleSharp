using Infrastructure.CacheManager.API.Caching;
using Infrastructure.CacheManager.API.Caching.CacheTypes;

namespace Infrastructure.CacheManager.API.Factory;

/// <summary>
/// Factory interface for creating different types of caches
/// </summary>
public interface ICacheFactory
{
    /// <summary>
    /// Creates a basic cache with key-value storage capabilities
    /// </summary>
    ICache<TKey, TValue> CreateCache<TKey, TValue>(CacheOptions options);

    /// <summary>
    /// Creates a cache with priority-based retention capabilities
    /// </summary>
    IPrioritizedCache<TKey, TValue> CreatePrioritizedCache<TKey, TValue>(CacheOptions options);

    /// <summary>
    /// Creates a cache with dependency tracking capabilities
    /// </summary>
    IDependencyTrackingCache<TKey, TValue> CreateDependencyTrackingCache<TKey, TValue>(CacheOptions options);

    /// <summary>
    /// Creates an advanced cache with both priority-based retention and dependency tracking
    /// </summary>
    IAdvancedCache<TKey, TValue> CreateAdvancedCache<TKey, TValue>(CacheOptions options);
}