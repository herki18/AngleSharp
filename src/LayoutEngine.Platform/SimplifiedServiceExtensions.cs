using System;
using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.Contracts.Platform.Abstractions;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Platform.Abstractions;
using LayoutEngine.Platform.Threading;
using LayoutEngine.Platform.Update;

namespace LayoutEngine.Platform;

using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;
using Contracts.Platform.Lifecycle;
using DOM;
using Lifecycle;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for registering simplified platform services in the dependency injection container.
/// </summary>
public static class SimplifiedServiceExtensions
{
    /// <summary>
    /// Adds simplified platform services with single-threaded execution model.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimplifiedPlatformServices(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));


        // Register abstraction interfaces
        // services.TryAddSingleton<IMutationObserverFactory, MutationObserverFactory>();
        // services.TryAddSingleton<IResizeObserverFactory, ResizeObserverFactory>();
        services.TryAddSingleton<IWindowProvider, DefaultWindowProvider>();
        services.TryAddSingleton<ITimeProvider, SystemTimeProvider>();

        // Register platform services
        services.TryAddSingleton<IDocumentLifecycleCoordinator, DocumentLifecycleCoordinator>();
        services.TryAddSingleton<IElementAdapter, ElementAdapter>();
        services.TryAddSingleton<IThreadPool, ThreadPool>();

        services.TryAddSingleton<IFrameTimingStrategy, SynchronousFrameTimingStrategy>();

        services.TryAddSingleton<IFrameScheduler, SimplifiedFrameScheduler>();
        services.TryAddSingleton<IUpdateScheduler, SimplifiedUpdateScheduler>();
        services.TryAddSingleton<IThreadingCoordinator, SimplifiedThreadingCoordinator>();

        return services;
    }

    /// <summary>
    /// Adds simplified platform services with configuration options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Action to configure platform options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimplifiedPlatformServices(
        this IServiceCollection services,
        Action<SimplifiedPlatformOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Create and configure options
        var options = new SimplifiedPlatformOptions();
        configureOptions(options);

        // Register services
        services.AddSimplifiedPlatformServices();

        // Configure services with options
        services.AddSingleton<SynchronousFrameTimingStrategy>(sp =>
            new SynchronousFrameTimingStrategy(
                sp.GetRequiredService<ITimeProvider>(),
                options.TargetFramesPerSecond));

        return services;
    }
}

/// <summary>
/// Configuration options for simplified platform services.
/// </summary>
public class SimplifiedPlatformOptions
{
    /// <summary>
    /// Gets or sets the target frames per second for the frame scheduler.
    /// </summary>
    public int TargetFramesPerSecond { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum updates to process per frame.
    /// </summary>
    public int MaxUpdatesPerFrame { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum actions to process per update cycle.
    /// </summary>
    public int MaxActionsPerUpdate { get; set; } = 10;
}