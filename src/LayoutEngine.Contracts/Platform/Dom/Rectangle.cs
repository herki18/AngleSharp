namespace LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
public struct Rectangle
{
    /// <summary>
    /// Gets the x-coordinate of the rectangle.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the y-coordinate of the rectangle.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Gets the width of the rectangle.
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// Gets the height of the rectangle.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> struct.
    /// </summary>
    /// <param name="x">The x-coordinate of the rectangle.</param>
    /// <param name="y">The y-coordinate of the rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public Rectangle(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}