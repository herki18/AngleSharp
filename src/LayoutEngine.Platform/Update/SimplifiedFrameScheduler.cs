using System;
using System.Collections.Generic;
using LayoutEngine.Contracts.Platform.Abstractions;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;

namespace LayoutEngine.Platform.Update;

using Abstractions;

/// <summary>
/// Simplified frame scheduler that runs animations on a single thread.
/// </summary>
public class SimplifiedFrameScheduler : IFrameScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ITimeProvider _timeProvider;
    private readonly IFrameTimingStrategy _timingStrategy;
    private readonly Dictionary<int, Action<double>> _callbacks = new();

    private long _frameNumber;
    private double _lastFrameTime;
    private int _nextCallbackId = 1;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the SimplifiedFrameScheduler.
    /// </summary>
    public SimplifiedFrameScheduler(
        IEventAggregator eventAggregator,
        IFrameTimingStrategy timingStrategy,
        ITimeProvider? timeProvider = null)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _timingStrategy = timingStrategy ?? throw new ArgumentNullException(nameof(timingStrategy));
        _timeProvider = timeProvider ?? new SystemTimeProvider();
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
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedFrameScheduler));

        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        int callbackId = _nextCallbackId++;
        _callbacks[callbackId] = callback;

        // If we're in synchronous mode, run the frame loop immediately
        if (_timingStrategy.IsSynchronousModeEnabled)
        {
            ProcessFrame();
        }
        else
        {
            // Otherwise, request a frame through the timing strategy
            _timingStrategy.RequestNextFrame(ProcessFrame);
        }

        return callbackId;
    }

    /// <summary>
    /// Cancels a previously scheduled animation frame callback.
    /// </summary>
    public void CancelAnimationFrame(int requestId)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SimplifiedFrameScheduler));

        _callbacks.Remove(requestId);
    }

    /// <summary>
    /// Processes a single frame, executing all scheduled callbacks.
    /// </summary>
    public void ProcessFrame()
    {
        if (_isDisposed)
            return;

        // Get current time
        _lastFrameTime = _timeProvider.GetCurrentTimeMilliseconds();
        _frameNumber++;

        try
        {
            // Signal the beginning of the frame
            _eventAggregator.Publish(new BeginFrameEvent(_frameNumber, _lastFrameTime), EventPriority.High);

            // Execute all scheduled callbacks
            ExecuteCallbacks(_lastFrameTime);

            // Signal the end of the frame
            _eventAggregator.Publish(new EndFrameEvent(_frameNumber, _lastFrameTime), EventPriority.High);
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.Error.WriteLine($"Error in frame processing: {ex}");
        }
    }

    /// <summary>
    /// Executes all registered callbacks and clears the queue.
    /// </summary>
    private void ExecuteCallbacks(double timestamp)
    {
        if (_callbacks.Count == 0)
            return;

        // Create a copy of the callbacks to avoid modification issues during iteration
        var callbacksCopy = new Dictionary<int, Action<double>>(_callbacks);
        _callbacks.Clear();

        foreach (var callback in callbacksCopy.Values)
        {
            try
            {
                callback(timestamp);
            }
            catch (Exception ex)
            {
                // Log the exception but continue processing other callbacks
                Console.Error.WriteLine($"Error in animation frame callback: {ex}");
            }
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
        _callbacks.Clear();
    }
}