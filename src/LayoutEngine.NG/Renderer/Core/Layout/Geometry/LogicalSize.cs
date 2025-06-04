// Copyright 2016 The Chromium Authors
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

// For LayoutUnit and LayoutMath

// For WritingMode and TextUtils

namespace LayoutEngine.NG.Renderer.Core.Layout.Geometry;

using System;
using System.Globalization;
using NG.Layout;
using NG.Layout.Fragments;
using NG.Layout.Geometry;
using Platform.Geometry;
using Platform.Text;

/// <summary>
/// LogicalSize is the size of a rect (typically a fragment) in the logical
/// coordinate system.
/// For more information about physical and logical coordinate systems, see:
/// https://chromium.googlesource.com/chromium/src/+/main/third_party/blink/renderer/core/layout/README.md#coordinate-spaces
/// </summary>
public struct LogicalSize : IEquatable<LogicalSize>
{
    /// <summary>
    /// Represents an empty size (0, 0).
    /// </summary>
    public static readonly LogicalSize Empty = new LogicalSize(LayoutUnit.Zero, LayoutUnit.Zero);

    /// <summary>
    /// Represents an indefinite logical size.
    /// Corresponds to C++ kIndefiniteLogicalSize.
    /// </summary>
    public static readonly LogicalSize Indefinite =
        new LogicalSize(LayoutUnit.Indefinite, LayoutUnit.Indefinite);

    public LayoutUnit InlineSize; // Public field to match C++
    public LayoutUnit BlockSize;  // Public field to match C++

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalSize"/> struct.
    /// </summary>
    public LogicalSize(LayoutUnit inlineSize, LayoutUnit blockSize)
    {
        InlineSize = inlineSize;
        BlockSize = blockSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalSize"/> struct.
    /// For testing only.
    /// </summary>
    public LogicalSize(int inlineSize, int blockSize)
    {
        InlineSize = new LayoutUnit(inlineSize);
        BlockSize = new LayoutUnit(blockSize);
    }

    // C++: LogicalSize(double, double) = delete;
    // In C#, this is achieved by not providing such a constructor.
    // Explicit constructors for LayoutUnit from double/float handle conversion.

    public static bool operator ==(LogicalSize left, LogicalSize right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LogicalSize left, LogicalSize right)
    {
        return !(left == right);
    }

    public static LogicalSize operator *(LogicalSize size, float scale)
    {
        // C++: LayoutUnit(inline_size * scale)
        // Assuming LayoutUnit * float operator exists or direct float conversion is acceptable here.
        return new LogicalSize(size.InlineSize * scale,
            size.BlockSize * scale);
    }

    /// <summary>
    /// Gets a value indicating whether this size is empty.
    /// C++: inline_size == LayoutUnit() || block_size == LayoutUnit();
    /// LayoutUnit() in C++ defaults to a raw value of 0.
    /// </summary>
    public bool IsEmpty()
    {
        return InlineSize == LayoutUnit.Zero || BlockSize == LayoutUnit.Zero;
    }

    /// <summary>
    /// Expands the size by the given offsets.
    /// </summary>
    public void Expand(LayoutUnit inlineOffset, LayoutUnit blockOffset)
    {
        InlineSize += inlineOffset;
        BlockSize += blockOffset;
    }

    /// <summary>
    /// Shrinks the size by the given offsets.
    /// </summary>
    public void Shrink(LayoutUnit inlineOffset, LayoutUnit blockOffset)
    {
        InlineSize -= inlineOffset;
        BlockSize -= blockOffset;
    }

    /// <summary>
    /// Returns a new LogicalSize with inline and block sizes clamped to be non-negative.
    /// </summary>
    public LogicalSize ClampNegativeToZero()
    {
        return new LogicalSize(InlineSize.ClampNegativeToZero(),
            BlockSize.ClampNegativeToZero());
    }

    /// <summary>
    /// Returns a new LogicalSize with indefinite inline and block sizes clamped to zero.
    /// </summary>
    public LogicalSize ClampIndefiniteToZero()
    {
        return new LogicalSize(InlineSize.ClampIndefiniteToZero(),
            BlockSize.ClampIndefiniteToZero());
    }

    public override bool Equals(object obj)
    {
        return obj is LogicalSize other && Equals(other);
    }

    public bool Equals(LogicalSize other)
    {
        return InlineSize.Equals(other.InlineSize) && BlockSize.Equals(other.BlockSize);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(InlineSize, BlockSize);
    }

    /// <summary>
    /// Returns a string representation of the logical size.
    /// Matches C++ Blink's format (e.g., "100x50").
    /// </summary>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}x{1}",
            InlineSize.ToString(), BlockSize.ToString());
    }

    // Operators involving BoxStrut
    public static LogicalSize operator -(LogicalSize a, BoxStrut b)
    {
        return new LogicalSize(a.InlineSize - b.InlineSum(), a.BlockSize - b.BlockSum());
    }

    // operator -= for (LogicalSize, BoxStrut)
    // In C#, `a -= b;` becomes `a = a - b;` for structs if `-` is defined.

    public static LogicalSize operator +(LogicalSize a, BoxStrut b)
    {
        return new LogicalSize(a.InlineSize + b.InlineSum(), a.BlockSize + b.BlockSum());
    }

    // Operators involving LogicalOffset
    public static LogicalOffset operator +(LogicalOffset offset, LogicalSize size)
    {
        return new LogicalOffset(offset.InlineOffset + size.InlineSize,
            offset.BlockOffset + size.BlockSize);
    }

    // operator += for (LogicalOffset, LogicalSize)
    // In C#, `offset += size;` becomes `offset = offset + size;` for structs.

    // Conversion functions
    public static LogicalSize ToLogicalSize(PhysicalSize size, WritingMode mode)
    {
        return TextUtils.IsHorizontalWritingMode(mode)
            ? new LogicalSize(new LayoutUnit(size.Width), new LayoutUnit(size.Height))
            : new LogicalSize(new LayoutUnit(size.Height), new LayoutUnit(size.Width));
    }

    public static PhysicalSize ToPhysicalSize(LogicalSize size, WritingMode mode)
    {
        return TextUtils.IsHorizontalWritingMode(mode)
            ? new PhysicalSize(size.InlineSize.ToFloat(), size.BlockSize.ToFloat())
            : new PhysicalSize(size.BlockSize.ToFloat(), size.InlineSize.ToFloat());
    }
}

/// <summary>
/// LogicalDelta resolves the ambiguity of subtractions between LogicalOffsets.
/// </summary>
public readonly struct LogicalDelta // No direct inheritance from LogicalSize in C# struct
{
    public readonly LayoutUnit InlineDelta;
    public readonly LayoutUnit BlockDelta;

    public LogicalDelta(LayoutUnit inlineDelta, LayoutUnit blockDelta)
    {
        InlineDelta = inlineDelta;
        BlockDelta = blockDelta;
    }

    // C++: using LogicalSize::LogicalSize;
    // This using-declaration for constructors is not directly translatable.
    // We provide explicit constructors for LogicalDelta.

    // Implicit conversion to LogicalOffset
    public static implicit operator LogicalOffset(LogicalDelta delta)
    {
        return new LogicalOffset(delta.InlineDelta, delta.BlockDelta);
    }

    // Implicit conversion to LogicalSize
    public static implicit operator LogicalSize(LogicalDelta delta)
    {
        return new LogicalSize(delta.InlineDelta, delta.BlockDelta);
    }

    // Define the subtraction operator for LogicalOffsets to return a LogicalDelta
    public static LogicalDelta operator -(LogicalOffset a, LogicalOffset b)
    {
        return new LogicalDelta(a.InlineOffset - b.InlineOffset,
            a.BlockOffset - b.BlockOffset);
    }
}