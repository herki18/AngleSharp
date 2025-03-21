namespace LayoutEngine.Contracts.Platform.Resource.Abstractions;

using Contracts.Resource;

/// <summary>
/// Interface for determining resource type from URL
/// </summary>
public interface IResourceTypeResolver
{
    /// <summary>
    /// Determines the resource type from a URL
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>Resource type</returns>
    ResourceType GetResourceTypeFromUrl(string url);
}