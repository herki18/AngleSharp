namespace LayoutEngine.Contracts.LayoutSystem;

using System.Collections.Generic;
using AngleSharp.Dom;
using Platform.Dom;
using StyleSystem;

/// <summary>
/// Represents a layout box in the layout tree.
/// </summary>
public interface ILayoutBox
{
    /// <summary>
    /// Gets the element this layout box represents.
    /// </summary>
    IElement? Element { get; }

    /// <summary>
    /// Gets the box type.
    /// </summary>
    BoxType BoxType { get; }

    /// <summary>
    /// Gets the computed style for this box.
    /// </summary>
    IComputedStyle? Style { get; }

    /// <summary>
    /// Gets the parent layout box.
    /// </summary>
    ILayoutBox? Parent { get; }

    /// <summary>
    /// Gets the child layout boxes.
    /// </summary>
    IReadOnlyList<ILayoutBox> Children { get; }

    /// <summary>
    /// Gets the x-coordinate relative to the parent.
    /// </summary>
    float X { get; }

    /// <summary>
    /// Gets the y-coordinate relative to the parent.
    /// </summary>
    float Y { get; }

    /// <summary>
    /// Gets the width of the content box.
    /// </summary>
    float Width { get; }

    /// <summary>
    /// Gets the height of the content box.
    /// </summary>
    float Height { get; }

    /// <summary>
    /// Gets the margin box.
    /// </summary>
    BoxEdges Margin { get; }

    /// <summary>
    /// Gets the border box.
    /// </summary>
    BoxEdges Border { get; }

    /// <summary>
    /// Gets the padding box.
    /// </summary>
    BoxEdges Padding { get; }

    /// <summary>
    /// Gets the content rect in local coordinates.
    /// </summary>
    Rect ContentRect { get; }

    /// <summary>
    /// Gets the absolute coordinates of this box.
    /// </summary>
    /// <returns>The absolute coordinates.</returns>
    Rect GetAbsoluteRect();

    /// <summary>
    /// Checks if a point is inside this layout box.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>True if the point is inside.</returns>
    bool ContainsPoint(float x, float y);
}