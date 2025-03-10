namespace AngleSharp.StyleSystem.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Schedules style recalculation work for elements, coordinating when and how styles are computed.
/// </summary>
public interface IStyleRecalcScheduler
{
    /// <summary>
    /// Schedules style recalculation for a single element.
    /// </summary>
    /// <param name="element">The element to recalculate styles for.</param>
    /// <param name="priority">The priority of the recalculation.</param>
    void ScheduleElementRecalc(IElement element, RecalcPriority priority = RecalcPriority.Normal);

    /// <summary>
    /// Schedules style recalculation for an element and all its descendants.
    /// </summary>
    /// <param name="rootElement">The root element of the subtree to recalculate.</param>
    /// <param name="priority">The priority of the recalculation.</param>
    void ScheduleSubtreeRecalc(IElement rootElement, RecalcPriority priority = RecalcPriority.Normal);

    /// <summary>
    /// Processes all scheduled style recalculations synchronously.
    /// </summary>
    void ProcessImmediately();

    /// <summary>
    /// Processes all scheduled style recalculations asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels all pending style recalculations.
    /// </summary>
    void CancelPendingWork();

    /// <summary>
    /// Gets whether there is any style recalculation work pending.
    /// </summary>
    bool HasPendingWork { get; }
}