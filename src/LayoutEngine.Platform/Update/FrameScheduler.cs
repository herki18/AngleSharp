using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Infrastructure.EventAggregator;

namespace LayoutEngine.Platform.Update;

using System.Threading;
using Contracts.Platform.Events;
using Contracts.Platform.Updates;
using Contracts.Threading;
using Infrastructure.EventAggregator.API.Aggregation;

/// <summary>
/// Schedules animation frames.
/// Manages frame timing and coordinates with the browser's rendering cycle.
/// </summary>
public sealed class FrameScheduler : IFrameScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly ConcurrentDictionary<int, FrameCallback> _callbacks = new();
    private readonly Stopwatch _frameTimer = new();
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private long _frameNumber;
    private double _lastFrameTime;
    private bool _isRunning;
    private bool _isDisposed;
    private int _nextCallbackId;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameScheduler"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing frame events.</param>
    /// <param name="threadingCoordinator">The threading coordinator for thread scheduling.</param>
    public FrameScheduler(
        IEventAggregator eventAggregator,
        IThreadingCoordinator threadingCoordinator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
    }

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public long CurrentFrameNumber => _frameNumber;

    /// <summary>
    /// Gets the time of the last frame.
    /// </summary>
    public double LastFrameTime => _lastFrameTime;

    /// <summary>
    /// Schedules a callback for the next animation frame.
    /// </summary>
    /// <param name="callback">The callback to invoke when the frame starts.</param>
    /// <returns>A request ID that can be used to cancel the callback.</returns>
    public int RequestAnimationFrame(Action<double> callback)
    {
        ThrowIfDisposed();

        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        // Generate callback ID
        var callbackId = Interlocked.Increment(ref _nextCallbackId);

        // Create and store frame callback
        var frameCallback = new FrameCallback(callback);
        _callbacks[callbackId] = frameCallback;

        // Start frame loop if not already running
        EnsureFrameLoopRunning();

        return callbackId;
    }

    /// <summary>
    /// Cancels a scheduled animation frame callback.
    /// </summary>
    /// <param name="requestId">The request ID of the callback to cancel.</param>
    public void CancelAnimationFrame(int requestId)
    {
        ThrowIfDisposed();

        _callbacks.TryRemove(requestId, out _);
    }

    /// <summary>
    /// Ensures the frame loop is running.
    /// </summary>
    private void EnsureFrameLoopRunning()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _frameTimer.Start();

        // Start frame loop on main thread
        _threadingCoordinator.ScheduleOnMainThread(RunFrameLoop);
    }

    /// <summary>
    /// Runs the frame loop.
    /// </summary>
    private void RunFrameLoop()
    {
        if (_isDisposed)
        {
            _isRunning = false;
            return;
        }

        // If no callbacks, stop frame loop
        if (_callbacks.IsEmpty)
        {
            _isRunning = false;
            return;
        }

        // Get current time
        _lastFrameTime = _frameTimer.Elapsed.TotalMilliseconds;

        // Increment frame number
        _frameNumber++;

        try
        {
            // Publish begin frame event
            _eventAggregator.Publish(new BeginFrameEvent(_frameNumber, _lastFrameTime));

            // Invoke all callbacks
            InvokeFrameCallbacks(_lastFrameTime);

            // Publish end frame event
            _eventAggregator.Publish(new EndFrameEvent(_frameNumber, _lastFrameTime));
        }
        catch (Exception)
        {
            // Log exception
        }
        finally
        {
            // Schedule next frame if still running
            if (_isRunning && !_isDisposed)
            {
                // Use browser's requestAnimationFrame
                RequestBrowserAnimationFrame();
            }
            else
            {
                _isRunning = false;
            }
        }
    }

    /// <summary>
    /// Invokes all frame callbacks with the current frame time.
    /// </summary>
    /// <param name="timestamp">The current frame timestamp.</param>
    private void InvokeFrameCallbacks(double timestamp)
    {
        // Create a copy of the callbacks to avoid concurrent modification
        var callbacksCopy = new List<FrameCallback>(_callbacks.Values);

        // Clear callbacks (one-shot model)
        _callbacks.Clear();

        // Invoke each callback
        foreach (var callback in callbacksCopy)
        {
            try
            {
                callback.Invoke(timestamp);
            }
            catch (Exception)
            {
                // Log exception
            }
        }
    }

    /// <summary>
    /// Requests the next animation frame from the browser.
    /// </summary>
    private void RequestBrowserAnimationFrame()
    {
        // In a real browser environment, this would use window.requestAnimationFrame
        // For now, simulate with a timer on the main thread
        _threadingCoordinator.ScheduleOnMainThread(RunFrameLoop);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(FrameScheduler));
        }
    }

    /// <summary>
    /// Disposes the FrameScheduler and cancels all pending callbacks.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Unsubscribe from events
        foreach (var subscription in _subscriptions)
        {
            _eventAggregator.Unsubscribe(subscription);
        }

        _subscriptions.Clear();

        // Clear callbacks
        _callbacks.Clear();

        // Stop frame timer
        _frameTimer.Stop();

        _isRunning = false;
    }

    /// <summary>
    /// Represents a frame callback.
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