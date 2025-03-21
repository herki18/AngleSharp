using System;
using System.Collections.Generic;
using LayoutEngine.Contracts.Resource;

namespace LayoutEngine.Platform;

using Threading;

/// <summary>
/// Configuration options for the Platform services.
/// </summary>
public class PlatformOptions
{
    /// <summary>
    /// Gets or sets the thread pool options.
    /// </summary>
    public ThreadPoolOptions ThreadPool { get; set; } = new ThreadPoolOptions();

    /// <summary>
    /// Gets or sets the maximum number of concurrent resource loads.
    /// </summary>
    public int MaxConcurrentResourceLoads { get; set; } = 6;

    /// <summary>
    /// Gets or sets the default timeout for resource loading in milliseconds.
    /// </summary>
    public int DefaultResourceLoadTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the default error policy for resource loading.
    /// </summary>
    public ResourceErrorPolicy DefaultResourceErrorPolicy { get; set; } = ResourceErrorPolicy.Retry;

    /// <summary>
    /// Gets or sets the maximum number of retries for resource loading.
    /// </summary>
    public int MaxResourceLoadRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retries in milliseconds.
    /// </summary>
    public int ResourceLoadRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the target frames per second for the frame scheduler.
    /// </summary>
    public int TargetFramesPerSecond { get; set; } = 60;
}