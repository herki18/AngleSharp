namespace AngleSharp.StyleSystem.Threading;

using System;
using AngleSharp.StyleSystem.Integration;
using Interfaces;

/// <summary>
/// Extension methods for IStyleRecalcScheduler integration with the DocumentLifecycleCoordinator.
/// </summary>
public static class StyleRecalcSchedulerExtensions
{
    /// <summary>
    /// Integrates the style recalc scheduler with the document lifecycle coordinator.
    /// </summary>
    /// <param name="coordinator">The document lifecycle coordinator.</param>
    /// <param name="scheduler">The style recalc scheduler.</param>
    /// <returns>The document lifecycle coordinator for chaining.</returns>
    public static DocumentLifecycleCoordinator UseStyleRecalcScheduler(
        this DocumentLifecycleCoordinator coordinator, IStyleRecalcScheduler scheduler)
    {
        if (coordinator == null)
            throw new ArgumentNullException(nameof(coordinator));
        if (scheduler == null)
            throw new ArgumentNullException(nameof(scheduler));

        // Set up integration (to be expanded in a real implementation)

        return coordinator;
    }
}