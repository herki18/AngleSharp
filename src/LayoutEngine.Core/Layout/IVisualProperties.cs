namespace LayoutEngine.Core.Layout;

public interface IVisualProperties
{
    // Background color, texture, etc.
    string BackgroundColor { get; }

    // Border properties
    float BorderTopWidth { get; }
    float BorderRightWidth { get; }
    float BorderBottomWidth { get; }
    float BorderLeftWidth { get; }
    string BorderTopColor { get; }
    string BorderRightColor { get; }
    string BorderBottomColor { get; }
    string BorderLeftColor { get; }

    // Text properties
    string Color { get; }
    string FontFamily { get; }
    float FontSize { get; }
    string FontWeight { get; }


    int ZIndex { get; }
}