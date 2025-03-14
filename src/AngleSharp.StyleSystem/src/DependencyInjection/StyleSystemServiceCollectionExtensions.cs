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
    /// Extension methods for registering StyleSystem services with an IServiceCollection.
    /// </summary>
    public static class StyleSystemServiceCollectionExtensions
    {
        /// <summary>
        /// Adds StyleSystem services to the specified IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddStyleSystem(this IServiceCollection services)
        {
            return services.AddStyleSystem(options => { });
        }

        /// <summary>
        /// Adds StyleSystem services to the specified IServiceCollection with custom options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <param name="configure">Action to configure StyleSystem options.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddStyleSystem(
            this IServiceCollection services,
            Action<StyleSystemOptions> configure)
        {
            // Register options
            var options = new StyleSystemOptions();
            configure(options);
            services.AddSingleton(options);

            // Register core services
            services.AddSingleton<IStyleEngine, StyleEngine>();
            services.AddSingleton<IPropertyTreeManager, PropertyTreeManager>();
            services.AddSingleton<IStyleSheetManager, StyleSheetManager>(sp =>
                new StyleSheetManager(sp.GetRequiredService<IBrowsingContext>(),
                    options.LoadUserAgentStylesheets));
            services.AddSingleton<IStyleCache, StyleCache>();
            services.AddSingleton<IStyleInvalidationTracker, StyleInvalidationTracker>();

            // Register computation services
            services.AddSingleton<IRuleCollector, RuleCollector>();
            services.AddSingleton<ICascadeResolver, CascadeResolver>();
            services.AddSingleton<IInheritanceProcessor, InheritanceProcessor>();
            services.AddSingleton<IVariableResolver, VariableResolver>();
            services.AddSingleton<IValueCalculator, ValueCalculator>();
            services.AddSingleton<IStylePropertyMapper, StylePropertyMapper>();
            services.AddSingleton<IComputedStyleBuilder, ComputedStyleBuilder>();
            services.AddSingleton<IComputedStyleFactory, ComputedStyleFactory>();

            // Register integration services
            services.AddSingleton<DocumentLifecycleCoordinator>();
            services.AddSingleton<DomMutationTracker>();
            services.AddSingleton<IStyleTreeResolver, StyleTreeResolver>();

            // Register threading services
            services.AddSingleton<IStyleTaskScheduler>(sp =>
                new StyleTaskScheduler(
                    sp.GetRequiredService<IStyleEngine>(),
                    options.ThrottleIntervalMs,
                    options.BatchSize));

            services.AddSingleton<IMainThreadStyleWork, MainThreadStyleWork>();
            services.AddSingleton<IStyleRecalcScheduler>(sp =>
                new StyleRecalcScheduler(
                    sp.GetRequiredService<IStyleEngine>(),
                    sp.GetRequiredService<IBrowsingContext>(),
                    sp.GetRequiredService<IStyleTaskScheduler>(),
                    sp.GetRequiredService<IMainThreadStyleWork>(),
                    options.MaxWorkerThreads > 0 ? sp.GetService<IWorkerThreadStylePool>() : null,
                    options.ThrottleIntervalMs,
                    options.BatchSize));

            // Conditionally register worker thread pool if enabled
            if (options.MaxWorkerThreads > 0)
            {
                services.AddSingleton<IWorkerThreadStylePool>(sp =>
                    new WorkerThreadStylePool(
                        (StyleEngine)sp.GetRequiredService<IStyleEngine>(),
                        options.MaxWorkerThreads));
            }

            // Register default render device if none is provided
            services.AddSingleton<IRenderDevice>(sp =>
                sp.GetService<IRenderDevice>() ?? new DefaultRenderDevice());

            // Register the service that ties everything together
            services.AddSingleton<StyleSystemService>();

            return services;
        }
    }
}