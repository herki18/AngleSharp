namespace LayoutEngine.NG.Layout;

using System;

/// <summary>
/// Represents a physical offset in the layout coordinate system.
/// In LayoutNG, PhysicalOffset represents device-independent pixel positions.
/// This is used for positioning fragments and layout objects.
/// </summary>
public struct PhysicalOffset
{
    /// <summary>
    /// The horizontal offset from the origin.
    /// In LayoutNG, this is typically named 'left' or accessed via Left().
    /// </summary>
    public float Left { get; set; }

    /// <summary>
    /// The vertical offset from the origin.
    /// In LayoutNG, this is typically named 'top' or accessed via Top().
    /// </summary>
    public float Top { get; set; }

    /// <summary>
    /// Creates a new PhysicalOffset with the specified coordinates.
    /// </summary>
    public PhysicalOffset(float left, float top)
    {
        Left = left;
        Top = top;
    }

    /// <summary>
    /// Returns a zero offset.
    /// In LayoutNG, this is a common helper for initialization.
    /// </summary>
    public static PhysicalOffset Zero => new PhysicalOffset(0, 0);

    /// <summary>
    /// Adds two offsets together.
    /// In LayoutNG, this is used for accumulating positions during layout.
    /// </summary>
    public static PhysicalOffset operator +(PhysicalOffset a, PhysicalOffset b)
    {
        return new PhysicalOffset(a.Left + b.Left, a.Top + b.Top);
    }

    /// <summary>
    /// Subtracts one offset from another.
    /// In LayoutNG, this is used for calculating relative positions.
    /// </summary>
    public static PhysicalOffset operator -(PhysicalOffset a, PhysicalOffset b)
    {
        return new PhysicalOffset(a.Left - b.Left, a.Top - b.Top);
    }

    /// <summary>
    /// Checks equality between two offsets.
    /// </summary>
    public static bool operator ==(PhysicalOffset a, PhysicalOffset b)
    {
        return a.Left == b.Left && a.Top == b.Top;
    }

    /// <summary>
    /// Checks inequality between two offsets.
    /// </summary>
    public static bool operator !=(PhysicalOffset a, PhysicalOffset b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        return obj is PhysicalOffset offset && this == offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Top);
    }

    public override string ToString()
    {
        return $"PhysicalOffset({Left}, {Top})";
    }
}