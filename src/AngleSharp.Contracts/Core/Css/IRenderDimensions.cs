namespace AngleSharp.Css;

using System;

public interface IRenderDimensions
{
    Double RenderWidth { get; }
    Double RenderHeight { get; }
    Double FontSize { get; }
}