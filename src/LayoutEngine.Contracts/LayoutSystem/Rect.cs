namespace LayoutEngine.Contracts.LayoutSystem;

using LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
public struct Rect
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(float px, float py)
    {
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }
}