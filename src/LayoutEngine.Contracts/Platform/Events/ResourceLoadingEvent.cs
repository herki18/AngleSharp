namespace LayoutEngine.Contracts.Platform.Events;

using System;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when a resource is loading.
/// </summary>
public class ResourceLoadingEvent : EventBase
{
    /// <summary>
    /// Gets the URL of the resource.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLoadingEvent"/> class.
    /// </summary>
    /// <param name="url">The URL of the resource.</param>
    public ResourceLoadingEvent(string url)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
    }
}