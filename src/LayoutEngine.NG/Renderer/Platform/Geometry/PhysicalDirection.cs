namespace LayoutEngine.NG.Renderer.Platform.Geometry;

/// <summary>
/// Represents physical directions (up, right, down, left).
/// Corresponds to Blink's enum class PhysicalDirection.
/// </summary>
public enum PhysicalDirection : byte // C++ uint8_t maps to C# byte
{
    Up = 0,    // Corresponds to C++ kUp
    Right,     // Corresponds to C++ kRight
    Down,      // Corresponds to C++ kDown
    Left       // Corresponds to C++ kLeft
}