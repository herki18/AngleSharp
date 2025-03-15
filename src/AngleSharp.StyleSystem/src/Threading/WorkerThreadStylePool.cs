using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.StyleSystem.Interfaces;
using AngleSharp.StyleSystem.Models;

namespace AngleSharp.StyleSystem.Threading;

/// <summary>
/// Manages a pool of worker threads for style computation tasks using System.Threading.Channels.
/// </summary>
public class WorkerThreadStylePool : IWorkerThreadStylePool, IDisposable
{
    private readonly Channel<WorkItem> _workChannel;
    private readonly IStyleEngine _styleEngine;
    private readonly Task[] _workerTasks;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _syncLock = new();
    private readonly ConcurrentStyleCache _styleCache = new();
    private bool _isDisposed;

    /// <summary>
    /// Creates a new WorkerThreadStylePool with the specified number of worker threads.
    /// </summary>
    /// <param name="styleEngine">The style engine to use for style computations.</param>
    /// <param name="threadCount">The number of worker threads to create. If 0, defaults to processor count - 1.</param>
    public WorkerThreadStylePool(IStyleEngine styleEngine, int threadCount = 0)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));

        ThreadCount = threadCount > 0 ? threadCount : Math.Max(1, Environment.ProcessorCount - 1);

        // Create unbounded channel with single writer and multiple readers
        var options = new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        };

        _workChannel = Channel.CreateUnbounded<WorkItem>(options);

        // Initialize and start worker tasks
        _workerTasks = new Task[ThreadCount];
        for (int i = 0; i < ThreadCount; i++)
        {
            int workerId = i;
            _workerTasks[i] = WorkerLoopAsync(workerId, _cts.Token);
        }
    }

    /// <inheritdoc />
    public void EnqueueElement(IElement element, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (_isDisposed)
            return;

        var workItem = new WorkItem(element, priority);
        _workChannel.Writer.TryWrite(workItem);
    }

    /// <inheritdoc />
    public void EnqueueElements(IEnumerable<IElement> elements, RecalcPriority priority = RecalcPriority.Normal)
    {
        if (elements == null)
            throw new ArgumentNullException(nameof(elements));

        if (_isDisposed)
            return;

        foreach (var element in elements)
        {
            EnqueueElement(element, priority);
        }
    }

    /// <inheritdoc />
    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            return;

        // Processing is continuous, so this method just checks for completion
        await Task.Yield();
    }

    /// <inheritdoc />
    public async Task SynchronizeResultsAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            return;

        lock (_syncLock)
        {
            // Apply cached styles to elements
            foreach (var entry in _styleCache.GetEntries())
            {
                try
                {
                    var element = entry.Key;
                    var style = entry.Value;

                    // This would normally update the global style cache with our computed styles
                    // For demonstration, we'll just show a placeholder
                    System.Diagnostics.Debug.WriteLine($"Synchronizing computed style for element {element.NodeName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error synchronizing results: {ex.Message}");
                }
            }

            // Clear the cache after synchronization
            _styleCache.Clear();
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool IsActive => !_cts.IsCancellationRequested &&
                            _workerTasks.Any(t => t.Status == TaskStatus.Running);

    /// <inheritdoc />
    public bool HasPendingWork => !_workChannel.Reader.Completion.IsCompleted ||
                                  _styleCache.Count > 0;

    /// <inheritdoc />
    public int ThreadCount { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _cts.Cancel();
        _workChannel.Writer.Complete();

        try
        {
            // Wait for worker tasks to complete with timeout
            Task.WaitAll(_workerTasks, TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Ignore task cancellation exceptions
        }

        _cts.Dispose();
        _styleCache.Clear();

        _isDisposed = true;
    }

    private async Task WorkerLoopAsync(int workerId, CancellationToken cancellationToken)
    {
        try
        {
            // Process items as they become available
            await foreach (var workItem in _workChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var style = _styleEngine.ComputeElementStyle(workItem.Element);

                    // Store computed style in local cache for later synchronization
                    _styleCache.Add(workItem.Element, style);

                    // Allow other tasks to execute, weighted by priority
                    if (workItem.Priority < RecalcPriority.High)
                    {
                        await Task.Yield();
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Log but continue processing
                    System.Diagnostics.Debug.WriteLine(
                        $"Worker {workerId}: Error computing style: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal cancellation, do nothing
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Worker {workerId}: Fatal error: {ex.Message}");
        }
    }

    /// <summary>
    /// Represents a work item for style computation.
    /// </summary>
    private class WorkItem
    {
        public IElement Element { get; }
        public RecalcPriority Priority { get; }

        public WorkItem(IElement element, RecalcPriority priority)
        {
            Element = element;
            Priority = priority;
        }
    }

    /// <summary>
    /// Thread-safe cache for computed styles.
    /// </summary>
    private class ConcurrentStyleCache
    {
        private readonly Dictionary<IElement, IComputedStyle> _cache = new Dictionary<IElement, IComputedStyle>();
        private readonly object _lock = new object();

        public void Add(IElement element, IComputedStyle style)
        {
            lock (_lock)
            {
                _cache[element] = style;
            }
        }

        public IEnumerable<KeyValuePair<IElement, IComputedStyle>> GetEntries()
        {
            lock (_lock)
            {
                return _cache.ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }
    }
}