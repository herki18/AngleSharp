using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LayoutEngine.Contracts.Platform.Resource;

using Contracts.Resource;

/// <summary>
/// Defines the interface for platform-specific resource provider implementations
/// </summary>
public interface IPlatformResourceProvider
{
    /// <summary>
    /// Creates a resource loader appropriate for the current platform
    /// </summary>
    IResourceLoader CreateResourceLoader(
        Infrastructure.EventAggregator.API.Aggregation.IEventAggregator eventAggregator,
        Infrastructure.CacheManager.API.Management.ICacheManager cacheManager,
        Threading.IThreadingCoordinator threadingCoordinator,
        IResourceErrorHandler errorHandler);

    /// <summary>
    /// Creates a font metrics provider appropriate for the current platform
    /// </summary>
    IFontMetricsProvider CreateFontMetricsProvider(
        Infrastructure.EventAggregator.API.Aggregation.IEventAggregator eventAggregator,
        Infrastructure.CacheManager.API.Management.ICacheManager cacheManager,
        Threading.IThreadingCoordinator threadingCoordinator,
        IResourceLoader resourceLoader);

    /// <summary>
    /// Creates a resource reference manager appropriate for the current platform
    /// </summary>
    IResourceReferenceManager CreateResourceReferenceManager(
        Infrastructure.EventAggregator.API.Aggregation.IEventAggregator eventAggregator,
        Infrastructure.CacheManager.API.Management.ICacheManager cacheManager,
        IResourceLoader resourceLoader,
        Threading.IThreadingCoordinator threadingCoordinator,
        Updates.IIdleTaskScheduler idleTaskScheduler);

    /// <summary>
    /// Indicates whether the platform provider can directly handle the specified resource type
    /// </summary>
    bool CanHandleResourceType(ResourceType resourceType);

    /// <summary>
    /// Resolves a platform-specific URL from a generic resource URL
    /// </summary>
    string ResolveResourceUrl(string url, ResourceType resourceType);
}

/// <summary>
/// Extended resource error handler interface for platform-specific implementations
/// </summary>
public interface IResourceErrorHandler
{
    /// <summary>
    /// Handles an error that occurred during resource loading
    /// </summary>
    void HandleError(string url, Exception error);

    /// <summary>
    /// Gets the error policy for the specified URL
    /// </summary>
    ResourceErrorPolicy GetErrorPolicy(string url);

    /// <summary>
    /// Registers a fallback URL for the specified URL
    /// </summary>
    void RegisterFallback(string url, string fallbackUrl);
}

/// <summary>
/// Extended font metrics provider interface for platform-specific implementations
/// </summary>
public interface IExtendedFontMetricsProvider : IFontMetricsProvider
{
    /// <summary>
    /// Gets the list of available font families on the current platform
    /// </summary>
    Task<IReadOnlyList<string>> GetAvailableFontFamiliesAsync();

    /// <summary>
    /// Gets the default font family for the specified generic family
    /// </summary>
    Task<string> GetDefaultFontFamilyAsync(string genericFamily);

    /// <summary>
    /// Registers a custom font from a resource
    /// </summary>
    Task<bool> RegisterCustomFontAsync(IResource fontResource);
}

/// <summary>
/// Extended resource loader interface for platform-specific implementations
/// </summary>
public interface IExtendedResourceLoader : IResourceLoader
{
    /// <summary>
    /// Gets the platform-specific metadata for the specified resource
    /// </summary>
    Task<IDictionary<string, string>> GetResourceMetadataAsync(string url);

    /// <summary>
    /// Registers a resource handler for the specified resource type
    /// </summary>
    void RegisterResourceHandler(ResourceType resourceType, Func<string, Task<IResource>> handler);

    /// <summary>
    /// Gets resource statistics for the loader
    /// </summary>
    ResourceLoaderStatistics GetStatistics();
}

/// <summary>
/// Statistics for resource loading
/// </summary>
public class ResourceLoaderStatistics
{
    /// <summary>
    /// The number of loaded resources
    /// </summary>
    public int LoadedResourceCount { get; set; }

    /// <summary>
    /// The number of resources currently loading
    /// </summary>
    public int LoadingResourceCount { get; set; }

    /// <summary>
    /// The number of resource load errors
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// The total bytes loaded
    /// </summary>
    public long TotalBytesLoaded { get; set; }

    /// <summary>
    /// The memory usage of loaded resources
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Resource type statistics
    /// </summary>
    public IDictionary<ResourceType, int> ResourceTypeCount { get; set; } =
        new Dictionary<ResourceType, int>();
}