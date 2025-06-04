// Copyright (C) 2003, 2006 Apple Computer, Inc.  All rights reserved.
// Copyright 2017 The Chromium Authors
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

// For Debug.Fail

namespace LayoutEngine.NG.Renderer.Platform.Text;

/// <summary>
/// Provides utility functions related to text direction and writing modes,
/// mirroring Blink's global helper functions.
/// </summary>
public static class TextUtils
{
    /// <summary>
    /// Checks if the given text direction is Left-to-Right.
    /// </summary>
    public static bool IsLtr(TextDirection direction)
    {
        return direction == TextDirection.Ltr;
    }

    /// <summary>
    /// Checks if the given text direction is Right-to-Left.
    /// </summary>
    public static bool IsRtl(TextDirection direction)
    {
        // In C++, this is `direction != TextDirection::kLtr;`
        // which is equivalent to `direction == TextDirection::kRtl;`
        // if Ltr and Rtl are the only two options.
        return direction == TextDirection.Rtl;
    }

    /// <summary>
    /// Determines text direction from a Unicode bidi level.
    /// Even levels are LTR, odd levels are RTL.
    /// </summary>
    public static TextDirection DirectionFromLevel(uint level) // C++ unsigned is uint
    {
        return (level & 1) == 1 ? TextDirection.Rtl : TextDirection.Ltr;
    }

    /// <summary>
    /// Provides a string representation for TextDirection, similar to C++ operator&lt;&lt;.
    /// </summary>
    public static string ToString(TextDirection direction)
    {
        return IsLtr(direction) ? "LTR" : "RTL";
    }

    // The C++ function `ToBaseTextDirection` converts Blink's TextDirection
    // to `base::i18n::TextDirection`. This is specific to Chromium's base library.
    // If a similar conversion is needed in your C# project to a different
    // internationalization library's enum, a similar method can be added here.
    // For now, it's omitted as `base::i18n::TextDirection` is not part of this conversion scope.
    // Example placeholder if needed:
    /*
    public static BaseI18nTextDirection ToBaseTextDirection(TextDirection direction)
    {
        switch (direction)
        {
            case TextDirection.Ltr:
                return BaseI18nTextDirection.LEFT_TO_RIGHT; // Assuming a C# enum BaseI18nTextDirection exists
            case TextDirection.Rtl:
                return BaseI18nTextDirection.RIGHT_TO_LEFT;
        }
        Debug.Fail("Unhandled TextDirection in ToBaseTextDirection");
        return default; // Should not be reached
    }
    */

    // ... other existing TextUtils methods (IsHorizontalWritingMode, etc.) ...
    public static bool IsHorizontalWritingMode(WritingMode writingMode)
    {
        return writingMode == WritingMode.HorizontalTb;
    }

    public static bool IsVerticalWritingMode(WritingMode writingMode)
    {
        return writingMode == WritingMode.VerticalLr ||
               writingMode == WritingMode.VerticalRl;
    }

    public static bool IsFlippedLinesWritingMode(WritingMode writingMode)
    {
        return writingMode == WritingMode.VerticalLr;
    }

    public static WritingMode ToLineWritingMode(WritingMode writingMode)
    {
        return !IsFlippedLinesWritingMode(writingMode) ? writingMode
            : WritingMode.VerticalRl;
    }

    public static bool IsFlippedBlocksWritingMode(WritingMode writingMode)
    {
        return writingMode == WritingMode.VerticalRl ||
               writingMode == WritingMode.SidewaysRl;
    }

    public static bool IsParallelWritingMode(WritingMode a, WritingMode b)
    {
        return (a == WritingMode.HorizontalTb) == (b == WritingMode.HorizontalTb);
    }

    public static bool IsHorizontalTypographicMode(WritingMode writingMode)
    {
        return writingMode == WritingMode.HorizontalTb ||
               writingMode == WritingMode.SidewaysLr ||
               writingMode == WritingMode.SidewaysRl;
    }

    public static string ToString(WritingMode writingMode)
    {
        switch (writingMode)
        {
            case WritingMode.HorizontalTb:
                return "horizontal-tb";
            case WritingMode.VerticalRl:
                return "vertical-rl";
            case WritingMode.VerticalLr:
                return "vertical-lr";
            case WritingMode.SidewaysRl:
                return "sideways-rl";
            case WritingMode.SidewaysLr:
                return "sideways-lr";
            default:
                return ((byte)writingMode).ToString();
        }
    }
}