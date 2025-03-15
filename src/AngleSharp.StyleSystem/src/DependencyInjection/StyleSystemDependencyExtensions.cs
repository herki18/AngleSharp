namespace AngleSharp.StyleSystem.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Reflection;
using AngleSharp.StyleSystem.Services;
using Integration;
using Interfaces;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring StyleSystem with proper Dependency Injection.
/// </summary>
public static class StyleSystemDependencyExtensions
{
    /// <summary>
    /// Registers StyleSystem services directly with AngleSharp's service collection for backward compatibility.
    /// </summary>
    /// <param name="context">The browsing context to register services with.</param>
    /// <param name="serviceProvider">The DI service provider containing StyleSystem services.</param>
    /// <returns>The browsing context for chaining.</returns>
    public static IBrowsingContext RegisterStyleSystemServices(
        this IBrowsingContext context,
        IServiceProvider serviceProvider)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        // Get the private _services field via reflection
        var contextType = context.GetType();
        var servicesField = contextType.GetField("_services",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (servicesField?.GetValue(context) is List<object> services)
        {
            // Add our services to the AngleSharp service collection
            RegisterService<IStyleEngine>(services, serviceProvider);
            RegisterService<IStyleCache>(services, serviceProvider);
            RegisterService<IStyleSheetManager>(services, serviceProvider);
            RegisterService<IStyleTreeResolver>(services, serviceProvider);
            RegisterService<IStyleRecalcScheduler>(services, serviceProvider);
            RegisterService<IStyleInvalidationTracker>(services, serviceProvider);
            RegisterService<IComputedStyleBuilder>(services, serviceProvider);
            RegisterService<IComputedStyleFactory>(services, serviceProvider);
            RegisterService<IDocumentLifecycleCoordinator>(services, serviceProvider);
            RegisterService<IPropertyTreeManager>(services, serviceProvider);
            RegisterService<IVariableResolver>(services, serviceProvider);
            RegisterService<IValueCalculator>(services, serviceProvider);
            RegisterService<ICascadeResolver>(services, serviceProvider);
            RegisterService<IInheritanceProcessor>(services, serviceProvider);
            RegisterService<IRuleCollector>(services, serviceProvider);

            // Register concrete types as well for backward compatibility
            var styleSystemService = serviceProvider.GetService<StyleSystemService>();
            if (styleSystemService != null)
            {
                services.Add(styleSystemService);

                // Ensure StyleSystemService is initialized
                if (!styleSystemService.IsInitialized)
                {
                    styleSystemService.Initialize(context);
                }
            }
        }

        return context;
    }

    private static void RegisterService<T>(List<object> services, IServiceProvider serviceProvider)
        where T : class
    {
        var service = serviceProvider.GetService<T>();
        if (service != null)
        {
            services.Add(service);
        }
    }

    /// <summary>
    /// Directly registers all required StyleSystem services with a service collection
    /// </summary>
    /// <param name="services">The service collection to register with</param>
    /// <param name="context">The browsing context to use</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAngleSharpStyleSystem(
        this IServiceCollection services,
        IBrowsingContext context,
        Action<StyleSystemOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        // Register the context first
        services.AddSingleton(context);

        // Register core AngleSharp services
        services.AddAngleSharpServices(context);

        // Register StyleSystem services
        var options = new StyleSystemOptions();
        configure?.Invoke(options);

        // Fix the null reference warning by passing the non-null lambda
        services.AddStyleSystem(opt =>
        {
            opt.BatchSize = options.BatchSize;
            opt.CollectMetrics = options.CollectMetrics;
            opt.EnableOptimization = options.EnableOptimization;
            opt.LoadUserAgentStylesheets = options.LoadUserAgentStylesheets;
            opt.MaxWorkerThreads = options.MaxWorkerThreads;
            opt.ThrottleIntervalMs = options.ThrottleIntervalMs;
            opt.UpdateStylesImmediately = options.UpdateStylesImmediately;
        });

        return services;
    }
}