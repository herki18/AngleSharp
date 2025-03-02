namespace AngleSharp.Css;

using System;

public interface IRenderDevice : IRenderDimensions
{
    Int32 ViewPortWidth { get; }
    Int32 ViewPortHeight { get; }
    Boolean IsInterlaced { get; }
    Boolean IsScripting { get; }
    Boolean IsGrid { get; }
    Int32 DeviceWidth { get; }
    Int32 DeviceHeight { get; }
    Int32 Resolution { get; }
    Int32 Frequency { get; }
    Int32 ColorBits { get; }
    Int32 MonochromeBits { get; }
    DeviceCategory Category { get; }
    void SetViewport(Int32 width, Int32 height);
}