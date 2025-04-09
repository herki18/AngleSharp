namespace AngleSharp.StyleSystem.Models;

using System;

/// <summary>
/// Represents edges (margin, border, padding).
/// </summary>
public readonly struct Edges
{
    /// <summary>
    /// Gets the top edge.
    /// </summary>
    public Single Top { get; }

    /// <summary>
    /// Gets the right edge.
    /// </summary>
    public Single Right { get; }

    /// <summary>
    /// Gets the bottom edge.
    /// </summary>
    public Single Bottom { get; }

    /// <summary>
    /// Gets the left edge.
    /// </summary>
    public Single Left { get; }
}

