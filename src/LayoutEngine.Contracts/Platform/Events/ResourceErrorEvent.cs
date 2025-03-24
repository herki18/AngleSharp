namespace LayoutEngine.Contracts.Platform.Events;

using System;
using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when a resource fails to load.
/// </summary>
public class ResourceErrorEvent : EventBase
{
    /// <summary>
    /// Gets the URL of the resource.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the error that occurred.
    /// </summary>
    public Exception Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceErrorEvent"/> class.
    /// </summary>
    /// <param name="url">The URL of the resource.</param>
    /// <param name="error">The error that occurred.</param>
    public ResourceErrorEvent(string url, Exception error)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }
}