using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Resource;
#pragma warning disable CS8618, CS9264

namespace LayoutEngine.Platform.Resource;

using Contracts.Platform.Events;
using Contracts.Platform.Threading;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.API.Models;
using Infrastructure.EventAggregator.API.Aggregation;

/// <summary>
/// Loads external resources asynchronously.
/// Manages resource loading and caching.
/// </summary>
public sealed class ResourceLoader : IResourceLoader, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ICacheManager _cacheManager;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly IResourceErrorHandler _errorHandler;
    private readonly ConcurrentDictionary<string, Task<IResource>> _loadingTasks = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _loadLocks = new();
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private readonly IPrioritizedCache<string, IResource> _resourceCache;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLoader"/> class.
    /// </summary>
    /// <param name="eventAggregator">The event aggregator for publishing resource events.</param>
    /// <param name="cacheManager">The cache manager for resource caching.</param>
    /// <param name="threadingCoordinator">The threading coordinator for thread scheduling.</param>
    /// <param name="errorHandler">The resource error handler.</param>
    public ResourceLoader(
        IEventAggregator eventAggregator,
        ICacheManager cacheManager,
        IThreadingCoordinator threadingCoordinator,
        IResourceErrorHandler errorHandler)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _threadingCoordinator = threadingCoordinator ?? throw new ArgumentNullException(nameof(threadingCoordinator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

        // // Get or create resource cache
        // var styleCache = cacheManager.GetOrCreateCache<IDependencyTrackingCache<string, StyleData>>(
        //     "StyleCache",
        //     name => MemoryCacheFactory.CreateDependencyTrackingCache<string, StyleData>(
        //         name,
        //         CachePriority.High)
        // );
        // _resourceCache = _cacheManager.GetOrCreateCache<IPrioritizedCache<string, IResource>>("ResourceCache");

        // Subscribe to memory pressure events
        _subscriptions.Add(_eventAggregator.Subscribe<MemoryPressureEvent>(OnMemoryPressure));
    }

    /// <summary>
    /// Loads a resource asynchronously.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <returns>A task that completes with the loaded resource.</returns>
    public Task<IResource> LoadResourceAsync(string url)
    {
        return LoadResourceAsync(url, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Loads a resource asynchronously with timeout.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <param name="timeout">The timeout for loading the resource.</param>
    /// <returns>A task that completes with the loaded resource.</returns>
    public async Task<IResource> LoadResourceAsync(string url, TimeSpan timeout)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        IResource? cachedResource = null;
        // Check cache first
        if (_resourceCache.TryGetValue(url, out cachedResource) && cachedResource != null)
        {
            return cachedResource;
        }

        // Check if already loading
        if (_loadingTasks.TryGetValue(url, out var loadingTask))
        {
            return await loadingTask.ConfigureAwait(false);
        }

        // Get or create lock for this URL
        var loadLock = _loadLocks.GetOrAdd(url, _ => new SemaphoreSlim(1, 1));

        try
        {
            // Acquire lock
            await loadLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // Check cache again after acquiring lock
                if (_resourceCache.TryGetValue(url, out cachedResource) && cachedResource != null)
                {
                    return cachedResource;
                }

                // Check if another thread started loading while waiting for lock
                if (_loadingTasks.TryGetValue(url, out loadingTask))
                {
                    return await loadingTask.ConfigureAwait(false);
                }

                // Start loading
                var tcs = new TaskCompletionSource<IResource>();
                _loadingTasks[url] = tcs.Task;

                // Publish resource loading event
                _eventAggregator.Publish(new ResourceLoadingEvent(url));

                try
                {
                    // Create loading task
                    var task = LoadResourceInternalAsync(url, timeout);

                    // Wait for resource to load
                    var resource = await task.ConfigureAwait(false);

                    // Add to cache
                    var priority = DetermineCachePriority(resource);
                    var entryPriority = (CacheEntryPriority)(int)priority;
                    _resourceCache.Set(url, resource, entryPriority);

                    // Complete task
                    tcs.SetResult(resource);

                    // Publish resource loaded event
                    _eventAggregator.Publish(new ResourceLoadedEvent(url, resource));

                    return resource;
                }
                catch (Exception ex)
                {
                    // Handle loading error
                    _errorHandler.HandleError(url, ex);

                    // Publish resource error event
                    _eventAggregator.Publish(new ResourceErrorEvent(url, ex));

                    // Fail the task
                    tcs.SetException(ex);
                    throw;
                }
                finally
                {
                    // Remove loading task
                    _loadingTasks.TryRemove(url, out _);
                }
            }
            finally
            {
                // Release lock
                loadLock.Release();
            }
        }
        finally
        {
            // Clean up lock if no longer needed
            if (_loadingTasks.TryGetValue(url, out _) == false &&
                _resourceCache.TryGetValue(url, out _) == false)
            {
                if (_loadLocks.TryRemove(url, out var removedLock))
                {
                    removedLock.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Preloads a resource in the background.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    public void PreloadResource(string url)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        // Check if already loaded or loading
        if (_resourceCache.TryGetValue(url, out _) || _loadingTasks.ContainsKey(url))
        {
            return;
        }

        // Start loading in the background
        _threadingCoordinator.ScheduleOnWorkerThread(() =>
        {
            // Use a low priority for preloading
            LoadResourceAsync(url).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var ex = t.Exception?.InnerException ?? t.Exception;
                    if (ex != null)
                    {
                        _errorHandler.HandleError(url, ex);
                    }
                    else
                    {
                        // Fallback with a default exception
                        _errorHandler.HandleError(url, new Exception("Unknown error occurred during resource loading"));
                    }
                }
            });
        });
    }

    /// <summary>
    /// Cancels loading a resource.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <returns>True if loading was canceled, otherwise false.</returns>
    public bool CancelLoad(string url)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        // Check if loading
        if (!_loadingTasks.ContainsKey(url))
        {
            return false;
        }

        // Remove loading task
        return _loadingTasks.TryRemove(url, out _);
    }

    /// <summary>
    /// Checks if a resource is already loaded.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <returns>True if the resource is loaded, otherwise false.</returns>
    public bool IsResourceLoaded(string url)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        return _resourceCache.TryGetValue(url, out _);
    }

    /// <summary>
    /// Handles memory pressure events by trimming the resource cache.
    /// </summary>
    private void OnMemoryPressure(MemoryPressureEvent e)
    {
        // Under memory pressure, trim the resource cache
        var severity = e.Severity;
        var trimPercentage = severity switch
        {
            MemoryPressureSeverity.Low => 25,
            MemoryPressureSeverity.Medium => 50,
            MemoryPressureSeverity.High => 75,
            MemoryPressureSeverity.Critical => 100,
            _ => 0
        };

        if (trimPercentage > 0)
        {
            // Trim low priority resources first - swapped argument order and using CacheEntryPriority
            _resourceCache.TrimByPriority(trimPercentage, (CacheEntryPriority)(int)CacheEntryPriority.Low);

            // If critical, also trim normal priority
            if (severity >= MemoryPressureSeverity.High)
            {
                _resourceCache.TrimByPriority(trimPercentage, (CacheEntryPriority)(int)CacheEntryPriority.Normal);
            }
        }
    }

    /// <summary>
    /// Loads a resource internally.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <param name="timeout">The timeout for loading the resource.</param>
    /// <returns>A task that completes with the loaded resource.</returns>
    private async Task<IResource> LoadResourceInternalAsync(string url, TimeSpan timeout)
    {
        // Determine resource type from URL
        var resourceType = GetResourceTypeFromUrl(url);

        // Create appropriate resource loader
        var resource = resourceType switch
        {
            ResourceType.Image => await LoadImageAsync(url, timeout).ConfigureAwait(false),
            ResourceType.Font => await LoadFontAsync(url, timeout).ConfigureAwait(false),
            ResourceType.StyleSheet => await LoadStyleSheetAsync(url, timeout).ConfigureAwait(false),
            _ => await LoadGenericResourceAsync(url, timeout).ConfigureAwait(false)
        };

        return resource;
    }

    /// <summary>
    /// Determines the resource type from the URL.
    /// </summary>
    private ResourceType GetResourceTypeFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return ResourceType.Unknown;
        }

        var extension = System.IO.Path.GetExtension(url).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".svg" => ResourceType.Image,
            ".woff" or ".woff2" or ".ttf" or ".otf" or ".eot" => ResourceType.Font,
            ".css" => ResourceType.StyleSheet,
            ".js" => ResourceType.Script,
            _ => ResourceType.Unknown
        };
    }

    /// <summary>
    /// Determines the cache priority for a resource.
    /// </summary>
    private CachePriority DetermineCachePriority(IResource resource)
    {
        // Priority based on resource type and size
        return resource.ResourceType switch
        {
            ResourceType.Font => CachePriority.High, // Fonts are important for text rendering
            ResourceType.Image => resource.Data.Length > 1024 * 1024 ? CachePriority.Low : CachePriority.Normal, // Large images are low priority
            ResourceType.StyleSheet => CachePriority.High, // StyleSheets are important for styling
            ResourceType.Script => CachePriority.Normal, // Scripts are normal priority
            _ => CachePriority.Normal // Default to normal priority
        };
    }

    /// <summary>
    /// Loads an image resource.
    /// </summary>
    private Task<IResource> LoadImageAsync(string url, TimeSpan timeout)
    {
        // In a real implementation, this would use the appropriate image loading API
        // For now, return a dummy resource
        return Task.FromResult<IResource>(new DummyResource(url, ResourceType.Image));
    }

    /// <summary>
    /// Loads a font resource.
    /// </summary>
    private Task<IResource> LoadFontAsync(string url, TimeSpan timeout)
    {
        // In a real implementation, this would use the appropriate font loading API
        // For now, return a dummy resource
        return Task.FromResult<IResource>(new DummyResource(url, ResourceType.Font));
    }

    /// <summary>
    /// Loads a stylesheet resource.
    /// </summary>
    private Task<IResource> LoadStyleSheetAsync(string url, TimeSpan timeout)
    {
        // In a real implementation, this would use the appropriate stylesheet loading API
        // For now, return a dummy resource
        return Task.FromResult<IResource>(new DummyResource(url, ResourceType.StyleSheet));
    }

    /// <summary>
    /// Loads a generic resource.
    /// </summary>
    private Task<IResource> LoadGenericResourceAsync(string url, TimeSpan timeout)
    {
        // In a real implementation, this would use the appropriate resource loading API
        // For now, return a dummy resource
        return Task.FromResult<IResource>(new DummyResource(url, ResourceType.Unknown));
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ResourceLoader));
        }
    }

    /// <summary>
    /// Disposes the ResourceLoader and cleans up resources.
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

        // Dispose all locks
        foreach (var lockPair in _loadLocks)
        {
            lockPair.Value.Dispose();
        }

        _loadLocks.Clear();
    }

    /// <summary>
    /// Dummy resource implementation for testing.
    /// </summary>
    private class DummyResource : IResource
    {
        public DummyResource(string url, ResourceType resourceType)
        {
            Url = url;
            ResourceType = resourceType;
            ContentType = resourceType switch
            {
                ResourceType.Image => "image/png",
                ResourceType.Font => "font/woff2",
                ResourceType.StyleSheet => "text/css",
                ResourceType.Script => "text/javascript",
                _ => "application/octet-stream"
            };
            Data = Array.Empty<byte>();
            IsLoaded = true;
            Metadata = new Dictionary<string, string>();
        }

        public string Url { get; }
        public string ContentType { get; }
        public byte[] Data { get; }
        public bool IsLoaded { get; }
        public ResourceType ResourceType { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}