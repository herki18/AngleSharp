using System;
using AngleSharp.Browser;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.DependencyInjection;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;

namespace AngleSharp.StyleSystem
{
    /// <summary>
    /// Extension methods for accessing StyleSystem services from AngleSharp's context.
    /// </summary>
    public static class StyleSystemContextExtensions
    {
        /// <summary>
        /// Gets a StyleSystem service from the context.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <param name="context">The browsing context.</param>
        /// <returns>The service instance or null if not found.</returns>
        public static T? GetStyleSystemService<T>(this IBrowsingContext context) where T : class
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var integration = context.GetService<AngleSharpServiceIntegration>();
            if (integration != null)
            {
                // Initialize on first access
                integration.Initialize(context);
                return integration.GetService<T>();
            }

            // Fallback to legacy service resolution for backward compatibility
            return context.GetService<T>();
        }

        /// <summary>
        /// Gets a required StyleSystem service from the context.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <param name="context">The browsing context.</param>
        /// <returns>The service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the service is not found.</exception>
        public static T GetRequiredStyleSystemService<T>(this IBrowsingContext context) where T : class
        {
            var service = GetStyleSystemService<T>(context);
            if (service == null)
            {
                throw new InvalidOperationException($"Required service of type {typeof(T).Name} was not found. Ensure StyleSystem is properly registered with dependency injection.");
            }

            return service;
        }

        /// <summary>
        /// Gets the computed style for an element using the StyleSystem.
        /// </summary>
        /// <param name="element">The element to get computed style for.</param>
        /// <returns>The computed style for the element, or null if StyleSystem is not available.</returns>
        public static IComputedStyle? GetComputedStyleDI(this IElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var context = element.OwnerDocument?.Context;
            if (context == null)
                return null;

            var styleEngine = context.GetStyleSystemService<IStyleEngine>();
            return styleEngine?.ComputeElementStyle(element);
        }

        /// <summary>
        /// Gets the computed style for an element with a specific pseudo-element using the StyleSystem.
        /// </summary>
        /// <param name="element">The element to get computed style for.</param>
        /// <param name="pseudoElement">The pseudo-element selector (e.g. "::before").</param>
        /// <returns>The computed style for the element, or null if StyleSystem is not available.</returns>
        public static IComputedStyle? GetComputedStyleDI(this IElement element, string pseudoElement)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var context = element.OwnerDocument?.Context;
            if (context == null)
                return null;

            var styleEngine = context.GetStyleSystemService<IStyleEngine>();
            return styleEngine?.ComputeElementStyle(element, pseudoElement);
        }

        /// <summary>
        /// Gets optimization metrics from the StyleSystem.
        /// </summary>
        /// <param name="context">The browsing context.</param>
        /// <returns>The optimization metrics or null if unavailable.</returns>
        public static OptimizationMetrics? GetStyleOptimizationMetricsDI(this IBrowsingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var styleEngine = context.GetStyleSystemService<IStyleEngine>();
            return styleEngine?.GetOptimizationMetrics();
        }

        /// <summary>
        /// Recalculates styles for the entire document using the StyleSystem.
        /// </summary>
        /// <param name="context">The browsing context.</param>
        /// <returns>The browsing context for chaining.</returns>
        public static IBrowsingContext RecalculateStylesDI(this IBrowsingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var styleEngine = context.GetStyleSystemService<IStyleEngine>();
            if (styleEngine != null && context.Active?.DocumentElement != null)
            {
                styleEngine.UpdateStyles(context.Active.DocumentElement);
            }

            return context;
        }
    }
}