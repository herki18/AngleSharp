namespace AngleSharp.LayoutEngine.Tests.Helpers;

using Css;

/// <summary>
/// Test implementation of IRenderDevice that provides consistent font metrics for tests.
/// </summary>
public class TestRenderDevice : IRenderDevice
{
    private int _deviceWidth = 1024;
    private int _deviceHeight = 768;
    private int _resolution = 96;

    /// <summary>
    /// Creates a new TestRenderDevice with the specified base font size.
    /// </summary>
    /// <param name="baseFontSize">The base font size in pixels (default is 16px).</param>
    public TestRenderDevice(double baseFontSize = 16.0)
    {
        FontSize = baseFontSize;
    }

    /// <summary>
    /// Gets the render width.
    /// </summary>
    public double RenderWidth => 1024.0;

    /// <summary>
    /// Gets the render height.
    /// </summary>
    public double RenderHeight => 768.0;

    /// <summary>
    /// Gets the base font size in pixels.
    /// </summary>
    public double FontSize { get; }

    /// <summary>
    /// Gets the device height as an integer.
    /// </summary>
    int IRenderDevice.DeviceHeight => _deviceHeight;

    /// <summary>
    /// Gets the resolution as an integer.
    /// </summary>
    int IRenderDevice.Resolution => _resolution;

    /// <summary>
    /// Gets the frequency of the device.
    /// </summary>
    public int Frequency => 60;

    /// <summary>
    /// Gets the device fixed resolution.
    /// </summary>
    public double Resolution => 96.0;

    /// <summary>
    /// Gets the viewport width.
    /// </summary>
    public int ViewPortWidth => 1024;

    /// <summary>
    /// Gets the viewport height.
    /// </summary>
    public int ViewPortHeight => 768;

    /// <summary>
    /// Gets a value determining if the display is interlaced.
    /// </summary>
    public bool IsInterlaced => false;

    /// <summary>
    /// Gets a value determining if scripting is enabled.
    /// </summary>
    public bool IsScripting => true;

    /// <summary>
    /// Gets a value determining if the device is grid-based.
    /// </summary>
    public bool IsGrid => false;

    /// <summary>
    /// Gets the device width as an integer.
    /// </summary>
    int IRenderDevice.DeviceWidth => _deviceWidth;

    /// <summary>
    /// Gets the device width.
    /// </summary>
    public double DeviceWidth => 1024.0;

    /// <summary>
    /// Gets the device height.
    /// </summary>
    public double DeviceHeight => 768.0;

    /// <summary>
    /// Gets a value determining if the device is mobile.
    /// </summary>
    public bool IsMobile => false;

    /// <summary>
    /// Gets a value determining if the device is in portrait orientation.
    /// </summary>
    public bool IsPortrait => false;

    /// <summary>
    /// Gets a value determining if the device is in landscape orientation.
    /// </summary>
    public bool IsLandscape => !IsPortrait;

    /// <summary>
    /// Gets a value determining the color bits in monochrome displays.
    /// </summary>
    public int MonochromeBits => 0;

    /// <summary>
    /// Gets the device category.
    /// </summary>
    public DeviceCategory Category => DeviceCategory.Screen;

    public void SetViewport(int width, int height)
    {
        _deviceWidth = width;
        _deviceHeight = height;
    }

    /// <summary>
    /// Gets a value determining the number of colors.
    /// </summary>
    public int ColorBits => 24;
}

/// <summary>
/// Extension methods for configuring AngleSharp with a test render device.
/// </summary>
public static class TestRenderDeviceExtensions
{
    /// <summary>
    /// Adds a test render device to the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to extend.</param>
    /// <param name="baseFontSize">The base font size to use (default is 16px).</param>
    /// <returns>The updated configuration.</returns>
    public static IConfiguration WithTestRenderDevice(this IConfiguration configuration, double baseFontSize = 16.0)
    {
        var device = new TestRenderDevice(baseFontSize);
        return configuration.With(device);
    }
}