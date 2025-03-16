namespace Infrastructure.EventAggregator;

using System;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the EventAggregator with Microsoft DI
/// </summary>
public static class EventAggregatorServiceExtensions
{
    /// <summary>
    /// Adds the EventAggregator module to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional action to configure EventAggregator options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEventAggregatorModule(
        this IServiceCollection services,
        Action<EventAggregatorOptions>? configureOptions = null)
    {
        // Register options if provided
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register the EventAggregator as a singleton
        services.AddSingleton<IEventAggregator, EventAggregator>();

        return services;
    }
}

/// <summary>
/// Configuration options for the EventAggregator
/// </summary>
public class EventAggregatorOptions
{
    /// <summary>
    /// Gets or sets the cleanup interval for unused event subjects
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether to enable automatic cleanup of unused subjects
    /// </summary>
    public bool EnableAutomaticCleanup { get; set; } = true;
}