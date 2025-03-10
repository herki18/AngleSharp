namespace AngleSharp.StyleSystem.Core.Interfaces;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages a pool of worker threads for parallel style computation.
/// </summary>
/// <remarks>
/// This component is defined as an interface but not fully implemented in the current version.
/// It represents the future extension point for multi-threading support.
/// </remarks>
public interface IWorkerThreadStylePool : IDisposable
{
    /// <summary>
    /// Enqueues an element for style computation in the worker thread pool.
    /// </summary>
    /// <param name="element">The element to compute styles for.</param>
    /// <param name="priority">The priority of the computation work.</param>
    void EnqueueElement(IElement element, RecalcPriority priority = RecalcPriority.Normal);

    /// <summary>
    /// Enqueues a batch of elements for style computation in the worker thread pool.
    /// </summary>
    /// <param name="elements">The elements to compute styles for.</param>
    /// <param name="priority">The priority of the computation work.</param>
    void EnqueueElements(IEnumerable<IElement> elements, RecalcPriority priority = RecalcPriority.Normal);

    /// <summary>
    /// Starts processing the queued style computations.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartProcessingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes the computed styles back to the main thread.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SynchronizeResultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the worker thread pool is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets a value indicating whether there is pending work.
    /// </summary>
    bool HasPendingWork { get; }

    /// <summary>
    /// Gets the number of worker threads in the pool.
    /// </summary>
    int ThreadCount { get; }
}