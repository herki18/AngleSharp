namespace AngleSharp.StyleSystem.Core;

using System;
using AngleSharp.Dom;

/// <summary>
/// Extension methods for style optimization.
/// </summary>
public static class StyleOptimizationExtensions
{
    /// <summary>
    /// Enables style tree optimization for the browsing context.
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
    /// Disables style tree optimization for the browsing context.
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
    /// Gets optimization metrics for the browsing context.
    /// </summary>
    /// <param name="context">The browsing context.</param>
    /// <returns>The optimization metrics or null if not available.</returns>
    public static OptimizationMetrics? GetStyleOptimizationMetrics(this IBrowsingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var styleEngine = context.GetService<StyleEngine>();
        return styleEngine?.GetOptimizationMetrics();
    }

    /// <summary>
    /// Forces optimization of all styles in the browsing context.
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
}