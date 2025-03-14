namespace AngleSharp.StyleSystem.Services;

/// <summary>
/// Options for configuring the style system.
/// </summary>
public class StyleSystemOptions
{
    /// <summary>
    /// Gets or sets whether style optimization is enabled.
    /// </summary>
    public bool EnableOptimization { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to collect optimization metrics.
    /// </summary>
    public bool CollectMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to update styles immediately on changes.
    /// </summary>
    public bool UpdateStylesImmediately { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of worker threads for style computation.
    /// </summary>
    /// <remarks>
    /// Set to 0 to disable worker threads.
    /// </remarks>
    public int MaxWorkerThreads { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to load user agent stylesheets.
    /// </summary>
    public bool LoadUserAgentStylesheets { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in milliseconds for throttling style calculations.
    /// </summary>
    public int ThrottleIntervalMs { get; set; } = 16;

    /// <summary>
    /// Gets or sets the batch size for style tasks.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}