using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform.DOM;
using LayoutEngine.Platform.Lifecycle;
using LayoutEngine.Platform.Threading;
using LayoutEngine.Platform.Update;
using LayoutEngine.Platform.Resource;
namespace LayoutEngine.Platform;

using Contracts.Platform.Abstractions;
using Contracts.Platform.Dom;
using Contracts.Platform.Dom.Abstractions;
using Contracts.Platform.Lifecycle;
using Contracts.Platform.Resource.Abstractions;
using Contracts.Platform.Threading;
using Contracts.Platform.Updates;
using LayoutEngine.Platform.DOM.Abstractions;
using LayoutEngine.Platform.Resource.Abstractions;
using LayoutEngine.Platform.Abstractions;

public static class PlatformServiceExtensions
{
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register abstraction interfaces
        services.TryAddSingleton<IMutationObserverFactory, MutationObserverFactory>();
        services.TryAddSingleton<IResizeObserverFactory, ResizeObserverFactory>();
        services.TryAddSingleton<IWindowProvider, DefaultWindowProvider>();
        services.TryAddSingleton<ITimeProvider, SystemTimeProvider>();
        services.TryAddSingleton<IResourceLoadingStrategy, DefaultResourceLoadingStrategy>();
        services.TryAddSingleton<IResourceTypeResolver, DefaultResourceTypeResolver>();

        // Register platform services
        services.TryAddSingleton<IDocumentLifecycleCoordinator, DocumentLifecycleCoordinator>();
        services.TryAddSingleton<IElementAdapter, ElementAdapter>();
        services.TryAddSingleton<IDomMutationTracker, DomMutationTracker>();
        services.TryAddSingleton<IViewportDetector, ViewportDetector>();
        services.TryAddSingleton<IThreadPool, ThreadPool>();
        services.TryAddSingleton<IThreadingCoordinator, ThreadingCoordinator>();
        services.TryAddSingleton<IUpdateScheduler, UpdateScheduler>();
        services.TryAddSingleton<IFrameScheduler, FrameScheduler>();
        services.TryAddSingleton<IIdleTaskScheduler, IdleTaskScheduler>();
        services.TryAddSingleton<IResourceLoader, ResourceLoader>();
        services.TryAddSingleton<IResourceErrorHandler, ResourceErrorHandler>();

        return services;
    }

    public static IServiceCollection AddPlatformServices(
        this IServiceCollection services,
        Action<PlatformOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);
        services.AddPlatformServices();
        return services;
    }

    // New method for adding testable platform services
    public static IServiceCollection AddTestPlatformServices(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register test abstractions
        services.TryAddSingleton<ITimeProvider, TestTimeProvider>();

        // Register platform service implementations
        // but still use real implementations for most services
        services.TryAddSingleton<IDocumentLifecycleCoordinator, DocumentLifecycleCoordinator>();
        services.TryAddSingleton<IElementAdapter, ElementAdapter>();
        services.TryAddSingleton<IDomMutationTracker, DomMutationTracker>();
        services.TryAddSingleton<IViewportDetector, ViewportDetector>();
        services.TryAddSingleton<IThreadPool, ThreadPool>();
        services.TryAddSingleton<IThreadingCoordinator, ThreadingCoordinator>();
        services.TryAddSingleton<IUpdateScheduler, UpdateScheduler>();
        services.TryAddSingleton<IFrameScheduler, FrameScheduler>();
        services.TryAddSingleton<IIdleTaskScheduler, IdleTaskScheduler>();
        services.TryAddSingleton<IResourceLoader, ResourceLoader>();
        services.TryAddSingleton<IResourceErrorHandler, ResourceErrorHandler>();

        // Configure services for test mode
        services.PostConfigure<PlatformOptions>(options =>
        {
            options.ThreadPool.MaxThreads = 1;
            options.MaxConcurrentResourceLoads = 1;
        });

        return services;
    }
}