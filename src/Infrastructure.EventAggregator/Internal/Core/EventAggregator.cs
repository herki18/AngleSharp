namespace Infrastructure.EventAggregator.Internal.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using API.Aggregation;
using API.Events;

public class EventAggregator : IEventAggregator, IDisposable
{
    private readonly Dictionary<Type, SubjectInfo> _subjects = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly CompositeDisposable _disposable = new();
    private bool _isDisposed;

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

    public EventAggregator()
    {
        StartCleanupTimer();
    }

    public void Publish<TEvent>(TEvent eventData) where TEvent : class, IEvent
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        _lock.EnterReadLock();
        try
        {
            if (_subjects.TryGetValue(typeof(TEvent), out var subjectInfo))
            {
                try
                {
                    ((ISubject<TEvent>)subjectInfo.Subject).OnNext(eventData);
                    subjectInfo.LastUsed = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    // Propagate error to subscribers instead of throwing
                    ((ISubject<TEvent>)subjectInfo.Subject).OnError(ex);
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IObservable<TEvent> GetObservable<TEvent>() where TEvent : class, IEvent
    {
        return GetObservable<TEvent>(Scheduler.Default);
    }

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

    public ISubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent
    {
        return Subscribe<TEvent>(null, handler);
    }

    public ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler) where TEvent : class, IEvent
    {
        return Subscribe(filter, handler, Scheduler.Default);
    }

    public ISubscriptionToken Subscribe<TEvent>(Predicate<TEvent>? filter, Action<TEvent> handler, IScheduler scheduler) where TEvent : class, IEvent
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

        // Add error handling
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

    public IObservable<TEvent> GetThrottledObservable<TEvent>(TimeSpan throttleInterval) where TEvent : class, IEvent
    {
        return GetObservable<TEvent>().Throttle(throttleInterval);
    }

    public IObservable<IList<TEvent>> GetBufferedObservable<TEvent>(TimeSpan bufferInterval) where TEvent : class, IEvent
    {
        return GetObservable<TEvent>().Buffer(bufferInterval);
    }

    public IObservable<TEvent> GetSampledObservable<TEvent>(TimeSpan sampleInterval) where TEvent : class, IEvent
    {
        return GetObservable<TEvent>().Sample(sampleInterval);
    }

    public IObservable<TEvent> GetDistinctObservable<TEvent, TKey>(Func<TEvent, TKey> keySelector) where TEvent : class, IEvent
    {
        return GetObservable<TEvent>().DistinctUntilChanged(keySelector);
    }

    public void Unsubscribe(ISubscriptionToken token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        token.Dispose();
    }

    public void ClearSubscriptions<TEvent>() where TEvent : class, IEvent
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        _lock.EnterWriteLock();
        try
        {
            if (_subjects.TryGetValue(typeof(TEvent), out var subjectInfo))
            {
                // Complete the current subject
                if (subjectInfo.Subject is Subject<TEvent> s)
                {
                    s.OnCompleted();
                }

                // Remove and dispose the old subject
                _subjects.Remove(typeof(TEvent));
                subjectInfo.Dispose();

                // Create a new subject for future subscribers
                var newSubject = new Subject<TEvent>();
                _subjects[typeof(TEvent)] = new SubjectInfo(newSubject, subjectInfo.DefaultScheduler);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

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
    }

    #region Private Implementation

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

    private void StartCleanupTimer()
    {
        // Run cleanup every 5 minutes to remove unused subjects
        var cleanupInterval = TimeSpan.FromMinutes(5);
        var timer = Observable.Interval(cleanupInterval)
            .Subscribe(_ => CleanupUnusedSubjects());

        _disposable.Add(timer);
    }

    private void CleanupUnusedSubjects()
    {
        // Remove subjects that have no subscribers and haven't been used in 30 minutes
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