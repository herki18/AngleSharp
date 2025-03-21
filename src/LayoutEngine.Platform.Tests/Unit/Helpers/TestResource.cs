namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using LayoutEngine.Contracts.Resource;

/// <summary>
/// Test resource for testing
/// </summary>
public class TestResource : IResource
{
    private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();

    public TestResource(string url, ResourceType resourceType)
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
    }

    public string Url { get; }
    public string ContentType { get; }
    public byte[] Data { get; set; }
    public bool IsLoaded { get; set; }
    public ResourceType ResourceType { get; }

    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    /// <summary>
    /// Adds metadata
    /// </summary>
    public void AddMetadata(string key, string value)
    {
        _metadata[key] = value;
    }
}