namespace LayoutEngine.Contracts.LayoutSystem;

/// <summary>
/// Represents the four edges (top, right, bottom, left) of a box,
/// typically used for margin, padding, or border widths in pixels.
/// </summary>
public struct BoxEdges
{
    /// <summary>Gets or sets the top edge value.</summary>
    public float Top { get; set; }

    /// <summary>Gets or sets the right edge value.</summary>
    public float Right { get; set; }

    /// <summary>Gets or sets the bottom edge value.</summary>
    public float Bottom { get; set; }

    /// <summary>Gets or sets the left edge value.</summary>
    public float Left { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoxEdges"/> struct
    /// with all edges set to the same value.
    /// </summary>
    /// <param name="all">The value for all edges.</param>
    public BoxEdges(float all)
    {
        Top = Right = Bottom = Left = all;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoxEdges"/> struct
    /// with specified values for each edge.
    /// </summary>
    /// <param name="top">The top edge value.</param>
    /// <param name="right">The right edge value.</param>
    /// <param name="bottom">The bottom edge value.</param>
    /// <param name="left">The left edge value.</param>
    public BoxEdges(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    // The Uniform static method can be kept if desired, but needs float parameter.
    // Alternatively, the constructor BoxEdges(float all) serves a similar purpose.
    /// <summary>
    /// Creates a BoxEdges instance with all edges set to the same value.
    /// </summary>
    /// <param name="value">The value for all edges.</param>
    /// <returns>A BoxEdges struct with uniform edge values.</returns>
    public static BoxEdges Uniform(float value) => new(value, value, value, value);
}