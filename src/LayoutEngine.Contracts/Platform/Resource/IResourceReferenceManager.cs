using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Resource;

namespace LayoutEngine.Contracts.Platform.Resource;

/// <summary>
/// Manages references to external resources.
/// </summary>
public interface IResourceReferenceManager
{
    /// <summary>
    /// Creates a reference to a resource.
    /// </summary>
    /// <param name="url">The URL of the resource.</param>
    /// <param name="resourceType">The type of the resource.</param>
    /// <param name="priority">The priority of the resource.</param>
    /// <returns>A resource reference.</returns>
    IResourceReference CreateReference(string url, ResourceType resourceType, ResourcePriority priority = ResourcePriority.Normal);

    /// <summary>
    /// Gets an existing resource reference if it exists.
    /// </summary>
    /// <param name="url">The URL of the resource.</param>
    /// <returns>The resource reference, or null if it doesn't exist.</returns>
    IResourceReference? GetReference(string url);

    /// <summary>
    /// Resolves a resource reference to an actual resource.
    /// </summary>
    /// <param name="reference">The resource reference.</param>
    /// <returns>The resource.</returns>
    Task<IResource> ResolveReferenceAsync(IResourceReference reference);

    /// <summary>
    /// Updates the priority of a resource reference.
    /// </summary>
    /// <param name="reference">The resource reference.</param>
    /// <param name="priority">The new priority.</param>
    void UpdatePriority(IResourceReference reference, ResourcePriority priority);

    /// <summary>
    /// Gets all resource references of a specific type.
    /// </summary>
    /// <param name="resourceType">The type of resources to get.</param>
    /// <returns>A collection of resource references.</returns>
    IReadOnlyCollection<IResourceReference> GetReferences(ResourceType resourceType);

    /// <summary>
    /// Releases a resource reference.
    /// </summary>
    /// <param name="reference">The resource reference to release.</param>
    void ReleaseReference(IResourceReference reference);

    /// <summary>
    /// Tracks a dependency between a component and a resource.
    /// </summary>
    /// <param name="ownerId">The ID of the component that owns the reference.</param>
    /// <param name="reference">The resource reference.</param>
    void TrackDependency(string ownerId, IResourceReference reference);

    /// <summary>
    /// Invalidates all resources owned by a component.
    /// </summary>
    /// <param name="ownerId">The ID of the component.</param>
    /// <returns>The number of resources invalidated.</returns>
    int InvalidateResources(string ownerId);
}

/// <summary>
/// Represents a reference to an external resource.
/// </summary>
public interface IResourceReference : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this reference.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the URL of the resource.
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Gets the type of the resource.
    /// </summary>
    ResourceType ResourceType { get; }

    /// <summary>
    /// Gets the priority of the resource.
    /// </summary>
    ResourcePriority Priority { get; }

    /// <summary>
    /// Gets the state of the resource.
    /// </summary>
    ResourceState State { get; }

    /// <summary>
    /// Gets the time when this reference was created.
    /// </summary>
    DateTime CreatedTime { get; }

    /// <summary>
    /// Gets the time when this reference was last accessed.
    /// </summary>
    DateTime LastAccessedTime { get; }

    /// <summary>
    /// Gets the number of components that reference this resource.
    /// </summary>
    int ReferenceCount { get; }
}

/// <summary>
/// Represents the state of a resource.
/// </summary>
public enum ResourceState
{
    /// <summary>
    /// The resource reference has been created but the resource has not been requested.
    /// </summary>
    Created,

    /// <summary>
    /// The resource is currently being loaded.
    /// </summary>
    Loading,

    /// <summary>
    /// The resource has been loaded successfully.
    /// </summary>
    Loaded,

    /// <summary>
    /// The resource failed to load.
    /// </summary>
    Error,

    /// <summary>
    /// The resource has been released.
    /// </summary>
    Released
}

/// <summary>
/// Defines the priority levels for resources.
/// </summary>
public enum ResourcePriority
{
    /// <summary>
    /// Low priority resources that are loaded only when idle.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority resources that are loaded in the background.
    /// </summary>
    Normal = 10,

    /// <summary>
    /// High priority resources that are loaded as soon as possible.
    /// </summary>
    High = 20,

    /// <summary>
    /// Critical priority resources that are loaded immediately, potentially blocking rendering.
    /// </summary>
    Critical = 30
}

/// <summary>
/// Event raised when a resource reference state changes.
/// </summary>
public class ResourceReferenceStateChangedEvent : EventBase
{
    /// <summary>
    /// Gets the resource reference.
    /// </summary>
    public IResourceReference Reference { get; }

    /// <summary>
    /// Gets the previous state of the resource.
    /// </summary>
    public ResourceState PreviousState { get; }

    /// <summary>
    /// Gets the new state of the resource.
    /// </summary>
    public ResourceState NewState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceReferenceStateChangedEvent"/> class.
    /// </summary>
    public ResourceReferenceStateChangedEvent(
        IResourceReference reference,
        ResourceState previousState,
        ResourceState newState)
    {
        Reference = reference;
        PreviousState = previousState;
        NewState = newState;
    }
}