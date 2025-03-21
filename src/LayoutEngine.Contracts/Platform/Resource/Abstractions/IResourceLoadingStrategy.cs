namespace LayoutEngine.Contracts.Platform.Resource.Abstractions;

using System;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Resource;

/// <summary>
/// Interface for resource loading strategy
/// </summary>
public interface IResourceLoadingStrategy
{
    /// <summary>
    /// Loads a resource from the specified URL
    /// </summary>
    /// <param name="url">URL to load</param>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="timeout">Timeout for loading</param>
    /// <returns>Loaded resource</returns>
    Task<IResource> LoadResourceAsync(string url, ResourceType resourceType, TimeSpan timeout);
}