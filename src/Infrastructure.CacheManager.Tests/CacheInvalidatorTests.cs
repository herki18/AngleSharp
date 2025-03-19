using AutoFixture;
using Infrastructure.CacheManager.API.Caching;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Invalidation;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.Internal.Invalidation;
using NSubstitute;

namespace Infrastructure.CacheManager.Tests;

using NSubstitute.ExceptionExtensions;

public class CacheInvalidatorTests
{
    private readonly Fixture _fixture;
    private readonly ICacheManager _mockCacheManager;
    private readonly CacheInvalidator _sut;

    public CacheInvalidatorTests()
    {
        _fixture = new Fixture();
        _mockCacheManager = Substitute.For<ICacheManager>();
        _sut = new CacheInvalidator(_mockCacheManager);
    }

    [Fact]
    public void Constructor_WithNullCacheManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CacheInvalidator(null!));
    }

    [Fact]
    public void Invalidate_WithValidKey_ShouldRemoveCacheEntry()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var key = _fixture.Create<string>();
        var mockCache = Substitute.For<ICache<string, object>>();
        mockCache.Remove(key).Returns(true);
        _mockCacheManager.GetCache<ICache<string, object>>(cacheName).Returns(mockCache);

        // Act
        var result = _sut.Invalidate(cacheName, key);

        // Assert
        Assert.True(result);
        mockCache.Received(1).Remove(key);
    }

    [Fact]
    public void Invalidate_WithNonExistentCache_ShouldReturnFalse()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var key = _fixture.Create<string>();
        _mockCacheManager.GetCache<ICache<string, object>>(cacheName)
            .Throws<KeyNotFoundException>();

        // Act
        var result = _sut.Invalidate(cacheName, key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Invalidate_WithInvalidCastTypeForCache_ShouldReturnFalse()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var key = _fixture.Create<string>();
        _mockCacheManager.GetCache<ICache<string, object>>(cacheName)
            .Throws<InvalidCastException>();

        // Act
        var result = _sut.Invalidate(cacheName, key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InvalidateWithDependents_WithValidKey_ShouldRemoveCacheEntryAndDependents()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var key = _fixture.Create<string>();
        var mockCache = Substitute.For<IDependencyTrackingCache<string, object>>();
        mockCache.InvalidateWithDependents(key).Returns(3);
        _mockCacheManager.GetCache<IDependencyTrackingCache<string, object>>(cacheName).Returns(mockCache);

        // Act
        var result = _sut.InvalidateWithDependents(cacheName, key);

        // Assert
        Assert.Equal(3, result);
        mockCache.Received(1).InvalidateWithDependents(key);
    }

    [Fact]
    public void InvalidateWithDependents_WithNonExistentCache_ShouldReturnZero()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var key = _fixture.Create<string>();
        _mockCacheManager.GetCache<IDependencyTrackingCache<string, object>>(cacheName)
            .Throws<KeyNotFoundException>();

        // Act
        var result = _sut.InvalidateWithDependents(cacheName, key);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void InvalidateWithDependents_WithInvalidCastTypeForCache_ShouldTrySimpleInvalidate()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var key = _fixture.Create<string>();
        _mockCacheManager.GetCache<IDependencyTrackingCache<string, object>>(cacheName)
            .Throws<InvalidCastException>();

        var mockSimpleCache = Substitute.For<ICache<string, object>>();
        mockSimpleCache.Remove(key).Returns(true);
        _mockCacheManager.GetCache<ICache<string, object>>(cacheName).Returns(mockSimpleCache);

        // Act
        var result = _sut.InvalidateWithDependents(cacheName, key);

        // Assert
        Assert.Equal(1, result);
        mockSimpleCache.Received(1).Remove(key);
    }

    [Fact]
    public void InvalidateMultiple_WithValidKeys_ShouldRemoveMultipleCacheEntries()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var keys = new[] { "key1", "key2", "key3" };
        var mockCache = Substitute.For<ICache<string, object>>();

        mockCache.Remove("key1").Returns(true);
        mockCache.Remove("key2").Returns(true);
        mockCache.Remove("key3").Returns(false);

        _mockCacheManager.GetCache<ICache<string, object>>(cacheName).Returns(mockCache);

        // Act
        var result = _sut.InvalidateMultiple(cacheName, keys);

        // Assert
        Assert.Equal(2, result);
        mockCache.Received(1).Remove("key1");
        mockCache.Received(1).Remove("key2");
        mockCache.Received(1).Remove("key3");
    }

    [Fact]
    public void InvalidateMultiple_WithNullKeys_ShouldThrowArgumentNullException()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.InvalidateMultiple<string>(cacheName, null!));
    }

    [Fact]
    public void InvalidateByScope_AllCaches_ShouldClearAllCaches()
    {
        // Act
        var result = _sut.InvalidateByScope(InvalidationScope.AllCaches);

        // Assert
        Assert.Equal(-1, result);
        _mockCacheManager.Received(1).ClearAllCaches();
    }

    [Fact]
    public void InvalidateByScope_StyleScope_ShouldClearStyleCaches()
    {
        // Arrange
        var caches = new List<ITrimableCache>
        {
            CreateCacheMock("StyleCache", 10),
            CreateCacheMock("LayoutStyleCache", 15),
            CreateCacheMock("LayoutCache", 20)
        };
        _mockCacheManager.GetAllCaches().Returns(caches);

        // Act
        var result = _sut.InvalidateByScope(InvalidationScope.Style);

        // Assert
        Assert.Equal(25, result);
        caches[0].Received(1).Clear();
        caches[1].Received(1).Clear();
        caches[2].DidNotReceive().Clear();
    }

    [Fact]
    public void InvalidateByScope_LayoutScope_ShouldClearLayoutCaches()
    {
        // Arrange
        var caches = new List<ITrimableCache>
        {
            CreateCacheMock("StyleCache", 10),
            CreateCacheMock("LayoutCache", 15),
            CreateCacheMock("RenderLayoutCache", 20)
        };
        _mockCacheManager.GetAllCaches().Returns(caches);

        // Act
        var result = _sut.InvalidateByScope(InvalidationScope.Layout);

        // Assert
        Assert.Equal(35, result);
        caches[0].DidNotReceive().Clear();
        caches[1].Received(1).Clear();
        caches[2].Received(1).Clear();
    }

    [Fact]
    public void TrackDependency_WithDependencyTrackingCache_ShouldAddDependency()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var dependentKey = _fixture.Create<string>();
        var dependencyKey = _fixture.Create<string>();

        var mockCache = Substitute.For<IDependencyTrackingCache<string, object>>();
        _mockCacheManager.GetCache<IDependencyTrackingCache<string, object>>(cacheName).Returns(mockCache);

        // Act
        _sut.TrackDependency(cacheName, dependentKey, dependencyKey);

        // Assert
        mockCache.Received(1).AddDependencies(dependentKey, Arg.Is<IEnumerable<string>>(deps =>
            deps.Contains(dependencyKey)));
    }

    [Fact]
    public void TrackDependencies_WithDependencyTrackingCache_ShouldAddMultipleDependencies()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var dependentKey = _fixture.Create<string>();
        var dependencyKeys = new[] { "dep1", "dep2", "dep3" };

        var mockCache = Substitute.For<IDependencyTrackingCache<string, object>>();
        _mockCacheManager.GetCache<IDependencyTrackingCache<string, object>>(cacheName).Returns(mockCache);

        // Act
        _sut.TrackDependencies(cacheName, dependentKey, dependencyKeys);

        // Assert
        mockCache.Received(1).AddDependencies(dependentKey, Arg.Is<IEnumerable<string>>(deps =>
            deps.Contains("dep1") && deps.Contains("dep2") && deps.Contains("dep3")));
    }

    [Fact]
    public void TrackDependencies_WithNullDependencyKeys_ShouldThrowArgumentNullException()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var dependentKey = _fixture.Create<string>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.TrackDependencies(cacheName, dependentKey, null!));
    }

    private ITrimableCache CreateCacheMock(string name, int count)
    {
        var cache = Substitute.For<ITrimableCache>();
        cache.Name.Returns(name);
        cache.Count.Returns(count);
        return cache;
    }
}