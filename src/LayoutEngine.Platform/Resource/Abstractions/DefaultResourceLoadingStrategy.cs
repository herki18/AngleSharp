namespace LayoutEngine.Platform.Resource.Abstractions;

using System;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Platform.Resource.Abstractions;
using LayoutEngine.Contracts.Resource;

/// <summary>
/// Default implementation of resource loading strategy
/// </summary>
public class DefaultResourceLoadingStrategy : IResourceLoadingStrategy
{
    /// <summary>
    /// Loads a resource from the specified URL
    /// </summary>
    public Task<IResource> LoadResourceAsync(string url, ResourceType resourceType, TimeSpan timeout)
    {
        // This would be implemented to actually load resources
        // But for now we return dummy resources
        IResource resource = resourceType switch
        {
            ResourceType.Image => new DummyResource(url, ResourceType.Image),
            ResourceType.Font => new DummyResource(url, ResourceType.Font),
            ResourceType.StyleSheet => new DummyResource(url, ResourceType.StyleSheet),
            ResourceType.Script => new DummyResource(url, ResourceType.Script),
            _ => new DummyResource(url, ResourceType.Unknown)
        };

        return Task.FromResult(resource);
    }
}