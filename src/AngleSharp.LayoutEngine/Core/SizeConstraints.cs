namespace AngleSharp.LayoutEngine.Core;

using System;

#pragma warning disable CS8618, CS9264
/// <summary>
/// Contains size constraints for layout operations.
/// </summary>
public class SizeConstraints
{
    /// <summary>
    /// Gets or sets the available width.
    /// </summary>
    public float AvailableWidth { get; set; }

    /// <summary>
    /// Gets or sets the available height.
    /// </summary>
    public float AvailableHeight { get; set; }

    /// <summary>
    /// Gets or sets the minimum width.
    /// </summary>
    public float MinWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum width.
    /// </summary>
    public float MaxWidth { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// Gets or sets the minimum height.
    /// </summary>
    public float MinHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum height.
    /// </summary>
    public float MaxHeight { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// Applies constraints to a width value.
    /// </summary>
    public float ApplyWidthConstraints(float width)
    {
        return Math.Clamp(width, MinWidth, MaxWidth);
    }

    /// <summary>
    /// Applies constraints to a height value.
    /// </summary>
    public float ApplyHeightConstraints(float height)
    {
        return Math.Clamp(height, MinHeight, MaxHeight);
    }
}