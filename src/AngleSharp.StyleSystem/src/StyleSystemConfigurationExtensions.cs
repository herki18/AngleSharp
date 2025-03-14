using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp.Browser;
using AngleSharp.StyleSystem.DependencyInjection;
using AngleSharp.StyleSystem.Services;

namespace AngleSharp.StyleSystem
{
    using Css.Parser;

    /// <summary>
    /// Extensions for integrating StyleSystem with AngleSharp configuration.
    /// </summary>
    public static class StyleSystemConfigurationExtensions
    {
        /// <summary>
        /// Adds StyleSystem to the AngleSharp configuration using dependency injection.
        /// </summary>
        /// <param name="configuration">The configuration to extend.</param>
        /// <returns>The extended configuration.</returns>
        public static IConfiguration WithStyleSystemDI(this IConfiguration configuration)
        {
            return WithStyleSystemDI(configuration, options => { });
        }

        /// <summary>
        /// Adds StyleSystem to the AngleSharp configuration using dependency injection with custom options.
        /// </summary>
        /// <param name="configuration">The configuration to extend.</param>
        /// <param name="configure">Action to configure StyleSystem options.</param>
        /// <returns>The extended configuration.</returns>
        public static IConfiguration WithStyleSystemDI(
            this IConfiguration configuration,
            Action<StyleSystemOptions> configure)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Create service collection and register StyleSystem
            var services = new ServiceCollection();

            var context = BrowsingContext.New(configuration);

            // Add IBrowsingContext factory to service collection
            services.AddSingleton(provider => context);

            ICssParser? cssParser = context.GetService<ICssParser>();
            if(cssParser != null)
            {
                services.AddSingleton(provider => cssParser);
            }

            // Add StyleSystem with options
            services.AddStyleSystem(configure);

            // Build provider
            var serviceProvider = services.BuildServiceProvider();

            // Create integration service
            var integrationService = new AngleSharpServiceIntegration(serviceProvider);

            // Register with AngleSharp
            return new Configuration(
                configuration.Services.Concat(new[] { integrationService }));
        }

        /// <summary>
        /// Legacy extension method for backward compatibility.
        /// Redirects to the new DI-based implementation.
        /// </summary>
        /// <param name="configuration">The configuration to extend.</param>
        /// <returns>The extended configuration.</returns>
        public static IConfiguration WithStyleSystem(this IConfiguration configuration)
        {
            return WithStyleSystemDI(configuration);
        }

        /// <summary>
        /// Legacy extension method for backward compatibility.
        /// Redirects to the new DI-based implementation.
        /// </summary>
        /// <param name="configuration">The configuration to extend.</param>
        /// <param name="configure">Action to configure StyleSystem options.</param>
        /// <returns>The extended configuration.</returns>
        public static IConfiguration WithStyleSystem(
            this IConfiguration configuration,
            Action<StyleSystemOptions> configure)
        {
            return WithStyleSystemDI(configuration, configure);
        }
    }
}