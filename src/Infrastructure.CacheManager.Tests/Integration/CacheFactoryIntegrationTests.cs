using System;
using System.Collections.Generic;
using Infrastructure.CacheManager.API.Caching.CacheTypes;
using Infrastructure.CacheManager.API.Factory;
using Infrastructure.CacheManager.API.Management;
using Infrastructure.CacheManager.API.Models;
using Infrastructure.CacheManager.DI;
using Infrastructure.CacheManager.Internal.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.CacheManager.Tests.Integration;

using API.Caching;

/// <summary>
/// Integration tests that verify the complete factory + cachemanager interaction
/// These tests use the real implementations, not mocks
/// </summary>
public class CacheFactoryIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICacheFactory _factory;
    private readonly ICacheManager _cacheManager;

    public CacheFactoryIntegrationTests()
    {
        // Create a real service provider with the actual implementations
        var services = new ServiceCollection();
        services.AddCacheManagerModule();
        _serviceProvider = services.BuildServiceProvider();

        _factory = _serviceProvider.GetRequiredService<ICacheFactory>();
        _cacheManager = _serviceProvider.GetRequiredService<ICacheManager>();
    }

    [Fact]
    public void FactoryCreatedCache_WithAutoRegister_ShouldBeRetrievableFromManager()
    {
        // Arrange
        var options = new CacheOptions("TestCache") { AutoRegister = true };

        // Act
        var cache = _factory.CreateCache<string, string>(options);
        var retrievedCache = _cacheManager.GetCache<ICache<string, string>>("TestCache");

        // Assert
        Assert.Same(cache, retrievedCache);
    }

    [Fact]
    public void FactoryCreatedCaches_WithDifferentTypes_ShouldWorkCorrectlyTogether()
    {
        // Arrange
        var basicOptions = new CacheOptions("BasicCache");
        var advancedOptions = new CacheOptions("AdvancedCache");

        // Act
        var basicCache = _factory.CreateCache<string, int>(basicOptions);
        var advancedCache = _factory.CreateAdvancedCache<string, string>(advancedOptions);

        // Verify they're both registered
        var retrievedBasic = _cacheManager.GetCache<ICache<string, int>>("BasicCache");
        var retrievedAdvanced = _cacheManager.GetCache<IAdvancedCache<string, string>>("AdvancedCache");

        // Add some data
        basicCache.Set("key1", 123);
        advancedCache.Set("key1", "value1", CacheEntryPriority.High);

        // Add dependencies in advanced cache
        advancedCache.SetWithDependencies("child", "childValue", new List<string> { "key1" });

        // Assert
        Assert.Equal(123, basicCache.GetOrCreate("key1", _ => 0));
        Assert.Equal("value1", advancedCache.GetOrCreate("key1", _ => ""));

        // Verify dependency tracking works
        Assert.True(advancedCache.Contains("child"));
        var invalidated = advancedCache.InvalidateWithDependents("key1");
        Assert.Equal(2, invalidated); // key1 + child
        Assert.False(advancedCache.Contains("key1"));
        Assert.False(advancedCache.Contains("child"));

        // Verify basic cache is unaffected
        Assert.True(basicCache.Contains("key1"));
    }

    [Fact]
    public void AdvancedCache_ShouldEnforceSizeLimit()
    {
        // Arrange
        var options = new CacheOptions("SizedCache")
        {
            SizeLimit = 30 // Only allow 30 units of cache entries
        };

        var cache = _factory.CreateAdvancedCache<string, string>(options);

        // Add entries with explicit sizes
        var entryOptions = new CacheEntryOptions { Size = 10 };

        // Act
        cache.Set("key1", "value1", CacheEntryPriority.High, entryOptions);
        cache.Set("key2", "value2", CacheEntryPriority.High, entryOptions);
        cache.Set("key3", "value3", CacheEntryPriority.High, entryOptions);

        // Adding a 4th should cause eviction (total would be 40 > limit of 30)
        cache.Set("key4", "value4", CacheEntryPriority.High, entryOptions);

        // Assert
        // At least one entry should have been evicted to stay under size limit
        var containsCount = 0;
        if (cache.Contains("key1")) containsCount++;
        if (cache.Contains("key2")) containsCount++;
        if (cache.Contains("key3")) containsCount++;
        if (cache.Contains("key4")) containsCount++;

        // We should have at most 3 items (30 size limit / 10 per item)
        Assert.True(containsCount <= 3);
    }

    [Fact]
    public void FactoryCreatedCache_WithEvictionCallback_ShouldInvokeCallback()
    {
        // Arrange
        var options = new CacheOptions("CallbackCache");
        var callbackInvoked = false;
        object? evictedValue = null;

        void OnEvicted(object obj)
        {
            callbackInvoked = true;
            evictedValue = obj;
        }

        var cache = _factory.CreateCache<string, string>(options);

        // Add an entry with a callback
        var entryOptions = new CacheEntryOptions
        {
            PostEvictionCallback = OnEvicted
        };

        cache.Set("testKey", "testValue", entryOptions);

        // Act - remove the entry
        cache.Remove("testKey");

        // Assert
        Assert.True(callbackInvoked);
        Assert.Equal("testKey", evictedValue);
    }

    [Fact]
    public void FactoryCreatedCache_WithExpiration_ShouldExpireEntries()
    {
        // Arrange
        var options = new CacheOptions("ExpiringCache");
        var cache = _factory.CreateCache<string, string>(options);

        // Add an entry with a very short expiration
        var entryOptions = new CacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(50)
        };

        // Act
        cache.Set("expiringKey", "value", entryOptions);

        // Verify it exists initially
        Assert.True(cache.Contains("expiringKey"));

        // Wait for expiration
        System.Threading.Thread.Sleep(100);

        // Assert
        Assert.False(cache.Contains("expiringKey"));
    }

    public void Dispose()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }
}