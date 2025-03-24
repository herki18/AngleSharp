using AngleSharp;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LayoutEngine;

/// <summary>
/// Extension methods for setting up the LayoutEngine in a dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the LayoutEngine rendering system to the service collection.
    /// </summary>
    public static IServiceCollection AddLayoutEngine(this IServiceCollection services, Action<LayoutEngineOptions>? configureOptions = null)
    {
        // Configure options
        var options = new LayoutEngineOptions();
        configureOptions?.Invoke(options);

        // Register options
        services.AddSingleton(options);

        // Register AngleSharp configuration
        services.AddSingleton<IConfiguration>(provider =>
        {
            var config = Configuration.Default
                .WithCss()
                .WithRenderDevice()
                .WithDefaultLoader();

            return config;
        });

        // // Register core services
        // services.AddSingleton<RenderingEngine>();
        //
        // // Register platform services
        // services.AddLayoutEnginePlatform(options);
        //
        // // Register system services
        // services.AddStyleSystem();
        // services.AddLayoutSystem();
        // services.AddRenderSystem();

        return services;
    }
}

/// <summary>
/// Configuration options for the LayoutEngine.
/// </summary>
public class LayoutEngineOptions
{
    /// <summary>
    /// Gets or sets the default device pixel ratio.
    /// </summary>
    public float DevicePixelRatio { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether to use hardware acceleration when available.
    /// </summary>
    public bool UseHardwareAcceleration { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of worker threads.
    /// </summary>
    public int MaxWorkerThreads { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets the maximum size of the style cache.
    /// </summary>
    public int StyleCacheSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum size of the layout cache.
    /// </summary>
    public int LayoutCacheSize { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to automatically handle memory pressure.
    /// </summary>
    public bool AutomaticMemoryManagement { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use parallel processing for style computation.
    /// </summary>
    public bool ParallelStyleComputation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use parallel processing for layout computation.
    /// </summary>
    public bool ParallelLayoutComputation { get; set; } = true;
}