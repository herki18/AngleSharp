using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using NSubstitute;
using AutoFixture;
using LayoutEngine.Platform.Resource;
using LayoutEngine.Platform.Tests.Helpers;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Contracts.Platform.Resource;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Platform.Updates;
using Infrastructure.EventAggregator.API.Aggregation;
using Infrastructure.CacheManager.API.Management;

namespace LayoutEngine.Platform.Tests.Resource;

public class ResourceReferenceManagerTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly TestEventAggregator _eventAggregator;
    private readonly ICacheManager _cacheManager;
    private readonly IResourceLoader _resourceLoader;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly IIdleTaskScheduler _idleTaskScheduler;
    private readonly ResourceReferenceManager _referenceManager;

    public ResourceReferenceManagerTests()
    {
        _fixture = new Fixture();
        _eventAggregator = new TestEventAggregator();
        _cacheManager = Substitute.For<ICacheManager>();
        _resourceLoader = Substitute.For<IResourceLoader>();
        _threadingCoordinator = Substitute.For<IThreadingCoordinator>();
        _idleTaskScheduler = Substitute.For<IIdleTaskScheduler>();

        _referenceManager = new ResourceReferenceManager(
            _eventAggregator,
            _cacheManager,
            _resourceLoader,
            _threadingCoordinator,
            _idleTaskScheduler);
    }

    [Fact]
    public void CreateReference_ShouldReturnValidReference()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        ResourcePriority priority = ResourcePriority.Normal;

        // Act
        var reference = _referenceManager.CreateReference(url, resourceType, priority);

        // Assert
        Assert.NotNull(reference);
        Assert.Equal(url, reference.Url);
        Assert.Equal(resourceType, reference.ResourceType);
        Assert.Equal(priority, reference.Priority);
        Assert.Equal(ResourceState.Created, reference.State);
    }

    [Fact]
    public void CreateReference_WithExistingUrl_ShouldReturnExistingReference()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        ResourcePriority priority = ResourcePriority.Normal;

        // Act
        var reference1 = _referenceManager.CreateReference(url, resourceType, priority);
        var reference2 = _referenceManager.CreateReference(url, resourceType, priority);

        // Assert
        Assert.Same(reference1, reference2);
    }

    [Fact]
    public void GetReference_WithExistingUrl_ShouldReturnReference()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        var reference = _referenceManager.CreateReference(url, resourceType);

        // Act
        var result = _referenceManager.GetReference(url);

        // Assert
        Assert.NotNull(result);
        Assert.Same(reference, result);
    }

    [Fact]
    public void GetReference_WithNonExistentUrl_ShouldReturnNull()
    {
        // Arrange
        string url = "https://example.com/nonexistent.png";

        // Act
        var result = _referenceManager.GetReference(url);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveReferenceAsync_ShouldLoadResource()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        var reference = _referenceManager.CreateReference(url, resourceType);
        var resource = new TestResource(url, resourceType);

        _resourceLoader.LoadResourceAsync(url).Returns(Task.FromResult<IResource>(resource));
        _resourceLoader.IsResourceLoaded(url).Returns(false);

        // Act
        var result = await _referenceManager.ResolveReferenceAsync(reference);

        // Assert
        Assert.NotNull(result);
        Assert.Same(resource, result);
        Assert.Equal(ResourceState.Loaded, reference.State);

        // Verify events
        var stateChangedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceStateChangedEvent>();
        Assert.Equal(2, stateChangedEvents.Count); // Created -> Loading -> Loaded
        Assert.Equal(ResourceState.Loading, stateChangedEvents[0].PreviousState);
        Assert.Equal(ResourceState.Loading, stateChangedEvents[0].NewState);
        Assert.Equal(ResourceState.Loading, stateChangedEvents[1].PreviousState);
        Assert.Equal(ResourceState.Loaded, stateChangedEvents[1].NewState);
    }

    [Fact]
    public void UpdatePriority_ShouldUpdateReferencePriority()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        ResourcePriority initialPriority = ResourcePriority.Low;
        ResourcePriority newPriority = ResourcePriority.High;

        var reference = _referenceManager.CreateReference(url, resourceType, initialPriority);

        // Act
        _referenceManager.UpdatePriority(reference, newPriority);

        // Assert
        Assert.Equal(newPriority, reference.Priority);
    }

    [Fact]
    public void GetReferences_ShouldReturnResourcesByType()
    {
        // Arrange
        _referenceManager.CreateReference("https://example.com/image1.png", ResourceType.Image);
        _referenceManager.CreateReference("https://example.com/image2.png", ResourceType.Image);
        _referenceManager.CreateReference("https://example.com/font.woff", ResourceType.Font);

        // Act
        var imageReferences = _referenceManager.GetReferences(ResourceType.Image);
        var fontReferences = _referenceManager.GetReferences(ResourceType.Font);
        var scriptReferences = _referenceManager.GetReferences(ResourceType.Script);

        // Assert
        Assert.Equal(2, imageReferences.Count);
        Assert.Single(fontReferences);
        Assert.Empty(scriptReferences);
    }

    [Fact]
    public void ReleaseReference_ShouldRemoveReferenceAndPublishEvent()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        var reference = _referenceManager.CreateReference(url, resourceType);

        // Act
        _referenceManager.ReleaseReference(reference);

        // Assert
        Assert.Null(_referenceManager.GetReference(url));

        // Verify events
        var stateChangedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceStateChangedEvent>();
        Assert.Single(stateChangedEvents);
        Assert.Equal(ResourceState.Created, stateChangedEvents[0].PreviousState);
        Assert.Equal(ResourceState.Released, stateChangedEvents[0].NewState);

        var releasedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceReleasedEvent>();
        Assert.Single(releasedEvents);
        Assert.Same(reference, releasedEvents[0].Reference);
    }

    [Fact]
    public void TrackDependency_ShouldAssociateReferenceWithOwner()
    {
        // Arrange
        string ownerId = "owner1";
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        var reference = _referenceManager.CreateReference(url, resourceType);

        // Act
        _referenceManager.TrackDependency(ownerId, reference);
        int invalidatedCount = _referenceManager.InvalidateResources(ownerId);

        // Assert
        Assert.Equal(1, invalidatedCount);
    }

    [Fact]
    public void InvalidateResources_WithNoResources_ShouldReturnZero()
    {
        // Arrange
        string ownerId = "nonexistent-owner";

        // Act
        int invalidatedCount = _referenceManager.InvalidateResources(ownerId);

        // Assert
        Assert.Equal(0, invalidatedCount);
    }

    [Fact]
    public async Task OnResourceLoaded_ShouldUpdateReferenceState()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        var reference = _referenceManager.CreateReference(url, resourceType);
        var resource = new TestResource(url, resourceType);

        // Set reference state to Loading first
        _resourceLoader.IsResourceLoaded(url).Returns(false);
        await _referenceManager.ResolveReferenceAsync(reference);

        // Clear events from previous operations
        _eventAggregator.ClearPublishedEvents();

        // Act
        _eventAggregator.Publish(new ResourceLoadedEvent(url, resource));

        // Assert
        var stateChangedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceStateChangedEvent>();
        Assert.Single(stateChangedEvents);
        Assert.Equal(ResourceState.Loading, stateChangedEvents[0].PreviousState);
        Assert.Equal(ResourceState.Loaded, stateChangedEvents[0].NewState);
    }

    public void Dispose()
    {
        _referenceManager.Dispose();
    }
}