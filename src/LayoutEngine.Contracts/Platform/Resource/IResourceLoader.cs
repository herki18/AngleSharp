using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace LayoutEngine.Contracts.Resource;
using Platform.Resource;
public interface IResourceLoader
{
    Task<IResource> LoadResourceAsync(string url);
    Task<IResource> LoadResourceAsync(string url, TimeSpan timeout);
    void PreloadResource(string url);
    bool CancelLoad(string url);
    bool IsResourceLoaded(string url);
}
public interface IResourceErrorHandler
{
    void HandleError(string url, Exception error);
    ResourceErrorPolicy GetErrorPolicy(string url);
    void RegisterFallback(string url, string fallbackUrl);
}
public interface IResource
{
    string Url { get; }
    string ContentType { get; }
    byte[] Data { get; }
    bool IsLoaded { get; }
    ResourceType ResourceType { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
}
public interface IFont : IResource
{
    string Family { get; }
    string Style { get; }
    int Weight { get; }
    IFontMetrics Metrics { get; }
}
public interface IImage : IResource
{
    int Width { get; }
    int Height { get; }
    string Format { get; }
    bool HasAlpha { get; }
}
public enum ResourceType
{
    Unknown,
    Image,
    Font,
    StyleSheet,
    Script
}
public enum ResourceErrorPolicy
{
    FailFast,
    Retry,
    UseFallback,
    Ignore
}