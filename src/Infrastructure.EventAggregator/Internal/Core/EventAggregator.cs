namespace Infrastructure.EventAggregator.Internal.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using API.Aggregation;
using API.Events;

/// <summary>
/// Implementation of the event aggregator pattern using Reactive Extensions.
/// </summary>
public class EventAggregator : IEventAggregator, IDisposable
{
    private readonly Dictionary<Type, SubjectInfo> _subjects = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly CompositeDisposable _disposable = new();
    private readonly PriorityEventQueue _eventQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _eventProcessingTask;
    private bool _isDisposed;

    /// <summary>
    /// Class to store information about event subjects.
    /// </summary>
    private class SubjectInfo : IDisposable
    {
        public object Subject { get; }
        public DateTime LastUsed { get; set; }
        public int SubscriberCount { get; set; }
        public IScheduler DefaultScheduler { get; set; }

        public SubjectInfo(object subject, IScheduler scheduler)
        {
            Subject = subject;
            LastUsed = DateTime.UtcNow;
            SubscriberCount = 0;
            DefaultScheduler = scheduler;
        }

        public void Dispose()
        {
            if (Subject is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the EventAggregator class.
    /// </summary>
    public EventAggregator()
    {
        StartCleanupTimer();
        _eventProcessingTask = Task.Run(ProcessEventsAsync);
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventData">The event data.</param>
    public void Publish<TEvent>(TEvent eventData) where TEvent : class, IEvent
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        // Determine priority
        EventPriority priority = EventPriority.Normal;
        if (eventData is IPrioritizedEvent prioritizedEvent)
        {
            priority = prioritizedEvent.Priority;
        }

        PublishInternal(eventData, priority);
    }

    /// <summary>
    /// Publishes an event with the specified priority.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventData">The event data.</param>
    /// <param name="priority">The priority to use when publishing the event.</param>
    public void Publish<TEvent>(TEvent eventData, EventPriority priority) where TEvent : class, IEvent
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        PublishInternal(eventData, priority);
    }

    /// <summary>
    /// Gets an observable for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <returns>An observable for the event type.</returns>
    public IObservable<TEvent> GetObservable<TEvent>() where TEvent : class, IEvent
    {
        return GetObservable<TEvent>(Scheduler.Default);
    }

    /// <summary>
    /// Gets an observable for an event type with a specific scheduler.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="scheduler">The scheduler to use.</param>
    /// <returns>An observable for the event type.</returns>
    public IObservable<TEvent> GetObservable<TEvent>(IScheduler scheduler) where TEvent : class, IEvent
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_subjects.TryGetValue(typeof(TEvent), out var existingSubject))
            {
                _lock.EnterWriteLock();
                try
                {
                    var subject = new Subject<TEvent>();
                    _subjects[typeof(TEvent)] = new SubjectInfo(subject, scheduler);
                    return subject.AsObservable().ObserveOn(scheduler);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            existingSubject.LastUsed = DateTime.UtcNow;
            return ((ISubject<TEvent>)existingSubject.Subject).AsObservable()
                .ObserveOn(existingSubject.DefaultScheduler);
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Subscribes to an event.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The event handler.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    public ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent
    {
        return Subscribe<TEvent>(null, handler);
    }

    /// <summary>
    /// Subscribes to an event with a filter.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="filter">The filter to apply to events.</param>
    /// <param name="handler">The event handler.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    public ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler) where TEvent : class, IEvent
    {
        return Subscribe(filter, handler, Scheduler.Default);
    }

    /// <summary>
    /// Subscribes to events with the minimum priority specified.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The event handler.</param>
    /// <param name="minimumPriority">The minimum priority of events to receive.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    public ISubscriptionToken SubscribeWithPriority<TEvent>(Action<TEvent> handler, EventPriority minimumPriority)
        where TEvent : class, IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        // Create a filter that checks event priority
        Predicate<TEvent> priorityFilter = e =>
        {
            if (e is IPrioritizedEvent prioritizedEvent)
            {
                return (int)prioritizedEvent.Priority >= (int)minimumPriority;
            }

            // Non-prioritized events are treated as Normal priority
            return (int)EventPriority.Normal >= (int)minimumPriority;
        };

        return Subscribe(priorityFilter, handler, Scheduler.Default);
    }

    /// <summary>
    /// Subscribes to an event with a filter and custom scheduler.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="filter">The filter to apply to events.</param>
    /// <param name="handler">The event handler.</param>
    /// <param name="scheduler">The scheduler to use.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    public ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler, IScheduler scheduler)
        where TEvent : class, IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        var observable = GetObservable<TEvent>(scheduler);
        if (filter != null)
        {
            observable = observable.Where(e => filter(e));
        }

        observable = observable.Catch<TEvent, Exception>(ex => {
            Console.WriteLine($"Error in event stream: {ex.Message}");
            return Observable.Empty<TEvent>();
        });

        var subscription = observable.Subscribe(
            onNext: handler,
            onError: ex => Console.WriteLine($"Error in subscription: {ex.Message}")
        );

        IncrementSubscriberCount<TEvent>();

        var disposable = new CompositeDisposable(
            subscription,
            Disposable.Create(() => DecrementSubscriberCount<TEvent>())
        );

        _disposable.Add(disposable);

        return new SubscriptionToken(typeof(TEvent), () => {
            disposable.Dispose();
            _disposable.Remove(disposable);
        });
    }

    /// <summary>
    /// Unsubscribes from an event.
    /// </summary>
    /// <param name="token">The subscription token.</param>
    public void Unsubscribe(ISubscriptionToken token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        token.Dispose();
    }

    /// <summary>
    /// Clears all subscriptions for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    public void ClearSubscriptions<TEvent>() where TEvent : class, IEvent
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        _lock.EnterWriteLock();
        try
        {
            if (_subjects.TryGetValue(typeof(TEvent), out var subjectInfo))
            {
                if (subjectInfo.Subject is Subject<TEvent> s)
                {
                    s.OnCompleted();
                }
                _subjects.Remove(typeof(TEvent));
                subjectInfo.Dispose();
                var newSubject = new Subject<TEvent>();
                _subjects[typeof(TEvent)] = new SubjectInfo(newSubject, subjectInfo.DefaultScheduler);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Disposes the event aggregator.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Cancel the processing task
        _cancellationTokenSource.Cancel();
        try
        {
            _eventProcessingTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // Task was cancelled, which is expected
        }

        _lock.EnterWriteLock();
        try
        {
            foreach (var subject in _subjects.Values)
            {
                subject.Dispose();
            }
            _subjects.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _disposable.Dispose();
        _lock.Dispose();
        _cancellationTokenSource.Dispose();
    }

    #region Private Implementation

    /// <summary>
    /// Publishes an event with a specific priority.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventData">The event data.</param>
    /// <param name="priority">The priority to use.</param>
    private void PublishInternal<TEvent>(TEvent eventData, EventPriority priority) where TEvent : class, IEvent
    {
        // Check if there are any subscribers before enqueueing
        bool hasSubscribers = false;
        _lock.EnterReadLock();
        try
        {
            hasSubscribers = _subjects.ContainsKey(typeof(TEvent));
        }
        finally
        {
            _lock.ExitReadLock();
        }

        if (hasSubscribers)
        {
            // Create a wrapper and enqueue it
            var wrapper = new PrioritizedEventWrapper(eventData, typeof(TEvent), priority);
            _eventQueue.Enqueue(wrapper);
        }
    }

    /// <summary>
    /// Processes events from the queue based on priority.
    /// </summary>
    private async Task ProcessEventsAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            PrioritizedEventWrapper wrapper = null;

            // Only dequeue if we have an event
            if (!_eventQueue.IsEmpty)
            {
                wrapper = _eventQueue.Dequeue();
            }

            if (wrapper != null)
            {
                // Process the event by publishing it to the appropriate subject
                _lock.EnterReadLock();
                try
                {
                    if (_subjects.TryGetValue(wrapper.EventType, out var subjectInfo))
                    {
                        try
                        {
                            // Use reflection to call OnNext with the correct type
                            var subjectType = subjectInfo.Subject.GetType();
                            var onNextMethod = subjectType.GetMethod("OnNext");
                            onNextMethod?.Invoke(subjectInfo.Subject, new[] { wrapper.EventData });

                            // Update last used timestamp
                            subjectInfo.LastUsed = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            // Use reflection to call OnError with the correct type
                            var subjectType = subjectInfo.Subject.GetType();
                            var onErrorMethod = subjectType.GetMethod("OnError");
                            onErrorMethod?.Invoke(subjectInfo.Subject, new object[] { ex });

                            Console.WriteLine($"Error publishing event: {ex.Message}");
                        }
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                // No events to process, wait a short time before checking again
                try
                {
                    await Task.Delay(10, _cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // Task was cancelled, which is expected during shutdown
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Increments the subscriber count for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    private void IncrementSubscriberCount<TEvent>() where TEvent : class, IEvent
    {
        _lock.EnterWriteLock();
        try
        {
            if (_subjects.TryGetValue(typeof(TEvent), out var subjectInfo))
            {
                subjectInfo.SubscriberCount++;
                subjectInfo.LastUsed = DateTime.UtcNow;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Decrements the subscriber count for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    private void DecrementSubscriberCount<TEvent>() where TEvent : class, IEvent
    {
        _lock.EnterWriteLock();
        try
        {
            if (_subjects.TryGetValue(typeof(TEvent), out var subjectInfo))
            {
                subjectInfo.SubscriberCount--;
                subjectInfo.LastUsed = DateTime.UtcNow;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Starts the timer to cleanup unused subjects.
    /// </summary>
    private void StartCleanupTimer()
    {
        var cleanupInterval = TimeSpan.FromMinutes(5);
        var timer = Observable.Interval(cleanupInterval)
            .Subscribe(_ => CleanupUnusedSubjects());
        _disposable.Add(timer);
    }

    /// <summary>
    /// Cleans up unused subjects.
    /// </summary>
    private void CleanupUnusedSubjects()
    {
        var staleThreshold = DateTime.UtcNow.AddMinutes(-30);
        _lock.EnterWriteLock();
        try
        {
            var staleSubjects = _subjects
                .Where(kvp => kvp.Value.SubscriberCount <= 0 && kvp.Value.LastUsed < staleThreshold)
                .ToList();
            foreach (var staleSubject in staleSubjects)
            {
                staleSubject.Value.Dispose();
                _subjects.Remove(staleSubject.Key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    #endregion
}