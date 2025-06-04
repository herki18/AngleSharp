namespace LayoutEngine.NG.Layout.Core;

using System;
using System.Collections.Generic;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using LayoutEngine.NG.Dom;
using LayoutEngine.NG.Layout.Fragments;
using LayoutEngine.NG.Layout.Process;
using LayoutEngine.NG.Style;

/// <summary>
/// Represents a box in the layout tree.
/// In LayoutNG, LayoutBox is the base class for all layout objects that generate a principal box.
/// This follows the actual LayoutNG naming convention (not LayoutBlock).
/// </summary>
public class LayoutBox : LayoutObject
{
    /// <summary>
    /// The position of this box.
    /// In LayoutNG, boxes store their physical location.
    /// Note: This is separate from Fragment.Offset which is relative to the containing block.
    /// </summary>
    public PhysicalOffset Location { get; set; }

    /// <summary>
    /// The box's padding area.
    /// In LayoutNG, padding is part of the box model.
    /// </summary>
    public BoxSpacing Padding { get; set; }

    /// <summary>
    /// The box's border widths.
    /// In LayoutNG, borders affect box sizing.
    /// </summary>
    public BoxSpacing Border { get; set; }

    /// <summary>
    /// The box's margin area.
    /// In LayoutNG, margins are outside the border box.
    /// </summary>
    public BoxSpacing Margin { get; set; }

    /// <summary>
    /// The box's overflow behavior.
    /// In LayoutNG, determines clipping and scrolling.
    /// </summary>
    public Overflow OverflowX { get; set; } = Overflow.Visible;
    public Overflow OverflowY { get; set; } = Overflow.Visible;

    /// <summary>
    /// Whether this box has been positioned.
    /// In LayoutNG, used during layout to track progress.
    /// </summary>
    public bool IsPositioned { get; set; }

    /// <summary>
    /// The view that contains this box.
    /// </summary>
    public LayoutView? View => GetDocument()?.GetLayoutView();

    /// <summary>
    /// Gets the border box size (content + padding + border).
    /// In LayoutNG, this is a commonly needed measurement.
    /// </summary>
    public PhysicalSize BorderBoxSize
    {
        get
        {
            return new PhysicalSize(
                ContentSize.Width + Padding.Left + Padding.Right + Border.Left + Border.Right,
                ContentSize.Height + Padding.Top + Padding.Bottom + Border.Top + Border.Bottom
            );
        }
    }

    /// <summary>
    /// Gets the padding box size (content + padding).
    /// </summary>
    public PhysicalSize PaddingBoxSize
    {
        get
        {
            return new PhysicalSize(
                ContentSize.Width + Padding.Left + Padding.Right,
                ContentSize.Height + Padding.Top + Padding.Bottom
            );
        }
    }

    #region Type Checking Methods

    public virtual bool IsLayoutBlockFlow() => false;
    public virtual bool IsBlockInInline() => false;
    public virtual bool IsLayoutCustom() => false;
    public virtual bool IsColumnSpanAll() => false;
    public virtual bool IsFloating() => Style?.Float != Float.None;
    public virtual bool IsOutOfFlowPositioned() =>
        Style?.Position == PositionMode.Absolute || Style?.Position == PositionMode.Fixed;
    public virtual bool IsLayoutReplaced() => false;
    public virtual bool CanContainAbsolutePositionObjects() =>
        Style?.Position != PositionMode.Static || IsLayoutView();
    public virtual bool CanContainFixedPositionObjects() => IsLayoutView();
    public virtual bool IsBody() => Node?.NodeName?.ToUpper() == "BODY";
    public virtual bool IsLayoutView() => false;
    public virtual bool IsDocumentElement() => Node == GetDocument()?.DocumentElement;
    public virtual bool IsFlexItem() => Parent?.IsFlexibleBox() == true;
    public virtual bool IsFlexibleBox() => Style?.Display == DisplayMode.Flex;
    public virtual bool IsLayoutGrid() => Style?.Display == DisplayMode.Grid;
    public virtual bool IsLayoutMasonry() => false; // TODO: Implement masonry layout
    public virtual bool ShouldBeConsideredAsReplaced() => IsLayoutReplaced();
    public virtual bool IsLayoutListItem() => false;
    public virtual bool IsLayoutOutsideListMarker() => false;
    public virtual bool IsButtonOrInputButton() =>
        Node is IHtmlButtonElement ||
        (Node is IHtmlInputElement input && (input.Type == "button" || input.Type == "submit"));
    public virtual bool IsFieldset() => Node is IHtmlFieldSetElement;
    public virtual bool IsInitialLetterBox() => false; // TODO: Implement initial-letter
    public virtual bool IsMedia() => Node is IHtmlMediaElement;
    public virtual bool IsCanvas() => Node is IHtmlCanvasElement;
    public virtual bool IsRenderedLegend() => Node is IHtmlLegendElement && Parent?.IsFieldset() == true;
    public virtual bool IsSVGText() => false; // TODO: Implement SVG support
    public virtual bool IsTable() => Style?.Display == DisplayMode.Table;
    public virtual bool IsLayoutTextCombine() => false; // TODO: Implement text-combine
    public virtual bool IsTableCaption() => Style?.Display == DisplayMode.TableCaption;
    public virtual bool IsTableSection() => false;
    public virtual bool IsTableRow() => Style?.Display == DisplayMode.TableRow;
    public virtual bool IsTableCell() => Style?.Display == DisplayMode.TableCell;
    public virtual bool IsTextArea() => Node is IHtmlTextAreaElement;
    public virtual bool IsTextControl() => Node is IHtmlInputElement || Node is IHtmlTextAreaElement;
    public virtual bool IsTextField() => Node is IHtmlInputElement input &&
        (input.Type == "text" || input.Type == "email" || input.Type == "password" ||
         input.Type == "search" || input.Type == "tel" || input.Type == "url");
    public virtual bool IsMathMLRoot() => false; // TODO: Implement MathML support
    public virtual bool IsMathML() => false; // TODO: Implement MathML support
    public virtual bool IsAnonymous() => Node == null;
    public virtual bool IsAnonymousBlockFlow() => IsAnonymous() && IsLayoutBlockFlow();
    public virtual bool IsHorizontalWritingMode() => true; // TODO: Implement writing-mode
    public virtual bool IsHorizontalTypographicMode() => true; // TODO: Implement writing-mode
    public virtual bool IsMonolithic() => IsLayoutReplaced() || OverflowY != Overflow.Visible;
    public virtual bool IsScrollContainer() => OverflowX == Overflow.Auto || OverflowX == Overflow.Scroll ||
                                              OverflowY == Overflow.Auto || OverflowY == Overflow.Scroll;
    public virtual bool CreatesNewFormattingContext() => EstablishesFormattingContext();

    #endregion

    #region Document Access

    public IDocument GetDocument() => Node?.OwnerDocument ?? throw new InvalidOperationException("No document");
    public INode? GetNode() => Node;
    public INode? EnclosingNode()
    {
        var node = GetNode();
        if (node != null)
            return node;

        var parent = Parent;
        while (parent != null)
        {
            if (parent.GetNode() != null)
                return parent.GetNode();
            parent = parent.Parent;
        }

        return null;
    }

    public ComputedStyle StyleRef() => Style ?? throw new InvalidOperationException("No style");

    #endregion

    #region Size Containment

    public virtual bool ShouldApplySizeContainment() =>
        Style?.Contain?.HasFlag(ContainmentType.Size) == true;
    public virtual bool ShouldApplyInlineSizeContainment() =>
        Style?.Contain?.HasFlag(ContainmentType.InlineSize) == true || ShouldApplySizeContainment();
    public virtual bool ShouldApplyBlockSizeContainment() =>
        Style?.Contain?.HasFlag(ContainmentType.BlockSize) == true || ShouldApplySizeContainment();
    public virtual bool CanMatchSizeContainerQueries() => false; // TODO: Implement container queries

    #endregion

    #region Intrinsic Sizing

    public virtual float OverrideIntrinsicContentInlineSize() => float.NaN;
    public virtual float OverrideIntrinsicContentBlockSize() => float.NaN;
    public virtual float DefaultIntrinsicContentInlineSize() => float.NaN;
    public virtual float DefaultIntrinsicContentBlockSize() => float.NaN;

    #endregion

    #region Display Lock

    public virtual bool ChildLayoutBlockedByDisplayLock() => false; // TODO: Implement display locking

    #endregion

    #region Custom Layout

    public virtual CustomLayoutChild? GetCustomLayoutChild() => null; // TODO: Implement custom layout

    #endregion

    #region Fragment Traversal

    public virtual bool CanTraversePhysicalFragments() => true;

    #endregion

    #region Layout

    /// <summary>
    /// Creates a physical fragment for this layout box.
    /// </summary>
    public override PhysicalFragment CreatePhysicalFragment()
    {
        return PhysicalFragment.CreateBuilder()
            .SetLayoutObject(this)
            .SetOffset(Location)
            .SetSize(BorderBoxSize)
            .SetMargins(new PhysicalBoxStrut(
                Margin.Top, // BlockStart
                Margin.Right, // InlineEnd
                Margin.Bottom, // BlockEnd
                Margin.Left // InlineStart
            ))
            .SetBorders(new PhysicalBoxStrut(
                Border.Top,
                Border.Right,
                Border.Bottom,
                Border.Left
            ))
            .SetPadding(new PhysicalBoxStrut(
                Padding.Top,
                Padding.Right,
                Padding.Bottom,
                Padding.Left
            ))
            .Build();
    }

    /// <summary>
    /// Performs layout for this box.
    /// In LayoutNG, this is typically overridden by specific layout algorithms.
    /// </summary>
    public virtual void Layout()
    {
        // Default implementation - mark as no longer needing layout
        NeedsLayout = false;

        // Update content size if not set
        if (ContentSize.Width == 0 && ContentSize.Height == 0)
        {
            ContentSize = new PhysicalSize(100, 50); // Default size
        }
    }

    public override LayoutObjectType GetLayoutObjectType()
    {
        return LayoutObjectType.Box;
    }

    #endregion

    #region Layout Size

    public virtual PhysicalSize GetLayoutSize(IncludeScrollbarsOption option)
    {
        // Simplified implementation
        return BorderBoxSize;
    }

    #endregion

    #region Layout Results and Caching

    private readonly List<LayoutResult> _layoutResults = new();
    private bool _shouldSkipLayoutCache;
    private bool _intrinsicLogicalWidthsDirty = true;
    private bool _hasBrokenSpine;
    private bool _everHadLayout;

    public bool NeedsLayout { get; set; } = true;
    public bool EverHadLayout => _everHadLayout;
    public bool HasBrokenSpine() => _hasBrokenSpine;
    public void SetHasBrokenSpine(bool value) => _hasBrokenSpine = value;
    public void ClearHasBrokenSpine() => _hasBrokenSpine = false;

    public LayoutResult? GetCachedLayoutResult(BlockBreakToken? breakToken)
    {
        // Simplified cache lookup
        if (_layoutResults.Count > 0)
            return _layoutResults[0];
        return null;
    }

    public LayoutResult? GetSingleCachedLayoutResult()
    {
        return _layoutResults.Count == 1 ? _layoutResults[0] : null;
    }

    public void SetCachedLayoutResult(LayoutResult result, int index)
    {
        while (_layoutResults.Count <= index)
            _layoutResults.Add(null!);
        _layoutResults[index] = result;
        _everHadLayout = true;
    }

    public LayoutResult? GetLayoutResult(int index)
    {
        return index < _layoutResults.Count ? _layoutResults[index] : null;
    }

    public void SetLayoutResult(LayoutResult result, int index)
    {
        while (_layoutResults.Count <= index)
            _layoutResults.Add(null!);
        _layoutResults[index] = result;
        _everHadLayout = true;
    }

    public void ShrinkLayoutResults(int size)
    {
        while (_layoutResults.Count > size)
            _layoutResults.RemoveAt(_layoutResults.Count - 1);
    }

    public void FinalizeLayoutResults()
    {
        // TODO: Implement finalization logic
    }

    public bool ShouldSkipLayoutCache() => _shouldSkipLayoutCache;
    public void SetShouldSkipLayoutCache(bool value) => _shouldSkipLayoutCache = value;

    public void AddMeasureLayoutResult(LayoutResult result)
    {
        // TODO: Store measure results separately
    }

    #endregion

    #region Intrinsic Sizes

    private MinMaxSizesResult? _cachedIntrinsicSizes;
    private float? _cachedBlockSize;
    private bool _intrinsicLogicalWidthsDependsOnBlockConstraints;

    public bool IntrinsicLogicalWidthsDirty() => _intrinsicLogicalWidthsDirty;
    public void SetIntrinsicLogicalWidthsDirty(bool markOnly = false) => _intrinsicLogicalWidthsDirty = true;

    public bool IntrinsicLogicalWidthsDependsOnBlockConstraints() =>
        _intrinsicLogicalWidthsDependsOnBlockConstraints;

    public MinMaxSizesResult? CachedIndefiniteIntrinsicLogicalWidths()
    {
        return _cachedBlockSize == null ? _cachedIntrinsicSizes : null;
    }

    public MinMaxSizesResult? CachedIntrinsicLogicalWidths(float blockSize)
    {
        return _cachedBlockSize == blockSize ? _cachedIntrinsicSizes : null;
    }

    public void SetIntrinsicLogicalWidths(float blockSize, MinMaxSizesResult result)
    {
        _cachedBlockSize = blockSize;
        _cachedIntrinsicSizes = result;
        _intrinsicLogicalWidthsDirty = false;
        _intrinsicLogicalWidthsDependsOnBlockConstraints = result.DependsOnBlockConstraints;
    }

    #endregion

    #region Physical Fragments

    private readonly List<PhysicalBoxFragment> _physicalFragments = new();

    public IReadOnlyList<PhysicalBoxFragment> PhysicalFragments => _physicalFragments;

    public int PhysicalFragmentCount() => _physicalFragments.Count;

    public void AddPhysicalFragment(PhysicalBoxFragment fragment)
    {
        _physicalFragments.Add(fragment);
    }

    #endregion

    #region Layout Invalidation

    public void SetNeedsLayout(LayoutInvalidationReason reason, bool markOnlyThis = false)
    {
        NeedsLayout = true;
        if (!markOnlyThis && Parent != null)
        {
            Parent.SetChildNeedsLayout(markOnlyThis);
        }
    }

    public void SetChildNeedsLayout(bool markOnlyThis = false)
    {
        // TODO: Implement child needs layout logic
        if (!markOnlyThis && Parent != null)
        {
            Parent.SetChildNeedsLayout(markOnlyThis);
        }
    }

    public void ClearNeedsLayout()
    {
        NeedsLayout = false;
    }

    public void SetShouldCheckForPaintInvalidation()
    {
        // TODO: Implement paint invalidation check
    }

    #endregion

    #region Size and Overflow

    private PhysicalSize _size;

    public PhysicalSize Size
    {
        get => _size;
        set => _size = value;
    }

    public void SetLocation(PhysicalOffset location)
    {
        Location = location;
    }

    public void SizeChanged()
    {
        // TODO: Handle size change notifications
    }

    public void UpdateAfterLayout()
    {
        // TODO: Implement post-layout updates
    }

    public float LogicalHeightForEmptyLine()
    {
        // Simplified implementation
        return Style?.LineHeight ?? 20;
    }

    #endregion

    #region Scrolling

    private PaintLayerScrollableArea? _scrollableArea;

    public PaintLayerScrollableArea? GetScrollableArea() => _scrollableArea;

    public LayoutBox? GetScrollMarkerGroup()
    {
        // TODO: Implement scroll marker group lookup
        return null;
    }

    public LayoutBlock? ScrollerFromScrollMarkerGroup()
    {
        // TODO: Implement scroller lookup from marker group
        return null;
    }

    #endregion

    #region Children

    public LayoutBox? FirstChildBox()
    {
        return FirstChild as LayoutBox;
    }

    public LayoutObject? SlowFirstChild() => FirstChild;

    public void Remove()
    {
        if (Parent != null)
        {
            // TODO: Implement proper removal
            if (PreviousSibling != null)
                PreviousSibling.NextSibling = NextSibling;
            if (NextSibling != null)
                NextSibling.PreviousSibling = PreviousSibling;
            if (Parent.FirstChild == this)
                Parent.FirstChild = NextSibling;
        }
    }

    #endregion

    #region Form Controls

    public bool ApplyControlFixedSize(INode? node)
    {
        // TODO: Implement form control fixed sizing logic
        return false;
    }

    #endregion

    #region Multicol Support

    public virtual LayoutMultiColumnFlowThread? MultiColumnFlowThread() => null;

    #endregion

    #region Transform Support

    public bool ShouldUseTransformFromContainer(LayoutBox? container)
    {
        // TODO: Implement transform check
        return false;
    }

    public void GetTransformFromContainer(
        LayoutBox? container,
        PhysicalOffset offset,
        Transform transform,
        PhysicalSize? size,
        Transform? fragmentTransform)
    {
        // TODO: Implement transform calculation
    }

    #endregion

    #region Additional Type Checks

    public virtual bool IsFrameSet() => false;
    public virtual bool IsFragmentationContextRoot() => false;

    #endregion
}

/// <summary>
/// Represents spacing values for box model calculations.
/// In LayoutNG, used for margin, padding, and border.
/// </summary>
public struct BoxSpacing
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
}

/// <summary>
/// CSS overflow values.
/// </summary>
public enum Overflow
{
    Visible,
    Hidden,
    Scroll,
    Auto,
    Clip
}

/// <summary>
/// Options for including scrollbars in size calculations.
/// </summary>
public enum IncludeScrollbarsOption
{
    ExcludeScrollbars,
    IncludeScrollbars
}

/// <summary>
/// CSS float values.
/// </summary>
public enum Float
{
    None,
    Left,
    Right
}

/// <summary>
/// Logical axes for containment.
/// </summary>
[Flags]
public enum LogicalAxes
{
    None = 0,
    Inline = 1,
    Block = 2,
    Both = Inline | Block
}

/// <summary>
/// CSS containment types.
/// </summary>
[Flags]
public enum ContainmentType
{
    None = 0,
    Layout = 1,
    Style = 2,
    Paint = 4,
    Size = 8,
    InlineSize = 16,
    BlockSize = 32
}