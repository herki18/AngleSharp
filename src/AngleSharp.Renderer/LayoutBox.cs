namespace AngleSharp.Renderer;

#pragma warning disable CS1591
/// <summary>
/// The final (used) layout geometry for an element or text node.
/// </summary>
public class LayoutBox
{
    // Global position
    public float X { get; set; }
    public float Y { get; set; }

    // Size of the entire box (including padding and borders)
    public float BoxWidth { get; set; }
    public float BoxHeight { get; set; }

    // Relative position (e.g., offset within parent's coordinate system)
    public float RelativeX { get; set; }
    public float RelativeY { get; set; }

    // Box model metrics
    public float MarginTop { get; set; }
    public float MarginRight { get; set; }
    public float MarginBottom { get; set; }
    public float MarginLeft { get; set; }

    public float BorderTop { get; set; }
    public float BorderRight { get; set; }
    public float BorderBottom { get; set; }
    public float BorderLeft { get; set; }

    public float PaddingTop { get; set; }
    public float PaddingRight { get; set; }
    public float PaddingBottom { get; set; }
    public float PaddingLeft { get; set; }

    // Derived measurements
    public float ContentWidth => BoxWidth - (PaddingLeft + PaddingRight + BorderLeft + BorderRight);
    public float ContentHeight => BoxHeight - (PaddingTop + PaddingBottom + BorderTop + BorderBottom);

    public bool IsInMarginToCollapsedChain { get; set; }

    public LayoutBox(
        float x, float y,
        float width, float height)
    {
        X = x;
        Y = y;
        BoxWidth = width;
        BoxHeight = height;
    }

    public override string ToString()
    {
        return $"LayoutBox(Global: ({X}, {Y}), Size: ({BoxWidth} x {BoxHeight}), " +
               $"Margins: (T:{MarginTop}, R:{MarginRight}, B:{MarginBottom}, L:{MarginLeft}))";
    }
}