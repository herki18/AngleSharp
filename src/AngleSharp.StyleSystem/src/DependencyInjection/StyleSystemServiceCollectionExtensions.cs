using System;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp.Css;
using AngleSharp.StyleSystem.Computation;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;
using AngleSharp.StyleSystem.Services;
using AngleSharp.StyleSystem.Tasks;
using AngleSharp.StyleSystem.Threading;
using AngleSharp.StyleSystem.Events;

namespace AngleSharp.StyleSystem.DependencyInjection
{
    public static class StyleSystemServiceCollectionExtensions
    {
        public static IServiceCollection AddStyleSystem(this IServiceCollection services)
        {
            return services.AddStyleSystem(options => { });
        }

        public static IServiceCollection AddStyleSystem(
            this IServiceCollection services,
            Action<StyleSystemOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new StyleSystemOptions();
            configure(options);

            services.AddSingleton(options);

            // Add the EventAggregator first as it's needed by many services
            services.AddSingleton<IEventAggregator, EventAggregator>();

            services.AddSingleton<IPropertyTreeManager, PropertyTreeManager>();
            services.AddSingleton<IStyleSheetManager, StyleSheetManager>(sp =>
                new StyleSheetManager(
                    sp.GetRequiredService<IBrowsingContext>(),
                    options.LoadUserAgentStylesheets));

            services.AddSingleton<IStyleCache, StyleCache>();
            services.AddSingleton<IStyleInvalidationTracker, StyleInvalidationTracker>();
            services.AddSingleton<IRuleCollector, RuleCollector>();
            services.AddSingleton<ICascadeResolver, CascadeResolver>();
            services.AddSingleton<IInheritanceProcessor, InheritanceProcessor>();
            services.AddSingleton<IVariableResolver, VariableResolver>();
            services.AddSingleton<IValueCalculator, ValueCalculator>();
            services.AddSingleton<IStylePropertyMapper, StylePropertyMapper>();

            // Services that use the EventAggregator
            services.AddSingleton<DocumentLifecycleCoordinator>();
            services.AddSingleton<DomMutationTracker>();
            services.AddSingleton<IStyleEngine, StyleEngine>();
            services.AddSingleton<IStyleTreeResolver, StyleTreeResolver>();

            services.AddSingleton<IComputedStyleBuilder, ComputedStyleBuilder>();
            services.AddSingleton<IComputedStyleFactory, ComputedStyleFactory>();

            services.AddSingleton<IStyleTaskScheduler>(sp =>
                new StyleTaskScheduler(
                    sp.GetRequiredService<IStyleEngine>(),
                    options.ThrottleIntervalMs,
                    options.BatchSize));

            services.AddSingleton<IMainThreadStyleWork, MainThreadStyleWork>();

            if (options.MaxWorkerThreads > 0)
            {
                services.AddSingleton<IWorkerThreadStylePool>(sp =>
                    new WorkerThreadStylePool(
                        (StyleEngine)sp.GetRequiredService<IStyleEngine>(),
                        options.MaxWorkerThreads));
            }

            services.AddSingleton<IStyleRecalcScheduler>(sp =>
                new StyleRecalcScheduler(
                    sp.GetRequiredService<IStyleEngine>(),
                    sp.GetRequiredService<IBrowsingContext>(),
                    sp.GetRequiredService<IStyleTaskScheduler>(),
                    sp.GetRequiredService<IEventAggregator>(),
                    sp.GetRequiredService<IMainThreadStyleWork>(),
                    options.MaxWorkerThreads > 0 ? sp.GetService<IWorkerThreadStylePool>() : null,
                    options.ThrottleIntervalMs,
                    options.BatchSize));

            services.AddSingleton<IRenderDevice>(sp =>
                sp.GetService<IRenderDevice>() ?? new DefaultRenderDevice());

            services.AddSingleton<StyleSystemService>();

            return services;
        }
    }
}