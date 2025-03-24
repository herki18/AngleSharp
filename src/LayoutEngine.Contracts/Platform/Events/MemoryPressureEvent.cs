namespace LayoutEngine.Contracts.Platform.Events;

using Contracts.Resource;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when memory pressure is detected.
/// </summary>
public class MemoryPressureEvent : PrioritizedEventBase
{
    /// <summary>
    /// Gets the severity of memory pressure.
    /// </summary>
    public MemoryPressureSeverity Severity { get; }

    /// <summary>
    /// Gets the current memory usage.
    /// </summary>
    public long CurrentMemoryUsage { get; }

    /// <summary>
    /// Gets the memory usage threshold.
    /// </summary>
    public long MemoryThreshold { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPressureEvent"/> class.
    /// </summary>
    /// <param name="severity">The severity of memory pressure.</param>
    /// <param name="currentMemoryUsage">The current memory usage.</param>
    /// <param name="memoryThreshold">The memory usage threshold.</param>
    public MemoryPressureEvent(
        MemoryPressureSeverity severity,
        long currentMemoryUsage,
        long memoryThreshold)
        : base(EventPriority.Critical)
    {
        Severity = severity;
        CurrentMemoryUsage = currentMemoryUsage;
        MemoryThreshold = memoryThreshold;
    }
}