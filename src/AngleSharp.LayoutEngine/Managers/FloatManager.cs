#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8600, CS8602, CS8603, CS8625
namespace AngleSharp.LayoutEngine.Managers;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.FormattingContexts.Enums;

/// <summary>
/// Manages floating elements within the layout system.
/// Tracks the position of floats and handles the interaction between floated elements and normal flow.
/// </summary>
public class FloatManager
{
    // Tracks floats by Y position for efficient lookup
    private readonly SortedDictionary<float, List<FloatInfo>> _floatsByY = new SortedDictionary<float, List<FloatInfo>>();

    // Cache of float info by node for quick access
    private readonly Dictionary<LayoutNode, FloatInfo> _floatsByNode = new Dictionary<LayoutNode, FloatInfo>();

    /// <summary>
    /// Adds a floating element to be tracked by the float manager.
    /// </summary>
    /// <param name="node">The floating node.</param>
    /// <param name="context">The layout context.</param>
    public void AddFloat(LayoutNode node, LayoutContext context)
    {
        if (node.Float == FloatType.None)
            return;

        var box = node.Box;
        if (box == null)
            return;

        // Create float info
        var floatInfo = new FloatInfo
        {
            Node = node,
            Type = node.Float,
            Top = box.Y,
            Left = box.X,
            Right = box.X + box.Width,
            Bottom = box.Y + box.Height,
            Width = box.Width,
            Height = box.Height
        };

        // Add to tracking collections
        _floatsByNode[node] = floatInfo;

        if (!_floatsByY.TryGetValue(floatInfo.Top, out var floatsAtY))
        {
            floatsAtY = new List<FloatInfo>();
            _floatsByY[floatInfo.Top] = floatsAtY;
        }

        floatsAtY.Add(floatInfo);
    }

    /// <summary>
    /// Removes a floating element from tracking.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    public void RemoveFloat(LayoutNode node)
    {
        if (!_floatsByNode.TryGetValue(node, out var floatInfo))
            return;

        // Remove from Y tracking
        if (_floatsByY.TryGetValue(floatInfo.Top, out var floatsAtY))
        {
            floatsAtY.Remove(floatInfo);
            if (!floatsAtY.Any())
            {
                _floatsByY.Remove(floatInfo.Top);
            }
        }

        // Remove from node tracking
        _floatsByNode.Remove(node);
    }

    /// <summary>
    /// Clears all tracked floating elements.
    /// </summary>
    public void ClearFloats()
    {
        _floatsByY.Clear();
        _floatsByNode.Clear();
    }

    /// <summary>
    /// Gets the left edge position after accounting for left floats.
    /// </summary>
    /// <param name="y">The Y coordinate to check.</param>
    /// <param name="height">The height of the content being placed.</param>
    /// <param name="containerLeft">The left edge of the containing block.</param>
    /// <param name="containerWidth">The width of the containing block.</param>
    /// <returns>The adjusted left edge position.</returns>
    public float GetLeftEdge(float y, float height, float containerLeft, float containerWidth)
    {
        float leftEdge = containerLeft;

        // Find all floats that intersect with the horizontal line at y
        var intersectingFloats = GetFloatsIntersectingVerticalRange(y, y + height);

        // Consider only left floats
        var leftFloats = intersectingFloats.Where(f => f.Type == FloatType.Left).ToList();

        // Find the rightmost edge of left floats
        foreach (var leftFloat in leftFloats)
        {
            leftEdge = Math.Max(leftEdge, leftFloat.Right);
        }

        return leftEdge;
    }

    /// <summary>
    /// Gets the right edge position after accounting for right floats.
    /// </summary>
    /// <param name="y">The Y coordinate to check.</param>
    /// <param name="height">The height of the content being placed.</param>
    /// <param name="containerLeft">The left edge of the containing block.</param>
    /// <param name="containerWidth">The width of the containing block.</param>
    /// <returns>The adjusted right edge position.</returns>
    public float GetRightEdge(float y, float height, float containerLeft, float containerWidth)
    {
        float rightEdge = containerLeft + containerWidth;

        // Find all floats that intersect with the horizontal line at y
        var intersectingFloats = GetFloatsIntersectingVerticalRange(y, y + height);

        // Consider only right floats
        var rightFloats = intersectingFloats.Where(f => f.Type == FloatType.Right).ToList();

        // Find the leftmost edge of right floats
        foreach (var rightFloat in rightFloats)
        {
            rightEdge = Math.Min(rightEdge, rightFloat.Left);
        }

        return rightEdge;
    }

    /// <summary>
    /// Gets the available width at a specific vertical position after accounting for floats.
    /// </summary>
    /// <param name="y">The Y coordinate to check.</param>
    /// <param name="height">The height of the content being placed.</param>
    /// <param name="containerLeft">The left edge of the containing block.</param>
    /// <param name="containerWidth">The width of the containing block.</param>
    /// <returns>The available width and left position as a tuple.</returns>
    public (float Left, float Width) GetAvailableWidth(float y, float height, float containerLeft, float containerWidth)
    {
        float leftEdge = GetLeftEdge(y, height, containerLeft, containerWidth);
        float rightEdge = GetRightEdge(y, height, containerLeft, containerWidth);
        float availableWidth = Math.Max(0, rightEdge - leftEdge);

        return (leftEdge, availableWidth);
    }

    /// <summary>
    /// Finds the next Y position where the available width changes due to floats.
    /// </summary>
    /// <param name="startY">The starting Y position.</param>
    /// <param name="maxY">The maximum Y position to consider.</param>
    /// <returns>The next Y position where floats affect layout.</returns>
    public float GetNextFloatChangeY(float startY, float maxY)
    {
        // Find the next float boundary after startY
        foreach (var y in _floatsByY.Keys.Where(k => k > startY).OrderBy(k => k))
        {
            if (y <= maxY)
                return y;
        }

        // Find the next float bottom after startY
        var floatBottoms = _floatsByNode.Values
            .Select(f => f.Bottom)
            .Where(b => b > startY && b <= maxY)
            .OrderBy(b => b);

        if (floatBottoms.Any())
            return floatBottoms.First();

        return maxY;
    }

    /// <summary>
    /// Gets all floats that intersect with a specified vertical range.
    /// </summary>
    /// <param name="top">The top of the range.</param>
    /// <param name="bottom">The bottom of the range.</param>
    /// <returns>A list of floats that intersect with the range.</returns>
    private List<FloatInfo> GetFloatsIntersectingVerticalRange(float top, float bottom)
    {
        var result = new List<FloatInfo>();

        foreach (var floatInfo in _floatsByNode.Values)
        {
            // Check if the float intersects with the vertical range
            if (floatInfo.Bottom > top && floatInfo.Top < bottom)
            {
                result.Add(floatInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a box at a specified position would collide with any floats.
    /// </summary>
    /// <param name="left">The left edge of the box.</param>
    /// <param name="top">The top edge of the box.</param>
    /// <param name="width">The width of the box.</param>
    /// <param name="height">The height of the box.</param>
    /// <returns>True if the box would collide with floats, false otherwise.</returns>
    public bool CheckForCollision(float left, float top, float width, float height)
    {
        float right = left + width;
        float bottom = top + height;

        // Find floats that intersect with the vertical range
        var intersectingFloats = GetFloatsIntersectingVerticalRange(top, bottom);

        // Check for horizontal overlap
        foreach (var floatInfo in intersectingFloats)
        {
            if (right > floatInfo.Left && left < floatInfo.Right)
            {
                return true; // Collision detected
            }
        }

        return false; // No collision
    }

    /// <summary>
    /// Updates the position of a floating element.
    /// </summary>
    /// <param name="node">The node to update.</param>
    public void UpdateFloat(LayoutNode node)
    {
        // First remove the old float information
        RemoveFloat(node);

        // Then add the updated float
        AddFloat(node, null);
    }
}

/// <summary>
/// Holds information about a floating element.
/// </summary>
public class FloatInfo
{
    /// <summary>
    /// The floating layout node.
    /// </summary>
    public LayoutNode Node { get; set; }

    /// <summary>
    /// The type of float (left or right).
    /// </summary>
    public FloatType Type { get; set; }

    /// <summary>
    /// The top edge of the float.
    /// </summary>
    public float Top { get; set; }

    /// <summary>
    /// The left edge of the float.
    /// </summary>
    public float Left { get; set; }

    /// <summary>
    /// The right edge of the float.
    /// </summary>
    public float Right { get; set; }

    /// <summary>
    /// The bottom edge of the float.
    /// </summary>
    public float Bottom { get; set; }

    /// <summary>
    /// The width of the float.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// The height of the float.
    /// </summary>
    public float Height { get; set; }
}