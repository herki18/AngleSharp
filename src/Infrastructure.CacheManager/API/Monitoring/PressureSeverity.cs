namespace Infrastructure.CacheManager.API.Monitoring;

/// <summary>
/// Defines levels of memory pressure severity.
/// </summary>
public enum PressureSeverity
{
    /// <summary>
    /// No memory pressure detected.
    /// Normal operations can continue.
    /// </summary>
    None = 0,

    /// <summary>
    /// Low memory pressure detected.
    /// Trim low-priority cache entries.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium memory pressure detected.
    /// Remove all low-priority entries and trim normal priority entries.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High memory pressure detected.
    /// Clear all low and normal priority entries and trim high priority entries.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical memory pressure detected.
    /// Clear all non-critical caches entirely and trim portion of critical entries.
    /// </summary>
    Critical = 4
}