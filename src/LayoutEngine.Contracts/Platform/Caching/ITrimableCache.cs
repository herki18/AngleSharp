namespace LayoutEngine.Contracts.Platform.Caching;

/// <summary>
/// Represents a cache that can be trimmed.
/// </summary>
public interface ITrimableCache
{
    /// <summary>
    /// Gets the number of items in the cache.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the name of the cache.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Trims the cache by the specified percentage.
    /// </summary>
    /// <param name="percentage">The percentage to trim (0.0 to 1.0).</param>
    void Trim(double percentage);

    /// <summary>
    /// Clears the cache.
    /// </summary>
    void Clear();
}