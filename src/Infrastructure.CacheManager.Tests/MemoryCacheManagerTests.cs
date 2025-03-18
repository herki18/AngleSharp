using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoFixture;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.API.Monitoring;
using Infrastructure.CacheManager.Internal.Caches;
using Infrastructure.CacheManager.Internal.Core;
using NSubstitute;
using Xunit;

namespace Infrastructure.CacheManager.Tests;

public class MemoryCacheManagerTests
{
    private readonly Fixture _fixture;
    private readonly IMemoryPressureMonitor _mockMemoryPressureMonitor;
    private readonly MemoryCacheManager _sut;

    public MemoryCacheManagerTests()
    {
        _fixture = new Fixture();
        _mockMemoryPressureMonitor = Substitute.For<IMemoryPressureMonitor>();
        _sut = new MemoryCacheManager(_mockMemoryPressureMonitor, TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void RegisterCache_ShouldAddCacheToRegistry()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var cache = Substitute.For<ITrimableCache>();
        cache.Name.Returns(cacheName);

        // Act
        _sut.RegisterCache(cacheName, cache);
        var result = _sut.GetCache<ITrimableCache>(cacheName);

        // Assert
        Assert.Same(cache, result);
    }

    [Fact]
    public void RegisterCache_WithDuplicateName_ShouldThrowArgumentException()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var cache1 = Substitute.For<ITrimableCache>();
        var cache2 = Substitute.For<ITrimableCache>();
        cache1.Name.Returns(cacheName);
        cache2.Name.Returns(cacheName);
        _sut.RegisterCache(cacheName, cache1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.RegisterCache(cacheName, cache2));
    }

    [Fact]
    public void GetCache_WithInvalidCastType_ShouldThrowInvalidCastException()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var cache = new MemoryCacheBase<string, string>(cacheName);
        _sut.RegisterCache(cacheName, cache);

        // Act & Assert
        Assert.Throws<InvalidCastException>(() => _sut.GetCache<MemoryCacheBase<int, int>>(cacheName));
    }

    [Fact]
    public void GetCache_WithNonExistentName_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nonExistentName = _fixture.Create<string>();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _sut.GetCache<ITrimableCache>(nonExistentName));
    }

    [Fact]
    public void GetAllCaches_ShouldReturnAllRegisteredCaches()
    {
        // Arrange
        var caches = new List<ITrimableCache>();
        for (int i = 0; i < 3; i++)
        {
            var cacheName = _fixture.Create<string>();
            var cache = Substitute.For<ITrimableCache>();
            cache.Name.Returns(cacheName);
            caches.Add(cache);
            _sut.RegisterCache(cacheName, cache);
        }

        // Act
        var result = _sut.GetAllCaches();

        // Assert
        Assert.Equal(caches.Count, result.Count);
        foreach (var cache in caches)
        {
            Assert.Contains(cache, result);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void TrimAllCaches_WithZeroOrNegativePercentage_ShouldNotTrimCaches(double percentage)
    {
        // Arrange
        var cache1 = Substitute.For<ITrimableCache>();
        var cache2 = Substitute.For<ITrimableCache>();
        _sut.RegisterCache("cache1", cache1);
        _sut.RegisterCache("cache2", cache2);

        // Act
        var result = _sut.TrimAllCaches(percentage);

        // Assert
        Assert.Equal(0, result);
        cache1.DidNotReceive().Trim(Arg.Any<double>());
        cache2.DidNotReceive().Trim(Arg.Any<double>());
    }

    [Fact]
    public void TrimAllCaches_WithValidPercentage_ShouldTrimAllCaches()
    {
        // Arrange
        var cache1 = Substitute.For<ITrimableCache>();
        var cache2 = Substitute.For<ITrimableCache>();
        cache1.Trim(Arg.Any<double>()).Returns(5);
        cache2.Trim(Arg.Any<double>()).Returns(3);
        _sut.RegisterCache("cache1", cache1);
        _sut.RegisterCache("cache2", cache2);

        // Act
        var result = _sut.TrimAllCaches(50);

        // Assert
        Assert.Equal(8, result);
        cache1.Received(1).Trim(50);
        cache2.Received(1).Trim(50);
    }

    [Fact]
    public void TrimAllCaches_WithPercentageGreaterThan100_ShouldTrimWith100Percentage()
    {
        // Arrange
        var cache = Substitute.For<ITrimableCache>();
        _sut.RegisterCache("cache", cache);

        // Act
        _sut.TrimAllCaches(150);

        // Assert
        cache.Received(1).Trim(100);
    }

    [Fact]
    public void TrimByPriority_ShouldOnlyTrimCachesWithPriorityLessThanOrEqualToSpecified()
    {
        // Arrange
        var lowCache = Substitute.For<ITrimableCache>();
        var normalCache = Substitute.For<ITrimableCache>();
        var highCache = Substitute.For<ITrimableCache>();

        lowCache.Priority.Returns(CachePriority.Low);
        normalCache.Priority.Returns(CachePriority.Normal);
        highCache.Priority.Returns(CachePriority.High);

        lowCache.Trim(Arg.Any<double>()).Returns(2);
        normalCache.Trim(Arg.Any<double>()).Returns(3);

        _sut.RegisterCache("lowCache", lowCache);
        _sut.RegisterCache("normalCache", normalCache);
        _sut.RegisterCache("highCache", highCache);

        // Act
        var result = _sut.TrimByPriority(CachePriority.Normal, 50);

        // Assert
        Assert.Equal(5, result);
        lowCache.Received(1).Trim(50);
        normalCache.Received(1).Trim(50);
        highCache.DidNotReceive().Trim(Arg.Any<double>());
    }

    [Fact]
    public void ClearAllCaches_ShouldClearAllRegisteredCaches()
    {
        // Arrange
        var cache1 = Substitute.For<ITrimableCache>();
        var cache2 = Substitute.For<ITrimableCache>();
        _sut.RegisterCache("cache1", cache1);
        _sut.RegisterCache("cache2", cache2);

        // Act
        _sut.ClearAllCaches();

        // Assert
        cache1.Received(1).Clear();
        cache2.Received(1).Clear();
    }

    [Fact]
    public void UnregisterCache_WithExistingCache_ShouldRemoveCache()
    {
        // Arrange
        var cacheName = _fixture.Create<string>();
        var cache = Substitute.For<ITrimableCache>();
        _sut.RegisterCache(cacheName, cache);

        // Act
        var result = _sut.UnregisterCache(cacheName);

        // Assert
        Assert.True(result);
        Assert.Throws<KeyNotFoundException>(() => _sut.GetCache<ITrimableCache>(cacheName));
    }

    [Fact]
    public void UnregisterCache_WithNonExistentCache_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentName = _fixture.Create<string>();

        // Act
        var result = _sut.UnregisterCache(nonExistentName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetTotalCacheSize_ShouldReturnSumOfAllCacheSizes()
    {
        // Arrange
        var cache1 = Substitute.For<ITrimableCache>();
        var cache2 = Substitute.For<ITrimableCache>();
        cache1.EstimatedSize.Returns(100L);
        cache2.EstimatedSize.Returns(200L);
        _sut.RegisterCache("cache1", cache1);
        _sut.RegisterCache("cache2", cache2);

        // Act
        var result = _sut.GetTotalCacheSize();

        // Assert
        Assert.Equal(300L, result);
    }

    [Fact]
    public void GetTotalEntryCount_ShouldReturnSumOfAllCacheCounts()
    {
        // Arrange
        var cache1 = Substitute.For<ITrimableCache>();
        var cache2 = Substitute.For<ITrimableCache>();
        cache1.Count.Returns(10);
        cache2.Count.Returns(20);
        _sut.RegisterCache("cache1", cache1);
        _sut.RegisterCache("cache2", cache2);

        // Act
        var result = _sut.GetTotalEntryCount();

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public void OnMemoryPressureDetected_WithLowPressure_ShouldTrimLowPriorityCaches()
    {
        // Arrange
        var lowCache = Substitute.For<ITrimableCache>();
        var normalCache = Substitute.For<ITrimableCache>();

        lowCache.Priority.Returns(CachePriority.Low);
        normalCache.Priority.Returns(CachePriority.Normal);

        _sut.RegisterCache("lowCache", lowCache);
        _sut.RegisterCache("normalCache", normalCache);

        // Act
        var eventArgs = new MemoryPressureEventArgs(
            PressureSeverity.Low,
            100L,
            200L,
            50.0);

        _mockMemoryPressureMonitor.MemoryPressureDetected += Raise.Event<EventHandler<MemoryPressureEventArgs>>(
            _mockMemoryPressureMonitor, eventArgs);

        // Assert
        lowCache.Received(1).Trim(50);
        normalCache.DidNotReceive().Trim(Arg.Any<double>());
    }

    [Fact]
    public void Dispose_ShouldDisposeAllCachesAndMemoryPressureMonitor()
    {
        // Arrange
        var disposableCache = Substitute.For<ITrimableCache, IDisposable>();
        _sut.RegisterCache("disposableCache", disposableCache);

        // Act
        _sut.Dispose();

        // Assert
        ((IDisposable)disposableCache).Received(1).Dispose();
        _mockMemoryPressureMonitor.Received(1).Dispose();
    }
}