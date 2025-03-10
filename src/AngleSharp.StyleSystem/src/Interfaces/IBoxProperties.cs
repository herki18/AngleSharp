namespace AngleSharp.StyleSystem.Interfaces;

using AngleSharp.Css.Values;
using AngleSharp.StyleSystem.Models;

/// <summary>
/// Box-related computed properties.
/// </summary>
public interface IBoxProperties
{
    /// <summary>
    /// Gets the computed width.
    /// </summary>
    CssLengthValue Width { get; }

    /// <summary>
    /// Gets the computed height.
    /// </summary>
    CssLengthValue Height { get; }

    /// <summary>
    /// Gets the logical inline size (width or height depending on writing mode).
    /// </summary>
    CssLengthValue InlineSize { get; }

    /// <summary>
    /// Gets the logical block size (height or width depending on writing mode).
    /// </summary>
    CssLengthValue BlockSize { get; }

    /// <summary>
    /// Gets the margin edges.
    /// </summary>
    Edges Margin { get; }

    /// <summary>
    /// Gets the border edges.
    /// </summary>
    Edges Border { get; }

    /// <summary>
    /// Gets the padding edges.
    /// </summary>
    Edges Padding { get; }

    /// <summary>
    /// Gets the logical margin edges.
    /// </summary>
    LogicalEdges LogicalMargin { get; }

    /// <summary>
    /// Gets the logical border edges.
    /// </summary>
    LogicalEdges LogicalBorder { get; }

    /// <summary>
    /// Gets the logical padding edges.
    /// </summary>
    LogicalEdges LogicalPadding { get; }
}