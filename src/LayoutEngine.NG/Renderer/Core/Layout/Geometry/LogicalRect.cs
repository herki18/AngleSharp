// Copyright 2017 The Chromium Authors
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

// For RectangleF, Rectangle

// For LayoutUnit static methods

namespace LayoutEngine.NG.Renderer.Core.Layout.Geometry;

using System;
using System.Drawing;
using System.Globalization;
using LayoutEngine.NG.Layout.Geometry;

/// <summary>
/// LogicalRect is the position and size of a rect (typically a fragment)
/// relative to the parent in the logical coordinate system.
/// For more information about physical and logical coordinate systems, see:
/// https://chromium.googlesource.com/chromium/src/+/main/third_party/blink/renderer/core/layout/README.md#coordinate-spaces
/// </summary>
public struct LogicalRect : IEquatable<LogicalRect>
{
    public LogicalOffset Offset; // Made public field to match C++
    public Geometry.LogicalSize Size;     // Made public field to match C++

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalRect"/> struct with default values.
    /// </summary>
    public LogicalRect()
    {
        Offset = LogicalOffset.Zero;
        Size = Geometry.LogicalSize.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalRect"/> struct.
    /// </summary>
    /// <param name="offset">The logical offset.</param>
    /// <param name="size">The logical size.</param>
    public LogicalRect(LogicalOffset offset, Geometry.LogicalSize size)
    {
        Offset = offset;
        Size = size;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalRect"/> struct.
    /// </summary>
    /// <param name="inlineOffset">The offset in the inline direction.</param>
    /// <param name="blockOffset">The offset in the block direction.</param>
    /// <param name="inlineSize">The size in the inline direction.</param>
    /// <param name="blockSize">The size in the block direction.</param>
    public LogicalRect(float inlineOffset, float blockOffset, float inlineSize, float blockSize)
    {
        Offset = new LogicalOffset(inlineOffset, blockOffset);
        Size = new Geometry.LogicalSize(inlineSize, blockSize);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalRect"/> struct.
    /// For testing only.
    /// </summary>
    public LogicalRect(int inlineOffset, int blockOffset, int inlineSize, int blockSize)
    {
        Offset = new LogicalOffset(inlineOffset, blockOffset);
        Size = new Geometry.LogicalSize(inlineSize, blockSize);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalRect"/> struct from a System.Drawing.Rectangle.
    /// </summary>
    /// <param name="r">The System.Drawing.Rectangle.</param>
    public LogicalRect(Rectangle r)
    {
        Offset = new LogicalOffset(r.X, r.Y);
        Size = new Geometry.LogicalSize(r.Width, r.Height);
    }


    public bool IsEmpty() => Size.IsEmpty();

    public float InlineStartOffset() => Offset.InlineOffset;
    public float BlockStartOffset() => Offset.BlockOffset;
    public float InlineSize() => Size.InlineSize;
    public float BlockSize() => Size.BlockSize;

    public float InlineEndOffset() => Offset.InlineOffset + Size.InlineSize;
    public float BlockEndOffset() => Offset.BlockOffset + Size.BlockSize;
    public LogicalOffset EndOffset() => Offset + Size;

    public static bool operator ==(LogicalRect left, LogicalRect right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LogicalRect left, LogicalRect right)
    {
        return !(left == right);
    }

    public static LogicalRect operator +(LogicalRect rect, LogicalOffset additionalOffset)
    {
        return new LogicalRect(rect.Offset + additionalOffset, rect.Size);
    }

    /// <summary>
    /// Unites this rectangle with another rectangle. If the other rectangle is empty,
    /// this rectangle is unchanged. If this rectangle is empty, it becomes the other rectangle.
    /// </summary>
    /// <param name="other">The rectangle to unite with.</param>
    public void Unite(LogicalRect other)
    {
        if (other.IsEmpty())
            return;
        if (IsEmpty())
        {
            this = other;
            return;
        }
        UniteEvenIfEmpty(other);
    }

    /// <summary>
    /// Unites this rectangle with another rectangle, even if one or both are empty.
    /// </summary>
    /// <param name="other">The rectangle to unite with.</param>
    public void UniteEvenIfEmpty(LogicalRect other)
    {
        LogicalOffset newEndOffset = Max(EndOffset(), other.EndOffset());
        LogicalOffset newStartOffset = Min(Offset, other.Offset);
        Size = newEndOffset - newStartOffset; // Assumes LogicalSize can be result of LogicalOffset - LogicalOffset
        Offset = new LogicalOffset(newEndOffset.InlineOffset - Size.InlineSize,
            newEndOffset.BlockOffset - Size.BlockSize);
    }

    /// <summary>
    /// Shift up the inline-start edge and the block-start by d, and shift down
    /// the inline-end edge and the block-end edge by d.
    /// </summary>
    /// <param name="d">The amount to inflate by.</param>
    public void Inflate(float d)
    {
        Offset.InlineOffset -= d;
        Size.InlineSize += d * 2;
        Offset.BlockOffset -= d;
        Size.BlockSize += d * 2;
    }

    /// <summary>
    /// Shift up the inline-start edge by inlineStart, shift up the block-start
    /// edge by blockStart, shift down the inline-end edge by inlineEnd, and
    /// shift down the block-end edge by blockEnd.
    /// </summary>
    public void ExpandEdges(float blockStart, float inlineEnd, float blockEnd, float inlineStart)
    {
        Offset.InlineOffset -= inlineStart;
        Offset.BlockOffset -= blockStart;
        Size.InlineSize += inlineStart + inlineEnd;
        Size.BlockSize += blockStart + blockEnd;
    }

    public void ContractEdges(float blockStart, float inlineEnd, float blockEnd, float inlineStart)
    {
        ExpandEdges(-blockStart, -inlineEnd, -blockEnd, -inlineStart);
    }

    /// <summary>
    /// Update inline-start offset without changing the inline-end offset.
    /// </summary>
    public void ShiftInlineStartEdgeTo(float edge)
    {
        float newSize = LayoutMath.ClampNegativeToZero(InlineEndOffset() - edge);
        Offset.InlineOffset = edge;
        Size.InlineSize = newSize;
    }

    /// <summary>
    /// Update block-start offset without changing the block-end offset.
    /// </summary>
    public void ShiftBlockStartEdgeTo(float edge)
    {
        float newBlockSize = LayoutMath.ClampNegativeToZero(BlockEndOffset() - edge);
        Offset.BlockOffset = edge;
        Size.BlockSize = newBlockSize;
    }

    /// <summary>
    /// Update inline-end offset without changing the inline-start offset.
    /// </summary>
    public void ShiftInlineEndEdgeTo(float edge)
    {
        Size.InlineSize = LayoutMath.ClampNegativeToZero(edge - Offset.InlineOffset);
    }

    /// <summary>
    /// Update block-end offset without changing the block-start offset.
    /// </summary>
    public void ShiftBlockEndEdgeTo(float edge)
    {
        Size.BlockSize = LayoutMath.ClampNegativeToZero(edge - Offset.BlockOffset);
    }

    /// <summary>
    /// Creates a LogicalRect that encloses the given System.Drawing.RectangleF.
    /// You can use this function only if we know rect is logical.
    /// </summary>
    /// <param name="rect">The System.Drawing.RectangleF to enclose.</param>
    /// <returns>The enclosing LogicalRect.</returns>
    public static LogicalRect EnclosingRect(RectangleF rect)
    {
        // C++ LayoutUnit::FromFloatFloor and FromFloatCeil are specific ways
        // to convert floats to LayoutUnit, which is a fixed-point type in Blink.
        // In C#, using float directly, Math.Floor and Math.Ceiling are appropriate.
        var offset = new LogicalOffset((float)Math.Floor(rect.X),
            (float)Math.Floor(rect.Y));
        var size = new Geometry.LogicalSize(
            (float)Math.Ceiling(rect.Right) - offset.InlineOffset,
            (float)Math.Ceiling(rect.Bottom) - offset.BlockOffset);
        return new LogicalRect(offset, size);
    }


    public override bool Equals(object obj)
    {
        return obj is LogicalRect other && Equals(other);
    }

    public bool Equals(LogicalRect other)
    {
        return Offset.Equals(other.Offset) && Size.Equals(other.Size);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Offset, Size);
    }

    /// <summary>
    /// Returns a string representation of the logical rect.
    /// Matches C++ Blink's ToString format (e.g., "10,20 100x50").
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        // C++ .ToString().Ascii().c_str() implies getting a simple string representation.
        // Assuming Offset and Size have appropriate ToString methods.
        return string.Format(CultureInfo.InvariantCulture, "{0} {1}",
            Offset.ToString(), Size.ToString());
    }

    // Helper Min/Max for LogicalOffset, similar to C++ anonymous namespace
    private static LogicalOffset Min(LogicalOffset a, LogicalOffset b)
    {
        return new LogicalOffset(Math.Min(a.InlineOffset, b.InlineOffset),
            Math.Min(a.BlockOffset, b.BlockOffset));
    }

    private static LogicalOffset Max(LogicalOffset a, LogicalOffset b)
    {
        return new LogicalOffset(Math.Max(a.InlineOffset, b.InlineOffset),
            Math.Max(a.BlockOffset, b.BlockOffset));
    }
}