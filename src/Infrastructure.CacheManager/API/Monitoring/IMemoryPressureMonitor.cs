namespace Infrastructure.CacheManager.API.Monitoring;

using System;

/// <summary>
/// Monitors system memory pressure and raises events when memory pressure is detected.
/// </summary>
public interface IMemoryPressureMonitor : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the memory monitoring is active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets the current memory pressure severity.
    /// </summary>
    PressureSeverity CurrentPressure { get; }

    /// <summary>
    /// Gets the current memory usage percentage of the process.
    /// </summary>
    double CurrentMemoryUsagePercentage { get; }

    /// <summary>
    /// Gets the current memory usage in bytes.
    /// </summary>
    long CurrentMemoryUsage { get; }

    /// <summary>
    /// Gets the memory threshold in bytes that triggers pressure detection.
    /// </summary>
    long MemoryThreshold { get; }

    /// <summary>
    /// Event raised when memory pressure is detected.
    /// </summary>
    event EventHandler<MemoryPressureEventArgs> MemoryPressureDetected;

    /// <summary>
    /// Starts monitoring memory pressure.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops monitoring memory pressure.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Performs an immediate check for memory pressure.
    /// </summary>
    /// <returns>The current memory pressure severity.</returns>
    PressureSeverity CheckMemoryPressure();
}