namespace AngleSharp.LayoutEngine.Tests.LayoutEngine;

using AngleSharp.Css;

public class MockRenderDevice : IRenderDevice
{
    public int ViewPortWidth { get; set; } = 1024;
    public int ViewPortHeight { get; set; } = 768;
    public bool IsInterlaced { get; set; } = false;
    public bool IsScripting { get; set; } = true;
    public bool IsGrid { get; set; } = false;
    public int DeviceWidth { get; set; } = 1366;
    public int DeviceHeight { get; set; } = 768;
    public int Resolution { get; set; } = 96;
    public int Frequency { get; set; } = 60;
    public int ColorBits { get; set; } = 24;
    public int MonochromeBits { get; set; } = 0;
    public DeviceCategory Category { get; set; } = DeviceCategory.Screen;
    public double RenderWidth { get; set; } = 1024;
    public double RenderHeight { get; set; } = 768;
    public double FontSize { get; set; } = 16;

    public void SetViewport(int width, int height)
    {
        ViewPortWidth = width;
        ViewPortHeight = height;
    }
}