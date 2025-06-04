// Copyright 2016 The Chromium Authors
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

// For LayoutUnit

// For TextDirection, WritingDirectionMode, TextUtils

namespace LayoutEngine.NG.Renderer.Platform.Geometry;

using System;
using System.Globalization;
using Core.Layout.Geometry;
using LogicalSize = Layout.Fragments.LogicalSize;

/// <summary>
/// This struct is used for storing margins, borders or padding of a box on all
/// four logical edges.
/// </summary>
public struct BoxStrut : IEquatable<BoxStrut>
{
    public LayoutUnit InlineStart;
    public LayoutUnit InlineEnd;
    public LayoutUnit BlockStart;
    public LayoutUnit BlockEnd;

    public BoxStrut(LayoutUnit inlineStart, LayoutUnit inlineEnd, LayoutUnit blockStart, LayoutUnit blockEnd)
    {
        InlineStart = inlineStart;
        InlineEnd = inlineEnd;
        BlockStart = blockStart;
        BlockEnd = blockEnd;
    }

    public BoxStrut(LineBoxStrut lineRelative, bool isFlippedLines)
    {
        if (!isFlippedLines)
        {
            InlineStart = lineRelative.InlineStart;
            InlineEnd = lineRelative.InlineEnd;
            BlockStart = lineRelative.LineOver;
            BlockEnd = lineRelative.LineUnder;
        }
        else
        {
            InlineStart = lineRelative.InlineStart;
            InlineEnd = lineRelative.InlineEnd;
            BlockStart = lineRelative.LineUnder; // Swapped
            BlockEnd = lineRelative.LineOver;   // Swapped
        }
    }

    /// <summary>
    /// Create a strut based on an inner rectangle positioned within an area.
    /// </summary>
    public BoxStrut(LogicalSize outerSize, LogicalRect innerRect)
    {
        InlineStart = innerRect.Offset.InlineOffset;
        InlineEnd = outerSize.InlineSize - innerRect.InlineEndOffset();
        BlockStart = innerRect.Offset.BlockOffset;
        BlockEnd = outerSize.BlockSize - innerRect.BlockEndOffset();
    }

    /// <summary>
    /// Update each of data members with Math.Min(this.member, other.member).
    /// This function returns a new BoxStrut with the intersected values.
    /// (C++ modifies in place and returns *this)
    /// </summary>
    public BoxStrut Intersect(BoxStrut other)
    {
        return new BoxStrut(
            LayoutUnit.Min(InlineStart, other.InlineStart), // Assuming LayoutUnit has Min/Max static methods or use Math.Min
            LayoutUnit.Min(InlineEnd, other.InlineEnd),
            LayoutUnit.Min(BlockStart, other.BlockStart),
            LayoutUnit.Min(BlockEnd, other.BlockEnd)
        );
    }
    // If in-place modification is desired:
    // public void IntersectWith(BoxStrut other)
    // {
    //     InlineStart = LayoutUnit.Min(InlineStart, other.InlineStart);
    //     InlineEnd = LayoutUnit.Min(InlineEnd, other.InlineEnd);
    //     BlockStart = LayoutUnit.Min(BlockStart, other.BlockStart);
    //     BlockEnd = LayoutUnit.Min(BlockEnd, other.BlockEnd);
    // }


    public LayoutUnit LineLeft(TextDirection direction)
    {
        return TextUtils.IsLtr(direction) ? InlineStart : InlineEnd;
    }

    public LayoutUnit LineRight(TextDirection direction)
    {
        return TextUtils.IsLtr(direction) ? InlineEnd : InlineStart;
    }

    public LayoutUnit InlineSum() => InlineStart + InlineEnd;
    public LayoutUnit BlockSum() => BlockStart + BlockEnd;

    public LogicalOffset StartOffset() => new LogicalOffset(InlineStart, BlockStart);

    public bool IsEmpty() => this.Equals(default(BoxStrut)); // Or check all fields are Zero

    public PhysicalBoxStrut ConvertToPhysical(WritingDirectionMode writingDirection)
    {
        LayoutUnit directionStart = InlineStart;
        LayoutUnit directionEnd = InlineEnd;
        if (writingDirection.IsRtl())
        {
            (directionStart, directionEnd) = (directionEnd, directionStart); // Swap
        }

        switch (writingDirection.GetWritingMode())
        {
            case WritingMode.HorizontalTb:
                return new PhysicalBoxStrut(BlockStart, directionEnd, BlockEnd, directionStart);
            case WritingMode.VerticalRl:
            case WritingMode.SidewaysRl:
                return new PhysicalBoxStrut(directionStart, BlockStart, directionEnd, BlockEnd);
            case WritingMode.VerticalLr:
                return new PhysicalBoxStrut(directionStart, BlockEnd, directionEnd, BlockStart);
            case WritingMode.SidewaysLr:
                return new PhysicalBoxStrut(directionEnd, BlockEnd, directionStart, BlockStart);
            default:
                // Should not be reached if all WritingModes are handled
                throw new ArgumentOutOfRangeException(nameof(writingDirection.GetWritingMode), "Unhandled writing mode.");
        }
    }

    public static BoxStrut operator +(BoxStrut a, BoxStrut b)
    {
        return new BoxStrut(
            a.InlineStart + b.InlineStart,
            a.InlineEnd + b.InlineEnd,
            a.BlockStart + b.BlockStart,
            a.BlockEnd + b.BlockEnd);
    }

    public static BoxStrut operator -(BoxStrut a, BoxStrut b)
    {
        return new BoxStrut(
            a.InlineStart - b.InlineStart,
            a.InlineEnd - b.InlineEnd,
            a.BlockStart - b.BlockStart,
            a.BlockEnd - b.BlockEnd);
    }

    public bool Equals(BoxStrut other)
    {
        return InlineStart == other.InlineStart &&
               InlineEnd == other.InlineEnd &&
               BlockStart == other.BlockStart &&
               BlockEnd == other.BlockEnd;
    }

    public override bool Equals(object obj) => obj is BoxStrut other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(InlineStart, InlineEnd, BlockStart, BlockEnd);

    public static bool operator ==(BoxStrut left, BoxStrut right) => left.Equals(right);
    public static bool operator !=(BoxStrut left, BoxStrut right) => !(left == right);

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "Inline: ({0} {1}) Block: ({2} {3})",
            InlineStart.ToInt(), InlineEnd.ToInt(),
            BlockStart.ToInt(), BlockEnd.ToInt());
    }
}