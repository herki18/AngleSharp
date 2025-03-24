namespace LayoutEngine.Contracts.Platform.Events;

using System;
using Contracts.Resource;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when a resource is loaded.
/// </summary>
public class ResourceLoadedEvent : EventBase
{
    /// <summary>
    /// Gets the URL of the resource.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the loaded resource.
    /// </summary>
    public IResource Resource { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLoadedEvent"/> class.
    /// </summary>
    /// <param name="url">The URL of the resource.</param>
    /// <param name="resource">The loaded resource.</param>
    public ResourceLoadedEvent(string url, IResource resource)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }
}