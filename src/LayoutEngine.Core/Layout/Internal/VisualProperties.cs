namespace LayoutEngine.Core.Layout.Internal;

using LayoutEngine.Core.Layout.Public;

public class VisualProperties : IVisualProperties
{
    public string BackgroundColor { get; set; } = "transparent";
    public float BorderTopWidth { get; set; } = 0;
    public float BorderRightWidth { get; set; } = 0;
    public float BorderBottomWidth { get; set; } = 0;
    public float BorderLeftWidth { get; set; } = 0;
    public string BorderTopColor { get; set; } = "black";
    public string BorderRightColor { get; set; } = "black";
    public string BorderBottomColor { get; set; } = "black";
    public string BorderLeftColor { get; set; } = "black";
    public string Color { get; set; } = "black";
    public string FontFamily { get; set; } = "sans-serif";
    public float FontSize { get; set; } = 16;
    public string FontWeight { get; set; } = "normal";

    public int ZIndex { get; set; } = 0;
}