using System;
using AutoFixture;
using Infrastructure.CacheManager.API.Factory;
using Infrastructure.CacheManager.API.Management;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Infrastructure.CacheManager.Tests.Factory;

public class CacheOptionsTests
{
    private readonly Fixture _fixture;

    public CacheOptionsTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var options = new CacheOptions();

        // Assert
        Assert.Empty(options.Name);
        Assert.Equal(CachePriority.Normal, options.Priority);
        Assert.Equal(TimeSpan.FromMinutes(10), options.CleanupInterval);
        Assert.NotNull(options.MemoryCacheOptions);
        Assert.Null(options.SizeLimit);
        Assert.True(options.AutoRegister);
    }

    [Fact]
    public void NamedConstructor_ShouldSetName()
    {
        // Arrange
        var name = _fixture.Create<string>();

        // Act
        var options = new CacheOptions(name);

        // Assert
        Assert.Equal(name, options.Name);
        Assert.Equal(CachePriority.Normal, options.Priority);
    }

    [Fact]
    public void NamedConstructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CacheOptions(null!));
    }

    [Fact]
    public void SizeLimit_ShouldUpdateMemoryCacheOptionsSizeLimit()
    {
        // Arrange
        var options = new CacheOptions();
        var sizeLimit = 1000L;

        // Act
        options.SizeLimit = sizeLimit;

        // Assert
        Assert.Equal(sizeLimit, options.SizeLimit);
        Assert.Equal(sizeLimit, options.MemoryCacheOptions.SizeLimit);
    }

    [Fact]
    public void Clone_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new CacheOptions
        {
            Name = _fixture.Create<string>(),
            Priority = CachePriority.High,
            CleanupInterval = TimeSpan.FromMinutes(5),
            AutoRegister = false
        };

        original.SizeLimit = 2000L;
        original.MemoryCacheOptions.ExpirationScanFrequency = TimeSpan.FromSeconds(30);
        original.MemoryCacheOptions.CompactionPercentage = 0.25;

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Priority, clone.Priority);
        Assert.Equal(original.CleanupInterval, clone.CleanupInterval);
        Assert.Equal(original.SizeLimit, clone.SizeLimit);
        Assert.Equal(original.AutoRegister, clone.AutoRegister);
        Assert.Equal(original.MemoryCacheOptions.ExpirationScanFrequency, clone.MemoryCacheOptions.ExpirationScanFrequency);
        Assert.Equal(original.MemoryCacheOptions.CompactionPercentage, clone.MemoryCacheOptions.CompactionPercentage);

        // Verify it's a deep copy
        clone.Name = "Modified";
        clone.SizeLimit = 3000L;
        Assert.NotEqual(original.Name, clone.Name);
        Assert.NotEqual(original.SizeLimit, clone.SizeLimit);
    }

    [Fact]
    public void MemoryCacheOptions_DefaultValues_ShouldBeReasonable()
    {
        // Arrange & Act
        var options = new CacheOptions();

        // Assert - just checking the defaults are non-null and sensible
        Assert.NotNull(options.MemoryCacheOptions);
        Assert.NotEqual(TimeSpan.Zero, options.MemoryCacheOptions.ExpirationScanFrequency);
        Assert.True(options.MemoryCacheOptions.CompactionPercentage > 0);
    }
}