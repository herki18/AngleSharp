using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LayoutEngine.Contracts.Resource;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LayoutEngine.Platform.Resource
{
    public class ResourceErrorHandler : IResourceErrorHandler
    {
        private readonly ILogger<ResourceErrorHandler> _logger;
        private readonly ResourceErrorOptions _options;
        private readonly ConcurrentDictionary<string, ResourceErrorInfo> _errorInfos = new();
        private readonly ConcurrentDictionary<string, string> _fallbacks = new();

        public ResourceErrorHandler(
            ILogger<ResourceErrorHandler> logger,
            IOptions<ResourceErrorOptions>? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new ResourceErrorOptions();
        }

        public void HandleError(string url, Exception error)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            _logger.LogError(error, "Error loading resource {Url}: {Message}", url, error.Message);

            // Get or create error info for this URL
            var errorInfo = _errorInfos.GetOrAdd(url, _ => new ResourceErrorInfo());

            // Update error information
            errorInfo.LastError = error;
            errorInfo.LastErrorTime = DateTime.UtcNow;
            errorInfo.ErrorCount++;

            // Log additional context if it's a recurring error
            if (errorInfo.ErrorCount > 1)
            {
                _logger.LogWarning(
                    "Resource {Url} has failed {ErrorCount} times. First error at {FirstErrorTime}",
                    url, errorInfo.ErrorCount, errorInfo.FirstErrorTime);
            }
        }

        public ResourceErrorPolicy GetErrorPolicy(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            // Check if there's a specific error policy for this URL pattern
            foreach (var policyPattern in _options.UrlPolicyPatterns)
            {
                if (url.Contains(policyPattern.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return policyPattern.Value;
                }
            }

            // Check if we have a fallback registered
            if (_fallbacks.ContainsKey(url))
            {
                return ResourceErrorPolicy.UseFallback;
            }

            // Check if we have prior errors for this URL
            if (_errorInfos.TryGetValue(url, out var errorInfo))
            {
                // If we have too many errors in a short time, fail fast
                if (errorInfo.ErrorCount >= _options.MaxErrorsBeforeFailFast &&
                    (DateTime.UtcNow - errorInfo.FirstErrorTime) < _options.FailFastTimeWindow)
                {
                    return ResourceErrorPolicy.FailFast;
                }

                // If we have some errors but not too many, retry
                if (errorInfo.ErrorCount < _options.MaxRetries)
                {
                    return ResourceErrorPolicy.Retry;
                }
            }

            // Default to the configured default policy
            return _options.DefaultPolicy;
        }

        public void RegisterFallback(string url, string fallbackUrl)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            if (string.IsNullOrEmpty(fallbackUrl))
                throw new ArgumentException("Fallback URL cannot be null or empty", nameof(fallbackUrl));

            _fallbacks[url] = fallbackUrl;
            _logger.LogInformation("Registered fallback for {Url} -> {FallbackUrl}", url, fallbackUrl);
        }

        private class ResourceErrorInfo
        {
            public DateTime FirstErrorTime { get; } = DateTime.UtcNow;
            public DateTime LastErrorTime { get; set; } = DateTime.UtcNow;
            public Exception LastError { get; set; } = null!;
            public int ErrorCount { get; set; } = 1;
        }
    }

    public class ResourceErrorOptions
    {
        public ResourceErrorPolicy DefaultPolicy { get; set; } = ResourceErrorPolicy.Retry;
        public int MaxRetries { get; set; } = 3;
        public int MaxErrorsBeforeFailFast { get; set; } = 5;
        public TimeSpan FailFastTimeWindow { get; set; } = TimeSpan.FromMinutes(5);
        public Dictionary<string, ResourceErrorPolicy> UrlPolicyPatterns { get; set; } =
            new Dictionary<string, ResourceErrorPolicy>();
    }
}