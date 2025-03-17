namespace LayoutEngine.Contracts.Platform.Caching;

/// <summary>
/// Manages caches for the rendering engine.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Registers a cache with the manager.
    /// </summary>
    /// <param name="cacheName">The name of the cache.</param>
    /// <param name="cache">The cache to register.</param>
    void RegisterCache(string cacheName, ITrimableCache cache);

    /// <summary>
    /// Trims all caches by the specified percentage.
    /// </summary>
    /// <param name="percentage">The percentage to trim (0.0 to 1.0).</param>
    void TrimAllCaches(double percentage);

    /// <summary>
    /// Clears all caches.
    /// </summary>
    void ClearAllCaches();
}