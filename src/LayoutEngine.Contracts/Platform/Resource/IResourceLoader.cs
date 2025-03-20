using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LayoutEngine.Contracts.Resource;

using Platform.Resource;

/// <summary>
/// Loads external resources.
/// </summary>
public interface IResourceLoader
{
    /// <summary>
    /// Loads a resource asynchronously.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <returns>A task that completes with the loaded resource.</returns>
    Task<IResource> LoadResourceAsync(string url);

    /// <summary>
    /// Loads a resource asynchronously with timeout.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <param name="timeout">The timeout for loading the resource.</param>
    /// <returns>A task that completes with the loaded resource.</returns>
    Task<IResource> LoadResourceAsync(string url, TimeSpan timeout);

    /// <summary>
    /// Preloads a resource in the background.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    void PreloadResource(string url);

    /// <summary>
    /// Cancels loading a resource.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <returns>True if loading was canceled, otherwise false.</returns>
    bool CancelLoad(string url);

    /// <summary>
    /// Checks if a resource is already loaded.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <returns>True if the resource is loaded, otherwise false.</returns>
    bool IsResourceLoaded(string url);
}

/// <summary>
/// Handles resource loading errors.
/// </summary>
public interface IResourceErrorHandler
{
    /// <summary>
    /// Handles a resource loading error.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <param name="error">The error that occurred.</param>
    void HandleError(string url, Exception error);

    /// <summary>
    /// Gets the error handling policy for a resource.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <returns>The error handling policy.</returns>
    ResourceErrorPolicy GetErrorPolicy(string url);

    /// <summary>
    /// Registers a fallback for a resource.
    /// </summary>
    /// <param name="url">The resource URL.</param>
    /// <param name="fallbackUrl">The fallback URL.</param>
    void RegisterFallback(string url, string fallbackUrl);
}

/// <summary>
/// Represents a loaded resource.
/// </summary>
public interface IResource
{
    /// <summary>
    /// Gets the resource URL.
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Gets the resource content type.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the resource data.
    /// </summary>
    byte[] Data { get; }

    /// <summary>
    /// Gets whether the resource is loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the resource type.
    /// </summary>
    ResourceType ResourceType { get; }

    /// <summary>
    /// Gets the resource metadata.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>
/// Represents a loaded font.
/// </summary>
public interface IFont : IResource
{
    /// <summary>
    /// Gets the font family.
    /// </summary>
    string Family { get; }

    /// <summary>
    /// Gets the font style.
    /// </summary>
    string Style { get; }

    /// <summary>
    /// Gets the font weight.
    /// </summary>
    int Weight { get; }

    /// <summary>
    /// Gets the font metrics.
    /// </summary>
    IFontMetrics Metrics { get; }
}

/// <summary>
/// Represents a loaded image.
/// </summary>
public interface IImage : IResource
{
    /// <summary>
    /// Gets the image width.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the image height.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the image format.
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets whether the image has alpha channel.
    /// </summary>
    bool HasAlpha { get; }
}

/// <summary>
/// Defines the type of resource.
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// Unknown resource type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Image resource.
    /// </summary>
    Image,

    /// <summary>
    /// Font resource.
    /// </summary>
    Font,

    /// <summary>
    /// Stylesheet resource.
    /// </summary>
    StyleSheet,

    /// <summary>
    /// Script resource.
    /// </summary>
    Script
}

/// <summary>
/// Defines the policy for handling resource errors.
/// </summary>
public enum ResourceErrorPolicy
{
    /// <summary>
    /// Fail immediately on error.
    /// </summary>
    FailFast,

    /// <summary>
    /// Retry loading the resource.
    /// </summary>
    Retry,

    /// <summary>
    /// Use a fallback resource.
    /// </summary>
    UseFallback,

    /// <summary>
    /// Ignore the error and continue.
    /// </summary>
    Ignore
}

/// <summary>
/// Defines the severity of memory pressure.
/// </summary>
public enum MemoryPressureSeverity
{
    /// <summary>
    /// No memory pressure.
    /// </summary>
    None,

    /// <summary>
    /// Low memory pressure.
    /// </summary>
    Low,

    /// <summary>
    /// Medium memory pressure.
    /// </summary>
    Medium,

    /// <summary>
    /// High memory pressure.
    /// </summary>
    High,

    /// <summary>
    /// Critical memory pressure.
    /// </summary>
    Critical
}