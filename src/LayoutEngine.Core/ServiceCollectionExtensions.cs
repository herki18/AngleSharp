namespace LayoutEngine.Core;

using AngleSharp;
using Infrastructure.CacheManager.DI;
using Infrastructure.EventAggregator.DI;
using LayoutEngine.Contracts.Platform.Dom.Abstractions;
using Microsoft.Extensions.DependencyInjection;

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