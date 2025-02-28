namespace AngleSharp.LayoutEngine.Box;

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
public class Rect
{
    /// <summary>
    /// X coordinate of the rectangle.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y coordinate of the rectangle.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Height of the rectangle.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Gets the left edge of the rectangle.
    /// </summary>
    public float Left => X;

    /// <summary>
    /// Gets the right edge of the rectangle.
    /// </summary>
    public float Right => X + Width;

    /// <summary>
    /// Gets the top edge of the rectangle.
    /// </summary>
    public float Top => Y;

    /// <summary>
    /// Gets the bottom edge of the rectangle.
    /// </summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// Creates an empty rectangle.
    /// </summary>
    public Rect()
    {
    }

    /// <summary>
    /// Creates a rectangle with the specified position and size.
    /// </summary>
    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Determines if this rectangle contains a point.
    /// </summary>
    public bool Contains(float x, float y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

    /// <summary>
    /// Determines if this rectangle intersects with another rectangle.
    /// </summary>
    public bool IntersectsWith(Rect other)
    {
        return !(Right < other.Left || Left > other.Right ||
                 Bottom < other.Top || Top > other.Bottom);
    }

    /// <summary>
    /// Creates a copy of this rectangle.
    /// </summary>
    public Rect Clone()
    {
        return new Rect(X, Y, Width, Height);
    }

    /// <summary>
    /// Returns a string representation of this rectangle.
    /// </summary>
    public override string ToString()
    {
        return $"Rect({X:F1},{Y:F1},{Width:F1},{Height:F1})";
    }
}