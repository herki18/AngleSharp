namespace LayoutEngine.Core;

using AngleSharp;

/// <summary>
/// Configuration for the LayoutEngine.
/// </summary>
public class LayoutEngineConfiguration
{
    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static LayoutEngineConfiguration Default => new LayoutEngineConfiguration
    {
        AngleSharpConfiguration = Configuration.Default
            .WithDefaultLoader()
            .WithCss()
    };

    /// <summary>
    /// Gets or sets the AngleSharp configuration.
    /// </summary>
    public IConfiguration AngleSharpConfiguration { get; set; } = Configuration.Default.WithDefaultLoader().WithCss();

    /// <summary>
    /// Gets or sets the target frames per second.
    /// </summary>
    public int TargetFramesPerSecond { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether background processing is enabled.
    /// </summary>
    public bool EnableBackgroundProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to preload resources by default.
    /// </summary>
    public bool PreloadResources { get; set; } = true;
}