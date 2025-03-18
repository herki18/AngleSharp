namespace Infrastructure.CacheManager.API.Invalidation;

/// <summary>
/// Defines scopes for cache invalidation operations.
/// </summary>
public enum InvalidationScope
{
    /// <summary>
    /// Invalidate a specific cache entry only.
    /// </summary>
    Entry = 0,

    /// <summary>
    /// Invalidate entries related to a specific DOM element.
    /// </summary>
    Element = 1,

    /// <summary>
    /// Invalidate entries related to a specific DOM element and its children.
    /// </summary>
    ElementSubtree = 2,

    /// <summary>
    /// Invalidate entries related to style computations.
    /// </summary>
    Style = 10,

    /// <summary>
    /// Invalidate entries related to layout computations.
    /// </summary>
    Layout = 11,

    /// <summary>
    /// Invalidate entries related to rendering.
    /// </summary>
    Render = 12,

    /// <summary>
    /// Invalidate entries related to resources.
    /// </summary>
    Resource = 13,

    /// <summary>
    /// Invalidate all cache entries in a specific cache.
    /// </summary>
    SingleCache = 20,

    /// <summary>
    /// Invalidate all cache entries in all caches.
    /// </summary>
    AllCaches = 30
}