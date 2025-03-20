using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Resource;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Contracts.Threading;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.EventAggregator.API.Aggregation;

namespace LayoutEngine.Platform.Resource;

using System.Threading;
using Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Manages references to external resources.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceReferenceManager"/> class.
    /// </summary>
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

        // Subscribe to events
        _subscriptions.Add(_eventAggregator.Subscribe<ResourceLoadedEvent>(OnResourceLoaded));
        _subscriptions.Add(_eventAggregator.Subscribe<ResourceErrorEvent>(OnResourceError));
        _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));

        // Initialize resource type dictionary
        foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
        {
            _referencesByType[resourceType] = new HashSet<IResourceReference>();
        }
    }

    /// <inheritdoc />
    public IResourceReference CreateReference(string url, ResourceType resourceType, ResourcePriority priority = ResourcePriority.Normal)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        // Check if reference already exists
        if (_referencesByUrl.TryGetValue(url, out var existingReference))
        {
            // Update priority if needed
            if (existingReference.Priority < priority)
            {
                UpdatePriority(existingReference, priority);
            }

            // Update access time
            existingReference.UpdateAccessTime();

            return existingReference;
        }

        // Create new reference
        var reference = new ResourceReference(url, resourceType, priority);

        // Add to dictionaries
        if (_referencesByUrl.TryAdd(url, reference) && _referencesById.TryAdd(reference.Id, reference))
        {
            // Add to type-based collection
            lock (_referencesByType)
            {
                _referencesByType[resourceType].Add(reference);
            }

            // Preload resource based on priority
            PreloadResourceByPriority(reference);

            return reference;
        }

        // If we couldn't add the reference, try to get existing one again
        if (_referencesByUrl.TryGetValue(url, out existingReference))
        {
            return existingReference;
        }

        // Shouldn't happen but just in case
        throw new InvalidOperationException($"Failed to create or retrieve resource reference for URL: {url}");
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IResource> ResolveReferenceAsync(IResourceReference reference)
    {
        ThrowIfDisposed();

        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        if (reference is not ResourceReference resourceReference)
            throw new ArgumentException("Reference must be created by this manager", nameof(reference));

        // Update access time
        resourceReference.UpdateAccessTime();

        // Check if already loaded
        if (resourceReference.State == ResourceState.Loaded && _resourceLoader.IsResourceLoaded(reference.Url))
        {
            var cachedResource = await _resourceLoader.LoadResourceAsync(reference.Url).ConfigureAwait(false);
            return cachedResource;
        }

        // Update state to Loading if not already loading or loaded
        if (resourceReference.State == ResourceState.Created)
        {
            UpdateReferenceState(resourceReference, ResourceState.Loading);
        }

        // Load the resource
        try
        {
            var resource = await _resourceLoader.LoadResourceAsync(reference.Url).ConfigureAwait(false);

            // Update state to Loaded
            UpdateReferenceState(resourceReference, ResourceState.Loaded);

            return resource;
        }
        catch (Exception)
        {
            // Update state to Error
            UpdateReferenceState(resourceReference, ResourceState.Error);
            throw;
        }
    }

    /// <inheritdoc />
    public void UpdatePriority(IResourceReference reference, ResourcePriority priority)
    {
        ThrowIfDisposed();

        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        if (reference is not ResourceReference resourceReference)
            throw new ArgumentException("Reference must be created by this manager", nameof(reference));

        // Only update if new priority is higher
        if (resourceReference.Priority >= priority)
            return;

        // Update priority
        resourceReference.UpdatePriority(priority);

        // If the resource is not loaded or loading, schedule loading based on new priority
        if (resourceReference.State == ResourceState.Created || resourceReference.State == ResourceState.Error)
        {
            PreloadResourceByPriority(resourceReference);
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void ReleaseReference(IResourceReference reference)
    {
        ThrowIfDisposed();

        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        if (reference is not ResourceReference resourceReference)
            throw new ArgumentException("Reference must be created by this manager", nameof(reference));

        // Update state to Released
        UpdateReferenceState(resourceReference, ResourceState.Released);

        // Remove from dictionaries
        _referencesByUrl.TryRemove(reference.Url, out _);
        _referencesById.TryRemove(reference.Id, out _);

        // Remove from type-based collection
        lock (_referencesByType)
        {
            _referencesByType[reference.ResourceType].Remove(reference);
        }

        // Remove from all owners
        foreach (var entry in _referencesByOwner)
        {
            lock (entry.Value)
            {
                entry.Value.Remove(reference);
            }
        }

        // Publish event
        _eventAggregator.Publish(new ResourceReferenceReleasedEvent(reference));
    }

    /// <inheritdoc />
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

        // Update reference count
        if (reference is ResourceReference resourceReference)
        {
            resourceReference.IncrementReferenceCount();
        }
    }

    /// <inheritdoc />
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

                    // If no more references, release it
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
                // Load immediately
                _resourceLoader.LoadResourceAsync(reference.Url);
                UpdateReferenceState(reference, ResourceState.Loading);
                break;

            case ResourcePriority.High:
                // Load on worker thread
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
                // Preload in background
                _resourceLoader.PreloadResource(reference.Url);
                UpdateReferenceState(reference, ResourceState.Loading);
                break;

            case ResourcePriority.Low:
                // Load during idle time
                _idleTaskScheduler.ScheduleIdleTask(() =>
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

        // Publish state change event
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
            // Release low priority resources if memory pressure is high
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
                // Release normal priority resources if memory pressure is critical
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

    /// <inheritdoc />
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

        // Clear all collections
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
            // Nothing to dispose
        }
    }
}

/// <summary>
/// Event raised when a resource reference is released.
/// </summary>
public class ResourceReferenceReleasedEvent : EventBase
{
    /// <summary>
    /// Gets the resource reference.
    /// </summary>
    public IResourceReference Reference { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceReferenceReleasedEvent"/> class.
    /// </summary>
    public ResourceReferenceReleasedEvent(IResourceReference reference)
    {
        Reference = reference;
    }
}