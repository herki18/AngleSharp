namespace AngleSharp.StyleSystem.Core.Interfaces;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages style computation work that must be executed on the main thread.
/// </summary>
public interface IMainThreadStyleWork
{
    /// <summary>
    /// Enqueues an element for style computation on the main thread.
    /// </summary>
    /// <param name="element">The element to compute styles for.</param>
    /// <param name="priority">The priority of the computation work.</param>
    void EnqueueElement(IElement element, RecalcPriority priority = RecalcPriority.Normal);

    /// <summary>
    /// Processes all pending style work synchronously on the main thread.
    /// </summary>
    void ProcessSync();

    /// <summary>
    /// Processes all pending style work asynchronously on the main thread.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether there is pending work.
    /// </summary>
    bool HasPendingWork { get; }
}