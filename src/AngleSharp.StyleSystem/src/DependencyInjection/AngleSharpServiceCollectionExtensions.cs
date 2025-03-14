using System;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp;
using AngleSharp.Browser;
using AngleSharp.Css;
using AngleSharp.Css.Parser;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io;

namespace AngleSharp.StyleSystem.DependencyInjection
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extension methods for registering AngleSharp services in a dependency injection container.
    /// </summary>
    public static class AngleSharpServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AngleSharp services from the given browsing context to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="context">The browsing context to extract services from.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAngleSharpServices(
            this IServiceCollection services,
            IBrowsingContext context)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Register the browsing context itself
            services.AddSingleton<IBrowsingContext>(context);

            // Register common AngleSharp services
            RegisterServiceIfAvailable<ICssParser>(services, context);
            RegisterServiceIfAvailable<ICssSelectorParser>(services, context);
            RegisterServiceIfAvailable<IDeclarationFactory>(services, context);
            RegisterServiceIfAvailable<IRenderDevice>(services, context);
            RegisterServiceIfAvailable<IHtmlParser>(services, context);
            RegisterServiceIfAvailable<IDocumentFactory>(services, context);
            RegisterServiceIfAvailable<ICssDefaultStyleSheetProvider>(services, context);
            RegisterServiceIfAvailable<IMarkupFormatter>(services, context);
            RegisterServiceIfAvailable<IStyleFormatter>(services, context);

            return services;
        }

        /// <summary>
        /// Adds core AngleSharp services with a new browsing context to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">Optional configuration for creating a new browsing context.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAngleSharp(
            this IServiceCollection services,
            IConfiguration? configuration = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Create a new context with the provided configuration or a default one
            var config = configuration ?? Configuration.Default;
            var context = BrowsingContext.New(config);

            // Register all available services
            return services.AddAngleSharpServices(context);
        }

        /// <summary>
        /// Registers a service from the browsing context if it's available.
        /// </summary>
        private static void RegisterServiceIfAvailable<T>(
            IServiceCollection services,
            IBrowsingContext context) where T : class
        {
            var service = context.GetService<T>();
            if (service != null)
            {
                services.AddSingleton<T>(service);
            }
        }

        /// <summary>
        /// Registers a collection of services from the browsing context if they're available.
        /// </summary>
        private static void RegisterServicesIfAvailable<T>(
            IServiceCollection services,
            IBrowsingContext context) where T : class
        {
            var servicesList = context.GetServices<T>().ToList();
            if (servicesList.Count > 0)
            {
                foreach (var service in servicesList)
                {
                    services.AddSingleton<T>(service);
                }

                // Also register the collection itself
                services.AddSingleton<IEnumerable<T>>(servicesList);
            }
        }
    }
}