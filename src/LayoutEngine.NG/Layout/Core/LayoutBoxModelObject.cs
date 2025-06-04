namespace LayoutEngine.NG.Layout.Core;

using System;
using LayoutEngine.NG.Layout.Fragments;

/// <summary>
/// Base class for layout objects that have a box model (boxes and inlines).
/// </summary>
public class LayoutBoxModelObject : LayoutObject
{
    public bool HasSelfPaintingLayer()
    {
        // TODO: Implement HasSelfPaintingLayer
        throw new NotImplementedException();
    }

    public PaintLayer? Layer()
    {
        // TODO: Implement Layer
        return null;
    }

    // TODO: Implement LayoutBoxModelObject
}

/// <summary>
/// Represents an out-of-flow positioned node with its positioning information.
/// </summary>
public class PhysicalOofPositionedNode
{
    // TODO: Implement PhysicalOofPositionedNode
}

/// <summary>
/// Represents anchor positioning query information.
/// </summary>
public class PhysicalAnchorQuery
{
    public bool IsEmpty()
    {
        // TODO: Implement IsEmpty
        return true;
    }

    // TODO: Implement PhysicalAnchorQuery
}


/// <summary>
/// Out-of-flow data for fragmented contexts.
/// </summary>
public class FragmentedOofData : PhysicalFragment.OofData
{
    public bool HasNestedMulticolsWithOOFs()
    {
        // TODO: Implement HasNestedMulticolsWithOOFs
        return false;
    }

    // TODO: Implement FragmentedOofData
}


/// <summary>
/// Builder for creating physical fragments.
/// </summary>
public class FragmentBuilder
{
    public LayoutObject? LayoutObject { get; set; }
    public LogicalSize Size { get; set; }
    public StyleVariant StyleVariant { get; set; }
    public bool IsHiddenForPaint { get; set; }
    public bool HasFloatingDescendantsForPaint { get; set; }
    public bool HasAdjoiningObjectDescendants { get; set; }
    public bool IsOpaque { get; set; }
    public bool IsBlockInInline { get; set; }
    public bool IsLineForParallelFlow { get; set; }
    public bool MayHaveDescendantAboveBlockStart { get; set; }
    public bool HasCollapsedBorders { get; set; }
    public List<LayoutBoxModelObject>? StickyDescendants { get; set; }
    public List<Element>? SnapAreas { get; set; }
    public LayoutObject? ScrollStartTarget { get; set; }
    public BreakToken? BreakToken { get; set; }

    public WritingMode GetWritingMode()
    {
        // TODO: Implement GetWritingMode
        return WritingMode.HorizontalTb;
    }

    public bool HasFragmentedOutOfFlowData()
    {
        // TODO: Implement HasFragmentedOutOfFlowData
        return false;
    }

    public bool HasOutOfFlowFragmentChild()
    {
        // TODO: Implement HasOutOfFlowFragmentChild
        return false;
    }

    public bool HasOutOfFlowInFragmentainerSubtree()
    {
        // TODO: Implement HasOutOfFlowInFragmentainerSubtree
        return false;
    }

    public bool HasPropagatedData()
    {
        // TODO: Implement HasPropagatedData
        return StickyDescendants != null || SnapAreas != null || ScrollStartTarget != null;
    }

    public bool NeedsOofData()
    {
        // TODO: Implement NeedsOofData
        return false;
    }

    // TODO: Implement FragmentBuilder
}