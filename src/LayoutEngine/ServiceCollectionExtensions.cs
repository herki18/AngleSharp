// using AngleSharp;
// using AngleSharp.Html.Parser;
// using LayoutEngine.Contracts;
// using LayoutEngine.Contracts.Rendering;
// using LayoutEngine.Platform;
// using LayoutEngine.StyleSystem;
// using LayoutEngine.LayoutSystem;
// using LayoutEngine.RenderSystem;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using System;
//
// namespace LayoutEngine
// {
//     /// <summary>
//     /// Extension methods for setting up the LayoutEngine in a dependency injection container.
//     /// </summary>
//     public static class ServiceCollectionExtensions
//     {
//         /// <summary>
//         /// Adds the LayoutEngine rendering system to the service collection.
//         /// </summary>
//         public static IServiceCollection AddLayoutEngine(this IServiceCollection services, Action<LayoutEngineOptions> configureOptions = null)
//         {
//             // Configure options
//             var options = new LayoutEngineOptions();
//             configureOptions?.Invoke(options);
//
//             // Register options
//             services.AddSingleton(options);
//
//             // Register AngleSharp configuration
//             services.AddSingleton<IConfiguration>(provider =>
//             {
//                 var config = Configuration.Default
//                     .WithCss()
//                     .WithRenderDevice()
//                     .WithDefaultLoader();
//
//                 return config;
//             });
//
//             // Register core services
//             services.AddSingleton<RenderingEngine>();
//
//             // Register platform services
//             services.AddLayoutEnginePlatform(options);
//
//             // Register system services
//             services.AddStyleSystem();
//             services.AddLayoutSystem();
//             services.AddRenderSystem();
//
//             return services;
//         }
//
//         /// <summary>
//         /// Adds the LayoutEngine.Platform services to the service collection.
//         /// </summary>
//         private static IServiceCollection AddLayoutEnginePlatform(this IServiceCollection services, LayoutEngineOptions options)
//         {
//             // Register platform services
//             services.AddSingleton<IDocumentLifecycleCoordinator, DocumentLifecycleCoordinator>();
//             services.AddSingleton<IEventAggregator, EventAggregator>();
//             services.AddSingleton<IUpdateScheduler, UpdateScheduler>();
//             services.AddSingleton<ICacheManager, CacheManager>();
//             services.AddSingleton<IThreadingCoordinator, ThreadingCoordinator>(provider =>
//                 new ThreadingCoordinator(options.MaxWorkerThreads));
//             services.AddSingleton<IDomMutationTracker, DomMutationTracker>();
//             services.AddSingleton<IElementAdapter, ElementAdapter>();
//
//             // Register factories
//             services.AddSingleton<IStyleEngineFactory, StyleEngineFactory>();
//             services.AddSingleton<ILayoutEngineFactory, LayoutEngineFactory>();
//             services.AddSingleton<IRenderEngineFactory, RenderEngineFactory>();
//
//             return services;
//         }
//
//         /// <summary>
//         /// Adds the LayoutEngine.StyleSystem services to the service collection.
//         /// </summary>
//         private static IServiceCollection AddStyleSystem(this IServiceCollection services)
//         {
//             // Register style system supporting services
//             services.AddSingleton<IRuleCollector, RuleCollector>();
//             services.AddSingleton<ICascadeResolver, CascadeResolver>();
//             services.AddSingleton<IInheritanceProcessor, InheritanceProcessor>();
//             services.AddSingleton<IComputedStyleBuilder, ComputedStyleBuilder>();
//             services.AddSingleton<IVariableResolver, VariableResolver>();
//             services.AddSingleton<IStyleSheetManager, StyleSheetManager>();
//
//             // Note: StyleEngine itself is NOT registered directly
//             // It is created per-document via the factory
//
//             return services;
//         }
//
//         /// <summary>
//         /// Adds the LayoutEngine.LayoutSystem services to the service collection.
//         /// </summary>
//         private static IServiceCollection AddLayoutSystem(this IServiceCollection services)
//         {
//             // Register layout system supporting services
//             services.AddSingleton<IBoxTreeBuilder, BoxTreeBuilder>();
//             services.AddSingleton<ILayoutAlgorithmSelector, LayoutAlgorithmSelector>();
//             services.AddSingleton<IFragmentTreeManager, FragmentTreeManager>();
//
//             // Register layout algorithms
//             services.AddSingleton<IBlockLayoutAlgorithm, BlockLayoutAlgorithm>();
//             services.AddSingleton<IInlineLayoutAlgorithm, InlineLayoutAlgorithm>();
//             services.AddSingleton<IFlexLayoutAlgorithm, FlexLayoutAlgorithm>();
//             services.AddSingleton<IGridLayoutAlgorithm, GridLayoutAlgorithm>();
//
//             // Note: LayoutEngine itself is NOT registered directly
//             // It is created per-document via the factory
//
//             return services;
//         }
//
//         /// <summary>
//         /// Adds the LayoutEngine.RenderSystem services to the service collection.
//         /// </summary>
//         private static IServiceCollection AddRenderSystem(this IServiceCollection services)
//         {
//             // Register render system supporting services
//             services.AddSingleton<IPaintTreeBuilder, PaintTreeBuilder>();
//             services.AddSingleton<IDamageTracker, DamageTracker>();
//             services.AddSingleton<IPaintChunkManager, PaintChunkManager>();
//             services.AddSingleton<ICompositingLayerManager, CompositingLayerManager>();
//
//             // Register renderers
//             services.AddSingleton<ITextRenderer, TextRenderer>();
//             services.AddSingleton<IImageRenderer, ImageRenderer>();
//             services.AddSingleton<IShapeRenderer, ShapeRenderer>();
//
//             // Note: RenderEngine itself is NOT registered directly
//             // It is created per-document via the factory
//
//             return services;
//         }
//     }
//
//     /// <summary>
//     /// Configuration options for the LayoutEngine.
//     /// </summary>
//     public class LayoutEngineOptions
//     {
//         /// <summary>
//         /// Gets or sets the default device pixel ratio.
//         /// </summary>
//         public float DevicePixelRatio { get; set; } = 1.0f;
//
//         /// <summary>
//         /// Gets or sets whether to use hardware acceleration when available.
//         /// </summary>
//         public bool UseHardwareAcceleration { get; set; } = true;
//
//         /// <summary>
//         /// Gets or sets the maximum number of worker threads.
//         /// </summary>
//         public int MaxWorkerThreads { get; set; } = Environment.ProcessorCount;
//
//         /// <summary>
//         /// Gets or sets the maximum size of the style cache.
//         /// </summary>
//         public int StyleCacheSize { get; set; } = 10000;
//
//         /// <summary>
//         /// Gets or sets the maximum size of the layout cache.
//         /// </summary>
//         public int LayoutCacheSize { get; set; } = 5000;
//
//         /// <summary>
//         /// Gets or sets whether to automatically handle memory pressure.
//         /// </summary>
//         public bool AutomaticMemoryManagement { get; set; } = true;
//
//         /// <summary>
//         /// Gets or sets whether to use parallel processing for style computation.
//         /// </summary>
//         public bool ParallelStyleComputation { get; set; } = true;
//
//         /// <summary>
//         /// Gets or sets whether to use parallel processing for layout computation.
//         /// </summary>
//         public bool ParallelLayoutComputation { get; set; } = true;
//     }
//
//     /// <summary>
//     /// Public interfaces for the LayoutEngine API
//     /// </summary>
//     namespace Contracts
//     {
//         using AngleSharp.Dom;
//
//         /// <summary>
//         /// Represents a rendering context for a specific document.
//         /// </summary>
//         public interface IDocumentRenderingContext
//         {
//             /// <summary>
//             /// Gets the document associated with this context.
//             /// </summary>
//             IDocument Document { get; }
//
//             /// <summary>
//             /// Forces a complete render cycle (style, layout, paint).
//             /// </summary>
//             void ForceRender();
//
//             /// <summary>
//             /// Renders the document to the specified target.
//             /// </summary>
//             void RenderTo(IRenderTarget target);
//
//             /// <summary>
//             /// Gets the computed style for an element.
//             /// </summary>
//             IComputedStyle GetComputedStyle(IElement element);
//
//             /// <summary>
//             /// Gets layout information for an element.
//             /// </summary>
//             ILayoutInfo GetLayoutInfo(IElement element);
//         }
//     }
// }