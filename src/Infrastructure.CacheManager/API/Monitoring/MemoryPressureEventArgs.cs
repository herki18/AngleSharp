namespace Infrastructure.CacheManager.API.Monitoring;

using System;

/// <summary>
/// Event arguments for memory pressure detection events.
/// </summary>
public class MemoryPressureEventArgs : EventArgs
{
    /// <summary>
    /// Gets the severity of the detected memory pressure.
    /// </summary>
    public PressureSeverity Severity { get; }

    /// <summary>
    /// Gets the current memory usage that triggered the event.
    /// </summary>
    public long CurrentMemoryUsage { get; }

    /// <summary>
    /// Gets the memory threshold that was exceeded.
    /// </summary>
    public long MemoryThreshold { get; }

    /// <summary>
    /// Gets the current memory usage as a percentage of available memory.
    /// </summary>
    public double MemoryUsagePercentage { get; }

    /// <summary>
    /// Gets the time when the pressure was detected.
    /// </summary>
    public DateTime DetectionTime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPressureEventArgs"/> class.
    /// </summary>
    /// <param name="severity">The severity of the detected memory pressure.</param>
    /// <param name="currentMemoryUsage">The current memory usage that triggered the event.</param>
    /// <param name="memoryThreshold">The memory threshold that was exceeded.</param>
    /// <param name="memoryUsagePercentage">The current memory usage as a percentage of available memory.</param>
    public MemoryPressureEventArgs(
        PressureSeverity severity,
        long currentMemoryUsage,
        long memoryThreshold,
        double memoryUsagePercentage)
    {
        Severity = severity;
        CurrentMemoryUsage = currentMemoryUsage;
        MemoryThreshold = memoryThreshold;
        MemoryUsagePercentage = memoryUsagePercentage;
        DetectionTime = DateTime.UtcNow;
    }
}