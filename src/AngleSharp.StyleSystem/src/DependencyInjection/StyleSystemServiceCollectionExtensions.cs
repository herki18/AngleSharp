using System;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp.Css;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;
using AngleSharp.StyleSystem.Services;
using AngleSharp.StyleSystem.Threading;
using AngleSharp.StyleSystem.Tasks;

namespace AngleSharp.StyleSystem.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering StyleSystem services with a ServiceCollection.
    /// </summary>
    public static class StyleSystemServiceCollectionExtensions
    {
        /// <summary>
        /// Adds StyleSystem services to the service collection with default options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddStyleSystem(this IServiceCollection services)
        {
            return services.AddStyleSystem(options => { });
        }

        /// <summary>
        /// Adds StyleSystem services to the service collection with customized options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configure">Action to configure StyleSystem options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddStyleSystem(
            this IServiceCollection services,
            Action<StyleSystemOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new StyleSystemOptions();
            configure(options);

            // Register options
            services.AddSingleton(options);

            // Core services - order matters for dependency resolution
            services.AddSingleton<IPropertyTreeManager, PropertyTreeManager>();
            services.AddSingleton<IStyleSheetManager, StyleSheetManager>(sp =>
                new StyleSheetManager(sp.GetRequiredService<IBrowsingContext>(),
                    options.LoadUserAgentStylesheets));
            services.AddSingleton<IStyleCache, StyleCache>();
            services.AddSingleton<IStyleInvalidationTracker, StyleInvalidationTracker>();
            services.AddSingleton<IRuleCollector, RuleCollector>();
            services.AddSingleton<ICascadeResolver, CascadeResolver>();
            services.AddSingleton<IInheritanceProcessor, InheritanceProcessor>();

            // Value processing components
            services.AddSingleton<IVariableResolver, VariableResolver>();
            services.AddSingleton<IValueCalculator, ValueCalculator>();
            services.AddSingleton<IStylePropertyMapper, StylePropertyMapper>();



            // Integration components
            services.AddSingleton<DocumentLifecycleCoordinator>();
            services.AddSingleton<DomMutationTracker>();
            services.AddSingleton<IStyleEngine, StyleEngine>();
            services.AddSingleton<IStyleTreeResolver, StyleTreeResolver>();

            // Style computation components
            services.AddSingleton<IComputedStyleBuilder, ComputedStyleBuilder>();
            services.AddSingleton<IComputedStyleFactory, ComputedStyleFactory>();

            // Task scheduling and threading
            services.AddSingleton<IStyleTaskScheduler>(sp =>
                new StyleTaskScheduler(
                    sp.GetRequiredService<IStyleEngine>(),
                    options.ThrottleIntervalMs,
                    options.BatchSize));

            services.AddSingleton<IMainThreadStyleWork, MainThreadStyleWork>();

            // Register worker thread pool if enabled
            if (options.MaxWorkerThreads > 0)
            {
                services.AddSingleton<IWorkerThreadStylePool>(sp =>
                    new WorkerThreadStylePool(
                        (StyleEngine)sp.GetRequiredService<IStyleEngine>(),
                        options.MaxWorkerThreads));
            }

            // Register style recalc scheduler
            services.AddSingleton<IStyleRecalcScheduler>(sp =>
                new StyleRecalcScheduler(
                    sp.GetRequiredService<IStyleEngine>(),
                    sp.GetRequiredService<IBrowsingContext>(),
                    sp.GetRequiredService<IStyleTaskScheduler>(),
                    sp.GetRequiredService<IMainThreadStyleWork>(),
                    options.MaxWorkerThreads > 0 ? sp.GetService<IWorkerThreadStylePool>() : null,
                    options.ThrottleIntervalMs,
                    options.BatchSize));

            // Fallback RenderDevice if not already registered
            services.AddSingleton<IRenderDevice>(sp =>
                sp.GetService<IRenderDevice>() ?? new DefaultRenderDevice());

            // Register the central StyleSystemService
            services.AddSingleton<StyleSystemService>();

            return services;
        }
    }
}