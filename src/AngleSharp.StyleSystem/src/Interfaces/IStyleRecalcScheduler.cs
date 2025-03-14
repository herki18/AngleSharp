namespace AngleSharp.StyleSystem.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Schedules style recalculation based on invalidations.
/// </summary>
public interface IStyleRecalcScheduler : IDisposable
{
    /// <summary>
    /// Schedules style recalculation for a single element.
    /// </summary>
    /// <param name="element">The element to recalculate style for.</param>
    /// <param name="priority">The priority of the recalculation.</param>
    void ScheduleElementRecalc(IElement element, RecalcPriority priority = RecalcPriority.Normal);

    /// <summary>
    /// Schedules style recalculation for an entire subtree of elements.
    /// </summary>
    /// <param name="rootElement">The root element of the subtree.</param>
    /// <param name="priority">The priority of the recalculation.</param>
    void ScheduleSubtreeRecalc(IElement rootElement, RecalcPriority priority = RecalcPriority.Normal);

    /// <summary>
    /// Processes pending style recalculations immediately.
    /// </summary>
    void ProcessImmediately();

    /// <summary>
    /// Processes pending style recalculations asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels all pending style recalculation work.
    /// </summary>
    void CancelPendingWork();

    /// <summary>
    /// Gets whether there is pending style recalculation work.
    /// </summary>
    bool HasPendingWork { get; }

    /// <summary>
    /// Gets or sets the throttle interval in milliseconds.
    /// </summary>
    int ThrottleIntervalMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size for processing elements.
    /// </summary>
    int MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets whether to use worker threads for style recalculation.
    /// </summary>
    bool UseWorkerThreads { get; set; }

    /// <summary>
    /// Forces all pending styles to be calculated at the end of the current frame.
    /// </summary>
    void FlushPendingStylesAtEndOfFrame();

    /// <summary>
    /// Sets a callback to be invoked when style recalculation is complete.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    void SetCompletionCallback(Action callback);
}