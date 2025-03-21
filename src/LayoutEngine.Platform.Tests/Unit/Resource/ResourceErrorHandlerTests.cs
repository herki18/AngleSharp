namespace LayoutEngine.Platform.Tests.Unit.Resource;

using System;
using AutoFixture;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform.Resource;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

public class ResourceErrorHandlerTests
{
    private readonly Fixture _fixture;
    private readonly ILogger<ResourceErrorHandler> _logger;
    private readonly ResourceErrorOptions _options;
    private readonly IOptions<ResourceErrorOptions> _optionsWrapper;
    private readonly ResourceErrorHandler _errorHandler;

    public ResourceErrorHandlerTests()
    {
        _fixture = new Fixture();
        _logger = Substitute.For<ILogger<ResourceErrorHandler>>();

        _options = new ResourceErrorOptions
        {
            DefaultPolicy = ResourceErrorPolicy.Retry,
            MaxRetries = 3,
            MaxErrorsBeforeFailFast = 5,
            FailFastTimeWindow = TimeSpan.FromMinutes(5),
            UrlPolicyPatterns = new Dictionary<string, ResourceErrorPolicy>
            {
                { "critical", ResourceErrorPolicy.FailFast },
                { "optional", ResourceErrorPolicy.Ignore },
                { "fallback", ResourceErrorPolicy.UseFallback }
            }
        };

        _optionsWrapper = Options.Create(_options);
        _errorHandler = new ResourceErrorHandler(_logger, _optionsWrapper);
    }

    [Fact]
    public void HandleError_ShouldLogError()
    {
        // Arrange
        string url = "https://example.com/image.png";
        var exception = new Exception("Test error");

        // Act
        _errorHandler.HandleError(url, exception);

        // Assert
        _logger.Received(1).LogError(
            Arg.Is<Exception>(e => e == exception),
            Arg.Is<string>(s => s.Contains("{Url}")),
            Arg.Is<string>(s => s == url),
            Arg.Is<string>(s => s == exception.Message)
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HandleError_WithInvalidUrl_ShouldThrow(string url)
    {
        // Arrange
        var exception = new Exception("Test error");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _errorHandler.HandleError(url!, exception));
    }

    [Fact]
    public void HandleError_WithNullException_ShouldThrow()
    {
        // Arrange
        string url = "https://example.com/image.png";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _errorHandler.HandleError(url, null!));
    }

    [Fact]
    public void HandleError_RepeatedErrors_ShouldLogWarning()
    {
        // Arrange
        string url = "https://example.com/image.png";
        var exception = new Exception("Test error");

        // Act
        _errorHandler.HandleError(url, exception);
        _errorHandler.HandleError(url, exception);

        // Assert
        _logger.Received(1).LogWarning(
            Arg.Is<string>(s => s.Contains("has failed {ErrorCount} times")),
            Arg.Is<string>(s => s == url),
            Arg.Is<int>(i => i == 2),
            Arg.Any<DateTime>()
        );
    }

    [Theory]
    [InlineData("https://example.com/critical-resource.png", ResourceErrorPolicy.FailFast)]
    [InlineData("https://example.com/optional-resource.png", ResourceErrorPolicy.Ignore)]
    [InlineData("https://example.com/fallback-resource.png", ResourceErrorPolicy.UseFallback)]
    [InlineData("https://example.com/normal-resource.png", ResourceErrorPolicy.Retry)]
    public void GetErrorPolicy_ShouldReturnCorrectPolicyBasedOnUrl(string url, ResourceErrorPolicy expectedPolicy)
    {
        // Act
        var policy = _errorHandler.GetErrorPolicy(url);

        // Assert
        Assert.Equal(expectedPolicy, policy);
    }

    [Fact]
    public void GetErrorPolicy_WithFallbackRegistered_ShouldReturnUseFallback()
    {
        // Arrange
        string url = "https://example.com/image.png";
        string fallbackUrl = "https://backup.example.com/image.png";
        _errorHandler.RegisterFallback(url, fallbackUrl);

        // Act
        var policy = _errorHandler.GetErrorPolicy(url);

        // Assert
        Assert.Equal(ResourceErrorPolicy.UseFallback, policy);
    }

    [Fact]
    public void GetErrorPolicy_WithExcessiveErrors_ShouldReturnFailFast()
    {
        // Arrange
        string url = "https://example.com/resource.png";
        var exception = new Exception("Test error");

        // Trigger multiple errors to exceed MaxErrorsBeforeFailFast
        for (int i = 0; i < _options.MaxErrorsBeforeFailFast; i++)
        {
            _errorHandler.HandleError(url, exception);
        }

        // Act
        var policy = _errorHandler.GetErrorPolicy(url);

        // Assert
        Assert.Equal(ResourceErrorPolicy.FailFast, policy);
    }

    [Fact]
    public void GetErrorPolicy_WithFewErrors_ShouldReturnRetry()
    {
        // Arrange
        string url = "https://example.com/resource.png";
        var exception = new Exception("Test error");

        // Trigger fewer errors than MaxRetries
        _errorHandler.HandleError(url, exception);
        _errorHandler.HandleError(url, exception);

        // Act
        var policy = _errorHandler.GetErrorPolicy(url);

        // Assert
        Assert.Equal(ResourceErrorPolicy.Retry, policy);
    }

    [Fact]
    public void RegisterFallback_ShouldRegisterFallbackUrl()
    {
        // Arrange
        string url = "https://example.com/image.png";
        string fallbackUrl = "https://backup.example.com/image.png";

        // Act
        _errorHandler.RegisterFallback(url, fallbackUrl);
        var policy = _errorHandler.GetErrorPolicy(url);

        // Assert
        Assert.Equal(ResourceErrorPolicy.UseFallback, policy);
        _logger.Received(1).LogInformation(
            Arg.Is<string>(s => s.Contains("Registered fallback")),
            Arg.Is<string>(s => s == url),
            Arg.Is<string>(s => s == fallbackUrl)
        );
    }

    [Theory]
    [InlineData(null, "fallback")]
    [InlineData("", "fallback")]
    [InlineData("url", null)]
    [InlineData("url", "")]
    public void RegisterFallback_WithInvalidUrls_ShouldThrow(string url, string fallbackUrl)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _errorHandler.RegisterFallback(url!, fallbackUrl!));
    }
}