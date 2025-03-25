using System;
using AutoFixture;
using Infrastructure.CacheManager.API.Caching;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Factory;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.Internal.Factory;
using NSubstitute;
using Xunit;

namespace Infrastructure.CacheManager.Tests.Factory;

public class MemoryCacheFactoryTests
{
    private readonly Fixture _fixture;
    private readonly ICacheManager _mockCacheManager;
    private readonly MemoryCacheFactory _sut;

    public MemoryCacheFactoryTests()
    {
        _fixture = new Fixture();
        _mockCacheManager = Substitute.For<ICacheManager>();
        _sut = new MemoryCacheFactory(_mockCacheManager);
    }

    [Fact]
    public void Constructor_WithNullCacheManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MemoryCacheFactory(null!));
    }

    [Fact]
    public void CreateCache_ShouldReturnICacheImplementation()
    {
        // Arrange
        var options = new CacheOptions(_fixture.Create<string>());

        // Act
        var result = _sut.CreateCache<string, string>(options);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ICache<string, string>>(result);
    }

    [Fact]
    public void CreateCache_WithAutoRegisterTrue_ShouldRegisterWithCacheManager()
    {
        // Arrange
        var options = new CacheOptions(_fixture.Create<string>())
        {
            AutoRegister = true
        };

        // Act
        var result = _sut.CreateCache<string, string>(options);

        // Assert
        _mockCacheManager.Received(1).RegisterCache(options.Name, Arg.Is<ITrimableCache>(c => c == result));
    }

    [Fact]
    public void CreateCache_WithAutoRegisterFalse_ShouldNotRegisterWithCacheManager()
    {
        // Arrange
        var options = new CacheOptions(_fixture.Create<string>())
        {
            AutoRegister = false
        };

        // Act
        var result = _sut.CreateCache<string, string>(options);

        // Assert
        _mockCacheManager.DidNotReceive().RegisterCache(Arg.Any<string>(), Arg.Any<ITrimableCache>());
    }

    [Fact]
    public void CreateCache_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.CreateCache<string, string>(null!));
    }

    [Fact]
    public void CreateCache_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new CacheOptions(string.Empty);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.CreateCache<string, string>(options));
    }

    [Fact]
    public void CreatePrioritizedCache_ShouldReturnIPrioritizedCacheImplementation()
    {
        // Arrange
        var options = new CacheOptions(_fixture.Create<string>());

        // Act
        var result = _sut.CreatePrioritizedCache<string, string>(options);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IPrioritizedCache<string, string>>(result);
    }

    [Fact]
    public void CreateDependencyTrackingCache_ShouldReturnIDependencyTrackingCacheImplementation()
    {
        // Arrange
        var options = new CacheOptions(_fixture.Create<string>());

        // Act
        var result = _sut.CreateDependencyTrackingCache<string, string>(options);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IDependencyTrackingCache<string, string>>(result);
    }

    [Fact]
    public void CreateAdvancedCache_ShouldReturnIAdvancedCacheImplementation()
    {
        // Arrange
        var options = new CacheOptions(_fixture.Create<string>());

        // Act
        var result = _sut.CreateAdvancedCache<string, string>(options);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IAdvancedCache<string, string>>(result);

        // Verify it implements both interface types
        Assert.IsAssignableFrom<IPrioritizedCache<string, string>>(result);
        Assert.IsAssignableFrom<IDependencyTrackingCache<string, string>>(result);
    }

    [Fact]
    public void CreatedCaches_ShouldHaveCorrectName()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var options = new CacheOptions(cacheName);

        // Act
        var basicCache = _sut.CreateCache<string, string>(options);
        var prioritizedCache = _sut.CreatePrioritizedCache<string, string>(options);
        var dependencyCache = _sut.CreateDependencyTrackingCache<string, string>(options);
        var advancedCache = _sut.CreateAdvancedCache<string, string>(options);

        // Assert
        Assert.Equal(cacheName, basicCache.Name);
        Assert.Equal(cacheName, prioritizedCache.Name);
        Assert.Equal(cacheName, dependencyCache.Name);
        Assert.Equal(cacheName, advancedCache.Name);
    }

    [Fact]
    public void CreatedCaches_ShouldHaveCorrectPriority()
    {
        // Arrange
        var options = new CacheOptions(_fixture.Create<string>())
        {
            Priority = CachePriority.High
        };

        // Act
        var basicCache = _sut.CreateCache<string, string>(options);
        var prioritizedCache = _sut.CreatePrioritizedCache<string, string>(options);
        var dependencyCache = _sut.CreateDependencyTrackingCache<string, string>(options);
        var advancedCache = _sut.CreateAdvancedCache<string, string>(options);

        // Assert
        Assert.Equal(CachePriority.High, basicCache.Priority);
        Assert.Equal(CachePriority.High, prioritizedCache.Priority);
        Assert.Equal(CachePriority.High, dependencyCache.Priority);
        Assert.Equal(CachePriority.High, advancedCache.Priority);
    }
}