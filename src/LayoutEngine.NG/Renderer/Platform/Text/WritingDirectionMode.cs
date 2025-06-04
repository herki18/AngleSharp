// Copyright 2020 The Chromium Authors
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

namespace LayoutEngine.NG.Renderer.Platform.Text;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Geometry;
using LayoutEngine.NG.Layout;

/// <summary>
/// This class packs WritingMode and TextDirection, two enums that are often
/// used and passed around together.
/// Corresponds to Blink's WritingDirectionMode.
/// </summary>
public readonly struct WritingDirectionMode : IEquatable<WritingDirectionMode>
{
    private readonly WritingMode _writingMode;
    private readonly TextDirection _direction;

    // Predefined common modes for convenience
    public static readonly WritingDirectionMode HorizontalTbLtr =
        new WritingDirectionMode(WritingMode.HorizontalTb, TextDirection.Ltr);
    public static readonly WritingDirectionMode HorizontalTbRtl =
        new WritingDirectionMode(WritingMode.HorizontalTb, TextDirection.Rtl);
    public static readonly WritingDirectionMode VerticalRlTtbLtr =
        new WritingDirectionMode(WritingMode.VerticalRl, TextDirection.Ltr);
    public static readonly WritingDirectionMode VerticalRlTtbRtl =
        new WritingDirectionMode(WritingMode.VerticalRl, TextDirection.Rtl);
    public static readonly WritingDirectionMode VerticalLrTtbLtr =
        new WritingDirectionMode(WritingMode.VerticalLr, TextDirection.Ltr);
    public static readonly WritingDirectionMode VerticalLrTtbRtl =
        new WritingDirectionMode(WritingMode.VerticalLr, TextDirection.Rtl);
    public static readonly WritingDirectionMode SidewaysRlTtbLtr = // Assuming TTB for lines
        new WritingDirectionMode(WritingMode.SidewaysRl, TextDirection.Ltr);
    public static readonly WritingDirectionMode SidewaysRlTtbRtl =
        new WritingDirectionMode(WritingMode.SidewaysRl, TextDirection.Rtl);
    public static readonly WritingDirectionMode SidewaysLrTtbLtr =
        new WritingDirectionMode(WritingMode.SidewaysLr, TextDirection.Ltr);
    public static readonly WritingDirectionMode SidewaysLrTtbRtl =
        new WritingDirectionMode(WritingMode.SidewaysLr, TextDirection.Rtl);


    public WritingDirectionMode(WritingMode writingMode, TextDirection direction)
    {
        _writingMode = writingMode;
        _direction = direction;
    }

    //
    // Inline direction functions.
    //
    public TextDirection Direction => _direction;
    // SetDirection is not idiomatic for a readonly struct in C#.
    // If mutability is needed, this should be a class or a mutable struct (not recommended).
    // For now, assuming construction is the way to set these.

    public bool IsLtr() => TextUtils.IsLtr(_direction); // Using TextUtils helper
    public bool IsRtl() => TextUtils.IsRtl(_direction); // Using TextUtils helper

    //
    // Block direction functions.
    //
    public WritingMode GetWritingMode() => _writingMode;
    // SetWritingMode is not idiomatic for a readonly struct.

    public bool IsHorizontal() => TextUtils.IsHorizontalWritingMode(_writingMode);

    /// <summary>
    /// Block progression increases in the opposite direction to normal;
    /// e.g., modes like vertical-rl.
    /// </summary>
    public bool IsFlippedBlocks() => TextUtils.IsFlippedBlocksWritingMode(_writingMode);

    public bool IsFlippedInlines() => IsRtl() ^ (_writingMode == WritingMode.SidewaysLr);

    /// <summary>
    /// Bottom of the line occurs earlier in the block;
    /// e.g., modes like vertical-lr.
    /// </summary>
    public bool IsFlippedLines() => TextUtils.IsFlippedLinesWritingMode(_writingMode);

    /// <summary>
    /// Returns whether x/y is flipped.
    /// </summary>
    public bool IsFlippedX()
    {
        if (IsHorizontal())
            return IsRtl();
        return IsFlippedBlocks();
    }

    public bool IsFlippedY()
    {
        if (IsHorizontal())
        {
            Debug.Assert(!IsFlippedBlocks());
            return false;
        }
        return IsFlippedInlines();
    }

    //
    // Functions for both inline and block directions.
    //
    public bool IsHorizontalLtr() => IsHorizontal() && IsLtr();

    //
    // PhysicalDirection and LogicalDirection conversion maps
    //
    private static readonly Dictionary<WritingMode, PhysicalDirection> _inlineStartMapLtr =
        new Dictionary<WritingMode, PhysicalDirection>
        {
            { WritingMode.HorizontalTb, PhysicalDirection.Left },
            { WritingMode.VerticalRl,   PhysicalDirection.Up },
            { WritingMode.VerticalLr,   PhysicalDirection.Up },
            { WritingMode.SidewaysRl,   PhysicalDirection.Up },
            { WritingMode.SidewaysLr,   PhysicalDirection.Down }
        };

    private static readonly Dictionary<WritingMode, PhysicalDirection> _inlineEndMapLtr =
        new Dictionary<WritingMode, PhysicalDirection>
        {
            { WritingMode.HorizontalTb, PhysicalDirection.Right },
            { WritingMode.VerticalRl,   PhysicalDirection.Down },
            { WritingMode.VerticalLr,   PhysicalDirection.Down },
            { WritingMode.SidewaysRl,   PhysicalDirection.Down },
            { WritingMode.SidewaysLr,   PhysicalDirection.Up }
        };

    private static readonly Dictionary<WritingMode, PhysicalDirection> _blockStartMap =
        new Dictionary<WritingMode, PhysicalDirection>
        {
            { WritingMode.HorizontalTb, PhysicalDirection.Up },
            { WritingMode.VerticalRl,   PhysicalDirection.Right },
            { WritingMode.VerticalLr,   PhysicalDirection.Left },
            { WritingMode.SidewaysRl,   PhysicalDirection.Right },
            { WritingMode.SidewaysLr,   PhysicalDirection.Left }
        };

    private static readonly Dictionary<WritingMode, PhysicalDirection> _blockEndMap =
        new Dictionary<WritingMode, PhysicalDirection>
        {
            { WritingMode.HorizontalTb, PhysicalDirection.Down },
            { WritingMode.VerticalRl,   PhysicalDirection.Left },
            { WritingMode.VerticalLr,   PhysicalDirection.Right },
            { WritingMode.SidewaysRl,   PhysicalDirection.Left },
            { WritingMode.SidewaysLr,   PhysicalDirection.Right }
        };

    private static readonly Dictionary<WritingMode, PhysicalDirection> _lineOverMap =
        new Dictionary<WritingMode, PhysicalDirection>
        {
            { WritingMode.HorizontalTb, PhysicalDirection.Up },
            { WritingMode.VerticalRl,   PhysicalDirection.Right },
            { WritingMode.VerticalLr,   PhysicalDirection.Right },
            { WritingMode.SidewaysRl,   PhysicalDirection.Right },
            { WritingMode.SidewaysLr,   PhysicalDirection.Left }
        };

    private static readonly Dictionary<WritingMode, PhysicalDirection> _lineUnderMap =
        new Dictionary<WritingMode, PhysicalDirection>
        {
            { WritingMode.HorizontalTb, PhysicalDirection.Down },
            { WritingMode.VerticalRl,   PhysicalDirection.Left },
            { WritingMode.VerticalLr,   PhysicalDirection.Left },
            { WritingMode.SidewaysRl,   PhysicalDirection.Left },
            { WritingMode.SidewaysLr,   PhysicalDirection.Right }
        };


    private static readonly Dictionary<WritingMode, LogicalDirection> _topMapLtr =
        new Dictionary<WritingMode, LogicalDirection>
        {
            { WritingMode.HorizontalTb, LogicalDirection.BlockStart },
            { WritingMode.VerticalRl,   LogicalDirection.InlineStart },
            { WritingMode.VerticalLr,   LogicalDirection.InlineStart },
            { WritingMode.SidewaysRl,   LogicalDirection.InlineStart },
            { WritingMode.SidewaysLr,   LogicalDirection.InlineEnd }
        };

    private static readonly Dictionary<WritingMode, LogicalDirection> _rightMapLtr =
        new Dictionary<WritingMode, LogicalDirection>
        {
            { WritingMode.HorizontalTb, LogicalDirection.InlineEnd },
            { WritingMode.VerticalRl,   LogicalDirection.BlockStart },
            { WritingMode.VerticalLr,   LogicalDirection.BlockEnd },
            { WritingMode.SidewaysRl,   LogicalDirection.BlockStart },
            { WritingMode.SidewaysLr,   LogicalDirection.BlockEnd }
        };

    private static readonly Dictionary<WritingMode, LogicalDirection> _bottomMapLtr =
        new Dictionary<WritingMode, LogicalDirection>
        {
            { WritingMode.HorizontalTb, LogicalDirection.BlockEnd },
            { WritingMode.VerticalRl,   LogicalDirection.InlineEnd },
            { WritingMode.VerticalLr,   LogicalDirection.InlineEnd },
            { WritingMode.SidewaysRl,   LogicalDirection.InlineEnd },
            { WritingMode.SidewaysLr,   LogicalDirection.InlineStart }
        };

    private static readonly Dictionary<WritingMode, LogicalDirection> _leftMapLtr =
        new Dictionary<WritingMode, LogicalDirection>
        {
            { WritingMode.HorizontalTb, LogicalDirection.InlineStart },
            { WritingMode.VerticalRl,   LogicalDirection.BlockEnd },
            { WritingMode.VerticalLr,   LogicalDirection.BlockStart },
            { WritingMode.SidewaysRl,   LogicalDirection.BlockEnd },
            { WritingMode.SidewaysLr,   LogicalDirection.BlockStart }
        };


    /// <summary>
    /// Returns a physical direction corresponding to logical inline-start.
    /// </summary>
    public PhysicalDirection InlineStart()
    {
        return _direction == TextDirection.Ltr ? _inlineStartMapLtr[_writingMode] : _inlineEndMapLtr[_writingMode];
    }

    /// <summary>
    /// Returns a physical direction corresponding to logical inline-end.
    /// </summary>
    public PhysicalDirection InlineEnd()
    {
        return _direction == TextDirection.Ltr ? _inlineEndMapLtr[_writingMode] : _inlineStartMapLtr[_writingMode];
    }

    /// <summary>
    /// Returns a physical direction corresponding to logical block-start.
    /// </summary>
    public PhysicalDirection BlockStart() => _blockStartMap[_writingMode];

    /// <summary>
    /// Returns a physical direction corresponding to logical block-end.
    /// </summary>
    public PhysicalDirection BlockEnd() => _blockEndMap[_writingMode];

    /// <summary>
    /// Returns a physical direction corresponding to logical line-over.
    /// </summary>
    public PhysicalDirection LineOver() => _lineOverMap[_writingMode];

    /// <summary>
    /// Returns a physical direction corresponding to logical line-under.
    /// </summary>
    public PhysicalDirection LineUnder() => _lineUnderMap[_writingMode];


    /// <summary>
    /// Returns a logical direction corresponding to physical top.
    /// </summary>
    public LogicalDirection Top()
    {
        // In C++, the condition is `IsLtr() || IsHorizontalWritingMode(writing_mode_)`
        // which means if it's LTR OR horizontal, use _topMapLtr.
        // If it's RTL AND vertical, use _bottomMapLtr.
        if (IsLtr() || TextUtils.IsHorizontalWritingMode(_writingMode))
        {
            return _topMapLtr[_writingMode];
        }
        return _bottomMapLtr[_writingMode]; // RTL and Vertical
    }

    /// <summary>
    /// Returns a logical direction corresponding to physical right.
    /// </summary>
    public LogicalDirection Right()
    {
        // In C++, the condition is `IsLtr() || !IsHorizontalWritingMode(writing_mode_)`
        // which means if it's LTR OR vertical, use _rightMapLtr.
        // If it's RTL AND horizontal, use _leftMapLtr.
        if (IsLtr() || !TextUtils.IsHorizontalWritingMode(_writingMode))
        {
            return _rightMapLtr[_writingMode];
        }
        return _leftMapLtr[_writingMode]; // RTL and Horizontal
    }

    /// <summary>
    /// Returns a logical direction corresponding to physical bottom.
    /// </summary>
    public LogicalDirection Bottom()
    {
        if (IsLtr() || TextUtils.IsHorizontalWritingMode(_writingMode))
        {
            return _bottomMapLtr[_writingMode];
        }
        return _topMapLtr[_writingMode]; // RTL and Vertical
    }

    /// <summary>
    /// Returns a logical direction corresponding to physical left.
    /// </summary>
    public LogicalDirection Left()
    {
        if (IsLtr() || !TextUtils.IsHorizontalWritingMode(_writingMode))
        {
            return _leftMapLtr[_writingMode];
        }
        return _rightMapLtr[_writingMode]; // RTL and Horizontal
    }


    public bool Equals(WritingDirectionMode other)
    {
        return _writingMode == other._writingMode && _direction == other._direction;
    }

    public override bool Equals(object obj)
    {
        return obj is WritingDirectionMode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_writingMode, _direction);
    }

    public static bool operator ==(WritingDirectionMode left, WritingDirectionMode right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WritingDirectionMode left, WritingDirectionMode right)
    {
        return !(left == right);
    }

    public override string ToString() => $"{_writingMode} {_direction}";
}
//
// // Helper TextUtils class (can be in its own file or part of Text namespace)
// // TODO: Ensure these helpers are correctly implemented based on Blink's definitions.
// public static class TextUtils
// {
//     public static bool IsLtr(TextDirection direction) => direction == TextDirection.Ltr;
//     public static bool IsRtl(TextDirection direction) => direction == TextDirection.Rtl;
//
//     public static bool IsHorizontalWritingMode(WritingMode mode) =>
//         mode == WritingMode.HorizontalTb ||
//         mode == WritingMode.LrTb || // Assuming LrTb is horizontal
//         mode == WritingMode.RlTb;   // Assuming RlTb is horizontal
//
//     public static bool IsVerticalWritingMode(WritingMode mode) =>
//         !IsHorizontalWritingMode(mode);
//
//
//     public static bool IsFlippedBlocksWritingMode(WritingMode mode)
//     {
//         // Corresponds to IsFlippedBlocksWritingMode in Blink
//         // Example: vertical-rl is typically flipped blocks.
//         // horizontal-tb is not.
//         return mode == WritingMode.VerticalRl || mode == WritingMode.SidewaysRl; // Adjust based on Blink's definition
//     }
//
//     public static bool IsFlippedLinesWritingMode(WritingMode mode)
//     {
//         // Corresponds to IsFlippedLinesWritingMode in Blink
//         // Example: vertical-lr is typically flipped lines.
//         return mode == WritingMode.VerticalLr || mode == WritingMode.SidewaysLr; // Adjust based on Blink's definition
//     }
// }