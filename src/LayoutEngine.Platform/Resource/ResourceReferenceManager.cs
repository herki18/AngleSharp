namespace LayoutEngine.Platform.Resource;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.EventAggregator.API.Events;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Resource;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using LayoutEngine.Contracts.Resource;

public sealed class ResourceReferenceManager : IResourceReferenceManager, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ICacheManager _cacheManager;
    private readonly IResourceLoader _resourceLoader;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly IIdleTaskScheduler _idleTaskScheduler;
    private readonly ConcurrentDictionary<string, ResourceReference> _referencesByUrl = new();
    private readonly ConcurrentDictionary<Guid, ResourceReference> _referencesById = new();
    private readonly ConcurrentDictionary<string, HashSet<IResourceReference>> _referencesByOwner = new();
    private readonly ConcurrentDictionary<ResourceType, HashSet<IResourceReference>> _referencesByType = new();
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private bool _isDisposed;

    public ResourceReferenceManager(
        IEventAggregator eventAggregator,
        ICacheManager cacheManager,
        IResourceLoader resourceLoader,
        IThreadingCoordinator threadingCoordinator,
        IIdleTaskScheduler idleTaskScheduler)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
        _idleTaskScheduler = idleTaskScheduler ?? throw new ArgumentNullException(nameof(idleTaskScheduler));
        _subscriptions.Add(_eventAggregator.Subscribe<ResourceLoadedEvent>(OnResourceLoaded));
        _subscriptions.Add(_eventAggregator.Subscribe<ResourceErrorEvent>(OnResourceError));
        _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));
        foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
        {
            _referencesByType[resourceType] = new HashSet<IResourceReference>();
        }
    }

    public IResourceReference CreateReference(string url, ResourceType resourceType, ResourcePriority priority = ResourcePriority.Normal)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        if (_referencesByUrl.TryGetValue(url, out var existingReference))
        {
            if (existingReference.Priority < priority)
            {
                UpdatePriority(existingReference, priority);
            }
            existingReference.UpdateAccessTime();
            return existingReference;
        }
        var reference = new ResourceReference(url, resourceType, priority);
        if (_referencesByUrl.TryAdd(url, reference) && _referencesById.TryAdd(reference.Id, reference))
        {
            lock (_referencesByType)
            {
                _referencesByType[resourceType].Add(reference);
            }
            PreloadResourceByPriority(reference);
            return reference;
        }
        if (_referencesByUrl.TryGetValue(url, out existingReference))
        {
            return existingReference;
        }
        throw new InvalidOperationException($"Failed to create or retrieve resource reference for URL: {url}");
    }

    public IResourceReference? GetReference(string url)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        if (_referencesByUrl.TryGetValue(url, out var reference))
        {
            reference.UpdateAccessTime();
            return reference;
        }
        return null;
    }

    public async Task<IResource> ResolveReferenceAsync(IResourceReference reference)
    {
        ThrowIfDisposed();
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));
        if (reference is not ResourceReference resourceReference)
            throw new ArgumentException("Reference must be created by this manager", nameof(reference));
        resourceReference.UpdateAccessTime();
        if (resourceReference.State == ResourceState.Loaded && _resourceLoader.IsResourceLoaded(reference.Url))
        {
            var cachedResource = await _resourceLoader.LoadResourceAsync(reference.Url).ConfigureAwait(false);
            return cachedResource;
        }
        if (resourceReference.State == ResourceState.Created)
        {
            UpdateReferenceState(resourceReference, ResourceState.Loading);
        }
        try
        {
            var resource = await _resourceLoader.LoadResourceAsync(reference.Url).ConfigureAwait(false);
            UpdateReferenceState(resourceReference, ResourceState.Loaded);
            return resource;
        }
        catch (Exception)
        {
            UpdateReferenceState(resourceReference, ResourceState.Error);
            throw;
        }
    }

    public void UpdatePriority(IResourceReference reference, ResourcePriority priority)
    {
        ThrowIfDisposed();
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));
        if (reference is not ResourceReference resourceReference)
            throw new ArgumentException("Reference must be created by this manager", nameof(reference));
        if (resourceReference.Priority >= priority)
            return;
        resourceReference.UpdatePriority(priority);
        if (resourceReference.State == ResourceState.Created || resourceReference.State == ResourceState.Error)
        {
            PreloadResourceByPriority(resourceReference);
        }
    }

    public IReadOnlyCollection<IResourceReference> GetReferences(ResourceType resourceType)
    {
        ThrowIfDisposed();
        if (_referencesByType.TryGetValue(resourceType, out var references))
        {
            lock (references)
            {
                return references.ToList();
            }
        }
        return Array.Empty<IResourceReference>();
    }

    public void ReleaseReference(IResourceReference reference)
    {
        ThrowIfDisposed();
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));
        if (reference is not ResourceReference resourceReference)
            throw new ArgumentException("Reference must be created by this manager", nameof(reference));
        UpdateReferenceState(resourceReference, ResourceState.Released);
        _referencesByUrl.TryRemove(reference.Url, out _);
        _referencesById.TryRemove(reference.Id, out _);
        lock (_referencesByType)
        {
            _referencesByType[reference.ResourceType].Remove(reference);
        }
        foreach (var entry in _referencesByOwner)
        {
            lock (entry.Value)
            {
                entry.Value.Remove(reference);
            }
        }
        _eventAggregator.Publish(new ResourceReferenceReleasedEvent(reference));
    }

    public void TrackDependency(string ownerId, IResourceReference reference)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(ownerId))
            throw new ArgumentException("Owner ID cannot be null or empty", nameof(ownerId));
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));
        var references = _referencesByOwner.GetOrAdd(ownerId, _ => new HashSet<IResourceReference>());
        lock (references)
        {
            references.Add(reference);
        }
        if (reference is ResourceReference resourceReference)
        {
            resourceReference.IncrementReferenceCount();
        }
    }

    public int InvalidateResources(string ownerId)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(ownerId))
            throw new ArgumentException("Owner ID cannot be null or empty", nameof(ownerId));
        if (!_referencesByOwner.TryGetValue(ownerId, out var references))
            return 0;
        int count = 0;
        lock (references)
        {
            foreach (var reference in references.ToList())
            {
                if (reference is ResourceReference resourceReference)
                {
                    resourceReference.DecrementReferenceCount();
                    if (resourceReference.ReferenceCount <= 0)
                    {
                        ReleaseReference(reference);
                    }
                    count++;
                }
            }
            references.Clear();
        }
        return count;
    }

    private void PreloadResourceByPriority(ResourceReference reference)
    {
        switch (reference.Priority)
        {
            case ResourcePriority.Critical:
                _threadingCoordinator.ScheduleOnWorkerThread(() =>
                {
                    if (!_isDisposed && reference.State == ResourceState.Created)
                    {
                        UpdateReferenceState(reference, ResourceState.Loading);
                        _resourceLoader.LoadResourceAsync(reference.Url);
                    }
                });
                break;
            case ResourcePriority.High:
                _threadingCoordinator.ScheduleOnWorkerThread(() =>
                {
                    if (!_isDisposed && reference.State == ResourceState.Created)
                    {
                        UpdateReferenceState(reference, ResourceState.Loading);
                        _resourceLoader.LoadResourceAsync(reference.Url);
                    }
                });
                break;
            case ResourcePriority.Normal:
                _resourceLoader.PreloadResource(reference.Url);
                // Don't update state to Loading immediately for Normal priority
                // Let resource loading events handle state changes
                break;
            case ResourcePriority.Low:
                _idleTaskScheduler.ScheduleIdleTask(ct =>
                {
                    if (!_isDisposed && reference.State == ResourceState.Created)
                    {
                        UpdateReferenceState(reference, ResourceState.Loading);
                        _resourceLoader.LoadResourceAsync(reference.Url);
                    }
                }, IdleTaskPriority.Low);
                break;
        }
    }

    private void UpdateReferenceState(ResourceReference reference, ResourceState newState)
    {
        var previousState = reference.State;
        if (previousState == newState)
            return;
        reference.UpdateState(newState);
        _eventAggregator.Publish(new ResourceReferenceStateChangedEvent(
            reference, previousState, newState));
    }

    private void OnResourceLoaded(ResourceLoadedEvent e)
    {
        if (_referencesByUrl.TryGetValue(e.Url, out var reference) &&
            reference.State != ResourceState.Loaded)
        {
            UpdateReferenceState(reference, ResourceState.Loaded);
        }
    }

    private void OnResourceError(ResourceErrorEvent e)
    {
        if (_referencesByUrl.TryGetValue(e.Url, out var reference) &&
            reference.State == ResourceState.Loading)
        {
            UpdateReferenceState(reference, ResourceState.Error);
        }
    }

    private void OnMemoryPressure(MemoryPressureEvent e)
    {
        if (e.Severity >= MemoryPressureSeverity.High)
        {
            var lowPriorityReferences = new List<ResourceReference>();
            foreach (var reference in _referencesById.Values)
            {
                if (reference.Priority == ResourcePriority.Low &&
                    reference.State == ResourceState.Loaded)
                {
                    lowPriorityReferences.Add(reference);
                }
            }
            foreach (var reference in lowPriorityReferences)
            {
                UpdateReferenceState(reference, ResourceState.Created);
            }
            if (e.Severity >= MemoryPressureSeverity.Critical)
            {
                var normalPriorityReferences = new List<ResourceReference>();
                foreach (var reference in _referencesById.Values)
                {
                    if (reference.Priority == ResourcePriority.Normal &&
                        reference.State == ResourceState.Loaded)
                    {
                        normalPriorityReferences.Add(reference);
                    }
                }
                foreach (var reference in normalPriorityReferences)
                {
                    UpdateReferenceState(reference, ResourceState.Created);
                }
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ResourceReferenceManager));
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
        _referencesByUrl.Clear();
        _referencesById.Clear();
        _referencesByOwner.Clear();
        foreach (var references in _referencesByType.Values)
        {
            references.Clear();
        }
        _referencesByType.Clear();
    }

    private sealed class ResourceReference : IResourceReference
    {
        private volatile ResourceState _state;
        private volatile ResourcePriority _priority;
        private volatile int _referenceCount;

        public Guid Id { get; }
        public string Url { get; }
        public ResourceType ResourceType { get; }
        public ResourceState State => _state;
        public ResourcePriority Priority => _priority;
        public DateTime CreatedTime { get; }
        public DateTime LastAccessedTime { get; private set; }
        public int ReferenceCount => _referenceCount;

        public ResourceReference(string url, ResourceType resourceType, ResourcePriority priority)
        {
            Id = Guid.NewGuid();
            Url = url;
            ResourceType = resourceType;
            _state = ResourceState.Created;
            _priority = priority;
            _referenceCount = 0;
            CreatedTime = DateTime.UtcNow;
            LastAccessedTime = CreatedTime;
        }

        public void UpdateState(ResourceState newState)
        {
            _state = newState;
        }

        public void UpdatePriority(ResourcePriority newPriority)
        {
            _priority = newPriority;
        }

        public void UpdateAccessTime()
        {
            LastAccessedTime = DateTime.UtcNow;
        }

        public void IncrementReferenceCount()
        {
            Interlocked.Increment(ref _referenceCount);
        }

        public void DecrementReferenceCount()
        {
            Interlocked.Decrement(ref _referenceCount);
        }

        public void Dispose()
        {
        }
    }
}

public class ResourceReferenceReleasedEvent : EventBase
{
    public IResourceReference Reference { get; }

    public ResourceReferenceReleasedEvent(IResourceReference reference)
    {
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
    }
}