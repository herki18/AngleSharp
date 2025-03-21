namespace LayoutEngine.Platform.Tests.Unit.Resource;

using System;
using System.Threading.Tasks;
using AutoFixture;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Management;
using LayoutEngine.Contracts.Platform.Events;
using LayoutEngine.Contracts.Platform.Threading;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform.Resource;
using LayoutEngine.Platform.Tests.Unit.Helpers;
using NSubstitute;
using Xunit;

public class ResourceLoaderTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly TestEventAggregator _eventAggregator;
    private readonly ICacheManager _cacheManager;
    private readonly IThreadingCoordinator _threadingCoordinator;
    private readonly IResourceErrorHandler _errorHandler;
    private readonly IPrioritizedCache<string, IResource> _resourceCache;
    private readonly ResourceLoader _resourceLoader;

    public ResourceLoaderTests()
    {
        _fixture = new Fixture();
        _eventAggregator = new TestEventAggregator();
        _cacheManager = Substitute.For<ICacheManager>();
        _threadingCoordinator = Substitute.For<IThreadingCoordinator>();
        _errorHandler = Substitute.For<IResourceErrorHandler>();

        // Setup cache
        _resourceCache = Substitute.For<IPrioritizedCache<string, IResource>>();
        _cacheManager.GetCache<IPrioritizedCache<string, IResource>>("ResourceCache").Returns(_resourceCache);

        _resourceLoader = new ResourceLoader(_eventAggregator, _cacheManager, _threadingCoordinator, _errorHandler);
    }

    [Fact]
    public async Task LoadResourceAsync_WhenResourceIsCached_ShouldReturnCachedResource()
    {
        // Arrange
        string url = "https://example.com/image.png";
        var cachedResource = new TestResource(url, ResourceType.Image);

        // NSubstitute setup for TryGetValue when resource is found
        _resourceCache.TryGetValue(url, out Arg.Any<IResource?>())
            .Returns(callInfo => {
                callInfo[1] = cachedResource;
                return true;
            });

        // Act
        var result = await _resourceLoader.LoadResourceAsync(url);

        // Assert
        Assert.Same(cachedResource, result);
        _resourceCache.Received(1).TryGetValue(url, out Arg.Any<IResource?>());
    }

    [Fact]
    public async Task LoadResourceAsync_WhenResourceIsNotCached_ShouldLoadAndCacheResource()
    {
        // Arrange
        string url = "https://example.com/image.png";

        // NSubstitute setup for TryGetValue when resource is not found
        _resourceCache.TryGetValue(url, out Arg.Any<IResource?>())
            .Returns(callInfo => {
                callInfo[1] = null;
                return false;
            });

        // Act
        var result = await _resourceLoader.LoadResourceAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(url, result.Url);
        Assert.Equal(ResourceType.Image, result.ResourceType);

        // Verify events
        var loadingEvents = _eventAggregator.GetPublishedEvents<ResourceLoadingEvent>();
        Assert.Single(loadingEvents);
        Assert.Equal(url, loadingEvents[0].Url);

        var loadedEvents = _eventAggregator.GetPublishedEvents<ResourceLoadedEvent>();
        Assert.Single(loadedEvents);
        Assert.Equal(url, loadedEvents[0].Url);
        Assert.Same(result, loadedEvents[0].Resource);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task LoadResourceAsync_WithInvalidUrl_ShouldThrow(string url)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _resourceLoader.LoadResourceAsync(url));
    }

    [Fact]
    public void PreloadResource_ShouldScheduleWorkerThread()
    {
        // Arrange
        string url = "https://example.com/image.png";

        // NSubstitute setup for TryGetValue when resource is not found
        _resourceCache.TryGetValue(url, out Arg.Any<IResource?>())
            .Returns(callInfo => {
                callInfo[1] = null;
                return false;
            });

        // Act
        _resourceLoader.PreloadResource(url);

        // Assert
        _threadingCoordinator.Received(1).ScheduleOnWorkerThread(Arg.Any<Action>());
    }

    [Fact]
    public void IsResourceLoaded_WithLoadedResource_ShouldReturnTrue()
    {
        // Arrange
        string url = "https://example.com/image.png";
        var resource = new TestResource(url, ResourceType.Image);

        // NSubstitute setup for TryGetValue when resource is found
        _resourceCache.TryGetValue(url, out Arg.Any<IResource?>())
            .Returns(callInfo => {
                callInfo[1] = resource;
                return true;
            });

        // Act
        bool result = _resourceLoader.IsResourceLoaded(url);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsResourceLoaded_WithNotLoadedResource_ShouldReturnFalse()
    {
        // Arrange
        string url = "https://example.com/image.png";

        // NSubstitute setup for TryGetValue when resource is not found
        _resourceCache.TryGetValue(url, out Arg.Any<IResource?>())
            .Returns(callInfo => {
                callInfo[1] = null;
                return false;
            });

        // Act
        bool result = _resourceLoader.IsResourceLoaded(url);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void OnMemoryPressure_ShouldTrimResourceCache()
    {
        // Arrange
        var memoryPressureEvent = new MemoryPressureEvent(
            MemoryPressureSeverity.Medium,
            1000000,
            500000);

        // Act
        _eventAggregator.Publish(memoryPressureEvent);

        // Assert
        _resourceCache.Received(1).TrimByPriority(
            Arg.Any<double>(),
            Arg.Any<Infrastructure.CacheManager.API.Models.CacheEntryPriority>());
    }

    public void Dispose()
    {
        _resourceLoader.Dispose();
    }
}