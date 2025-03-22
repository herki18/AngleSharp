using System;
using System.Collections.Generic;
using AutoFixture;
using LayoutEngine.Contracts.Resource;
using LayoutEngine.Platform.Resource;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace LayoutEngine.Platform.Tests.Unit.Resource;

public class ResourceErrorHandlerTests
{
    private readonly Fixture _fixture;
    private readonly TestLogger<ResourceErrorHandler> _logger;
    private readonly ResourceErrorOptions _options;
    private readonly IOptions<ResourceErrorOptions> _optionsWrapper;
    private readonly ResourceErrorHandler _errorHandler;

    public ResourceErrorHandlerTests()
    {
        _fixture = new Fixture();
        _logger = new TestLogger<ResourceErrorHandler>();

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
        Assert.Single(_logger.ErrorMessages);
        Assert.Contains(url, _logger.FormattedErrors[0].Args);
        Assert.Contains(exception.Message, _logger.FormattedErrors[0].Args);
        Assert.Same(exception, _logger.FormattedErrors[0].Exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HandleError_WithInvalidUrl_ShouldThrow(string? url)
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
        Assert.Contains(_logger.WarningMessages, msg => msg.Contains("failed") && msg.Contains(url));
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
        Assert.Contains(_logger.InfoMessages, msg => msg.Contains("Registered fallback") && msg.Contains(url) && msg.Contains(fallbackUrl));
    }

    [Theory]
    [InlineData(null, "fallback")]
    [InlineData("", "fallback")]
    [InlineData("url", null)]
    [InlineData("url", "")]
    public void RegisterFallback_WithInvalidUrls_ShouldThrow(string? url, string? fallbackUrl)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _errorHandler.RegisterFallback(url!, fallbackUrl!));
    }

    // Enhanced test logger implementation to avoid NSubstitute issues
    private class TestLogger<T> : ILogger<T>
    {
        public List<string> WarningMessages { get; } = new List<string>();
        public List<string> ErrorMessages { get; } = new List<string>();
        public List<string> InfoMessages { get; } = new List<string>();
        public List<(Exception? Exception, object[] Args)> FormattedErrors { get; } = new List<(Exception?, object[])>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Warning:
                    WarningMessages.Add(message);
                    break;
                case LogLevel.Error:
                    ErrorMessages.Add(message);
                    // Extract the args that were passed to the logger
                    if (state is IReadOnlyList<KeyValuePair<string, object>> logValues)
                    {
                        var args = new List<object>();
                        foreach (var kv in logValues)
                        {
                            if (kv.Key != "{OriginalFormat}" && kv.Key != "exception")
                            {
                                args.Add(kv.Value);
                            }
                        }
                        FormattedErrors.Add((exception, args.ToArray()));
                    }
                    break;
                case LogLevel.Information:
                    InfoMessages.Add(message);
                    break;
            }
        }
    }
}