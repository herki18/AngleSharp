using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LayoutEngine.Contracts.Threading;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform.DOM;
using LayoutEngine.Platform.Lifecycle;
using LayoutEngine.Platform.Threading;
using LayoutEngine.Platform.Update;
using LayoutEngine.Platform.Resource;

namespace LayoutEngine.Platform;

using Contracts.Platform.Dom;
using Contracts.Platform.Lifecycle;
using Contracts.Platform.Updates;

/// <summary>
/// Extension methods for registering Platform services with the dependency injection container.
/// </summary>
public static class PlatformServiceExtensions
{
    /// <summary>
    /// Adds Platform services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Lifecycle services
        services.TryAddSingleton<ILifecycleStateValidator, LifecycleStateValidator>();
        services.TryAddSingleton<IDocumentLifecycleCoordinator, DocumentLifecycleCoordinator>();

        // DOM services
        services.TryAddSingleton<IElementAdapter, ElementAdapter>();
        services.TryAddSingleton<IDomMutationTracker, DomMutationTracker>();
        services.TryAddSingleton<IViewportDetector, ViewportDetector>();

        // Threading services
        services.TryAddSingleton<IThreadPool, ThreadPool>();
        services.TryAddSingleton<IThreadingCoordinator, ThreadingCoordinator>();

        // Update scheduling services
        services.TryAddSingleton<IUpdateScheduler, UpdateScheduler>();
        services.TryAddSingleton<IFrameScheduler, FrameScheduler>();
        services.TryAddSingleton<IIdleTaskScheduler, IdleTaskScheduler>();

        // Resource management services
        services.TryAddSingleton<IResourceLoader, ResourceLoader>();
        services.TryAddSingleton<IResourceErrorHandler, ResourceErrorHandler>();

        return services;
    }

    /// <summary>
    /// Adds Platform services with custom configuration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The action to configure platform options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlatformServices(
        this IServiceCollection services,
        Action<PlatformOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Configure options
        services.Configure(configureOptions);

        // Add standard services
        services.AddPlatformServices();

        return services;
    }
}