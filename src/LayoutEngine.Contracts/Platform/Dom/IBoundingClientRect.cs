namespace LayoutEngine.Contracts.Platform.Dom;

/// <summary>
/// Represents a bounding client rectangle.
/// </summary>
public interface IBoundingClientRect
{
    /// <summary>
    /// Gets the left position of the rectangle.
    /// </summary>
    double Left { get; }

    /// <summary>
    /// Gets the top position of the rectangle.
    /// </summary>
    double Top { get; }

    /// <summary>
    /// Gets the right position of the rectangle.
    /// </summary>
    double Right { get; }

    /// <summary>
    /// Gets the bottom position of the rectangle.
    /// </summary>
    double Bottom { get; }

    /// <summary>
    /// Gets the width of the rectangle.
    /// </summary>
    double Width { get; }

    /// <summary>
    /// Gets the height of the rectangle.
    /// </summary>
    double Height { get; }
}