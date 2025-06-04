namespace LayoutEngine.NG.Layout.Geometry;

using System;
using System.Globalization;
using Process;

/// <summary>
/// LogicalOffset is the position of a rect (typically a fragment) relative to
/// its parent rect in the logical coordinate system.
/// For more information about physical and logical coordinate systems, see:
/// https://chromium.googlesource.com/chromium/src/+/main/third_party/blink/renderer/core/layout/README.md#coordinate-spaces
/// </summary>
public struct LogicalOffset : IEquatable<LogicalOffset>
{
    /// <summary>
    /// Represents an offset of (0, 0).
    /// </summary>
    public static readonly LogicalOffset Zero = new LogicalOffset(0f, 0f);

    /// <summary>
    /// The offset in the inline direction.
    /// Corresponds to C++ LayoutUnit.
    /// </summary>
    public float InlineOffset; // Made public field to match C++ and allow direct modification

    /// <summary>
    /// The offset in the block direction.
    /// Corresponds to C++ LayoutUnit.
    /// </summary>
    public float BlockOffset;  // Made public field to match C++ and allow direct modification

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalOffset"/> struct.
    /// </summary>
    /// <param name="inlineOffset">The offset in the inline direction.</param>
    /// <param name="blockOffset">The offset in the block direction.</param>
    public LogicalOffset(float inlineOffset, float blockOffset)
    {
        InlineOffset = inlineOffset;
        BlockOffset = blockOffset;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalOffset"/> struct.
    /// For testing only.
    /// </summary>
    /// <param name="inlineOffset">The offset in the inline direction (as int).</param>
    /// <param name="blockOffset">The offset in the block direction (as int).</param>
    public LogicalOffset(int inlineOffset, int blockOffset)
    {
        InlineOffset = inlineOffset;
        BlockOffset = blockOffset;
    }

    // C++: PhysicalOffset ConvertToPhysical(WritingDirectionMode writing_direction,
    //                                   PhysicalSize outer_size,
    //                                   PhysicalSize inner_size) const;
    /// <summary>
    /// Converts a logical offset to a physical offset.
    /// See: https://drafts.csswg.org/css-writing-modes-3/#logical-to-physical
    /// PhysicalOffset will be the physical top left point of the rectangle
    /// described by offset + inner_size. Setting inner_size to 0,0 will return
    /// the same point.
    /// </summary>
    /// <param name="writingDirection">The writing direction mode.</param>
    /// <param name="outerSize">The size of the rect (typically a fragment).</param>
    /// <param name="innerSize">The size of the inner rect (typically a child fragment).</param>
    /// <returns>The physical offset.</returns>
    public PhysicalOffset ConvertToPhysical(
        WritingDirectionMode writingDirection,
        PhysicalSize outerSize,
        PhysicalSize innerSize)
    {
        return new WritingModeConverter(writingDirection, outerSize.ToLogicalSize(writingDirection))
            .ToPhysical(this, innerSize);
    }

    public static bool operator ==(LogicalOffset left, LogicalOffset right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LogicalOffset left, LogicalOffset right)
    {
        return !(left == right);
    }

    public static LogicalOffset operator +(LogicalOffset left, LogicalOffset right)
    {
        return new LogicalOffset(left.InlineOffset + right.InlineOffset,
            left.BlockOffset + right.BlockOffset);
    }

    // operator+= is implicitly handled by C# for structs if + is defined.
    // The C++ version `LogicalOffset& operator+=(const LogicalOffset& other)`
    // modifies in place. In C#, `a += b;` is `a = a + b;`.

    public static LogicalOffset operator -(LogicalOffset left, LogicalOffset right)
    {
        return new LogicalOffset(left.InlineOffset - right.InlineOffset,
            left.BlockOffset - right.BlockOffset);
    }
    // operator-= is implicitly handled by C# for structs if - is defined.

    public static bool operator >(LogicalOffset left, LogicalOffset right)
    {
        return left.InlineOffset > right.InlineOffset &&
               left.BlockOffset > right.BlockOffset;
    }

    public static bool operator >=(LogicalOffset left, LogicalOffset right)
    {
        return left.InlineOffset >= right.InlineOffset &&
               left.BlockOffset >= right.BlockOffset;
    }

    public static bool operator <(LogicalOffset left, LogicalOffset right)
    {
        return left.InlineOffset < right.InlineOffset &&
               left.BlockOffset < right.BlockOffset;
    }

    public static bool operator <=(LogicalOffset left, LogicalOffset right)
    {
        return left.InlineOffset <= right.InlineOffset &&
               left.BlockOffset <= right.BlockOffset;
    }

    public override bool Equals(object obj)
    {
        return obj is LogicalOffset other && Equals(other);
    }

    public bool Equals(LogicalOffset other)
    {
        // Using float.Equals for potentially better NaN handling, though direct == is often fine.
        return InlineOffset.Equals(other.InlineOffset) &&
               BlockOffset.Equals(other.BlockOffset);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(InlineOffset, BlockOffset);
    }

    /// <summary>
    /// Returns a string representation of the logical offset.
    /// Matches C++ Blink's ToString format (e.g., "10,20").
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        // Using ToInt() as in C++ Blink's ToString()
        return string.Format(CultureInfo.InvariantCulture, "{0},{1}",
            (int)InlineOffset, (int)BlockOffset);
    }
}