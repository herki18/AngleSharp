namespace Infrastructure.CacheManager.API.Caching.CacheTypes;

/// <summary>
/// Advanced cache interface that combines prioritized caching with dependency tracking
/// </summary>
/// <typeparam name="TKey">The type of keys in the cache</typeparam>
/// <typeparam name="TValue">The type of values in the cache</typeparam>
public interface IAdvancedCache<TKey, TValue> : IPrioritizedCache<TKey, TValue>, IDependencyTrackingCache<TKey, TValue>
{
    // This interface combines the capabilities of both prioritized and dependency tracking caches
    // No additional members needed as it inherits all needed functionality
}