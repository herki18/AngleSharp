namespace LayoutEngine.Platform.Tests.Unit.Resource;

using System;
using System.Threading.Tasks;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform.Resource.Abstractions;
using Xunit;

public class DefaultResourceLoadingStrategyTests
{
    private readonly DefaultResourceLoadingStrategy _strategy;

    public DefaultResourceLoadingStrategyTests()
    {
        _strategy = new DefaultResourceLoadingStrategy();
    }

    [Theory]
    [InlineData(ResourceType.Image)]
    [InlineData(ResourceType.Font)]
    [InlineData(ResourceType.StyleSheet)]
    [InlineData(ResourceType.Script)]
    [InlineData(ResourceType.Unknown)]
    public async Task LoadResourceAsync_ShouldReturnDummyResource(ResourceType resourceType)
    {
        // Arrange
        string url = $"https://example.com/resource.{GetExtensionForType(resourceType)}";
        TimeSpan timeout = TimeSpan.FromSeconds(30);

        // Act
        var resource = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(url, resource.Url);
        Assert.Equal(resourceType, resource.ResourceType);
        Assert.Equal(GetContentTypeForResourceType(resourceType), resource.ContentType);
        Assert.True(resource.IsLoaded);
        Assert.Empty(resource.Data);
    }

    [Fact]
    public async Task LoadResourceAsync_WithImageResource_ShouldHaveCorrectContentType()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        // Act
        var resource = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.Equal("image/png", resource.ContentType);
    }

    [Fact]
    public async Task LoadResourceAsync_WithFontResource_ShouldHaveCorrectContentType()
    {
        // Arrange
        string url = "https://example.com/font.woff2";
        ResourceType resourceType = ResourceType.Font;
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        // Act
        var resource = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.Equal("font/woff2", resource.ContentType);
    }

    [Fact]
    public async Task LoadResourceAsync_WithStyleSheetResource_ShouldHaveCorrectContentType()
    {
        // Arrange
        string url = "https://example.com/style.css";
        ResourceType resourceType = ResourceType.StyleSheet;
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        // Act
        var resource = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.Equal("text/css", resource.ContentType);
    }

    [Fact]
    public async Task LoadResourceAsync_WithScriptResource_ShouldHaveCorrectContentType()
    {
        // Arrange
        string url = "https://example.com/script.js";
        ResourceType resourceType = ResourceType.Script;
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        // Act
        var resource = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.Equal("text/javascript", resource.ContentType);
    }

    [Fact]
    public async Task LoadResourceAsync_WithUnknownResource_ShouldHaveDefaultContentType()
    {
        // Arrange
        string url = "https://example.com/unknown.ext";
        ResourceType resourceType = ResourceType.Unknown;
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        // Act
        var resource = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.Equal("application/octet-stream", resource.ContentType);
    }

    [Fact]
    public async Task LoadResourceAsync_WithInfiniteTimeout_ShouldWork()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        TimeSpan timeout = Timeout.InfiniteTimeSpan;

        // Act
        var resource = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(url, resource.Url);
    }

    [Fact]
    public async Task LoadResourceAsync_WithZeroTimeout_ShouldWork()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        TimeSpan timeout = TimeSpan.Zero;

        // Act
        var resource = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(url, resource.Url);
    }

    [Fact]
    public async Task LoadResourceAsync_MultipleResources_ShouldCreateUniqueInstances()
    {
        // Arrange
        string url1 = "https://example.com/image1.png";
        string url2 = "https://example.com/image2.png";
        ResourceType resourceType = ResourceType.Image;
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        // Act
        var resource1 = await _strategy.LoadResourceAsync(url1, resourceType, timeout);
        var resource2 = await _strategy.LoadResourceAsync(url2, resourceType, timeout);

        // Assert
        Assert.NotSame(resource1, resource2);
        Assert.Equal(url1, resource1.Url);
        Assert.Equal(url2, resource2.Url);
    }

    [Fact]
    public async Task LoadResourceAsync_SameUrlMultipleTimes_ShouldCreateSeparateInstances()
    {
        // Arrange
        string url = "https://example.com/image.png";
        ResourceType resourceType = ResourceType.Image;
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        // Act
        var resource1 = await _strategy.LoadResourceAsync(url, resourceType, timeout);
        var resource2 = await _strategy.LoadResourceAsync(url, resourceType, timeout);

        // Assert
        Assert.NotSame(resource1, resource2); // Should be separate instances
        Assert.Equal(url, resource1.Url);
        Assert.Equal(url, resource2.Url);
    }

    private string GetExtensionForType(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.Image => "png",
            ResourceType.Font => "woff2",
            ResourceType.StyleSheet => "css",
            ResourceType.Script => "js",
            _ => "dat",
        };
    }

    private string GetContentTypeForResourceType(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.Image => "image/png",
            ResourceType.Font => "font/woff2",
            ResourceType.StyleSheet => "text/css",
            ResourceType.Script => "text/javascript",
            _ => "application/octet-stream",
        };
    }
}