namespace LayoutEngine.NG.Renderer.Platform.Text;

/// <summary>
/// Defines CSS writing modes.
/// These values are named to match the CSS keywords they correspond to.
/// Corresponds to Blink's enum class WritingMode.
/// </summary>
public enum WritingMode : byte // C++ uint8_t maps to C# byte
{
    HorizontalTb = 0, // horizontal-tb
    VerticalRl = 1,   // vertical-rl
    VerticalLr = 2,   // vertical-lr
    SidewaysRl = 3,   // sideways-rl (LayoutNG only in C++)
    SidewaysLr = 4,   // sideways-lr (LayoutNG only in C++)

    // kMaxWritingMode is a C++ specific detail for array sizing, not directly needed for C# enum.
    // However, it's useful to know the range.
}