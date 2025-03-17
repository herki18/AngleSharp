namespace LayoutEngine.Contracts.Platform.Viewport;

/// <summary>
/// Represents viewport dimensions.
/// </summary>
public readonly struct ViewportDimensions
{
    /// <summary>
    /// Gets the width of the viewport in CSS pixels.
    /// </summary>
    public readonly int Width { get; }

    /// <summary>
    /// Gets the height of the viewport in CSS pixels.
    /// </summary>
    public readonly int Height { get; }

    /// <summary>
    /// Gets the device pixel ratio.
    /// </summary>
    public readonly float DevicePixelRatio { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewportDimensions"/> struct.
    /// </summary>
    /// <param name="width">The width in CSS pixels.</param>
    /// <param name="height">The height in CSS pixels.</param>
    /// <param name="devicePixelRatio">The device pixel ratio.</param>
    public ViewportDimensions(int width, int height, float devicePixelRatio)
    {
        Width = width;
        Height = height;
        DevicePixelRatio = devicePixelRatio;
    }

    /// <summary>
    /// Gets the width in device pixels.
    /// </summary>
    public int DeviceWidth => (int)(Width * DevicePixelRatio);

    /// <summary>
    /// Gets the height in device pixels.
    /// </summary>
    public int DeviceHeight => (int)(Height * DevicePixelRatio);
}