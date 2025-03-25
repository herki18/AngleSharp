using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
namespace LayoutEngine.Platform.Update;
using Contracts.Platform.Events;
using Contracts.Platform.Threading;
using Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Aggregation;
public sealed class UpdateScheduler : IUpdateScheduler, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly ConcurrentDictionary<Guid, IVisualUpdate> _pendingUpdates = new();
    private readonly ConcurrentDictionary<UpdatePriority, ConcurrentQueue<IVisualUpdate>> _priorityQueues = new();
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private readonly UpdatePriority[] _priorityLevels;
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private bool _isPaused;
    private bool _isProcessing;
    private bool _isDisposed;
    public UpdateScheduler(
        IEventAggregator eventAggregator,
        IThreadingCoordinator threadingCoordinator)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
        _priorityLevels = new[]
        {
            UpdatePriority.Critical,
            UpdatePriority.High,
            UpdatePriority.Normal,
            UpdatePriority.Low
        };
        foreach (var priority in _priorityLevels)
        {
            _priorityQueues[priority] = new ConcurrentQueue<IVisualUpdate>();
        }
        _subscriptions.Add(_eventAggregator.Subscribe<BeginFrameEvent>(OnBeginFrame));
    }
    public void ScheduleUpdate(IVisualUpdate update)
    {
        ScheduleUpdate(update, UpdatePriority.Normal);
    }
    public void ScheduleUpdate(IVisualUpdate update, UpdatePriority priority)
    {
        ThrowIfDisposed();
        if (update == null)
            throw new ArgumentNullException(nameof(update));
        if (!_pendingUpdates.TryAdd(update.Id, update))
        {
            return;
        }
        var queue = _priorityQueues[priority];
        queue.Enqueue(update);
        if (!_isProcessing && !_isPaused)
        {
            ScheduleProcessing();
        }
    }
    public bool CancelUpdate(IVisualUpdate update)
    {
        ThrowIfDisposed();
        if (update == null)
            throw new ArgumentNullException(nameof(update));
        return _pendingUpdates.TryRemove(update.Id, out _);
    }
    public void PauseUpdates()
    {
        ThrowIfDisposed();
        _isPaused = true;
    }
    public void ResumeUpdates()
    {
        ThrowIfDisposed();
        if (_isPaused)
        {
            _isPaused = false;
            if (_pendingUpdates.Count > 0 && !_isProcessing)
            {
                ScheduleProcessing();
            }
        }
    }
    private void OnBeginFrame(BeginFrameEvent e)
    {
        if (!_isProcessing && !_isPaused)
        {
            ScheduleProcessing();
        }
    }
    private void ScheduleProcessing()
    {
        _isProcessing = true;
        _threadingCoordinator.ScheduleOnMainThread(ProcessUpdates);
    }
    private async void ProcessUpdates()
    {
        if (_isDisposed || _isPaused)
        {
            _isProcessing = false;
            return;
        }
        try
        {
            await _processingLock.WaitAsync();
            try
            {
                foreach (var priority in _priorityLevels)
                {
                    ProcessPriorityQueue(priority);
                }
            }
            finally
            {
                _processingLock.Release();
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            _isProcessing = false;
            if (_pendingUpdates.Count > 0 && !_isPaused)
            {
                ScheduleProcessing();
            }
        }
    }
    private void ProcessPriorityQueue(UpdatePriority priority)
    {
        var queue = _priorityQueues[priority];
        while (queue.TryDequeue(out var update))
        {
            if (_pendingUpdates.TryRemove(update.Id, out _))
            {
                try
                {
                    ProcessUpdate(update);
                }
                catch (Exception)
                {
                }
            }
        }
    }
    private void ProcessUpdate(IVisualUpdate update)
    {
        switch (update.Type)
        {
            case UpdateType.Style:
                _eventAggregator.Publish(new StyleInvalidatedEvent(new[] { update.Element }));
                break;
            case UpdateType.Layout:
                _eventAggregator.Publish(new LayoutInvalidatedEvent(new[] { update.Element }));
                break;
            case UpdateType.Render:
                _eventAggregator.Publish(new RenderInvalidatedEvent(null));
                break;
            case UpdateType.Resource:
                break;
            case UpdateType.Full:
                _eventAggregator.Publish(new StyleInvalidatedEvent(new[] { update.Element }));
                break;
        }
        _eventAggregator.Publish(new UpdateProcessedEvent(update));
    }
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(UpdateScheduler));
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
        _pendingUpdates.Clear();
        foreach (var queue in _priorityQueues.Values)
        {
            while (queue.TryDequeue(out _)) { }
        }
        _priorityQueues.Clear();
        _processingLock.Dispose();
    }
}