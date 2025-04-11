using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;

namespace Infrastructure.EventAggregator.Internal.Core;

public class EventAggregator : IEventAggregator, IDisposable
{
    // Store subjects with their type information
    private readonly Dictionary<Type, SubjectInfo> _subjects = new Dictionary<Type, SubjectInfo>();

    // Track which concrete types can be published to which interface types
    private readonly Dictionary<Type, HashSet<Type>> _typeRegistry = new Dictionary<Type, HashSet<Type>>();

    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private readonly CompositeDisposable _disposable = new CompositeDisposable();
    private bool _isDisposed;

    private class SubjectInfo : IDisposable
    {
        public object Subject { get; }
        public Action<IEvent, EventPriority> PublishAction { get; } // Store the publish delegate with priority
        public DateTime LastUsed { get; set; }
        public int SubscriberCount { get; set; }

        public SubjectInfo(object subject, Action<IEvent, EventPriority> publishAction)
        {
            Subject = subject;
            PublishAction = publishAction;
            LastUsed = DateTime.UtcNow;
            SubscriberCount = 0;
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

        EventPriority priority = EventPriority.Normal;
        if (eventData is IPrioritizedEvent prioritizedEvent)
        {
            priority = prioritizedEvent.Priority;
        }

        PublishInternal(eventData, priority);
    }

    public void Publish<TEvent>(TEvent eventData, EventPriority priority) where TEvent : class, IEvent
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        PublishInternal(eventData, priority);
    }

    public IObservable<TEvent> GetObservable<TEvent>() where TEvent : class, IEvent
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        Type eventType = typeof(TEvent);

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_subjects.TryGetValue(eventType, out var existingSubject))
            {
                _lock.EnterWriteLock();
                try
                {
                    var subject = new Subject<TEvent>();

                    // Create a strongly-typed publish delegate for this subject
                    Action<IEvent, EventPriority> publishAction = (evt, priority) =>
                    {
                        if (evt is TEvent typedEvent)
                        {
                            ((ISubject<TEvent>)subject).OnNext(typedEvent);
                        }
                    };

                    _subjects[eventType] = new SubjectInfo(subject, publishAction);

                    // Register this type with itself and all assignable types
                    RegisterTypeHierarchy(eventType);

                    return subject.AsObservable();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            existingSubject.LastUsed = DateTime.UtcNow;
            return ((ISubject<TEvent>)existingSubject.Subject).AsObservable();
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
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        var observable = GetObservable<TEvent>();

        if (filter != null)
        {
            observable = observable.Where(e => filter(e));
        }

        observable = observable.Catch<TEvent, Exception>(ex => {
            Console.WriteLine($"Error in event stream: {ex.Message}");
            return Observable.Empty<TEvent>();
        });

        // Wrap the handler in a try-catch to prevent exceptions from affecting other subscribers
        var subscription = observable.Subscribe(
            onNext: e => {
                try {
                    handler(e);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error in event handler: {ex.Message}");
                    // Don't rethrow - allow other subscribers to receive the event
                }
            },
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

    public ISubscriptionToken SubscribeWithPriority<TEvent>(Action<TEvent> handler, EventPriority minimumPriority)
        where TEvent : class, IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EventAggregator));

        Predicate<TEvent> priorityFilter = e =>
        {
            if (e is IPrioritizedEvent prioritizedEvent)
            {
                return (int)prioritizedEvent.Priority >= (int)minimumPriority;
            }
            return (int)EventPriority.Normal >= (int)minimumPriority;
        };

        var observable = GetObservable<TEvent>().Where(e => priorityFilter(e));

        var subscription = observable.Subscribe(
            onNext: e => {
                try {
                    handler(e);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error in event handler: {ex.Message}");
                }
            },
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

        Type eventType = typeof(TEvent);

        _lock.EnterWriteLock();
        try
        {
            if (_subjects.TryGetValue(eventType, out var subjectInfo))
            {
                if (subjectInfo.Subject is Subject<TEvent> s)
                {
                    s.OnCompleted();
                }
                _subjects.Remove(eventType);
                subjectInfo.Dispose();

                var newSubject = new Subject<TEvent>();

                Action<IEvent, EventPriority> publishAction = (evt, priority) =>
                {
                    if (evt is TEvent typedEvent)
                    {
                        ((ISubject<TEvent>)newSubject).OnNext(typedEvent);
                    }
                };

                _subjects[eventType] = new SubjectInfo(newSubject, publishAction);
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
            _typeRegistry.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _disposable.Dispose();
        _lock.Dispose();
    }

    #region Private Implementation

    private void PublishInternal<TEvent>(TEvent eventData, EventPriority priority) where TEvent : class, IEvent
    {
        Type eventType = typeof(TEvent);

        _lock.EnterReadLock();
        try
        {
            // Find all target types to publish to
            HashSet<Type> targetTypes = new HashSet<Type>();

            // Add the direct type
            targetTypes.Add(eventType);

            // Add any registered target types
            if (_typeRegistry.TryGetValue(eventType, out var registeredTypes))
            {
                foreach (var type in registeredTypes)
                {
                    targetTypes.Add(type);
                }
            }

            // Publish to all compatible subjects
            foreach (var targetType in targetTypes)
            {
                if (_subjects.TryGetValue(targetType, out var subjectInfo))
                {
                    try
                    {
                        // Use the pre-compiled publish action to deliver the event
                        subjectInfo.PublishAction(eventData, priority);
                        subjectInfo.LastUsed = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't call OnError as it would terminate the subscription
                        Console.WriteLine($"Error publishing event: {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void RegisterTypeHierarchy(Type type)
    {
        // Register the type itself
        RegisterTypeRelationship(type, type);

        // If this is a concrete type, register its relationship with interfaces and base classes
        if (!type.IsInterface && !type.IsAbstract)
        {
            // Register all interfaces that extend IEvent
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (typeof(IEvent).IsAssignableFrom(interfaceType))
                {
                    RegisterTypeRelationship(type, interfaceType);
                }
            }

            // Register base types that implement IEvent
            var baseType = type.BaseType;
            while (baseType != null && typeof(IEvent).IsAssignableFrom(baseType))
            {
                RegisterTypeRelationship(type, baseType);
                baseType = baseType.BaseType;
            }
        }
    }

    private void RegisterTypeRelationship(Type concreteType, Type targetType)
    {
        // Register the relationship
        if (!_typeRegistry.TryGetValue(concreteType, out var targetTypes))
        {
            targetTypes = new HashSet<Type>();
            _typeRegistry[concreteType] = targetTypes;
        }

        if (concreteType != targetType)
        {
            targetTypes.Add(targetType);
        }
    }

    private void IncrementSubscriberCount<TEvent>() where TEvent : class, IEvent
    {
        Type eventType = typeof(TEvent);

        _lock.EnterWriteLock();
        try
        {
            if (_subjects.TryGetValue(eventType, out var subjectInfo))
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
        Type eventType = typeof(TEvent);

        _lock.EnterWriteLock();
        try
        {
            if (_subjects.TryGetValue(eventType, out var subjectInfo))
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
        var cleanupInterval = TimeSpan.FromMinutes(5);
        var timer = Observable.Interval(cleanupInterval)
            .Subscribe(_ => CleanupUnusedSubjects());
        _disposable.Add(timer);
    }

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

                // Remove type registrations for this type
                _typeRegistry.Remove(staleSubject.Key);

                // Remove references to this type from other type registrations
                foreach (var entry in _typeRegistry)
                {
                    entry.Value.Remove(staleSubject.Key);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    #endregion
}