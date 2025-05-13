namespace LayoutEngine.Core;

using AngleSharp;
using Contracts.Platform.Lifecycle;
using Infrastructure.CacheManager.DI;
using Infrastructure.EventAggregator.DI;
using Layout;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Style;
using Viewport;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLayoutEngine(this IServiceCollection services)
    {
        // Register AngleSharp configuration
        services.AddSingleton<IConfiguration>(provider =>
        {
            var config = Configuration.Default
                .WithCss()
                .WithRenderDevice()
                .WithDefaultLoader();

            return config;
        });

        // Register the engine
        services.AddSingleton<IEngine, Engine>();

        // Lifecycle management
        services.AddSingleton<IDocumentLifecycleCoordinator, DocumentLifecycleCoordinator>();

        // Core systems
        services.AddSingleton<IStyleSystem, StyleSystem>();
        services.AddSingleton<ILayoutSystem, LayoutSystem>();

        services.AddSingleton<ViewportManager>();
        services.AddSingleton<DomScrollEventBridge>();

        // Register the document manager
        services.AddSingleton<IDocumentManager, DocumentManager>();

        // Register the DOM mutation tracker and its factory
        services.AddSingleton<IMutationObserverFactory, MutationObserverFactory>();
        services.AddSingleton<IDomMutationTracker, DomMutationTracker>();

        services.AddEventAggregatorModule();
        services.AddCacheManagerModule();

        return services;
    }
}