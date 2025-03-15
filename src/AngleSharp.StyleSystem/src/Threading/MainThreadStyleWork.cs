namespace AngleSharp.StyleSystem.Threading;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Integration;
using AngleSharp.StyleSystem.Models;
using Interfaces;

/// <summary>
/// Implementation of the main thread style computation work manager.
/// </summary>
public class MainThreadStyleWork : IMainThreadStyleWork
{
    private readonly IStyleEngine _styleEngine;
    private readonly ConcurrentDictionary<IElement, RecalcPriority> _pendingElements;
    private readonly object _processingLock = new object();
    private bool _isProcessing;

    /// <summary>
    /// Creates a new instance of the MainThreadStyleWork class.
    /// </summary>
    /// <param name="styleEngine">The style engine to use for style computation.</param>
    public MainThreadStyleWork(IStyleEngine styleEngine)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _pendingElements = new ConcurrentDictionary<IElement, RecalcPriority>();
    }

    /// <inheritdoc/>
    public void EnqueueElement(IElement element, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        _pendingElements.AddOrUpdate(element, priority, (_, existing) =>
            (byte)priority > (byte)existing ? priority : existing);
    }

    /// <inheritdoc/>
    public void ProcessSync()
    {
        lock (_processingLock)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
        }

        try
        {
            ProcessPendingElements();
        }
        finally
        {
            lock (_processingLock)
            {
                _isProcessing = false;
            }
        }
    }

    /// <inheritdoc/>
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        bool shouldProcess = false;

        lock (_processingLock)
        {
            if (!_isProcessing)
            {
                _isProcessing = true;
                shouldProcess = true;
            }
        }

        if (shouldProcess)
        {
            try
            {
                // Since this needs to run on the main thread, we just defer the execution
                // but still process synchronously
                await Task.Yield();

                if (!cancellationToken.IsCancellationRequested)
                {
                    ProcessPendingElements();
                }
            }
            finally
            {
                lock (_processingLock)
                {
                    _isProcessing = false;
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool HasPendingWork => _pendingElements.Count > 0;

    /// <summary>
    /// Processes all pending elements.
    /// </summary>
    private void ProcessPendingElements()
    {
        // Create a snapshot of elements to process sorted by priority
        var elements = _pendingElements.OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        // Clear the pending elements dictionary
        _pendingElements.Clear();

        // Process each element
        foreach (var element in elements)
        {
            try
            {
                _styleEngine.ComputeElementStyle(element);
            }
            catch (Exception ex)
            {
                // Log error but continue processing
                Console.WriteLine($"Error computing styles for element: {ex.Message}");
            }
        }
    }
}