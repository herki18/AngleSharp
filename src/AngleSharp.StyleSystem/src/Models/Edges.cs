namespace AngleSharp.StyleSystem.Models;

using AngleSharp.Css.Values;

/// <summary>
/// Represents edges (margin, border, padding).
/// </summary>
public readonly struct Edges
{
    /// <summary>
    /// Gets the top edge.
    /// </summary>
    public CssLengthValue Top { get; }

    /// <summary>
    /// Gets the right edge.
    /// </summary>
    public CssLengthValue Right { get; }

    /// <summary>
    /// Gets the bottom edge.
    /// </summary>
    public CssLengthValue Bottom { get; }

    /// <summary>
    /// Gets the left edge.
    /// </summary>
    public CssLengthValue Left { get; }

    /// <summary>
    /// Creates a new Edges instance.
    /// </summary>
    public Edges(CssLengthValue top, CssLengthValue right, CssLengthValue bottom, CssLengthValue left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>
    /// Gets all edges as an array in clockwise order (top, right, bottom, left).
    /// </summary>
    public CssLengthValue[] ToArray() => new[] { Top, Right, Bottom, Left };

    /// <summary>
    /// Creates uniform edges with the same value for all sides.
    /// </summary>
    public static Edges Uniform(CssLengthValue value) => new Edges(value, value, value, value);
}

