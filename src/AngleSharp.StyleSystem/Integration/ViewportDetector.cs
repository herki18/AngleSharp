namespace AngleSharp.StyleSystem.Integration;

using System;
using AngleSharp.Css;
using AngleSharp.Dom;

/// <summary>
/// Helper class to detect if elements are within or near the viewport.
/// </summary>
public class ViewportDetector
{
    private readonly IRenderDevice _renderDevice;
    private readonly int _nearViewportMargin;

    /// <summary>
    /// Creates a new viewport detector.
    /// </summary>
    /// <param name="renderDevice">The render device providing viewport information.</param>
    /// <param name="nearViewportMargin">The margin in pixels to consider "near" the viewport.</param>
    public ViewportDetector(IRenderDevice renderDevice, int nearViewportMargin = 200)
    {
        _renderDevice = renderDevice ?? throw new ArgumentNullException(nameof(renderDevice));
        _nearViewportMargin = nearViewportMargin;
    }

    /// <summary>
    /// Determines if an element is within the viewport.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>True if the element is in the viewport; otherwise, false.</returns>
    public bool IsInViewport(IElement element)
    {
        if (element == null)
            return false;

        // A more sophisticated implementation would calculate the element's bounds
        // and check if they intersect with the viewport.
        // For now, we use a simple heuristic based on the element's parent chain length
        // to prioritize elements closer to the root.

        // Simplified check: document element and its immediate children are considered in viewport
        var parent = element.ParentElement;
        if (element == element.OwnerDocument?.DocumentElement ||
            (parent != null && parent == element.OwnerDocument?.DocumentElement))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if an element is near the viewport.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>True if the element is near the viewport; otherwise, false.</returns>
    public bool IsNearViewport(IElement element)
    {
        if (element == null)
            return false;

        // Simplified check: elements up to 3 levels deep are considered near viewport
        int depth = GetElementDepth(element);
        return depth <= 3;
    }

    private int GetElementDepth(IElement element)
    {
        int depth = 0;
        var current = element;

        while (current != null && current != current.OwnerDocument?.DocumentElement)
        {
            depth++;
            current = current.ParentElement;
        }

        return depth;
    }
}