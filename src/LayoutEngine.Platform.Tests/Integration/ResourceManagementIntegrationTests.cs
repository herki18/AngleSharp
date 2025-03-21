using LayoutEngine.Contracts.Resource;
using LayoutEngine.Contracts.Platform.Resource;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Resource.Abstractions;

namespace LayoutEngine.Platform.Tests.Integration;

using Resource;
using Unit.Helpers;

public class ResourceManagementIntegrationTests : IDisposable
{
    private readonly PlatformTestEnvironment _testEnvironment;
    private readonly TestEventAggregator _eventAggregator;
    private readonly IResourceLoader _resourceLoader;
    private readonly IResourceReferenceManager _referenceManager;
    private readonly TestResourceLoadingStrategy _resourceLoadingStrategy;

    public ResourceManagementIntegrationTests()
    {
        _testEnvironment = new PlatformTestEnvironment();
        _eventAggregator = _testEnvironment.EventAggregator;
        _resourceLoader = _testEnvironment.ResourceLoader;
        _referenceManager = _testEnvironment.GetService<IResourceReferenceManager>();
        _resourceLoadingStrategy =
            (TestResourceLoadingStrategy)_testEnvironment.GetService<IResourceLoadingStrategy>();
    }

    [Fact]
    public async Task LoadResource_ShouldLoadAndCacheResource()
    {
        // Arrange
        string url = "https://example.com/image.png";
        _eventAggregator.ClearPublishedEvents();

        // Act
        var resource = await _resourceLoader.LoadResourceAsync(url);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(url, resource.Url);
        Assert.Equal(ResourceType.Image, resource.ResourceType);
        Assert.True(resource.IsLoaded);

        var loadingEvents = _eventAggregator.GetPublishedEvents<ResourceLoadingEvent>();
        var loadedEvents = _eventAggregator.GetPublishedEvents<ResourceLoadedEvent>();

        Assert.Single(loadingEvents);
        Assert.Single(loadedEvents);
        Assert.Equal(url, loadingEvents[0].Url);
        Assert.Equal(url, loadedEvents[0].Url);
        Assert.Same(resource, loadedEvents[0].Resource);

        // Verify the resource is cached
        Assert.True(_resourceLoader.IsResourceLoaded(url));
    }

    [Fact]
    public Task CreateReference_ShouldTrackResourceReference()
    {
        // Arrange
        string url = "https://example.com/style.css";
        ResourceType resourceType = ResourceType.StyleSheet;
        _eventAggregator.ClearPublishedEvents();

        // Act
        var reference = _referenceManager.CreateReference(url, resourceType);

        // Assert
        Assert.NotNull(reference);
        Assert.Equal(url, reference.Url);
        Assert.Equal(resourceType, reference.ResourceType);
        Assert.Equal(ResourcePriority.Normal, reference.Priority);
        Assert.Equal(ResourceState.Created, reference.State);

        // Verify it's retrievable
        var retrievedRef = _referenceManager.GetReference(url);
        Assert.Same(reference, retrievedRef);

        // Verify by resource type
        var styleRefs = _referenceManager.GetReferences(ResourceType.StyleSheet);
        Assert.Contains(reference, styleRefs);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ResolveReference_ShouldLoadResourceAndUpdateState()
    {
        // Arrange
        string url = "https://example.com/font.woff2";
        ResourceType resourceType = ResourceType.Font;
        var reference = _referenceManager.CreateReference(url, resourceType);
        _eventAggregator.ClearPublishedEvents();

        // Act
        var resource = await _referenceManager.ResolveReferenceAsync(reference);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(url, resource.Url);
        Assert.Equal(resourceType, resource.ResourceType);

        // Verify state change events
        var stateChangedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceStateChangedEvent>();
        Assert.Equal(2, stateChangedEvents.Count); // Created -> Loading -> Loaded
        Assert.Equal(ResourceState.Loading, stateChangedEvents[0].NewState);
        Assert.Equal(ResourceState.Loaded, stateChangedEvents[1].NewState);

        // Verify the reference state
        Assert.Equal(ResourceState.Loaded, reference.State);
    }

    [Fact]
    public void ReleaseReference_ShouldRemoveReference()
    {
        // Arrange
        string url = "https://example.com/image.jpg";
        ResourceType resourceType = ResourceType.Image;
        var reference = _referenceManager.CreateReference(url, resourceType);
        _eventAggregator.ClearPublishedEvents();

        // Act
        _referenceManager.ReleaseReference(reference);

        // Assert
        Assert.Null(_referenceManager.GetReference(url));

        // Verify state change event
        var stateChangedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceStateChangedEvent>();
        Assert.Single(stateChangedEvents);
        Assert.Equal(ResourceState.Created, stateChangedEvents[0].PreviousState);
        Assert.Equal(ResourceState.Released, stateChangedEvents[0].NewState);

        // Verify released event
        var releasedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceReleasedEvent>();
        Assert.Single(releasedEvents);
        Assert.Same(reference, releasedEvents[0].Reference);
    }

    [Fact]
    public void TrackDependency_ShouldAssociateReferenceWithOwner()
    {
        // Arrange
        string ownerId = "test-component";
        string url1 = "https://example.com/image1.png";
        string url2 = "https://example.com/image2.png";

        var ref1 = _referenceManager.CreateReference(url1, ResourceType.Image);
        var ref2 = _referenceManager.CreateReference(url2, ResourceType.Image);

        // Act
        _referenceManager.TrackDependency(ownerId, ref1);
        _referenceManager.TrackDependency(ownerId, ref2);

        // Assert - we can't directly verify the tracking as it's internal
        // but we can verify the behavior when invalidating
        int invalidatedCount = _referenceManager.InvalidateResources(ownerId);

        Assert.Equal(2, invalidatedCount);
        Assert.Null(_referenceManager.GetReference(url1));
        Assert.Null(_referenceManager.GetReference(url2));
    }

    [Fact]
    public void InvalidateResources_ShouldReleaseTrackedReferences()
    {
        // Arrange
        string owner1 = "component1";
        string owner2 = "component2";

        var ref1 = _referenceManager.CreateReference("https://example.com/owner1-image.png", ResourceType.Image);
        var ref2 = _referenceManager.CreateReference("https://example.com/shared-image.png", ResourceType.Image);
        var ref3 = _referenceManager.CreateReference("https://example.com/owner2-image.png", ResourceType.Image);

        _referenceManager.TrackDependency(owner1, ref1);
        _referenceManager.TrackDependency(owner1, ref2);
        _referenceManager.TrackDependency(owner2, ref2);
        _referenceManager.TrackDependency(owner2, ref3);

        _eventAggregator.ClearPublishedEvents();

        // Act
        int invalidatedCount = _referenceManager.InvalidateResources(owner1);

        // Assert
        Assert.Equal(2, invalidatedCount);

        // ref1 should be released as it was only referenced by owner1
        Assert.Null(_referenceManager.GetReference(ref1.Url));

        // ref2 should still exist as it's also referenced by owner2
        Assert.NotNull(_referenceManager.GetReference(ref2.Url));

        // ref3 should still exist as it's referenced by owner2
        Assert.NotNull(_referenceManager.GetReference(ref3.Url));

        // Verify events
        var releasedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceReleasedEvent>();
        Assert.Single(releasedEvents);
        Assert.Equal(ref1.Id, releasedEvents[0].Reference.Id);
    }

    [Fact]
    public async Task ResourceLoadError_ShouldUpdateReferenceState()
    {
        // Arrange
        string url = "https://example.com/missing-image.png";
        var exception = new Exception("Resource not found");

        // Configure the error
        _resourceLoadingStrategy.AutoCreateResources = false;
        _resourceLoadingStrategy.SimulateLoadError(url, exception);

        var reference = _referenceManager.CreateReference(url, ResourceType.Image);
        _eventAggregator.ClearPublishedEvents();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _referenceManager.ResolveReferenceAsync(reference));

        // Verify state change events
        var stateChangedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceStateChangedEvent>();
        Assert.Equal(2, stateChangedEvents.Count); // Created -> Loading -> Error
        Assert.Equal(ResourceState.Loading, stateChangedEvents[0].NewState);
        Assert.Equal(ResourceState.Error, stateChangedEvents[1].NewState);

        // Verify error event
        var errorEvents = _eventAggregator.GetPublishedEvents<ResourceErrorEvent>();
        Assert.Single(errorEvents);
        Assert.Equal(url, errorEvents[0].Url);
        Assert.Same(exception, errorEvents[0].Error);
    }

    [Fact]
    public Task ResourcePriorities_ShouldAffectLoadingBehavior()
    {
        // Arrange
        _eventAggregator.ClearPublishedEvents();

        // Create references with different priorities
        var criticalRef = _referenceManager.CreateReference(
            "https://example.com/critical.css",
            ResourceType.StyleSheet,
            ResourcePriority.Critical);

        var highRef = _referenceManager.CreateReference(
            "https://example.com/high.js",
            ResourceType.Script,
            ResourcePriority.High);

        var normalRef = _referenceManager.CreateReference(
            "https://example.com/normal.png",
            ResourceType.Image,
            ResourcePriority.Normal);

        var lowRef = _referenceManager.CreateReference(
            "https://example.com/low.woff",
            ResourceType.Font,
            ResourcePriority.Low);

        // Act - process a frame to allow background loading to start
        _testEnvironment.AdvanceTimeAndRunFrame(16);

        // Assert
        // Verify that critical and high priority resources were scheduled for loading
        var loadingEvents = _eventAggregator.GetPublishedEvents<ResourceLoadingEvent>();
        Assert.Contains(loadingEvents, e => e.Url == criticalRef.Url);

        // Update priority should change the reference priority
        _referenceManager.UpdatePriority(lowRef, ResourcePriority.Critical);
        Assert.Equal(ResourcePriority.Critical, lowRef.Priority);
        return Task.CompletedTask;
    }

    [Fact]
    public void ResourceReferences_ShouldHaveDifferentContentTypes()
    {
        // Arrange
        var imageRef = _referenceManager.CreateReference(
            "https://example.com/image.png", ResourceType.Image);
        var fontRef = _referenceManager.CreateReference(
            "https://example.com/font.woff2", ResourceType.Font);
        var cssRef = _referenceManager.CreateReference(
            "https://example.com/style.css", ResourceType.StyleSheet);
        var jsRef = _referenceManager.CreateReference(
            "https://example.com/script.js", ResourceType.Script);

        // Resolve all references asynchronously
        Task.WhenAll(
            _referenceManager.ResolveReferenceAsync(imageRef),
            _referenceManager.ResolveReferenceAsync(fontRef),
            _referenceManager.ResolveReferenceAsync(cssRef),
            _referenceManager.ResolveReferenceAsync(jsRef)
        ).Wait();

        // Get all loaded resources
        var loadedEvents = _eventAggregator.GetPublishedEvents<ResourceLoadedEvent>();

        // Assert
        var imageResource = loadedEvents.First(e => e.Url == imageRef.Url).Resource;
        var fontResource = loadedEvents.First(e => e.Url == fontRef.Url).Resource;
        var cssResource = loadedEvents.First(e => e.Url == cssRef.Url).Resource;
        var jsResource = loadedEvents.First(e => e.Url == jsRef.Url).Resource;

        Assert.Equal("image/png", imageResource.ContentType);
        Assert.Equal("font/woff2", fontResource.ContentType);
        Assert.Equal("text/css", cssResource.ContentType);
        Assert.Equal("text/javascript", jsResource.ContentType);
    }

    [Fact]
    public void OnMemoryPressure_ShouldReleaseResources()
    {
        // Arrange
        var lowRef = _referenceManager.CreateReference(
            "https://example.com/low.png",
            ResourceType.Image,
            ResourcePriority.Low);

        var normalRef = _referenceManager.CreateReference(
            "https://example.com/normal.png",
            ResourceType.Image,
            ResourcePriority.Normal);

        var highRef = _referenceManager.CreateReference(
            "https://example.com/high.png",
            ResourceType.Image,
            ResourcePriority.High);

        // Resolve references to get them into Loaded state
        Task.WhenAll(
            _referenceManager.ResolveReferenceAsync(lowRef),
            _referenceManager.ResolveReferenceAsync(normalRef),
            _referenceManager.ResolveReferenceAsync(highRef)
        ).Wait();

        _eventAggregator.ClearPublishedEvents();

        // Act - Send memory pressure
        _eventAggregator.Publish(new MemoryPressureEvent(
            MemoryPressureSeverity.High, 1000000, 500000));

        // Process any pending work
        _testEnvironment.ExecuteAllPendingWork();

        // Assert
        // High pressure should affect low priority references
        var stateChangedEvents = _eventAggregator.GetPublishedEvents<ResourceReferenceStateChangedEvent>();

        // At least low priority should be affected
        Assert.Contains(stateChangedEvents,
            e => e.Reference.Priority == ResourcePriority.Low &&
                 e.PreviousState == ResourceState.Loaded);

        // High priority should not be affected
        Assert.DoesNotContain(stateChangedEvents,
            e => e.Reference.Priority == ResourcePriority.High &&
                 e.PreviousState == ResourceState.Loaded);
    }

    public void Dispose()
    {
        _testEnvironment.Dispose();
    }
}