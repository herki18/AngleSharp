namespace LayoutEngine.NG.Layout;

using System;
using System.Collections.Generic;
using Core;
using Inputs;

// Originally from BaselineAlgorithmType.cs
public enum BaselineAlgorithmType
{
    FirstLine,
    LastLine,
    Alphabetic,
    Ideographic,
    Middle,
    Hanging,
    Mathematical
}

[Flags]
public enum OverflowClipAxes
{
    None = 0,
    X = 1,
    Y = 2,
    Both = X | Y
}

public enum LayoutInvalidationReason
{
    Unknown,
    SizeChanged,
    AncestorMoved,
    StyleChange,
    DomChanged,
    ScrollAnchorNoLongerRelevant,
    WritingModeChanged,
    Fullscreen,
    ChildChanged,
    DepthChanged,
    ScrollMarkersChanged
}

public enum MathScriptType
{
    Sub,
    Super,
    SubSup,
    Under,
    Over,
    UnderOver,
    MultiScripts
}

public enum AspectRatioType
{
    None,
    Auto,
    Ratio,
    AutoAndRatio
}

public enum BoxSizing
{
    ContentBox,
    BorderBox
}

// Originally from StyleVariant.cs
public enum StyleVariant
{
    Standard,
    FirstLine,
    FirstLineInherited,
    Marker
}

public static class StyleVariantUtils
{
    public static bool UsesFirstLineStyle(StyleVariant variant)
    {
        return variant == StyleVariant.FirstLine ||
            variant == StyleVariant.FirstLineInherited;
    }
}

public enum ScrollSnapAlign
{
    None,
    Start,
    End,
    Center
}

public struct WritingDirection
{
    // This was an empty struct in the original StyleVariant.cs
}

// Originally from CustomLayoutChild.cs
public class CustomLayoutChild
{
    public LayoutBox? Box { get; set; }
    public string? Name { get; set; }
}

// Originally from ColumnSpannerPath.cs
public class ColumnSpannerPath
{
    public IList<BlockNode> Path { get; } = new List<BlockNode>();
    public BlockNode? SpannerNode { get; set; }
}

// Originally from EarlyBreak.cs
public class EarlyBreak
{
    public BreakAppeal BreakAppeal { get; set; }
    public LayoutInputNode? BreakBefore { get; set; }
    public LayoutInputNode? BreakAfter { get; set; }
}

public enum BreakAppeal
{
    Perfect,
    LastResort,
    ViolateOrphansAndWidows,
    BreakBetweenLines
}