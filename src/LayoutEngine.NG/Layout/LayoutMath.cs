namespace LayoutEngine.NG.Layout;

using System;
using LayoutEngine.NG.Layout.Fragments; // For BoxStrut used in PhysicalRect

// Originally from PhysicalOffset.cs
public struct PhysicalOffset
{
    public float Left { get; set; }
    public float Top { get; set; }

    public PhysicalOffset(float left, float top)
    {
        Left = left;
        Top = top;
    }

    public static PhysicalOffset Zero => new PhysicalOffset(0, 0);

    public static PhysicalOffset operator +(PhysicalOffset a, PhysicalOffset b)
    {
        return new PhysicalOffset(a.Left + b.Left, a.Top + b.Top);
    }

    public static PhysicalOffset operator -(PhysicalOffset a, PhysicalOffset b)
    {
        return new PhysicalOffset(a.Left - b.Left, a.Top - b.Top);
    }

    public static bool operator ==(PhysicalOffset a, PhysicalOffset b)
    {
        return a.Left == b.Left && a.Top == b.Top;
    }

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

// Originally from PhysicalSize.cs
public struct PhysicalSize
{
    public float Width { get; set; }
    public float Height { get; set; }

    public PhysicalSize(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public static PhysicalSize Zero => new PhysicalSize(0, 0);
    public bool IsEmpty => Width <= 0 || Height <= 0;
    public double Area => Width * Height;

    public static bool operator ==(PhysicalSize a, PhysicalSize b)
    {
        return a.Width == b.Width && a.Height == b.Height;
    }

    public static bool operator !=(PhysicalSize a, PhysicalSize b)
    {
        return !(a == b);
    }

    public PhysicalSize ExpandBy(float amount)
    {
        return new PhysicalSize(Width + amount * 2, Height + amount * 2);
    }

    public PhysicalSize ShrinkBy(float amount)
    {
        return new PhysicalSize(
            System.Math.Max(0, Width - amount * 2),
            System.Math.Max(0, Height - amount * 2)
        );
    }

    public PhysicalSize FitInto(PhysicalSize constraint)
    {
        return new PhysicalSize(
            System.Math.Min(Width, constraint.Width),
            System.Math.Min(Height, constraint.Height)
        );
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

// Originally from PhysicalBoxStrut.cs
public struct PhysicalBoxStrut
{
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }
    public float Left { get; }

    public PhysicalBoxStrut(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public float InlineStart => Left;
    public float InlineEnd => Right;
    public float BlockStart => Top;
    public float BlockEnd => Bottom;
    public float InlineSum => Left + Right;
    public float BlockSum => Top + Bottom;

    public static PhysicalBoxStrut Zero => new PhysicalBoxStrut(0, 0, 0, 0);
}

// Originally from Transform.cs
public class Transform
{
    private float[,] _matrix = new float[4, 4];

    public Transform()
    {
        MakeIdentity();
    }

    public void MakeIdentity()
    {
        _matrix =
            new float[,]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };
    }

    public void Translate(float x, float y, float z = 0)
    {
        _matrix[0, 3] += x;
        _matrix[1, 3] += y;
        _matrix[2, 3] += z;
    }

    public void Scale(float x, float y, float z = 1)
    {
        _matrix[0, 0] *= x;
        _matrix[1, 1] *= y;
        _matrix[2, 2] *= z;
    }

    public void Rotate(float degrees)
    {
        float radians = degrees * (float)Math.PI / 180;
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        var newMatrix =
            new float[,]
            {
                { cos, -sin, 0, 0 },
                { sin, cos, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };
        Multiply(newMatrix);
    }

    private void Multiply(float[,] other)
    {
        var result = new float[4, 4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    result[i, j] += _matrix[i, k] * other[k, j];
                }
            }
        }
        _matrix = result;
    }
}

public struct PhysicalRect
{
    public PhysicalOffset Offset { get; set; }
    public PhysicalSize Size { get; set; }

    public PhysicalRect(PhysicalOffset offset, PhysicalSize size)
    {
        Offset = offset;
        Size = size;
    }

    public PhysicalRect(float x, float y, float width, float height)
    {
        Offset = new PhysicalOffset(x, y);
        Size = new PhysicalSize(width, height);
    }

    public float X => Offset.Left;
    public float Y => Offset.Top;
    public float Width => Size.Width;
    public float Height => Size.Height;
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public void Contract(BoxStrut amount) // Assuming BoxStrut from Fragments namespace
    {
        Offset = new PhysicalOffset(
            Offset.Left + amount.Left,
            Offset.Top + amount.Top
        );
        Size = new PhysicalSize(
            Math.Max(0, Size.Width - amount.Left - amount.Right),
            Math.Max(0, Size.Height - amount.Top - amount.Bottom)
        );
    }

    public static PhysicalRect Empty => new PhysicalRect(0, 0, 0, 0);
}

// Originally from MinMaxSizes.cs
public struct MinMaxSizes
{
    public float MinSize { get; set; }
    public float MaxSize { get; set; }

    public MinMaxSizes(float minSize, float maxSize)
    {
        MinSize = minSize;
        MaxSize = maxSize;
    }

    public float ClampSizeToMinAndMax(float size)
    {
        return Math.Max(MinSize, Math.Min(MaxSize, size));
    }
}

public struct MinMaxSizesResult
{
    public MinMaxSizes Sizes { get; }
    public bool DependsOnBlockConstraints { get; }
    public bool AppliedAspectRatio { get; }

    public MinMaxSizesResult(
        MinMaxSizes sizes,
        bool dependsOnBlockConstraints,
        bool appliedAspectRatio = false
    )
    {
        Sizes = sizes;
        DependsOnBlockConstraints = dependsOnBlockConstraints;
        AppliedAspectRatio = appliedAspectRatio;
    }
}

public struct MinMaxSizesFloatInput
{
    public float FloatLeftInlineSize { get; set; }
    public float FloatRightInlineSize { get; set; }
}

public enum SizeType
{
    MinContent,
    MaxContent,
    Content
}