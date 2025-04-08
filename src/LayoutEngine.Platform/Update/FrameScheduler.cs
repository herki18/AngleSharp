using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LayoutEngine.Contracts.Platform.Abstractions;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Aggregation;

namespace LayoutEngine.Platform.Update;

using Abstractions;

/// <summary>
/// Schedules and coordinates animation frame execution.
/// </summary>
public sealed class FrameScheduler : IFrameScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly ITimeProvider _timeProvider;
    private readonly IFrameTimingStrategy _timingStrategy;
    private readonly ConcurrentDictionary<int, FrameCallback> _callbacks = new();
    private readonly List<ISubscriptionToken> _subscriptions = new();

    private long _frameNumber;
    private double _lastFrameTime;
    private bool _isRunning;
    private bool _isDisposed;
    private int _nextCallbackId;
    private bool _synchronousMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameScheduler"/> class.
    /// </summary>
    public FrameScheduler(
        IEventAggregator eventAggregator,
        IThreadingCoordinator threadingCoordinator,
        IFrameTimingStrategy timingStrategy,
        ITimeProvider? timeProvider = null)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
        _timingStrategy = timingStrategy ?? throw new ArgumentNullException(nameof(timingStrategy));
        _timeProvider = timeProvider ?? new SystemTimeProvider();

        // Initialize synchronous mode from timing strategy
        _synchronousMode = _timingStrategy.IsSynchronousModeEnabled;
    }

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public long CurrentFrameNumber => _frameNumber;

    /// <summary>
    /// Gets the timestamp of the last frame.
    /// </summary>
    public double LastFrameTime => _lastFrameTime;

    /// <summary>
    /// Registers a callback to be executed on the next animation frame.
    /// </summary>
    public int RequestAnimationFrame(Action<double> callback)
    {
        ThrowIfDisposed();
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        var callbackId = Interlocked.Increment(ref _nextCallbackId);
        var frameCallback = new FrameCallback(callback);
        _callbacks[callbackId] = frameCallback;
        EnsureFrameLoopRunning();
        return callbackId;
    }

    /// <summary>
    /// Cancels a previously scheduled animation frame callback.
    /// </summary>
    public void CancelAnimationFrame(int requestId)
    {
        ThrowIfDisposed();
        _callbacks.TryRemove(requestId, out _);
    }

    /// <summary>
    /// Enables or disables synchronous mode for testing.
    /// </summary>
    public void EnableSynchronousMode(bool enabled)
    {
        _synchronousMode = enabled;
    }

    /// <summary>
    /// Executes a single frame synchronously (for testing).
    /// </summary>
    public void RunFrameSynchronously()
    {
        if (!_synchronousMode)
            throw new InvalidOperationException("Synchronous mode must be enabled to run frames synchronously");

        RunFrameLoop();
    }

    /// <summary>
    /// Ensures the frame loop is running.
    /// </summary>
    private void EnsureFrameLoopRunning()
    {
        if (_isRunning)
            return;

        _isRunning = true;

        if (_synchronousMode)
            return; // In synchronous mode, don't automatically schedule frames

        // Use the platform timing strategy to schedule the next frame
        _timingStrategy.RequestNextFrame(RunFrameLoop);
    }

    /// <summary>
    /// Executes a single frame in the animation loop.
    /// </summary>
    private void RunFrameLoop()
    {
        if (_isDisposed)
        {
            _isRunning = false;
            return;
        }

        if (_callbacks.IsEmpty)
        {
            _isRunning = false;
            return;
        }

        // Get current time from the time provider
        _lastFrameTime = _timeProvider.GetCurrentTimeMilliseconds();
        _frameNumber++;

        try
        {
            // Signal the beginning of the frame
            _eventAggregator.Publish(new BeginFrameEvent(_frameNumber, _lastFrameTime));

            // Execute all frame callbacks
            InvokeFrameCallbacks(_lastFrameTime);

            // Signal the end of the frame
            _eventAggregator.Publish(new EndFrameEvent(_frameNumber, _lastFrameTime));
        }
        catch (Exception)
        {
            // Log the exception
        }
        finally
        {
            if (_isRunning && !_isDisposed && !_synchronousMode)
            {
                RequestBrowserAnimationFrame();
            }
            else if (!_synchronousMode)
            {
                _isRunning = false;
            }
        }
    }

    /// <summary>
    /// Invokes all registered frame callbacks.
    /// </summary>
    private void InvokeFrameCallbacks(double timestamp)
    {
        var callbacksCopy = new List<FrameCallback>(_callbacks.Values);
        _callbacks.Clear();

        foreach (var callback in callbacksCopy)
        {
            try
            {
                callback.Invoke(timestamp);
            }
            catch (Exception)
            {
                // Log the exception
            }
        }
    }

    /// <summary>
    /// Requests the next animation frame using the platform timing strategy.
    /// </summary>
    private void RequestBrowserAnimationFrame()
    {
        _timingStrategy.RequestNextFrame(RunFrameLoop);
    }

    // For testability
    internal bool IsRunning => _isRunning;
    internal int PendingCallbackCount => _callbacks.Count;

    /// <summary>
    /// Throws an exception if the scheduler is disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(FrameScheduler));
        }
    }

    /// <summary>
    /// Disposes the frame scheduler.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }

        _subscriptions.Clear();
        _callbacks.Clear();
        _isRunning = false;
    }

    /// <summary>
    /// Represents a callback to be executed on an animation frame.
    /// </summary>
    private class FrameCallback
    {
        private readonly Action<double> _callback;

        public FrameCallback(Action<double> callback)
        {
            _callback = callback;
        }

        public void Invoke(double timestamp)
        {
            _callback(timestamp);
        }
    }
}