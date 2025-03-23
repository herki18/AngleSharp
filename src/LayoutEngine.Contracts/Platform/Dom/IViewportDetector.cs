namespace LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Tracks viewport dimensions and changes.
/// </summary>
public interface IViewportDetector
{
    /// <summary>
    /// Gets the current viewport size.
    /// </summary>
    Size ViewportSize { get; }

    /// <summary>
    /// Gets the current device pixel ratio.
    /// </summary>
    double DevicePixelRatio { get; }

    /// <summary>
    /// Starts tracking viewport changes.
    /// </summary>
    void StartTracking();

    /// <summary>
    /// Stops tracking viewport changes.
    /// </summary>
    void StopTracking();

    /// <summary>
    /// Forces a check for viewport changes.
    /// </summary>
    /// <returns>True if the viewport changed, otherwise false.</returns>
    bool CheckForChanges();
}