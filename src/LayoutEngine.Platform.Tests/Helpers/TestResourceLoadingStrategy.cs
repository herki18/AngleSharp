namespace LayoutEngine.Platform.Tests.Helpers;

using Contracts.Platform.Resource.Abstractions;
using Contracts.Resource;

/// <summary>
/// Test resource loading strategy for testing
/// </summary>
public class TestResourceLoadingStrategy : IResourceLoadingStrategy
{
    private readonly Dictionary<string, IResource> _resources = new Dictionary<string, IResource>();
    private readonly Dictionary<string, TaskCompletionSource<IResource>> _pendingLoads = new Dictionary<string, TaskCompletionSource<IResource>>();

    public Task<IResource> LoadResourceAsync(string url, ResourceType resourceType, TimeSpan timeout)
    {
        // If resource exists, return it
        if (_resources.TryGetValue(url, out var resource))
        {
            return Task.FromResult(resource);
        }

        // If resource is pending, return the task
        if (_pendingLoads.TryGetValue(url, out var tcs))
        {
            return tcs.Task;
        }

        // Create a new pending load
        tcs = new TaskCompletionSource<IResource>();
        _pendingLoads[url] = tcs;

        // Create a default resource if automatic resource creation is enabled
        if (AutoCreateResources)
        {
            var defaultResource = new TestResource(url, resourceType);
            tcs.SetResult(defaultResource);
            _resources[url] = defaultResource;
            _pendingLoads.Remove(url);
            return tcs.Task;
        }

        return tcs.Task;
    }

    /// <summary>
    /// Gets or sets whether resources should be automatically created
    /// </summary>
    public bool AutoCreateResources { get; set; } = true;

    /// <summary>
    /// Registers a resource
    /// </summary>
    public void RegisterResource(IResource resource)
    {
        _resources[resource.Url] = resource;

        if (_pendingLoads.TryGetValue(resource.Url, out var tcs))
        {
            tcs.SetResult(resource);
            _pendingLoads.Remove(resource.Url);
        }
    }

    /// <summary>
    /// Simulates a resource load error
    /// </summary>
    public void SimulateLoadError(string url, Exception exception)
    {
        if (_pendingLoads.TryGetValue(url, out var tcs))
        {
            tcs.SetException(exception);
            _pendingLoads.Remove(url);
        }
    }

    /// <summary>
    /// Gets a registered resource
    /// </summary>
    public IResource? GetResource(string url)
    {
        if (_resources.TryGetValue(url, out var resource))
        {
            return resource;
        }

        return null;
    }

    /// <summary>
    /// Clears all registered resources
    /// </summary>
    public void ClearResources()
    {
        _resources.Clear();
    }
}