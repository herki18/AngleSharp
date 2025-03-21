namespace LayoutEngine.Platform.Resource.Abstractions;

using LayoutEngine.Contracts.Platform.Resource.Abstractions;
using LayoutEngine.Contracts.Resource;

/// <summary>
/// Default implementation of resource type resolver
/// </summary>
public class DefaultResourceTypeResolver : IResourceTypeResolver
{
    /// <summary>
    /// Determines the resource type from a URL
    /// </summary>
    public ResourceType GetResourceTypeFromUrl(string url)
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
}