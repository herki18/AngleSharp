namespace LayoutEngine.Platform.Resource.Abstractions;

using System;
using LayoutEngine.Contracts.Resource;

/// <summary>
/// Dummy resource for testing
/// </summary>
internal class DummyResource : IResource
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
        Metadata = new System.Collections.Generic.Dictionary<string, string>();
    }

    public string Url { get; }
    public string ContentType { get; }
    public byte[] Data { get; }
    public bool IsLoaded { get; }
    public ResourceType ResourceType { get; }
    public System.Collections.Generic.IReadOnlyDictionary<string, string> Metadata { get; }
}