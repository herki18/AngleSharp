namespace LayoutEngine.NG.Layout.Fragments;

using System;
using System.Collections.Generic;
using LayoutEngine.NG.Layout;
using LayoutEngine.NG.Layout.Core;

/// <summary>
/// Represents a physical box fragment in the layout tree.
/// This is the C# equivalent of Blink's PhysicalBoxFragment class.
/// </summary>
public class PhysicalBoxFragment : PhysicalFragment
{
    #region Nested Classes

    /// <summary>
    /// Scope that allows post-layout operations in debug builds.
    /// </summary>
#if DEBUG
    public class AllowPostLayoutScope : IDisposable
    {
        private static int _allowCount = 0;

        public AllowPostLayoutScope()
        {
            _allowCount++;
        }

        public void Dispose()
        {
            if (_allowCount > 0)
                _allowCount--;
        }

        public static bool IsAllowed => _allowCount > 0;
    }
#endif

    /// <summary>
    /// Provides mutable access for style recalculation.
    /// </summary>
    public class MutableForStyleRecalc
    {
        private readonly PhysicalBoxFragment _fragment;

        internal MutableForStyleRecalc(PhysicalBoxFragment fragment)
        {
            _fragment = fragment;
        }

        /// <summary>
        /// Sets the scrollable overflow for this fragment.
        /// </summary>
        public void SetScrollableOverflow(PhysicalRect scrollableOverflow)
        {
            bool hasScrollableOverflow = scrollableOverflow != new PhysicalRect(PhysicalOffset.Zero, _fragment.Size);

            if (hasScrollableOverflow)
            {
                _fragment.EnsureRareField(FieldId.ScrollableOverflow).ScrollableOverflow = scrollableOverflow;
            }
            else if (_fragment.HasScrollableOverflow)
            {
                _fragment._rareData?.RemoveField(FieldId.ScrollableOverflow);
            }
        }
    }

    /// <summary>
    /// Provides mutable access for container layout operations.
    /// </summary>
    public class MutableForContainerLayout
    {
        private readonly PhysicalBoxFragment _fragment;

        internal MutableForContainerLayout(PhysicalBoxFragment fragment)
        {
            _fragment = fragment;
        }

        /// <summary>
        /// Sets the margins for this fragment.
        /// </summary>
        public void SetMargins(PhysicalBoxStrut margins)
        {
            _fragment.EnsureRareField(FieldId.Margins).Margins = margins;
        }

        /// <summary>
        /// Sets the offset from the root fragmentation context.
        /// </summary>
        public void SetOffsetFromRootFragmentationContext(PhysicalOffset offset)
        {
            if (offset == PhysicalOffset.Zero && _fragment.GetRareField(FieldId.OffsetFromRootFragmentationContext) == null)
                return;

            _fragment.EnsureRareField(FieldId.OffsetFromRootFragmentationContext).OffsetFromRootFragmentationContext = offset;
        }
    }

    /// <summary>
    /// Provides mutable access for painting operations.
    /// </summary>
    public class MutableForPainting
    {
        private readonly PhysicalBoxFragment _fragment;

        internal MutableForPainting(PhysicalBoxFragment fragment)
        {
            _fragment = fragment;
        }

        /// <summary>
        /// Recalculates ink overflow for this fragment.
        /// </summary>
        public void RecalcInkOverflow()
        {
            _fragment.RecalcInkOverflow();
        }

        /// <summary>
        /// Recalculates ink overflow with the given contents rect.
        /// </summary>
        public void RecalcInkOverflow(PhysicalRect contents)
        {
            _fragment.RecalcInkOverflow(contents);
        }

#if DEBUG
        /// <summary>
        /// Invalidates ink overflow (debug only).
        /// </summary>
        public void InvalidateInkOverflow()
        {
            _fragment.InvalidateInkOverflow();
        }
#endif
    }

    /// <summary>
    /// Provides mutable access for cloning operations.
    /// </summary>
    public class MutableForCloning
    {
        private readonly PhysicalBoxFragment _fragment;

        internal MutableForCloning(PhysicalBoxFragment fragment)
        {
            _fragment = fragment;
        }

        /// <summary>
        /// Clears the IsFirstForNode flag.
        /// </summary>
        public void ClearIsFirstForNode()
        {
            _fragment._isFirstForNode = false;
        }

        /// <summary>
        /// Clears propagated out-of-flow positioned descendants.
        /// </summary>
        public void ClearPropagatedOOFs()
        {
            _fragment.ClearOofData();
        }

        /// <summary>
        /// Sets the break token for this fragment.
        /// </summary>
        public void SetBreakToken(BlockBreakToken? token)
        {
            _fragment._breakToken = token;
        }

        /// <summary>
        /// Gets the children collection for mutation.
        /// </summary>
        public IList<PhysicalFragmentLink> Children => _fragment._children;

        /// <summary>
        /// Replaces children with those from another fragment.
        /// </summary>
        public void ReplaceChildren(PhysicalBoxFragment newFragment)
        {
            // TODO: Check that neither fragment has inline formatting context
            _fragment._children.Clear();
            _fragment._children.AddRange(newFragment._children);

            // Replace propagated data
            _fragment._propagatedData = newFragment._propagatedData;
        }
    }

    /// <summary>
    /// Provides mutable access for out-of-flow fragmentation.
    /// </summary>
    public class MutableForOofFragmentation
    {
        private readonly PhysicalBoxFragment _fragment;

        public MutableForOofFragmentation(PhysicalBoxFragment fragment)
        {
            _fragment = fragment;
        }

        /// <summary>
        /// Adds a child fragmentainer for out-of-flow positioning.
        /// </summary>
        public void AddChildFragmentainer(PhysicalBoxFragment childFragment, LogicalOffset childOffset)
        {
            // TODO: Implement proper WritingModeConverter
            var physicalOffset = new PhysicalOffset(childOffset.InlineOffset, childOffset.BlockOffset);

            var link = new PhysicalFragmentLink
            {
                Fragment = childFragment,
                Offset = physicalOffset
            };

            _fragment._children.Add(link);
        }

        /// <summary>
        /// Merges content from a placeholder fragmentainer.
        /// </summary>
        public void Merge(PhysicalBoxFragment placeholderFragmentainer)
        {
            // Copy all child fragments
            foreach (var newChild in placeholderFragmentainer._children)
            {
                _fragment._children.Add(newChild);
                if (newChild.Fragment.IsOutOfFlowPositioned)
                    _fragment._hasOutOfFlowFragmentChild = true;
            }

            // Update break token if needed
            if (placeholderFragmentainer.BreakToken != null)
            {
                if (_fragment._breakToken is BlockBreakToken oldToken)
                {
                    // TODO: Implement BlockBreakToken.GetMutableForOofFragmentation().Merge()
                }
                else
                {
                    _fragment._breakToken = placeholderFragmentainer.BreakToken;
                }
            }

            // Copy anchor queries
            var query = placeholderFragmentainer.AnchorQuery();
            if (query != null)
            {
                _fragment.EnsureOofData();
                var anchorQuery = _fragment._oofData!.EnsureAnchorQuery();
                // TODO: Merge anchor queries
            }

            UpdateOverflow();
        }

        /// <summary>
        /// Updates overflow after modifications.
        /// </summary>
        public void UpdateOverflow()
        {
            // TODO: Implement ScrollableOverflowCalculator
            var overflow = _fragment.LocalRect;
            _fragment.GetMutableForStyleRecalc().SetScrollableOverflow(overflow);
        }
    }

    #endregion

    #region Fields

    private readonly List<PhysicalFragmentLink> _children;
    private PhysicalFragmentRareData? _rareData;
    private InkOverflow _inkOverflow;
    private readonly bool _hasItems;
    private readonly bool _isInlineFormattingContext;
    private bool _isFirstForNode;
    private readonly bool _isFragmentationContextRoot;
    private readonly bool _isMonolithic;
    private readonly bool _hasMovedChildrenInBlockDirection;

    // Border inclusion flags
    private readonly bool _includeBorderTop;
    private readonly bool _includeBorderRight;
    private readonly bool _includeBorderBottom;
    private readonly bool _includeBorderLeft;

    // Baseline values
    private bool _hasFirstBaseline;
    private bool _hasLastBaseline;
    private LayoutUnit _firstBaseline;
    private LayoutUnit _lastBaseline;
    private bool _useLastBaselineForInlineBaseline;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new PhysicalBoxFragment from a builder.
    /// </summary>
    public static PhysicalBoxFragment Create(
        BoxFragmentBuilder builder,
        WritingMode blockOrLineWritingMode)
    {
        // TODO: Implement full creation logic
        return new PhysicalBoxFragment(builder, blockOrLineWritingMode);
    }

    /// <summary>
    /// Creates a shallow copy of another PhysicalBoxFragment.
    /// </summary>
    public static PhysicalBoxFragment Clone(PhysicalBoxFragment other)
    {
        // TODO: Implement cloning logic
        return new PhysicalBoxFragment(other);
    }

    /// <summary>
    /// Creates a shallow copy with post-layout fragments.
    /// </summary>
    public static PhysicalBoxFragment CloneWithPostLayoutFragments(PhysicalBoxFragment other)
    {
        // TODO: Implement post-layout cloning logic
        var cloned = Clone(other);

#if DEBUG
        using (var allowScope = new AllowPostLayoutScope())
        {
            // TODO: Update children with post-layout fragments
        }
#endif

        return cloned;
    }

    #endregion

    #region Constructors

    private PhysicalBoxFragment(BoxFragmentBuilder builder, WritingMode blockOrLineWritingMode)
        : base(builder, blockOrLineWritingMode, FragmentType.Box, builder.BoxType)
    {
        _children = new List<PhysicalFragmentLink>();
        _inkOverflow = new InkOverflow();

        // TODO: Initialize from builder
        _hasItems = builder.HasItems;
        _isInlineFormattingContext = builder.IsInlineFormattingContext;
        _isFirstForNode = builder.IsFirstForNode;
        _isFragmentationContextRoot = builder.IsFragmentationContextRoot;
        _isMonolithic = builder.IsMonolithic;

        // Initialize border inclusion
        _includeBorderTop = builder.SidesToInclude.Top;
        _includeBorderRight = builder.SidesToInclude.Right;
        _includeBorderBottom = builder.SidesToInclude.Bottom;
        _includeBorderLeft = builder.SidesToInclude.Left;

        // Initialize baselines
        if (builder.FirstBaseline.HasValue)
        {
            _hasFirstBaseline = true;
            _firstBaseline = builder.FirstBaseline.Value;
        }

        if (builder.LastBaseline.HasValue)
        {
            _hasLastBaseline = true;
            _lastBaseline = builder.LastBaseline.Value;
        }

        _useLastBaselineForInlineBaseline = builder.UseLastBaselineForInlineBaseline;
    }

    private PhysicalBoxFragment(PhysicalBoxFragment other)
        : base(other)
    {
        _children = new List<PhysicalFragmentLink>(other._children);
        _inkOverflow = other._inkOverflow;
        _hasItems = other._hasItems;
        _isInlineFormattingContext = other._isInlineFormattingContext;
        _isFirstForNode = other._isFirstForNode;
        _isFragmentationContextRoot = other._isFragmentationContextRoot;
        _isMonolithic = other._isMonolithic;
        _hasMovedChildrenInBlockDirection = other._hasMovedChildrenInBlockDirection;

        _includeBorderTop = other._includeBorderTop;
        _includeBorderRight = other._includeBorderRight;
        _includeBorderBottom = other._includeBorderBottom;
        _includeBorderLeft = other._includeBorderLeft;

        _hasFirstBaseline = other._hasFirstBaseline;
        _hasLastBaseline = other._hasLastBaseline;
        _firstBaseline = other._firstBaseline;
        _lastBaseline = other._lastBaseline;
        _useLastBaselineForInlineBaseline = other._useLastBaselineForInlineBaseline;

        if (other._rareData != null)
        {
            _rareData = new PhysicalFragmentRareData(other._rareData);
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the children of this fragment.
    /// </summary>
    public IReadOnlyList<PhysicalFragmentLink> Children => _children.AsReadOnly();

    /// <summary>
    /// Gets the post-layout children (latest generation).
    /// </summary>
    public PostLayoutChildLinkList PostLayoutChildren => new PostLayoutChildLinkList(Children);

    /// <summary>
    /// Gets whether this fragment has inline items.
    /// </summary>
    public bool HasItems => _hasItems;

    /// <summary>
    /// Gets the inline items if available.
    /// </summary>
    public FragmentItems? Items => HasItems ? GetItemsFromAddress() : null;

    /// <summary>
    /// Gets the first baseline if available.
    /// </summary>
    public LayoutUnit? FirstBaseline => _hasFirstBaseline ? _firstBaseline : null;

    /// <summary>
    /// Gets the last baseline if available.
    /// </summary>
    public LayoutUnit? LastBaseline => _hasLastBaseline ? _lastBaseline : null;

    /// <summary>
    /// Gets whether to use last baseline for inline baseline.
    /// </summary>
    public bool UseLastBaselineForInlineBaseline => _useLastBaselineForInlineBaseline;

    /// <summary>
    /// Gets whether this is an inline formatting context.
    /// </summary>
    public bool IsInlineFormattingContext => _isInlineFormattingContext;

    /// <summary>
    /// Gets whether this is the first fragment for its node.
    /// </summary>
    public bool IsFirstForNode => _isFirstForNode;

    /// <summary>
    /// Gets whether this is the only fragment for its node.
    /// </summary>
    public bool IsOnlyForNode => IsFirstForNode && BreakToken == null;

    /// <summary>
    /// Gets whether this is a fragmentation context root.
    /// </summary>
    public bool IsFragmentationContextRoot => _isFragmentationContextRoot;

    /// <summary>
    /// Gets whether this is monolithic.
    /// </summary>
    public bool IsMonolithic => _isMonolithic;

    /// <summary>
    /// Gets whether children have been moved in block direction.
    /// </summary>
    public bool HasMovedChildrenInBlockDirection => _hasMovedChildrenInBlockDirection;

    /// <summary>
    /// Gets the break token as a BlockBreakToken.
    /// </summary>
    public new BlockBreakToken? BreakToken => base.BreakToken as BlockBreakToken;

    /// <summary>
    /// Gets the owner layout box.
    /// </summary>
    public LayoutBox? OwnerLayoutBox()
    {
        // TODO: Implement proper owner lookup
        var layoutBox = GetLayoutObject() as LayoutBox;

#if DEBUG
        // TODO: Add validation checks
#endif

        return layoutBox;
    }

    /// <summary>
    /// Gets the mutable owner layout box.
    /// </summary>
    public LayoutBox? MutableOwnerLayoutBox() => OwnerLayoutBox();

    #endregion

    #region Border and Padding

    /// <summary>
    /// Gets the borders for this fragment.
    /// </summary>
    public PhysicalBoxStrut Borders => GetRareField(FieldId.Borders)?.Borders ?? PhysicalBoxStrut.Zero;

    /// <summary>
    /// Gets the scrollbar for this fragment.
    /// </summary>
    public PhysicalBoxStrut Scrollbar => GetRareField(FieldId.Scrollbar)?.Scrollbar ?? PhysicalBoxStrut.Zero;

    /// <summary>
    /// Gets the padding for this fragment.
    /// </summary>
    public PhysicalBoxStrut Padding => GetRareField(FieldId.Padding)?.Padding ?? PhysicalBoxStrut.Zero;

    /// <summary>
    /// Gets the margins for this fragment.
    /// </summary>
    public PhysicalBoxStrut Margins => GetRareField(FieldId.Margins)?.Margins ?? PhysicalBoxStrut.Zero;

    /// <summary>
    /// Gets the content offset (borders + padding).
    /// </summary>
    public PhysicalOffset ContentOffset
    {
        get
        {
            if (!HasBorders && !HasPadding)
                return PhysicalOffset.Zero;

            var offset = PhysicalOffset.Zero;
            if (HasBorders)
                offset += Borders.Offset;
            if (HasPadding)
                offset += Padding.Offset;

            return offset;
        }
    }

    /// <summary>
    /// Gets the content rectangle.
    /// </summary>
    public PhysicalRect ContentRect()
    {
        var rect = new PhysicalRect(PhysicalOffset.Zero, Size);
        rect.Contract(Borders + Padding);
        return rect;
    }

    /// <summary>
    /// Gets which box sides to include.
    /// </summary>
    public PhysicalBoxSides SidesToInclude => new PhysicalBoxSides(
        _includeBorderTop,
        _includeBorderRight,
        _includeBorderBottom,
        _includeBorderLeft);

    private bool HasBorders => GetRareField(FieldId.Borders) != null;
    private bool HasPadding => GetRareField(FieldId.Padding) != null;

    #endregion

    #region Scrollable Overflow

    /// <summary>
    /// Gets the scrollable overflow rectangle.
    /// </summary>
    public PhysicalRect ScrollableOverflow =>
        GetRareField(FieldId.ScrollableOverflow)?.ScrollableOverflow ?? new PhysicalRect(PhysicalOffset.Zero, Size);

    /// <summary>
    /// Gets whether this fragment has scrollable overflow.
    /// </summary>
    public bool HasScrollableOverflow => GetRareField(FieldId.ScrollableOverflow) != null;

    #endregion

    #region Ink Overflow

    /// <summary>
    /// Gets the ink overflow type.
    /// </summary>
    public InkOverflowType InkOverflowType => _inkOverflow.Type;

    /// <summary>
    /// Gets whether ink overflow is computed.
    /// </summary>
    public bool IsInkOverflowComputed =>
        InkOverflowType != InkOverflowType.NotSet &&
        InkOverflowType != InkOverflowType.Invalidated;

    /// <summary>
    /// Gets whether this fragment has ink overflow.
    /// </summary>
    public bool HasInkOverflow => InkOverflowType != InkOverflowType.None;

    /// <summary>
    /// Gets the ink overflow rectangle.
    /// </summary>
    public PhysicalRect InkOverflowRect()
    {
        if (!CanUseFragmentsForInkOverflow())
        {
            // TODO: Fall back to LayoutBox overflow
            return LocalRect;
        }

        if (!HasInkOverflow)
            return LocalRect;

        // TODO: Implement full ink overflow calculation
        return LocalRect;
    }

    /// <summary>
    /// Gets the self ink overflow rectangle.
    /// </summary>
    public PhysicalRect SelfInkOverflowRect()
    {
        if (!CanUseFragmentsForInkOverflow())
        {
            // TODO: Fall back to LayoutBox overflow
            return LocalRect;
        }

        if (!HasInkOverflow)
            return LocalRect;

        return _inkOverflow.SelfRect(InkOverflowType, Size);
    }

    /// <summary>
    /// Gets the contents ink overflow rectangle.
    /// </summary>
    public PhysicalRect ContentsInkOverflowRect()
    {
        if (!CanUseFragmentsForInkOverflow())
        {
            // TODO: Fall back to LayoutBox overflow
            return LocalRect;
        }

        if (!HasInkOverflow)
            return LocalRect;

        return _inkOverflow.ContentsRect(InkOverflowType, Size);
    }

    /// <summary>
    /// Sets the ink overflow.
    /// </summary>
    public void SetInkOverflow(PhysicalRect self, PhysicalRect contents)
    {
        var newType = _inkOverflow.Set(InkOverflowType, self, contents, Size);
        SetInkOverflowType(newType);
    }

    /// <summary>
    /// Recalculates ink overflow.
    /// </summary>
    public void RecalcInkOverflow()
    {
        if (!CanUseFragmentsForInkOverflow())
            return;

        var contentsRect = RecalcContentsInkOverflow();
        RecalcInkOverflow(contentsRect);
    }

    /// <summary>
    /// Recalculates ink overflow with given contents.
    /// </summary>
    public void RecalcInkOverflow(PhysicalRect contents)
    {
        var selfRect = ComputeSelfInkOverflow();
        SetInkOverflow(selfRect, contents);
    }

#if DEBUG
    /// <summary>
    /// Invalidates ink overflow (debug only).
    /// </summary>
    public void InvalidateInkOverflow()
    {
        SetInkOverflowType(_inkOverflow.Invalidate(InkOverflowType));
    }
#endif

    private void SetInkOverflowType(InkOverflowType type)
    {
        _inkOverflow = new InkOverflow { Type = type };
    }

    private bool CanUseFragmentsForInkOverflow()
    {
        return GetLayoutObject()?.IsLayoutReplaced() != true;
    }

    private PhysicalRect RecalcContentsInkOverflow()
    {
        // TODO: Implement contents ink overflow calculation
        return LocalRect;
    }

    private PhysicalRect ComputeSelfInkOverflow()
    {
        // TODO: Implement self ink overflow calculation
        return LocalRect;
    }

    #endregion

    #region Table Support

    /// <summary>
    /// Gets the table grid rectangle.
    /// </summary>
    public LogicalRect? TableGridRect => GetRareField(FieldId.TableGridRect)?.TableGridRect;

    /// <summary>
    /// Gets the table column geometries.
    /// </summary>
    public IReadOnlyList<TableColumnGeometry>? TableColumnGeometries =>
        _rareData?.TableColumnGeometries?.AsReadOnly();

    /// <summary>
    /// Gets the table collapsed borders.
    /// </summary>
    public TableBorders? TableCollapsedBorders => _rareData?.TableCollapsedBorders;

    /// <summary>
    /// Gets the table collapsed borders geometry.
    /// </summary>
    public CollapsedTableBordersGeometry? TableCollapsedBordersGeometry =>
        GetRareField(FieldId.TableCollapsedBordersGeometry)?.TableCollapsedBordersGeometry;

    /// <summary>
    /// Gets the table cell column index.
    /// </summary>
    public int TableCellColumnIndex =>
        GetRareField(FieldId.TableCellColumnIndex)?.TableCellColumnIndex ?? 0;

    #endregion

    #region Mutable Accessors

    /// <summary>
    /// Gets a mutable accessor for style recalculation.
    /// </summary>
    public MutableForStyleRecalc GetMutableForStyleRecalc()
    {
        // TODO: Check lifecycle state
        return new MutableForStyleRecalc(this);
    }

    /// <summary>
    /// Gets a mutable accessor for container layout.
    /// </summary>
    public MutableForContainerLayout GetMutableForContainerLayout()
    {
        // TODO: Check lifecycle state
        return new MutableForContainerLayout(this);
    }

    /// <summary>
    /// Gets a mutable accessor for painting.
    /// </summary>
    public MutableForPainting GetMutableForPainting()
    {
        return new MutableForPainting(this);
    }

    /// <summary>
    /// Gets a mutable accessor for cloning.
    /// </summary>
    public MutableForCloning GetMutableForCloning()
    {
        return new MutableForCloning(this);
    }

    /// <summary>
    /// Gets a mutable accessor for out-of-flow fragmentation.
    /// </summary>
    public MutableForOofFragmentation GetMutableForOofFragmentation()
    {
        return new MutableForOofFragmentation(this);
    }

    #endregion

    #region Post Layout

    /// <summary>
    /// Gets the post-layout version of this fragment.
    /// </summary>
    public PhysicalBoxFragment? PostLayout()
    {
        // TODO: Implement DisableLayoutSideEffectsScope check

        var layoutObject = GetLayoutObject();
        if (layoutObject == null)
            return this;

        var box = layoutObject as LayoutBox;
        if (box == null)
            return this;

        // TODO: Implement fragment lookup from LayoutBox
        return this;
    }

    #endregion

    #region Hit Testing

    /// <summary>
    /// Checks if this fragment may intersect with a hit test.
    /// </summary>
    public bool MayIntersect(
        HitTestResult result,
        HitTestLocation hitTestLocation,
        PhysicalOffset accumulatedOffset)
    {
        var box = GetLayoutObject() as LayoutBox;
        if (box != null)
        {
            // TODO: Implement box hit test
            return true;
        }

        // For non-box fragments, return true for now
        return true;
    }

    /// <summary>
    /// Gets the position for a point within this fragment.
    /// </summary>
    public PositionWithAffinity PositionForPoint(PhysicalOffset point)
    {
        // TODO: Implement position for point logic
        throw new NotImplementedException();
    }

    #endregion

    #region Outline Support

    /// <summary>
    /// Gets whether this fragment is the outline owner.
    /// </summary>
    public bool IsOutlineOwner => !IsInlineBox || InlineContainerFragmentIfOutlineOwner() != null;

    /// <summary>
    /// Gets the inline container fragment if this is an outline owner.
    /// </summary>
    public PhysicalBoxFragment? InlineContainerFragmentIfOutlineOwner()
    {
        if (!IsInlineBox)
            return null;

        // TODO: Implement inline container lookup
        return null;
    }

    /// <summary>
    /// Adds self outline rectangles.
    /// </summary>
    public void AddSelfOutlineRects(
        PhysicalOffset additionalOffset,
        OutlineType outlineType,
        OutlineRectCollector collector,
        OutlineInfo? info)
    {
        // TODO: Implement outline rect collection
    }

    /// <summary>
    /// Adds outline rectangles.
    /// </summary>
    public void AddOutlineRects(
        PhysicalOffset additionalOffset,
        OutlineType outlineType,
        OutlineRectCollector collector)
    {
        AddOutlineRects(additionalOffset, outlineType, true, collector);
    }

    private void AddOutlineRects(
        PhysicalOffset additionalOffset,
        OutlineType outlineType,
        bool containerRelative,
        OutlineRectCollector collector)
    {
        // TODO: Implement outline rect collection
    }

    #endregion

    #region Overflow Clipping

    /// <summary>
    /// Gets the overflow clip rectangle.
    /// </summary>
    public PhysicalRect OverflowClipRect(
        PhysicalOffset location,
        OverlayScrollbarClipBehavior behavior = OverlayScrollbarClipBehavior.IgnoreOverlayScrollbarSize)
    {
        var box = GetLayoutObject() as LayoutBox;
        if (box != null)
        {
            // TODO: Implement overflow clip rect
            return new PhysicalRect(location, Size);
        }

        return new PhysicalRect(location, Size);
    }

    /// <summary>
    /// Gets the overflow clip margin outsets.
    /// </summary>
    public PhysicalBoxStrut OverflowClipMarginOutsets()
    {
        // TODO: Implement overflow clip margin calculation
        return PhysicalBoxStrut.Zero;
    }

    #endregion

    #region Scrolling

    /// <summary>
    /// Gets the pixel snapped scrolled content offset.
    /// </summary>
    public Vector2d PixelSnappedScrolledContentOffset()
    {
        var box = GetLayoutObject() as LayoutBox;
        if (box != null)
        {
            // TODO: Implement pixel snapped scroll offset
            return new Vector2d(0, 0);
        }

        return new Vector2d(0, 0);
    }

    /// <summary>
    /// Gets the scroll size.
    /// </summary>
    public PhysicalSize ScrollSize()
    {
        var box = GetLayoutObject() as LayoutBox;
        if (box != null)
        {
            // TODO: Implement scroll size
            return Size;
        }

        return Size;
    }

    #endregion

    #region Private Methods

    private PhysicalFragmentRareData.RareField? GetRareField(FieldId id)
    {
        return _rareData?.GetField(id);
    }

    private PhysicalFragmentRareData.RareField EnsureRareField(FieldId id)
    {
        if (_rareData == null)
        {
            _rareData = new PhysicalFragmentRareData(1);
        }
        return _rareData.EnsureField(id);
    }

    private void EnsureOofData()
    {
        if (_oofData == null)
        {
            _oofData = new OofData();
        }
    }

    private FragmentItems? GetItemsFromAddress()
    {
        // TODO: Implement fragment items address calculation
        return null;
    }

    #endregion

    #region Debugging

#if DEBUG
    /// <summary>
    /// Checks same for simplified layout.
    /// </summary>
    public void CheckSameForSimplifiedLayout(
        PhysicalBoxFragment other,
        bool checkSameBlockSize,
        bool checkNoFragmentation)
    {
        // TODO: Implement validation checks
    }

    /// <summary>
    /// Checks the integrity of this fragment.
    /// </summary>
    public void CheckIntegrity()
    {
        // TODO: Implement integrity checks
    }

    /// <summary>
    /// Asserts fragment tree self consistency.
    /// </summary>
    public void AssertFragmentTreeSelf()
    {
        // TODO: Implement tree self checks
    }

    /// <summary>
    /// Asserts fragment tree children consistency.
    /// </summary>
    public void AssertFragmentTreeChildren(bool allowDestroyedOrMoved = false)
    {
        // TODO: Implement tree children checks
    }
#endif

    #endregion
}