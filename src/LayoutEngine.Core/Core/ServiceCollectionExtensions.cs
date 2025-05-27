namespace LayoutEngine.Core.Core;

using AngleSharp;
using Infrastructure.CacheManager.DI;
using Infrastructure.EventAggregator.DI;
using LayoutEngine.Core.Layout;
using LayoutEngine.Core.Render;
using LayoutEngine.Core.Style.Internal;
using LayoutEngine.Core.Style.Public;
using LayoutEngine.Core.Viewport;
using Microsoft.Extensions.DependencyInjection;
using Style;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLayoutEngine(this IServiceCollection services)
    {
        services.AddEventAggregatorModule();
        services.AddCacheManagerModule();

        var config = new LayoutEngineConfiguration
        {
            TargetFramesPerSecond = 60
        };
        services.AddSingleton(config);

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
        services.AddSingleton<DocumentLifecycleStateMachine>();

        // Core systems
        services.AddStyleSystem();
        services.AddSingleton<ILayoutSystem, LayoutSystem>();

        // Register all dependencies for RenderSystem
        services.AddSingleton<FragmentRegistry>();
        services.AddSingleton<RenderCommandGenerator>();
        services.AddSingleton<RenderTreeWalker>();
        services.AddSingleton<IRenderSystem, RenderSystem>();

        services.AddSingleton<ViewportManager>();
        services.AddSingleton<DomScrollEventBridge>();

        // Register the document manager
        services.AddSingleton<IDocumentManager, DocumentManager>();

        // Register the DOM mutation tracker and its factory
        services.AddSingleton<IMutationObserverFactory, MutationObserverFactory>();
        services.AddSingleton<IDomMutationTracker, DomMutationTracker>();



        return services;
    }
}