using System;
using Microsoft.Extensions.DependencyInjection;
using LayoutEngine.Contracts.Platform.Abstractions;
using LayoutEngine.Contracts.Platform.Lifecycle; // Added
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.StyleSystem;       // Added
using LayoutEngine.Contracts.LayoutSystem;      // Added
using LayoutEngine.Platform.Abstractions;
using LayoutEngine.Platform.Lifecycle;         // Added
using LayoutEngine.Platform.Threading;
using LayoutEngine.Platform.Update;
using Microsoft.Extensions.Logging;             // Added
using Microsoft.Extensions.DependencyInjection.Extensions; // For TryAddSingleton

namespace LayoutEngine.Platform;

using Infrastructure.EventAggregator.API.Aggregation;

/// <summary>
/// Extension methods for registering simplified platform services in the dependency injection container.
/// These services typically operate synchronously or on a single thread, suitable for testing or simple hosts.
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

        // --- Register Core Abstractions ---
        services.TryAddSingleton<ITimeProvider, SystemTimeProvider>();
        // Use a frame timing strategy that executes frames immediately (synchronously)
        services.TryAddSingleton<IFrameTimingStrategy, SynchronousFrameTimingStrategy>();

        // --- Register Core Platform Services ---
        // Lifecycle Coordinator is crucial for managing state transitions
        services.TryAddSingleton<IDocumentLifecycleCoordinator, DocumentLifecycleCoordinator>();
        // Add other platform adapters/providers as needed
        // services.TryAddSingleton<IElementAdapter, ElementAdapter>();
        // services.TryAddSingleton<IWindowProvider, DefaultWindowProvider>();
        // services.TryAddSingleton<IResourceErrorHandler, ResourceErrorHandler>();
        // services.TryAddSingleton<IResourceLoader, ResourceLoader>(); // Register if needed by simplified services

        // --- Register Simplified Schedulers and Coordinator ---
        // Note: Ensure dependencies like IEventAggregator, IStyleEngine, ILayoutEngine, etc.,
        // are registered *before* these schedulers if they depend on them.

        // Frame scheduler that uses the synchronous timing strategy
        services.TryAddSingleton<IFrameScheduler, SimplifiedFrameScheduler>();

        // Update scheduler that processes updates immediately and calls subsystem ProcessUpdatesAsync
        // Requires IEventAggregator, IDocumentLifecycleCoordinator, IStyleEngine, ILayoutEngine
        services.TryAddSingleton<IUpdateScheduler>(sp => new SimplifiedUpdateScheduler(
            sp.GetRequiredService<IEventAggregator>(),
            sp.GetRequiredService<IDocumentLifecycleCoordinator>(),
            sp.GetRequiredService<IStyleEngine>(),       // Added dependency
            sp.GetRequiredService<ILayoutEngine>(),      // Added dependency
            sp.GetService<ILogger<SimplifiedUpdateScheduler>>() // Optional Logger dependency
        ));

        // Threading coordinator that executes all actions "synchronously" (queues then processes)
        services.TryAddSingleton<IThreadingCoordinator, SimplifiedThreadingCoordinator>();

        // Register Simplified Idle Task Scheduler if available/needed
        // services.TryAddSingleton<IIdleTaskScheduler, SimplifiedIdleTaskScheduler>();

        return services;
    }

    /// <summary>
    /// Adds simplified platform services with configuration options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Action to configure simplified platform options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimplifiedPlatformServices(
        this IServiceCollection services,
        Action<SimplifiedPlatformOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Configure and register the options object itself
        var options = new SimplifiedPlatformOptions();
        configureOptions(options);
        services.AddSingleton(options); // Register the options object

        // Register the core simplified services
        services.AddSimplifiedPlatformServices();

        // Example of configuring a service based on options:
        // Override the default SynchronousFrameTimingStrategy registration if options differ
        services.AddSingleton<SynchronousFrameTimingStrategy>(sp =>
            new SynchronousFrameTimingStrategy(
                sp.GetRequiredService<ITimeProvider>(),
                options.TargetFramesPerSecond // Use configured FPS
            ));
        // Override IFrameTimingStrategy to use the configured instance
        services.AddSingleton<IFrameTimingStrategy>(sp => sp.GetRequiredService<SynchronousFrameTimingStrategy>());


        // Configure SimplifiedUpdateScheduler if options affect it (e.g., MaxUpdatesPerFrame)
        // Since SimplifiedUpdateScheduler takes dependencies, we might need to re-register it
        // or use PostConfigure, but direct configuration in AddSingleton is often simpler.
        // services.AddSingleton<IUpdateScheduler>(sp => new SimplifiedUpdateScheduler(... use options ...));


        return services;
    }
}

/// <summary>
/// Configuration options specifically for the simplified platform services.
/// </summary>
public class SimplifiedPlatformOptions
{
    /// <summary>
    /// Gets or sets the target frames per second for the synchronous frame timing strategy.
    /// </summary>
    public int TargetFramesPerSecond { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum updates the simplified scheduler processes per explicit call.
    /// </summary>
    public int MaxUpdatesPerProcessingCall { get; set; } = 50; // Renamed for clarity

    // Add other options relevant to simplified services if needed
}