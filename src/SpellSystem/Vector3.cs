namespace SpellSystem;

#pragma warning disable CS8600
public struct Vector3
{
    public float x, y, z;

    public Vector3(float x, float y, float z)
    {
        this.x = x; this.y = y; this.z = z;
    }

    public float SqrMagnitude()
    {
        return x * x + y * y + z * z;
    }

    public float Magnitude()
    {
        return (float)Math.Sqrt(SqrMagnitude());
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        return (a - b).Magnitude();
    }

    public override string ToString()
    {
        return $"({x:F1}, {y:F1}, {z:F1})";
    }

    // Operator overloads for easy vector math
    public static Vector3 operator +(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static Vector3 operator *(Vector3 a, float scalar)
    {
        return new Vector3(a.x * scalar, a.y * scalar, a.z * scalar);
    }

    public static Vector3 operator /(Vector3 a, float scalar)
    {
        return new Vector3(a.x / scalar, a.y / scalar, a.z / scalar);
    }

    /// <summary>
    /// Returns the normalized (unit vector) direction.
    /// </summary>
    public Vector3 Normalized()
    {
        float mag = Magnitude();
        if (mag == 0) return new Vector3(0, 0, 0);
        return this / mag;
    }

    public static Vector3 Cross(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
    }

    public static float Dot(Vector3 a, Vector3 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    // Common vectors
    public static Vector3 Zero => new Vector3(0, 0, 0);
    public static Vector3 One => new Vector3(1, 1, 1);
    public static Vector3 Forward => new Vector3(0, 0, 1);
    public static Vector3 Right => new Vector3(1, 0, 0);
    public static Vector3 Up => new Vector3(0, 1, 0);
}