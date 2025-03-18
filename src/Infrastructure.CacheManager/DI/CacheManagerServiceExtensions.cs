namespace Infrastructure.CacheManager.DI;

using System;
using API.Invalidation;
using API.Management;
using API.Monitoring;
using Internal.Core;
using Internal.Invalidation;
using Internal.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for setting up cache manager related services in an <see cref="IServiceCollection" />.
/// </summary>
public static class CacheManagerServiceExtensions
{
    /// <summary>
    /// Adds CacheManager services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configureOptions">A callback to configure cache manager options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCacheManagerModule(
        this IServiceCollection services,
        Action<CacheManagerOptions>? configureOptions = null)
    {
        // Create default options
        var options = new CacheManagerOptions();

        // Apply customizations
        configureOptions?.Invoke(options);

        // Register memory pressure monitor
        services.TryAddSingleton<IMemoryPressureMonitor>(provider =>
            new MemoryPressureMonitor(
                options.LowMemoryThresholdPercentage,
                options.MediumMemoryThresholdPercentage,
                options.HighMemoryThresholdPercentage,
                options.CriticalMemoryThresholdPercentage,
                options.MemoryCheckInterval));

        // Register cache manager
        services.TryAddSingleton<ICacheManager>(provider =>
            new CacheManager(
                provider.GetRequiredService<IMemoryPressureMonitor>(),
                options.StatisticsCollectionInterval));

        // Register cache invalidator
        services.TryAddSingleton<ICacheInvalidator>(provider =>
            new CacheInvalidator(provider.GetRequiredService<ICacheManager>()));

        // Auto-start memory monitoring if enabled
        if (options.AutoStartMemoryMonitoring)
        {
            // services.AddHostedService<MemoryMonitoringService>();
        }

        return services;
    }
}

/// <summary>
/// Options for configuring the CacheManager.
/// </summary>
public class CacheManagerOptions
{
    /// <summary>
    /// Gets or sets the percentage of system memory that triggers low memory pressure.
    /// </summary>
    public double LowMemoryThresholdPercentage { get; set; } = 60;

    /// <summary>
    /// Gets or sets the percentage of system memory that triggers medium memory pressure.
    /// </summary>
    public double MediumMemoryThresholdPercentage { get; set; } = 70;

    /// <summary>
    /// Gets or sets the percentage of system memory that triggers high memory pressure.
    /// </summary>
    public double HighMemoryThresholdPercentage { get; set; } = 80;

    /// <summary>
    /// Gets or sets the percentage of system memory that triggers critical memory pressure.
    /// </summary>
    public double CriticalMemoryThresholdPercentage { get; set; } = 90;

    /// <summary>
    /// Gets or sets the interval at which to check memory pressure.
    /// </summary>
    public TimeSpan MemoryCheckInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the interval at which to collect cache statistics.
    /// </summary>
    public TimeSpan StatisticsCollectionInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets a value indicating whether to automatically start memory monitoring.
    /// </summary>
    public bool AutoStartMemoryMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic cleanup of expired entries.
    /// </summary>
    public bool EnableAutomaticCleanup { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval at which to clean up expired entries.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(10);
}