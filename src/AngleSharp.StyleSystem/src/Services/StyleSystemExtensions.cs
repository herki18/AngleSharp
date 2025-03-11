using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp;
using AngleSharp.Browser;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Services;
using AngleSharp.StyleSystem.Storage;

namespace AngleSharp.StyleSystem
{
    /// <summary>
    /// Extensions to integrate the StyleSystem with AngleSharp.
    /// </summary>
    public static class StyleSystemExtensions
    {
        private static readonly object _documentChangedKey = new object();

        /// <summary>
        /// Registers the StyleSystem service with the configuration.
        /// </summary>
        /// <param name="configuration">The configuration to extend.</param>
        /// <returns>The modified configuration.</returns>
        public static IConfiguration WithStyleSystem(this IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var service = new StyleSystemService();
            var services = configuration.Services.Concat(new[] { service });

            return new Configuration(services);
        }

        /// <summary>
        /// Registers the StyleSystem service with the configuration and provides additional options.
        /// </summary>
        /// <param name="configuration">The configuration to extend.</param>
        /// <param name="configureAction">The action to configure the StyleSystem.</param>
        /// <returns>The modified configuration.</returns>
        public static IConfiguration WithStyleSystem(this IConfiguration configuration, Action<StyleSystemOptions> configureAction)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configureAction == null)
                throw new ArgumentNullException(nameof(configureAction));

            var options = new StyleSystemOptions();
            configureAction(options);

            var service = new StyleSystemService();
            var services = configuration.Services.Concat(new[] { service });

            return new Configuration(services);
        }

        /// <summary>
        /// Gets the StyleSystem service from the browsing context.
        /// </summary>
        /// <param name="context">The browsing context.</param>
        /// <returns>The StyleSystem service if registered; otherwise, null.</returns>
        public static StyleSystemService? GetStyleSystem(this IBrowsingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Find the service in the registered services
            StyleSystemService? service = null;

            foreach (var svc in context.GetServices<object>())
            {
                if (svc is StyleSystemService styleSystem)
                {
                    service = styleSystem;
                    break;
                }
            }

            // Initialize with this context if found
            if (service != null && service.Context != context)
            {
                service.Initialize(context);
            }

            return service;
        }

        /// <summary>
        /// Forces a style recalculation for the entire document.
        /// </summary>
        /// <param name="context">The browsing context.</param>
        /// <returns>The browsing context for chaining.</returns>
        public static IBrowsingContext RecalculateStyles(this IBrowsingContext context)
        {
            var styleSystem = context.GetStyleSystem();
            if (styleSystem != null)
            {
                styleSystem.ForceStyleUpdate(context);
            }
            return context;
        }

        /// <summary>
        /// Gets the computed style for an element.
        /// </summary>
        /// <param name="element">The element to get computed style for.</param>
        /// <returns>The computed style or null if style system is not available.</returns>
        public static IComputedStyle? GetComputedStyle(this IElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var context = element.OwnerDocument?.Context;
            if (context == null)
                return null;

            var styleSystem = context.GetStyleSystem();
            return styleSystem?.StyleEngine?.ComputeElementStyle(element);
        }

        /// <summary>
        /// Gets the computed style for an element with a pseudo-element.
        /// </summary>
        /// <param name="element">The element to get computed style for.</param>
        /// <param name="pseudoElement">The pseudo-element selector (e.g., "::before", "::after").</param>
        /// <returns>The computed style or null if style system is not available.</returns>
        public static IComputedStyle? GetComputedStyle(this IElement element, string pseudoElement)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // Normalize pseudo-element format
            if (pseudoElement != null && !pseudoElement.StartsWith("::"))
            {
                pseudoElement = pseudoElement.StartsWith(":") ? ":" + pseudoElement : "::" + pseudoElement;
            }

            var context = element.OwnerDocument?.Context;
            if (context == null)
                return null;

            var styleSystem = context.GetStyleSystem();
            return styleSystem?.StyleEngine?.ComputeElementStyle(element, pseudoElement);
        }

        /// <summary>
        /// Gets the optimization metrics from the style system.
        /// </summary>
        /// <param name="context">The browsing context.</param>
        /// <returns>The optimization metrics or null if not available.</returns>
        public static OptimizationMetrics? GetStyleOptimizationMetrics(this IBrowsingContext context)
        {
            var styleSystem = context.GetStyleSystem();
            return styleSystem?.GetOptimizationMetrics();
        }

        /// <summary>
        /// Notifies the StyleSystem that a document has been activated in the browsing context.
        /// This is useful when you're manually activating documents.
        /// </summary>
        /// <param name="context">The browsing context.</param>
        /// <param name="document">The document that was activated.</param>
        /// <returns>The browsing context for chaining.</returns>
        public static IBrowsingContext NotifyDocumentChanged(this IBrowsingContext context, IDocument document)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var styleSystem = context.GetStyleSystem();
            if (styleSystem != null)
            {
                styleSystem.NotifyDocumentChanged(document);
            }

            return context;
        }
    }

    /// <summary>
    /// Options for configuring the StyleSystem.
    /// </summary>
    public class StyleSystemOptions
    {
        /// <summary>
        /// Gets or sets whether style optimization is enabled. Default is true.
        /// </summary>
        public bool EnableOptimization { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to collect optimization metrics. Default is false.
        /// </summary>
        public bool CollectMetrics { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to update styles immediately after context creation. Default is true.
        /// </summary>
        public bool UpdateStylesImmediately { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of worker threads to use for style computation.
        /// Default is 0, which uses Environment.ProcessorCount - 1.
        /// </summary>
        public int MaxWorkerThreads { get; set; } = 0;
    }
}