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
public sealed class ResourceLoader : IResourceLoader, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ICacheManager _cacheManager;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly IResourceErrorHandler _errorHandler;
    private readonly ConcurrentDictionary<string, Task<IResource>> _loadingTasks = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _loadLocks = new();
#pragma warning disable CS0649
    private readonly IPrioritizedCache<string, IResource> _resourceCache;
#pragma warning restore CS0649
    private readonly List<ISubscriptionToken> _subscriptions = new();
    private bool _isDisposed;
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
    }
    public Task<IResource> LoadResourceAsync(string url)
    {
        return LoadResourceAsync(url, Timeout.InfiniteTimeSpan);
    }
    public async Task<IResource> LoadResourceAsync(string url, TimeSpan timeout)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        IResource? cachedResource = null;
        if (_resourceCache.TryGetValue(url, out cachedResource) && cachedResource != null)
        {
            return cachedResource;
        }
        if (_loadingTasks.TryGetValue(url, out var loadingTask))
        {
            return await loadingTask.ConfigureAwait(false);
        }
        var loadLock = _loadLocks.GetOrAdd(url, _ => new SemaphoreSlim(1, 1));
        try
        {
            await loadLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_resourceCache.TryGetValue(url, out cachedResource) && cachedResource != null)
                {
                    return cachedResource;
                }
                if (_loadingTasks.TryGetValue(url, out loadingTask))
                {
                    return await loadingTask.ConfigureAwait(false);
                }
                var tcs = new TaskCompletionSource<IResource>();
                _loadingTasks[url] = tcs.Task;
                _eventAggregator.Publish(new ResourceLoadingEvent(url));
                try
                {
                    var task = LoadResourceInternalAsync(url, timeout);
                    var resource = await task.ConfigureAwait(false);
                    var priority = DetermineCachePriority(resource);
                    var entryPriority = (CacheEntryPriority)(int)priority;
                    _resourceCache.Set(url, resource, entryPriority);
                    tcs.SetResult(resource);
                    _eventAggregator.Publish(new ResourceLoadedEvent(url, resource));
                    return resource;
                }
                catch (Exception ex)
                {
                    _errorHandler.HandleError(url, ex);
                    _eventAggregator.Publish(new ResourceErrorEvent(url, ex));
                    tcs.SetException(ex);
                    throw;
                }
                finally
                {
                    _loadingTasks.TryRemove(url, out _);
                }
            }
            finally
            {
                loadLock.Release();
            }
        }
        finally
        {
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
    public void PreloadResource(string url)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        if (_resourceCache.TryGetValue(url, out _) || _loadingTasks.ContainsKey(url))
        {
            return;
        }
        _threadingCoordinator.ScheduleOnWorkerThread(() =>
        {
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
                        _errorHandler.HandleError(url, new Exception("Unknown error occurred during resource loading"));
                    }
                }
            });
        });
    }
    public bool CancelLoad(string url)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        if (!_loadingTasks.ContainsKey(url))
        {
            return false;
        }
        return _loadingTasks.TryRemove(url, out _);
    }
    public bool IsResourceLoaded(string url)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        return _resourceCache.TryGetValue(url, out _);
    }
    private async Task<IResource> LoadResourceInternalAsync(string url, TimeSpan timeout)
    {
        var resourceType = GetResourceTypeFromUrl(url);
        var resource = resourceType switch
        {
            ResourceType.Image => await LoadImageAsync(url, timeout).ConfigureAwait(false),
            ResourceType.Font => await LoadFontAsync(url, timeout).ConfigureAwait(false),
            ResourceType.StyleSheet => await LoadStyleSheetAsync(url, timeout).ConfigureAwait(false),
            _ => await LoadGenericResourceAsync(url, timeout).ConfigureAwait(false)
        };
        return resource;
    }
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
    private CachePriority DetermineCachePriority(IResource resource)
    {
        return resource.ResourceType switch
        {
            ResourceType.Font => CachePriority.High,
            ResourceType.Image => resource.Data.Length > 1024 * 1024 ? CachePriority.Low : CachePriority.Normal,
            ResourceType.StyleSheet => CachePriority.High,
            ResourceType.Script => CachePriority.Normal,
            _ => CachePriority.Normal
        };
    }
    private Task<IResource> LoadImageAsync(string url, TimeSpan timeout)
    {
        return Task.FromResult<IResource>(new DummyResource(url, ResourceType.Image));
    }
    private Task<IResource> LoadFontAsync(string url, TimeSpan timeout)
    {
        return Task.FromResult<IResource>(new DummyResource(url, ResourceType.Font));
    }
    private Task<IResource> LoadStyleSheetAsync(string url, TimeSpan timeout)
    {
        return Task.FromResult<IResource>(new DummyResource(url, ResourceType.StyleSheet));
    }
    private Task<IResource> LoadGenericResourceAsync(string url, TimeSpan timeout)
    {
        return Task.FromResult<IResource>(new DummyResource(url, ResourceType.Unknown));
    }
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ResourceLoader));
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
        foreach (var lockPair in _loadLocks)
        {
            lockPair.Value.Dispose();
        }
        _loadLocks.Clear();
    }
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