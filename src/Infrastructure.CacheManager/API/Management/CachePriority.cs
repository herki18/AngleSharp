namespace Infrastructure.CacheManager.API.Management;

/// <summary>
/// Defines priority levels for cache instances to determine the order of trimming during memory pressure.
/// </summary>
public enum CachePriority
{
    /// <summary>
    /// Low priority caches are trimmed first when memory pressure is detected.
    /// These typically contain data that is easy to regenerate or non-critical.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority caches are trimmed after low priority caches.
    /// These typically contain standard application data.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority caches are trimmed after normal priority caches.
    /// These typically contain data that is expensive to regenerate.
    /// </summary>
    High = 20,

    /// <summary>
    /// Critical priority caches are trimmed only in the most severe memory pressure situations.
    /// These contain essential data that should be preserved if possible.
    /// </summary>
    Critical = 30
}