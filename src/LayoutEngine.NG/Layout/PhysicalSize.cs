namespace LayoutEngine.NG.Layout;

using System;

/// <summary>
/// Represents a physical size in the layout coordinate system.
/// In LayoutNG, PhysicalSize represents device-independent pixel dimensions.
/// This is used for sizing fragments and layout objects.
/// </summary>
public struct PhysicalSize
{
    /// <summary>
    /// The width dimension.
    /// In LayoutNG, this is typically named 'width' or accessed via Width().
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// The height dimension.
    /// In LayoutNG, this is typically named 'height' or accessed via Height().
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Creates a new PhysicalSize with the specified dimensions.
    /// </summary>
    public PhysicalSize(float width, float height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Returns a zero size.
    /// In LayoutNG, this is a common helper for initialization.
    /// </summary>
    public static PhysicalSize Zero => new PhysicalSize(0, 0);

    /// <summary>
    /// Checks if this size is empty (zero width or height).
    /// In LayoutNG, this is IsEmpty() and used to skip certain operations.
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Gets the area (width * height).
    /// In LayoutNG, this might be used for paint optimization decisions.
    /// </summary>
    public float Area => Width * Height;

    /// <summary>
    /// Checks equality between two sizes.
    /// </summary>
    public static bool operator ==(PhysicalSize a, PhysicalSize b)
    {
        return a.Width == b.Width && a.Height == b.Height;
    }

    /// <summary>
    /// Checks inequality between two sizes.
    /// </summary>
    public static bool operator !=(PhysicalSize a, PhysicalSize b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Expands this size by the given amount on all sides.
    /// In LayoutNG, this might be named Expand() or ExpandedTo().
    /// </summary>
    public PhysicalSize ExpandBy(float amount)
    {
        return new PhysicalSize(Width + amount * 2, Height + amount * 2);
    }

    /// <summary>
    /// Shrinks this size by the given amount on all sides.
    /// In LayoutNG, this might be named Shrink() or ShrunkBy().
    /// </summary>
    public PhysicalSize ShrinkBy(float amount)
    {
        return new PhysicalSize(
            System.Math.Max(0, Width - amount * 2),
            System.Math.Max(0, Height - amount * 2));
    }

    /// <summary>
    /// Returns a size that fits within the given constraint.
    /// In LayoutNG, this is used for constraint calculations.
    /// </summary>
    public PhysicalSize FitInto(PhysicalSize constraint)
    {
        return new PhysicalSize(
            System.Math.Min(Width, constraint.Width),
            System.Math.Min(Height, constraint.Height));
    }

    public override bool Equals(object? obj)
    {
        return obj is PhysicalSize size && this == size;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public override string ToString()
    {
        return $"PhysicalSize({Width}x{Height})";
    }
}