using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LayoutEngine.Platform.Update;
using System.Threading;
using Abstractions;
using Contracts.Platform.Abstractions;
using Contracts.Platform.Dom.Abstractions;
using Contracts.Platform.Events;
using Contracts.Platform.Threading;
using Contracts.Platform.Updates;
using DOM.Abstractions;
using Infrastructure.EventAggregator.API.Aggregation;

public class FrameScheduler : IFrameScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<int, FrameCallback> _callbacks = new();
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private long _frameNumber;
    private double _lastFrameTime;
    private bool _isRunning;
    private bool _isDisposed;
    private int _nextCallbackId;
    private bool _synchronousMode;

    public FrameScheduler(
        IEventAggregator eventAggregator,
        IThreadingCoordinator threadingCoordinator,
        ITimeProvider? timeProvider = null)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
        _timeProvider = timeProvider ?? new SystemTimeProvider();
    }

    public long CurrentFrameNumber => _frameNumber;
    public double LastFrameTime => _lastFrameTime;

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

    public void CancelAnimationFrame(int requestId)
    {
        ThrowIfDisposed();
        _callbacks.TryRemove(requestId, out _);
    }

    // Added for testing
    public void EnableSynchronousMode(bool enabled)
    {
        _synchronousMode = enabled;
    }

    // Added for testing - run a single frame synchronously
    public void RunFrameSynchronously()
    {
        if (!_synchronousMode)
            throw new InvalidOperationException("Synchronous mode must be enabled to run frames synchronously");

        RunFrameLoop();
    }

    // Changed from private to protected virtual for testability
    protected virtual void EnsureFrameLoopRunning()
    {
        if (_isRunning)
            return;
        _isRunning = true;

        if (_synchronousMode)
            return; // In synchronous mode, don't automatically schedule frames

        _threadingCoordinator.ScheduleOnMainThread(RunFrameLoop);
    }

    // Changed from private to protected virtual for testability
    protected virtual void RunFrameLoop()
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
        _lastFrameTime = _timeProvider.GetCurrentTimeMilliseconds();
        _frameNumber++;
        try
        {
            _eventAggregator.Publish(new BeginFrameEvent(_frameNumber, _lastFrameTime));
            InvokeFrameCallbacks(_lastFrameTime);
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

    // Changed from private to protected virtual for testability
    protected virtual void InvokeFrameCallbacks(double timestamp)
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

    // Changed from private to protected virtual for testability
    protected virtual void RequestBrowserAnimationFrame()
    {
        _threadingCoordinator.ScheduleOnMainThread(RunFrameLoop);
    }

    // For testability
    internal bool IsRunning => _isRunning;
    internal int PendingCallbackCount => _callbacks.Count;

    protected virtual void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(FrameScheduler));
        }
    }

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