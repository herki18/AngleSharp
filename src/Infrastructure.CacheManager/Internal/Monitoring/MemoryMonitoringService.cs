namespace Infrastructure.CacheManager.Internal.Monitoring;

using System;
using System.Threading;
using System.Threading.Tasks;
using API.Monitoring;

/// <summary>
/// Hosted service that automatically starts memory monitoring when the application starts.
/// </summary>
internal class MemoryMonitoringService
{
    private readonly IMemoryPressureMonitor _memoryPressureMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryMonitoringService"/> class.
    /// </summary>
    /// <param name="memoryPressureMonitor">The memory pressure monitor to start.</param>
    public MemoryMonitoringService(IMemoryPressureMonitor memoryPressureMonitor)
    {
        _memoryPressureMonitor = memoryPressureMonitor ?? throw new ArgumentNullException(nameof(memoryPressureMonitor));
    }

    /// <summary>
    /// Starts memory monitoring.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start memory monitoring
        _memoryPressureMonitor.StartMonitoring();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops memory monitoring.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop memory monitoring
        _memoryPressureMonitor.StopMonitoring();

        return Task.CompletedTask;
    }
}