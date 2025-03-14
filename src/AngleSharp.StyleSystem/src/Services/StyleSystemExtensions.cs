namespace AngleSharp.StyleSystem.Services;

using System;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Storage;

/// <summary>
/// Extension methods for working with the StyleSystem.
/// </summary>
public static class StyleSystemExtensions
{
    #region Context Extensions

    /// <summary>
    /// Gets the StyleSystem service from the context, initializing it if necessary.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <returns>The StyleSystem service or null if not available.</returns>
    public static StyleSystemService? GetStyleSystem(this IBrowsingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var service = context.GetService<StyleSystemService>();
        if (service != null && !service.IsInitialized)
        {
            service.Initialize(context);
        }
        return service;
    }

    /// <summary>
    /// Gets a service from the StyleSystem.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <param name="context">The browsing context.</param>
    /// <returns>The requested service or null if not available.</returns>
    public static T? GetStyleSystemService<T>(this IBrowsingContext context) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var styleSystem = context.GetStyleSystem();
        return styleSystem?.GetService<T>();
    }

    /// <summary>
    /// Gets a required service from the StyleSystem. Throws an exception if not available.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <param name="context">The browsing context.</param>
    /// <returns>The requested service.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the service is not available.</exception>
    public static T GetRequiredStyleSystemService<T>(this IBrowsingContext context) where T : class
    {
        var service = GetStyleSystemService<T>(context);
        if (service == null)
        {
            throw new InvalidOperationException(
                $"Required service of type {typeof(T).Name} was not found. " +
                "Ensure StyleSystem is properly registered with the AngleSharp configuration.");
        }
        return service;
    }

    /// <summary>
    /// Recalculates styles for the current document in the context.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <returns>The browsing context.</returns>
    public static IBrowsingContext RecalculateStyles(this IBrowsingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var styleSystem = context.GetStyleSystem();
        if (styleSystem != null)
        {
            styleSystem.ForceStyleUpdate(context);
        }
        return context;
    }

    /// <summary>
    /// Gets metrics about style optimization.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <returns>Optimization metrics or null if not available.</returns>
    public static OptimizationMetrics? GetStyleOptimizationMetrics(this IBrowsingContext context)
    {
        var styleEngine = context.GetStyleSystemService<IStyleEngine>();
        return styleEngine?.GetOptimizationMetrics();
    }

    /// <summary>
    /// Notifies the StyleSystem that a document has changed.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <param name="document">The document that changed.</param>
    /// <returns>The browsing context.</returns>
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

    /// <summary>
    /// Enables style optimization for the browsing context.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <param name="collectMetrics">Whether to collect optimization metrics.</param>
    /// <returns>The browsing context.</returns>
    public static IBrowsingContext EnableStyleOptimization(this IBrowsingContext context, bool collectMetrics = false)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var styleEngine = context.GetService<StyleEngine>();
        if (styleEngine != null)
        {
            styleEngine.OptimizationEnabled = true;
            styleEngine.CollectMetrics = collectMetrics;
        }
        return context;
    }

    /// <summary>
    /// Disables style optimization for the browsing context.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <returns>The browsing context.</returns>
    public static IBrowsingContext DisableStyleOptimization(this IBrowsingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var styleEngine = context.GetService<StyleEngine>();
        if (styleEngine != null)
        {
            styleEngine.OptimizationEnabled = false;
            styleEngine.CollectMetrics = false;
        }
        return context;
    }

    /// <summary>
    /// Runs style optimization for all styles in the context.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <returns>The browsing context.</returns>
    public static IBrowsingContext OptimizeAllStyles(this IBrowsingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var styleEngine = context.GetService<StyleEngine>();
        styleEngine?.OptimizeAllStyles();
        return context;
    }

    #endregion

    #region Element Extensions

    /// <summary>
    /// Gets the computed style for an element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The computed style or null if not available.</returns>
    public static IComputedStyle? GetComputedStyle(this IElement element)
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
    /// Gets the computed style for an element with a pseudo-element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="pseudoElement">The pseudo-element selector.</param>
    /// <returns>The computed style or null if not available.</returns>
    public static IComputedStyle? GetComputedStyle(this IElement element, string pseudoElement)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (pseudoElement != null && !pseudoElement.StartsWith("::"))
        {
            pseudoElement = pseudoElement.StartsWith(":") ? ":" + pseudoElement : "::" + pseudoElement;
        }

        var context = element.OwnerDocument?.Context;
        if (context == null)
            return null;

        var styleEngine = context.GetStyleSystemService<IStyleEngine>();
        return styleEngine?.ComputeElementStyle(element, pseudoElement);
    }

    #endregion
}