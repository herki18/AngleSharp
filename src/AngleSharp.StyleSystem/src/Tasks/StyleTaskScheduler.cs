using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AngleSharp.StyleSystem.Tasks;

using Interfaces;
using Models;

/// <summary>
/// Schedules and processes style tasks using System.Threading.Channels.
/// </summary>
public class StyleTaskScheduler : IStyleTaskScheduler, IDisposable
{
    private readonly Channel<IStyleTask> _taskChannel;
    private readonly IStyleEngine _styleEngine;
    private readonly CancellationTokenSource _processingCts = new();
    private readonly Task _processingTask;
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly int _batchSize;
    private bool _isDisposed;

    /// <summary>
    /// Creates a new StyleTaskScheduler instance.
    /// </summary>
    /// <param name="styleEngine">The style engine to use for task execution.</param>
    /// <param name="throttleIntervalMs">The throttle interval in milliseconds.</param>
    /// <param name="batchSize">The maximum number of tasks to process in a batch.</param>
    public StyleTaskScheduler(
        IStyleEngine styleEngine,
        int throttleIntervalMs = 16,
        int batchSize = 100)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _batchSize = batchSize;

        // Create a bounded channel
        var options = new BoundedChannelOptions(batchSize * 10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _taskChannel = Channel.CreateBounded<IStyleTask>(options);

        // Start the background processing task
        _processingTask = ProcessTasksLoopAsync(_processingCts.Token);

        // Start a throttled processing task
        _ = Task.Run(async () => {
            while (!_processingCts.Token.IsCancellationRequested)
            {
                await Task.Delay(throttleIntervalMs, _processingCts.Token);
                await ProcessTasksAsync(_processingCts.Token);
            }
        }, _processingCts.Token);
    }

    /// <inheritdoc />
    public void EnqueueTask(IStyleTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (_isDisposed)
            return;

        // Try to write to channel without blocking
        if (!_taskChannel.Writer.TryWrite(task))
        {
            System.Diagnostics.Debug.WriteLine("Task channel full - tasks will be processed on next cycle");
        }
    }

    /// <inheritdoc />
    public void ProcessTasks()
    {
        if (_isDisposed)
            return;

        if (_processingLock.Wait(0))
        {
            try
            {
                // Process up to _batchSize tasks
                int count = 0;
                while (count < _batchSize && _taskChannel.Reader.TryRead(out var task))
                {
                    try
                    {
                        task.Execute(_styleEngine);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error executing task: {ex.Message}");
                    }
                }
            }
            finally
            {
                _processingLock.Release();
            }
        }
    }

    /// <inheritdoc />
    public async Task ProcessTasksAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            return;

        await _processingLock.WaitAsync(cancellationToken);
        try
        {
            // Process up to _batchSize tasks
            int count = 0;
            while (count < _batchSize &&
                   !cancellationToken.IsCancellationRequested &&
                   await _taskChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_taskChannel.Reader.TryRead(out var task))
                {
                    try
                    {
                        await Task.Run(() => task.Execute(_styleEngine), cancellationToken);
                        count++;
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error executing task: {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    /// <inheritdoc />
    public bool HasPendingTasks => !_taskChannel.Reader.Completion.IsCompleted;

    /// <inheritdoc />
    public void CancelPendingTasks()
    {
        _processingCts.Cancel();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _processingCts.Cancel();
        _taskChannel.Writer.Complete();

        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch { /* Ignore task cancellation exceptions */ }

        _processingCts.Dispose();
        _processingLock.Dispose();

        _isDisposed = true;
    }

    private async Task ProcessTasksLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Process tasks as they become available
            await foreach (var task in _taskChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await _processingLock.WaitAsync(cancellationToken);
                try
                {
                    task.Execute(_styleEngine);

                    // Yield based on priority to allow other work
                    if (task.Priority < RecalcPriority.High)
                    {
                        await Task.Yield();
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"Error executing task: {ex.Message}");
                }
                finally
                {
                    _processingLock.Release();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal cancellation, do nothing
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in task processing loop: {ex.Message}");
        }
    }
}