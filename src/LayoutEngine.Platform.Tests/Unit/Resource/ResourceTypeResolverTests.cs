namespace LayoutEngine.Platform.Tests.Unit.Resource;

using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform.Resource.Abstractions;
using Xunit;

public class ResourceTypeResolverTests
{
    private readonly DefaultResourceTypeResolver _resolver;

    public ResourceTypeResolverTests()
    {
        _resolver = new DefaultResourceTypeResolver();
    }

    [Theory]
    [InlineData("https://example.com/image.jpg", ResourceType.Image)]
    [InlineData("https://example.com/image.jpeg", ResourceType.Image)]
    [InlineData("https://example.com/image.png", ResourceType.Image)]
    [InlineData("https://example.com/image.gif", ResourceType.Image)]
    [InlineData("https://example.com/image.webp", ResourceType.Image)]
    [InlineData("https://example.com/image.svg", ResourceType.Image)]
    public void GetResourceTypeFromUrl_WithImageExtensions_ShouldReturnImageType(string url, ResourceType expectedType)
    {
        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData("https://example.com/font.woff", ResourceType.Font)]
    [InlineData("https://example.com/font.woff2", ResourceType.Font)]
    [InlineData("https://example.com/font.ttf", ResourceType.Font)]
    [InlineData("https://example.com/font.otf", ResourceType.Font)]
    [InlineData("https://example.com/font.eot", ResourceType.Font)]
    public void GetResourceTypeFromUrl_WithFontExtensions_ShouldReturnFontType(string url, ResourceType expectedType)
    {
        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData("https://example.com/styles.css", ResourceType.StyleSheet)]
    public void GetResourceTypeFromUrl_WithCssExtension_ShouldReturnStyleSheetType(string url, ResourceType expectedType)
    {
        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData("https://example.com/script.js", ResourceType.Script)]
    public void GetResourceTypeFromUrl_WithJsExtension_ShouldReturnScriptType(string url, ResourceType expectedType)
    {
        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData("https://example.com/file.txt", ResourceType.Unknown)]
    [InlineData("https://example.com/file.doc", ResourceType.Unknown)]
    [InlineData("https://example.com/file.pdf", ResourceType.Unknown)]
    [InlineData("https://example.com/file", ResourceType.Unknown)]
    public void GetResourceTypeFromUrl_WithUnknownExtensions_ShouldReturnUnknownType(string url, ResourceType expectedType)
    {
        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData(null, ResourceType.Unknown)]
    [InlineData("", ResourceType.Unknown)]
    [InlineData(" ", ResourceType.Unknown)]
    public void GetResourceTypeFromUrl_WithInvalidUrl_ShouldReturnUnknownType(string url, ResourceType expectedType)
    {
        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData("https://example.com/image.JPG", ResourceType.Image)]
    [InlineData("https://example.com/font.WOFF2", ResourceType.Font)]
    [InlineData("https://example.com/styles.CSS", ResourceType.StyleSheet)]
    [InlineData("https://example.com/script.JS", ResourceType.Script)]
    public void GetResourceTypeFromUrl_WithUppercaseExtensions_ShouldBeCaseInsensitive(string url, ResourceType expectedType)
    {
        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData("https://example.com/image.jpg?v=123", ResourceType.Image)]
    [InlineData("https://example.com/font.woff?version=2", ResourceType.Font)]
    [InlineData("https://example.com/styles.css?cache=no", ResourceType.StyleSheet)]
    [InlineData("https://example.com/script.js#fragment", ResourceType.Script)]
    public void GetResourceTypeFromUrl_WithQueryParamsOrFragments_ShouldIgnoreThemForTypeDetection(string url, ResourceType expectedType)
    {
        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Fact]
    public void GetResourceTypeFromUrl_WithComplexPath_ShouldFindExtensionCorrectly()
    {
        // Arrange
        string url = "https://example.com/path/to/nested/folders/with.dots/image.png";

        // Act
        var result = _resolver.GetResourceTypeFromUrl(url);

        // Assert
        Assert.Equal(ResourceType.Image, result);
    }
}