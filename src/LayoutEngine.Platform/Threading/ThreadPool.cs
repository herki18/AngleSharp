using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace LayoutEngine.Platform.Threading;

using Contracts.Platform.Threading;

/// <summary>
/// Manages a pool of worker threads for background processing.
/// Uses System.Threading.Channels for efficient work distribution.
/// </summary>
public sealed class ThreadPool : IThreadPool, IDisposable
{
    private readonly Channel<WorkItem> _workChannel;
    private readonly List<Task> _workers;
    private readonly CancellationTokenSource _cts;
    private readonly ThreadPoolOptions _options;
    private int _activeThreadCount;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadPool"/> class.
    /// </summary>
    /// <param name="options">The thread pool options.</param>
    public ThreadPool(IOptions<ThreadPoolOptions> options)
    {
        _options = options?.Value ?? new ThreadPoolOptions();

        // Create unbounded channel for work items
        _workChannel = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _cts = new CancellationTokenSource();
        _workers = new List<Task>(_options.MaxThreads);

        // Start worker tasks
        for (var i = 0; i < _options.MaxThreads; i++)
        {
            _workers.Add(Task.Factory.StartNew(
                WorkerLoop,
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default));
        }
    }

    /// <summary>
    /// Gets the number of active threads in the pool.
    /// </summary>
    public int ActiveThreadCount => _activeThreadCount;

    /// <summary>
    /// Queues a work item for execution on a worker thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the work item.</param>
    public void QueueWorkItem(Action action, WorkItemPriority priority = WorkItemPriority.Normal)
    {
        ThrowIfDisposed();

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var workItem = new WorkItem(action, priority);
        _workChannel.Writer.TryWrite(workItem);
    }

    /// <summary>
    /// Queues a work item for execution and returns a task that completes when the work is done.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the work item.</param>
    /// <returns>A task that completes when the work item completes.</returns>
    public Task QueueWorkItemAsync(Action action, WorkItemPriority priority = WorkItemPriority.Normal)
    {
        ThrowIfDisposed();

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var tcs = new TaskCompletionSource<bool>();

        QueueWorkItem(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, priority);

        return tcs.Task;
    }

    /// <summary>
    /// Worker loop that processes work items from the channel.
    /// </summary>
    private async Task WorkerLoop()
    {
        try
        {
            while (await _workChannel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                while (_workChannel.Reader.TryRead(out var workItem))
                {
                    try
                    {
                        Interlocked.Increment(ref _activeThreadCount);
                        workItem.Action();
                    }
                    catch (Exception)
                    {
                        // Log exception
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _activeThreadCount);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception)
        {
            // Log exception
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ThreadPool));
        }
    }

    /// <summary>
    /// Disposes the ThreadPool and cancels all pending work.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        try
        {
            // Cancel all work and wait for workers to complete
            _cts.Cancel();
            Task.WaitAll(_workers.ToArray(), TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore exceptions during shutdown
        }
        finally
        {
            _cts.Dispose();
        }
    }

    /// <summary>
    /// Represents a work item in the thread pool.
    /// </summary>
    private class WorkItem
    {
        public WorkItem(Action action, WorkItemPriority priority)
        {
            Action = action;
            Priority = priority;
        }

        public Action Action { get; }
        public WorkItemPriority Priority { get; }
    }
}

/// <summary>
/// Options for configuring the thread pool.
/// </summary>
public class ThreadPoolOptions
{
    /// <summary>
    /// Gets or sets the maximum number of threads in the pool.
    /// </summary>
    public int MaxThreads { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets whether to monitor thread health.
    /// </summary>
    public bool MonitorThreadHealth { get; set; } = true;

    /// <summary>
    /// Gets or sets the thread idle timeout in milliseconds.
    /// </summary>
    public int ThreadIdleTimeoutMs { get; set; } = 60000;
}