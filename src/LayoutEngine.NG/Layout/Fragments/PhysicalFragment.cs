namespace LayoutEngine.NG.Layout.Fragments;

using System;
using System.Collections.Generic;
using System.Text;
using Core;

/// <summary>
/// Represents the physical output of layout following LayoutNG principles.
/// The PhysicalFragment contains the output geometry from layout. The
/// fragment stores all of its information in the physical coordinate system for
/// use by paint, hit-testing etc.
/// </summary>
public class PhysicalFragment : GarbageCollected<PhysicalFragment>
{
    #region Enums

    /// <summary>
    /// Fragment types.
    /// </summary>
    public enum FragmentType
    {
        kFragmentBox = 0,
        kFragmentLineBox = 1
    }

    /// <summary>
    /// Box types for box fragments.
    /// </summary>
    public enum BoxType
    {
        kNormalBox,
        kInlineBox,
        /// <summary>
        /// A multi-column container creates column boxes as its children, which
        /// content is flowed into. This is a fragmentainer.
        /// </summary>
        kColumnBox,
        /// <summary>
        /// The containing block of a page. Used by printing.
        /// </summary>
        kPageContainer,
        /// <summary>
        /// The border box of a page. Used by printing.
        /// </summary>
        kPageBorderBox,
        /// <summary>
        /// Page margin fragment (e.g. author-specified header/footer). Used by printing.
        /// </summary>
        kPageMargin,
        /// <summary>
        /// A page area fragment. Used by printing. This is a fragmentainer.
        /// </summary>
        kPageArea,
        kAtomicInline,
        kFloating,
        kOutOfFlowPositioned,
        kBlockFlowRoot,
        kRenderedLegend,

        kMinimumFormattingContextRoot = kAtomicInline
    }

    /// <summary>
    /// Dump flags for debugging.
    /// </summary>
    [Flags]
    public enum DumpFlags
    {
        DumpHeaderText = 0x1,
        DumpSubtree = 0x2,
        DumpIndentation = 0x4,
        DumpType = 0x8,
        DumpOffset = 0x10,
        DumpSize = 0x20,
        DumpTextOffsets = 0x40,
        DumpSelfPainting = 0x80,
        DumpNodeName = 0x100,
        DumpItems = 0x200,
        DumpLegacyDescendants = 0x400,
        DumpAll = -1
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Data that is propagated from descendants.
    /// </summary>
    public class PropagatedData : GarbageCollected<PropagatedData>
    {
        public PropagatedData(
            List<LayoutBoxModelObject>? stickyDescendants,
            List<Element>? snapAreas,
            LayoutObject? scrollInitialTarget)
        {
            StickyDescendants = stickyDescendants;
            SnapAreas = snapAreas;
            ScrollInitialTarget = scrollInitialTarget;
        }

        public List<LayoutBoxModelObject>? StickyDescendants { get; }
        public List<Element>? SnapAreas { get; }
        public LayoutObject? ScrollInitialTarget { get; }

        public void Trace(Visitor visitor)
        {
            visitor.Trace(StickyDescendants);
            visitor.Trace(SnapAreas);
            visitor.Trace(ScrollInitialTarget);
        }
    }

    /// <summary>
    /// Out-of-flow positioned data.
    /// </summary>
    public class OofData : GarbageCollected<OofData>
    {
        private List<PhysicalOofPositionedNode> _oofPositionedDescendants = new();
        private PhysicalAnchorQuery? _anchorQuery;

        public List<PhysicalOofPositionedNode> OofPositionedDescendants => _oofPositionedDescendants;

        public void SetAnchorQuery(PhysicalAnchorQuery? query) => _anchorQuery = query;
        public PhysicalAnchorQuery? AnchorQuery => _anchorQuery;

        public PhysicalAnchorQuery EnsureAnchorQuery()
        {
            if (_anchorQuery == null)
                _anchorQuery = new PhysicalAnchorQuery();
            return _anchorQuery;
        }

        public virtual void Trace(Visitor visitor)
        {
            visitor.Trace(_oofPositionedDescendants);
            visitor.Trace(_anchorQuery);
        }
    }

    /// <summary>
    /// Iterator for post-layout children that skips invalid fragments.
    /// </summary>
    public class PostLayoutChildLinkList
    {
        private readonly IReadOnlyList<PhysicalFragmentLink> _buffer;

        public PostLayoutChildLinkList(IReadOnlyList<PhysicalFragmentLink> buffer)
        {
            _buffer = buffer;
        }

        public int Count => _buffer.Count;
        public bool IsEmpty => _buffer.Count == 0;

        // TODO: Implement proper iterator with filtering for destroyed/moved layout objects
        public IEnumerable<PhysicalFragmentLink> GetValidChildren()
        {
            foreach (var child in _buffer)
            {
                if (child.Fragment != null && !child.Fragment.IsLayoutObjectDestroyedOrMoved())
                {
                    var postLayout = child.Fragment.PostLayout();
                    if (postLayout != null)
                    {
                        yield return new PhysicalFragmentLink
                        {
                            Fragment = postLayout,
                            Offset = child.Offset
                        };
                    }
                }
            }
        }
    }

    #endregion

    #region Fields

    protected LayoutObject? _layoutObject;
    protected PhysicalSize _size;

    // Type and flags
    private readonly byte _type; // FragmentType
    private readonly byte _subType; // BoxType
    private readonly byte _styleVariant; // StyleVariant
    private readonly bool _isHiddenForPaint;

    // Various boolean flags
    private bool _hasFloatingDescendantsForPaint;
    private bool _hasAdjoiningObjectDescendants;
    private bool _dependsOnPercentageBlockSize;
    private bool _childrenValid = true;

    // Line box specific flags
    private bool _hasPropagatedDescendants;
    private bool _hasHanging;
    private bool _isOpaque;
    private bool _isBlockInInline;
    private bool _isLineForParallelFlow;
    private bool _isMathFraction;
    private bool _isMathOperator;
    private bool _mayHaveDescendantAboveBlockStart;

    // Box specific flags
    private bool _isFieldsetContainer;
    private bool _isTablePart;
    private bool _isPaintedAtomically;
    private bool _hasCollapsedBorders;
    private bool _hasFirstBaseline;
    private bool _hasLastBaseline;
    private bool _useLastBaselineForInlineBaseline;
    private readonly bool _hasFragmentedOutOfFlowData;
    private bool _hasOutOfFlowFragmentChild;
    private readonly bool _hasOutOfFlowInFragmentainerSubtree;

    // Text direction for line boxes
    private TextDirection _baseDirection;

    // Associated data
    private PropagatedData? _propagatedData;
    private BreakToken? _breakToken;
    private OofData? _oofData;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a PhysicalFragment from a FragmentBuilder.
    /// </summary>
    public PhysicalFragment(
        FragmentBuilder builder,
        WritingMode blockOrLineWritingMode,
        FragmentType type,
        byte subType)
    {
        _layoutObject = builder.LayoutObject;
        _size = ToPhysicalSize(builder.Size, builder.GetWritingMode());
        _type = (byte)type;
        _subType = subType;
        _styleVariant = (byte)builder.StyleVariant;
        _isHiddenForPaint = builder.IsHiddenForPaint;

        // Initialize flags from builder
        _hasFloatingDescendantsForPaint = builder.HasFloatingDescendantsForPaint;
        _hasAdjoiningObjectDescendants = builder.HasAdjoiningObjectDescendants;
        _dependsOnPercentageBlockSize = DependsOnPercentageBlockSize(builder);
        _isOpaque = builder.IsOpaque;
        _isBlockInInline = builder.IsBlockInInline;
        _isLineForParallelFlow = builder.IsLineForParallelFlow;
        _mayHaveDescendantAboveBlockStart = builder.MayHaveDescendantAboveBlockStart;
        _hasCollapsedBorders = builder.HasCollapsedBorders;
        _hasFragmentedOutOfFlowData = builder.HasFragmentedOutOfFlowData();
        _hasOutOfFlowFragmentChild = builder.HasOutOfFlowFragmentChild();
        _hasOutOfFlowInFragmentainerSubtree = builder.HasOutOfFlowInFragmentainerSubtree();

        // Set up associated data
        if (builder.HasPropagatedData())
        {
            _propagatedData = new PropagatedData(
                builder.StickyDescendants,
                builder.SnapAreas,
                builder.ScrollStartTarget);
        }

        _breakToken = builder.BreakToken;

        if (builder.NeedsOofData())
        {
            _oofData = OofDataFromBuilder(builder);
        }
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public PhysicalFragment(PhysicalFragment other)
    {
        _layoutObject = other._layoutObject;
        _size = other._size;
        _type = other._type;
        _subType = other._subType;
        _styleVariant = other._styleVariant;
        _isHiddenForPaint = other._isHiddenForPaint;

        // Copy all flags
        _hasFloatingDescendantsForPaint = other._hasFloatingDescendantsForPaint;
        _hasAdjoiningObjectDescendants = other._hasAdjoiningObjectDescendants;
        _dependsOnPercentageBlockSize = other._dependsOnPercentageBlockSize;
        _childrenValid = other._childrenValid;
        _hasPropagatedDescendants = other._hasPropagatedDescendants;
        _hasHanging = other._hasHanging;
        _isOpaque = other._isOpaque;
        _isBlockInInline = other._isBlockInInline;
        _isLineForParallelFlow = other._isLineForParallelFlow;
        _isMathFraction = other._isMathFraction;
        _isMathOperator = other._isMathOperator;
        _mayHaveDescendantAboveBlockStart = other._mayHaveDescendantAboveBlockStart;
        _isFieldsetContainer = other._isFieldsetContainer;
        _isTablePart = other._isTablePart;
        _isPaintedAtomically = other._isPaintedAtomically;
        _hasCollapsedBorders = other._hasCollapsedBorders;
        _hasFirstBaseline = other._hasFirstBaseline;
        _hasLastBaseline = other._hasLastBaseline;
        _useLastBaselineForInlineBaseline = other._useLastBaselineForInlineBaseline;
        _hasFragmentedOutOfFlowData = other._hasFragmentedOutOfFlowData;
        _hasOutOfFlowFragmentChild = other._hasOutOfFlowFragmentChild;
        _hasOutOfFlowInFragmentainerSubtree = other._hasOutOfFlowInFragmentainerSubtree;
        _baseDirection = other._baseDirection;

        // Copy references
        _propagatedData = other._propagatedData;
        _breakToken = other._breakToken;
        _oofData = other._oofData != null ? CloneOofData(other._oofData) : null;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the fragment type.
    /// </summary>
    public FragmentType Type => (FragmentType)_type;

    /// <summary>
    /// Gets whether this is a container (box or line box).
    /// </summary>
    public bool IsContainer => Type == FragmentType.kFragmentBox || Type == FragmentType.kFragmentLineBox;

    /// <summary>
    /// Gets whether this is a box fragment.
    /// </summary>
    public bool IsBox => Type == FragmentType.kFragmentBox;

    /// <summary>
    /// Gets whether this is a line box fragment.
    /// </summary>
    public bool IsLineBox => Type == FragmentType.kFragmentLineBox;

    /// <summary>
    /// Gets the box type (only valid for box fragments).
    /// </summary>
    public BoxType GetBoxType()
    {
        if (!IsBox)
            throw new InvalidOperationException("GetBoxType() called on non-box fragment");
        return (BoxType)_subType;
    }

    /// <summary>
    /// True if this is an inline box (e.g., span).
    /// </summary>
    public bool IsInlineBox => IsBox && GetBoxType() == BoxType.kInlineBox;

    /// <summary>
    /// True if this is a column box.
    /// </summary>
    public bool IsColumnBox => IsBox && GetBoxType() == BoxType.kColumnBox;

    /// <summary>
    /// True if this is a fragmentainer box.
    /// </summary>
    public static bool IsFragmentainerBoxType(BoxType type)
    {
        return type == BoxType.kColumnBox || type == BoxType.kPageArea;
    }

    /// <summary>
    /// True if this is a fragmentainer box.
    /// </summary>
    public bool IsFragmentainerBox => IsBox && IsFragmentainerBoxType(GetBoxType());

    /// <summary>
    /// True if this is a column span all element.
    /// </summary>
    public bool IsColumnSpanAll()
    {
        var box = GetLayoutObject() as LayoutBox;
        return box?.IsColumnSpanAll() ?? false;
    }

    /// <summary>
    /// True if this is an atomic inline.
    /// </summary>
    public bool IsAtomicInline => IsBox && GetBoxType() == BoxType.kAtomicInline;

    /// <summary>
    /// True if this box is a block-in-inline.
    /// </summary>
    public bool IsBlockInInline => _isBlockInInline;

    /// <summary>
    /// True if this is a line fragment that has a block/float child in a parallel fragmentation flow.
    /// </summary>
    public bool IsLineForParallelFlow => _isLineForParallelFlow;

    /// <summary>
    /// True if this fragment is in-flow in an inline formatting context.
    /// </summary>
    public bool IsInline => IsInlineBox || IsAtomicInline;

    /// <summary>
    /// True if this is a floating box.
    /// </summary>
    public bool IsFloating => IsBox && GetBoxType() == BoxType.kFloating;

    /// <summary>
    /// True if this is out-of-flow positioned.
    /// </summary>
    public bool IsOutOfFlowPositioned => IsBox && GetBoxType() == BoxType.kOutOfFlowPositioned;

    /// <summary>
    /// True if this is fixed positioned.
    /// </summary>
    public bool IsFixedPositioned => IsCSSBox() && _layoutObject?.IsFixedPositioned() == true;

    /// <summary>
    /// True if this is floating or out-of-flow positioned.
    /// </summary>
    public bool IsFloatingOrOutOfFlowPositioned => IsFloating || IsOutOfFlowPositioned;

    /// <summary>
    /// True if this is positioned.
    /// </summary>
    public bool IsPositioned => GetLayoutObject()?.IsPositioned() ?? false;

    /// <summary>
    /// True if this has sticky constrained position.
    /// </summary>
    public bool HasStickyConstrainedPosition =>
        IsCSSBox() && _layoutObject?.StyleRef().HasStickyConstrainedPosition() == true;

    /// <summary>
    /// True if this is an initial letter box.
    /// </summary>
    public bool IsInitialLetterBox => IsCSSBox() && _layoutObject?.IsInitialLetterBox() == true;

    /// <summary>
    /// True if this is a snap area.
    /// </summary>
    public bool IsSnapArea()
    {
        if (!IsCSSBox() || !(_layoutObject is LayoutBox))
            return false;
        return _layoutObject.StyleRef().GetScrollSnapAlign() != ScrollSnapAlign.None;
    }

    /// <summary>
    /// True if this is a rendered legend.
    /// </summary>
    public bool IsRenderedLegend => IsBox && GetBoxType() == BoxType.kRenderedLegend;

    /// <summary>
    /// True if this is MathML.
    /// </summary>
    public bool IsMathML => IsBox && GetSelfOrContainerLayoutObject()?.IsMathML() == true;

    /// <summary>
    /// True if this is a MathML fraction.
    /// </summary>
    public bool IsMathMLFraction => IsBox && _isMathFraction;

    /// <summary>
    /// True if this is a MathML operator.
    /// </summary>
    public bool IsMathMLOperator => IsBox && _isMathOperator;

    /// <summary>
    /// Return true if this fragment corresponds directly to an entry in the CSS box tree.
    /// </summary>
    public bool IsCSSBox() => !IsLineBox && !IsFragmentainerBox;

    /// <summary>
    /// True if this is a block flow.
    /// </summary>
    public bool IsBlockFlow() => !IsLineBox && _layoutObject?.IsLayoutBlockFlow() == true;

    /// <summary>
    /// True if this is an anonymous block flow.
    /// </summary>
    public bool IsAnonymousBlockFlow() => IsCSSBox() && _layoutObject?.IsAnonymousBlockFlow() == true;

    /// <summary>
    /// True if this is a frameset.
    /// </summary>
    public bool IsFrameSet() => IsCSSBox() && _layoutObject?.IsFrameSet() == true;

    /// <summary>
    /// True if this is a list marker.
    /// </summary>
    public bool IsListMarker() => IsCSSBox() && _layoutObject?.IsLayoutOutsideListMarker() == true;

    /// <summary>
    /// True if this is SVG.
    /// </summary>
    public bool IsSvg() => _layoutObject?.IsSVG() == true;

    /// <summary>
    /// True if this is SVG text.
    /// </summary>
    public bool IsSvgText() => _layoutObject?.IsSVGText() == true;

    /// <summary>
    /// True if this is a table part.
    /// </summary>
    public bool IsTablePart => _isTablePart;

    /// <summary>
    /// True if this is a table.
    /// </summary>
    public bool IsTable() => IsTablePart && _layoutObject?.IsTable() == true;

    /// <summary>
    /// True if this is a table row.
    /// </summary>
    public bool IsTableRow() => IsTablePart && _layoutObject?.IsTableRow() == true;

    /// <summary>
    /// True if this is a table section.
    /// </summary>
    public bool IsTableSection() => IsTablePart && _layoutObject?.IsTableSection() == true;

    /// <summary>
    /// True if this is a table cell.
    /// </summary>
    public bool IsTableCell() => IsTablePart && _layoutObject?.IsTableCell() == true;

    /// <summary>
    /// True if this is a grid.
    /// </summary>
    public bool IsGrid() => _layoutObject?.IsLayoutGrid() == true;

    /// <summary>
    /// True if this is a text control container.
    /// </summary>
    public bool IsTextControlContainer() => IsCSSBox() && IsTextControlContainer(GetNode());

    /// <summary>
    /// True if this is a text control placeholder.
    /// </summary>
    public bool IsTextControlPlaceholder() => IsCSSBox() && IsTextControlPlaceholder(GetNode());

    /// <summary>
    /// True if this is a fieldset container.
    /// </summary>
    public bool IsFieldsetContainer => _isFieldsetContainer;

    /// <summary>
    /// Returns whether the fragment should be atomically painted.
    /// </summary>
    public bool IsPaintedAtomically => _isPaintedAtomically;

    /// <summary>
    /// Returns whether the fragment is a table part with collapsed borders.
    /// </summary>
    public bool HasCollapsedBorders => _hasCollapsedBorders;

    /// <summary>
    /// True if this is a formatting context root.
    /// </summary>
    public bool IsFormattingContextRoot() => IsBox && GetBoxType() >= BoxType.kMinimumFormattingContextRoot;

    /// <summary>
    /// True if we have a descendant potentially above our block-start edge.
    /// </summary>
    public bool MayHaveDescendantAboveBlockStart => _mayHaveDescendantAboveBlockStart;

    /// <summary>
    /// Returns the border-box size.
    /// </summary>
    public PhysicalSize Size => _size;

    /// <summary>
    /// Returns the rect in the local coordinate of this fragment.
    /// </summary>
    public PhysicalRect LocalRect => new PhysicalRect(PhysicalOffset.Zero, _size);

    /// <summary>
    /// Gets the style variant.
    /// </summary>
    public StyleVariant GetStyleVariant() => (StyleVariant)_styleVariant;

    /// <summary>
    /// True if this uses first line style.
    /// </summary>
    public bool UsesFirstLineStyle() => StyleVariantUtils.UsesFirstLineStyle(GetStyleVariant());

    /// <summary>
    /// Returns the style for this fragment.
    /// </summary>
    public ComputedStyle Style => _layoutObject?.EffectiveStyle(GetStyleVariant()) ?? throw new InvalidOperationException("No layout object");

    /// <summary>
    /// Gets the document.
    /// </summary>
    public Document GetDocument() => _layoutObject?.GetDocument() ?? throw new InvalidOperationException("No layout object");

    /// <summary>
    /// Gets the node for this fragment (only for CSS boxes).
    /// </summary>
    public Node? GetNode() => IsCSSBox() ? _layoutObject?.GetNode() : null;

    /// <summary>
    /// Gets the generating node for this fragment.
    /// </summary>
    public Node? GeneratingNode() => IsCSSBox() ? _layoutObject?.GeneratingNode() : null;

    /// <summary>
    /// The node to return when hit-testing on this fragment.
    /// </summary>
    public Node? NodeForHitTest() => IsFragmentainerBox ? null : _layoutObject?.NodeForHitTest();

    /// <summary>
    /// Gets the non-pseudo node.
    /// </summary>
    public Node? NonPseudoNode() => IsCSSBox() ? _layoutObject?.NonPseudoNode() : null;

    /// <summary>
    /// Determines if this fragment is in self hit testing phase.
    /// </summary>
    public bool IsInSelfHitTestingPhase(HitTestPhase phase)
    {
        if (IsFragmentainerBox)
            return false;

        if (GetLayoutObject() is LayoutBox box)
            return box.IsInSelfHitTestingPhase(phase);

        if (IsInlineBox)
            return phase == HitTestPhase.kForeground;

        return phase == HitTestPhase.kSelfBlockBackground;
    }

    /// <summary>
    /// Whether there is a PaintLayer associated with the fragment.
    /// </summary>
    public bool HasLayer() => IsCSSBox() && _layoutObject?.HasLayer() == true;

    /// <summary>
    /// The PaintLayer associated with the fragment.
    /// </summary>
    public PaintLayer? Layer()
    {
        if (!HasLayer())
            return null;
        return (_layoutObject as LayoutBoxModelObject)?.Layer();
    }

    /// <summary>
    /// Whether this object has a self-painting layer.
    /// </summary>
    public bool HasSelfPaintingLayer()
    {
        if (!HasLayer())
            return false;
        return (_layoutObject as LayoutBoxModelObject)?.HasSelfPaintingLayer() == true;
    }

    /// <summary>
    /// True if overflow != 'visible'.
    /// </summary>
    public bool HasNonVisibleOverflow() => IsCSSBox() && _layoutObject?.HasNonVisibleOverflow() == true;

    /// <summary>
    /// Gets the overflow clip axes.
    /// </summary>
    public OverflowClipAxes GetOverflowClipAxes() =>
        IsCSSBox() ? (_layoutObject?.GetOverflowClipAxes() ?? OverflowClipAxes.None) : OverflowClipAxes.None;

    /// <summary>
    /// True if this has non-visible block overflow.
    /// </summary>
    public bool HasNonVisibleBlockOverflow()
    {
        var clipAxes = GetOverflowClipAxes();
        if (Style.IsHorizontalWritingMode())
            return (clipAxes & OverflowClipAxes.Y) != 0;
        return (clipAxes & OverflowClipAxes.X) != 0;
    }

    /// <summary>
    /// True if this is considered a scroll-container.
    /// </summary>
    public bool IsScrollContainer() => IsCSSBox() && _layoutObject?.IsScrollContainer() == true;

    /// <summary>
    /// True if this is the effective root scroller.
    /// </summary>
    public bool IsEffectiveRootScroller() => IsCSSBox() && _layoutObject?.IsEffectiveRootScroller() == true;

    /// <summary>
    /// True if should apply layout containment.
    /// </summary>
    public bool ShouldApplyLayoutContainment() => IsCSSBox() && _layoutObject?.ShouldApplyLayoutContainment() == true;

    /// <summary>
    /// True if should clip overflow along either axis.
    /// </summary>
    public bool ShouldClipOverflowAlongEitherAxis() =>
        IsCSSBox() && _layoutObject?.ShouldClipOverflowAlongEitherAxis() == true;

    /// <summary>
    /// True if should clip overflow along both axes.
    /// </summary>
    public bool ShouldClipOverflowAlongBothAxis() =>
        IsCSSBox() && _layoutObject?.ShouldClipOverflowAlongBothAxis() == true;

    /// <summary>
    /// True if should apply overflow clip margin.
    /// </summary>
    public bool ShouldApplyOverflowClipMargin() =>
        IsCSSBox() && _layoutObject?.ShouldApplyOverflowClipMargin() == true;

    /// <summary>
    /// Return whether we can traverse this fragment and its children directly.
    /// </summary>
    public bool CanTraverse() => _layoutObject?.CanTraversePhysicalFragments() == true;

    /// <summary>
    /// This fragment is hidden for paint purpose.
    /// </summary>
    public bool IsHiddenForPaint() => _isHiddenForPaint || _layoutObject?.IsTruncated() == true;

    /// <summary>
    /// This fragment is opaque for layout and paint.
    /// </summary>
    public bool IsOpaque => _isOpaque;

    /// <summary>
    /// Return true if this fragment is monolithic.
    /// </summary>
    public bool IsMonolithic()
    {
        if (IsLineBox)
            return !IsBlockInInline;

        if (this is PhysicalBoxFragment boxFragment)
            return boxFragment.IsMonolithic();

        return false;
    }

    /// <summary>
    /// Returns true this fragment is used as the implicit anchor.
    /// </summary>
    public bool IsImplicitAnchor()
    {
        if (GetNode() is Element element)
            return element.HasImplicitlyAnchoredElement();
        return false;
    }

    /// <summary>
    /// Returns true if this is an explicit anchor.
    /// </summary>
    public bool IsExplicitAnchor() => IsCSSBox() && Style.AnchorName() != null;

    /// <summary>
    /// Returns true if this is an anchor.
    /// </summary>
    public bool IsAnchor() => IsExplicitAnchor() || IsImplicitAnchor();

    /// <summary>
    /// Gets the layout object (only for CSS boxes).
    /// </summary>
    public LayoutObject? GetLayoutObject() => IsCSSBox() ? _layoutObject : null;

    /// <summary>
    /// Gets the mutable layout object.
    /// </summary>
    public LayoutObject? GetMutableLayoutObject() => IsCSSBox() ? _layoutObject : null;

    /// <summary>
    /// Gets the layout object or container layout object.
    /// </summary>
    public LayoutObject? GetSelfOrContainerLayoutObject() => _layoutObject;

    /// <summary>
    /// Gets the fragment data.
    /// </summary>
    public FragmentData? GetFragmentData()
    {
        var box = GetLayoutObject() as LayoutBox;
        if (box == null)
            return null;
        return box.FragmentDataFromPhysicalFragment(this as PhysicalBoxFragment);
    }

    /// <summary>
    /// Checks if the layout object has been destroyed or moved.
    /// </summary>
    public bool IsLayoutObjectDestroyedOrMoved() => _layoutObject == null;

    /// <summary>
    /// Called when the layout object will be destroyed.
    /// </summary>
    public void LayoutObjectWillBeDestroyed()
    {
        _layoutObject = null;
    }

    /// <summary>
    /// Returns the latest generation of the post-layout fragment.
    /// </summary>
    public PhysicalFragment? PostLayout()
    {
        if (this is PhysicalBoxFragment box)
            return box.PostLayout();
        return this;
    }

    /// <summary>
    /// Converts a child physical rect to logical rect.
    /// </summary>
    public LogicalRect ConvertChildToLogical(PhysicalRect physicalRect)
    {
        return WritingModeConverter.ToLogical(Style.GetWritingDirection(), Size, physicalRect);
    }

    /// <summary>
    /// Gets the break token.
    /// </summary>
    public BreakToken? GetBreakToken() => _breakToken;

    /// <summary>
    /// Gets the children.
    /// </summary>
    public virtual IReadOnlyList<PhysicalFragmentLink> Children()
    {
        if (Type == FragmentType.kFragmentBox && this is PhysicalBoxFragment boxFragment)
            return boxFragment.Children();
        return Array.Empty<PhysicalFragmentLink>();
    }

    /// <summary>
    /// Gets the post-layout children.
    /// </summary>
    public PostLayoutChildLinkList PostLayoutChildren()
    {
        return new PostLayoutChildLinkList(Children());
    }

    /// <summary>
    /// Returns true if we have any floating descendants for paint.
    /// </summary>
    public bool HasFloatingDescendantsForPaint => _hasFloatingDescendantsForPaint;

    /// <summary>
    /// Returns true if we have any adjoining object descendants.
    /// </summary>
    public bool HasAdjoiningObjectDescendants => _hasAdjoiningObjectDescendants;

    /// <summary>
    /// Returns true if we depend on percentage block size.
    /// </summary>
    public bool DependsOnPercentageBlockSize => _dependsOnPercentageBlockSize;

    /// <summary>
    /// Sets children as invalid.
    /// </summary>
    public void SetChildrenInvalid()
    {
        if (!_childrenValid)
            return;

        foreach (var child in Children())
        {
            // Clear the fragment reference
            // TODO: Implement when PhysicalFragmentLink is mutable
        }
        _childrenValid = false;
    }

    /// <summary>
    /// Checks if children are valid.
    /// </summary>
    public bool ChildrenValid() => _childrenValid;

    /// <summary>
    /// Gets sticky descendants.
    /// </summary>
    public List<LayoutBoxModelObject>? StickyDescendants() => _propagatedData?.StickyDescendants;

    /// <summary>
    /// Gets propagated sticky descendants.
    /// </summary>
    public List<LayoutBoxModelObject>? PropagatedStickyDescendants() =>
        IsScrollContainer() ? null : StickyDescendants();

    /// <summary>
    /// Gets scroll initial target.
    /// </summary>
    public LayoutObject? ScrollInitialTarget() => _propagatedData?.ScrollInitialTarget;

    /// <summary>
    /// Gets propagated scroll initial target.
    /// </summary>
    public LayoutObject? PropagatedScrollInitialTarget() =>
        IsScrollContainer() ? null : ScrollInitialTarget();

    /// <summary>
    /// Gets snap areas.
    /// </summary>
    public List<Element>? SnapAreas() => _propagatedData?.SnapAreas;

    /// <summary>
    /// Gets propagated snap areas.
    /// </summary>
    public List<Element>? PropagatedSnapAreas() => IsScrollContainer() ? null : SnapAreas();

    /// <summary>
    /// Checks if has propagated layout objects.
    /// </summary>
    public bool HasPropagatedLayoutObjects() =>
        PropagatedStickyDescendants() != null ||
        PropagatedScrollInitialTarget() != null ||
        PropagatedSnapAreas() != null;

    /// <summary>
    /// Returns true if some child is OOF in the fragment tree.
    /// </summary>
    public bool HasOutOfFlowFragmentChild => _hasOutOfFlowFragmentChild;

    /// <summary>
    /// If there is an OOF contained within a fragmentation context.
    /// </summary>
    public bool HasOutOfFlowInFragmentainerSubtree => _hasOutOfFlowInFragmentainerSubtree;

    /// <summary>
    /// Checks if has out-of-flow positioned descendants.
    /// </summary>
    public bool HasOutOfFlowPositionedDescendants() =>
        _oofData != null && _oofData.OofPositionedDescendants.Count > 0;

    /// <summary>
    /// Gets out-of-flow positioned descendants.
    /// </summary>
    public IReadOnlyList<PhysicalOofPositionedNode> OutOfFlowPositionedDescendants()
    {
        if (!HasOutOfFlowPositionedDescendants())
            return Array.Empty<PhysicalOofPositionedNode>();
        return _oofData!.OofPositionedDescendants;
    }

    /// <summary>
    /// Checks if has anchor query.
    /// </summary>
    public bool HasAnchorQuery() =>
        _oofData != null && _oofData.AnchorQuery != null && !_oofData.AnchorQuery.IsEmpty();

    /// <summary>
    /// Checks if has anchor query to propagate.
    /// </summary>
    public bool HasAnchorQueryToPropagate() => HasAnchorQuery() || IsAnchor();

    /// <summary>
    /// Gets the anchor query.
    /// </summary>
    public PhysicalAnchorQuery? AnchorQuery() => HasAnchorQuery() ? _oofData!.AnchorQuery : null;

    /// <summary>
    /// Gets the fragmented OOF data.
    /// </summary>
    public FragmentedOofData? GetFragmentedOofData()
    {
        if (!_hasFragmentedOutOfFlowData)
            return null;
        return _oofData as FragmentedOofData;
    }

    /// <summary>
    /// Return true if there are nested multicol container descendants with OOFs.
    /// </summary>
    public bool HasNestedMulticolsWithOOFs()
    {
        var oofData = GetFragmentedOofData();
        return oofData?.HasNestedMulticolsWithOOFs() == true;
    }

    /// <summary>
    /// Figure out if the child has any out-of-flow positioned descendants.
    /// </summary>
    public bool NeedsOOFPositionedInfoPropagation() => _oofData != null;

    #endregion

    #region Methods

    /// <summary>
    /// Converts size to physical size based on writing mode.
    /// </summary>
    private static PhysicalSize ToPhysicalSize(LogicalSize logicalSize, WritingMode writingMode)
    {
        // TODO: Implement proper logical to physical conversion
        return new PhysicalSize(logicalSize.InlineSize, logicalSize.BlockSize);
    }

    /// <summary>
    /// Checks if a node is a text control container.
    /// </summary>
    private static bool IsTextControlContainer(Node? node)
    {
        // TODO: Implement text control container check
        return false;
    }

    /// <summary>
    /// Checks if a node is a text control placeholder.
    /// </summary>
    private static bool IsTextControlPlaceholder(Node? node)
    {
        // TODO: Implement text control placeholder check
        return false;
    }

    /// <summary>
    /// Creates OOF data from builder.
    /// </summary>
    private OofData? OofDataFromBuilder(FragmentBuilder builder)
    {
        // TODO: Implement OOF data creation from builder
        return null;
    }

    /// <summary>
    /// Clones OOF data.
    /// </summary>
    private OofData CloneOofData(OofData original)
    {
        if (!_hasFragmentedOutOfFlowData)
            return new OofData();

        // TODO: Implement proper cloning for FragmentedOofData
        return new OofData();
    }

    /// <summary>
    /// Clears OOF data.
    /// </summary>
    public void ClearOofData()
    {
        if (_oofData == null)
            return;

        if (HasAnchorQuery())
            _oofData.OofPositionedDescendants.Clear();
        else
            _oofData = null;
    }

    /// <summary>
    /// Checks if depends on percentage block size.
    /// </summary>
    private static bool DependsOnPercentageBlockSize(FragmentBuilder builder)
    {
        // TODO: Implement percentage block size dependency check
        return false;
    }

    /// <summary>
    /// Checks the fragment type (debug).
    /// </summary>
    public void CheckType()
    {
#if DEBUG
        // TODO: Implement type checking logic
#endif
    }

    /// <summary>
    /// Returns a string representation of this fragment.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("Type: '{0}' Size: '{1}'", Type, Size);

        if (Type == FragmentType.kFragmentBox)
        {
            sb.AppendFormat(", BoxType: '{0}'", GetBoxType());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Dumps the fragment tree for debugging.
    /// </summary>
    public string DumpFragmentTree(DumpFlags flags = DumpFlags.DumpAll,
        PhysicalFragment? target = null,
        PhysicalOffset? fragmentOffset = null,
        int indent = 2)
    {
        // TODO: Implement fragment tree dumping
        return ToString();
    }

    /// <summary>
    /// Traces this fragment for garbage collection.
    /// </summary>
    public virtual void Trace(Visitor visitor)
    {
        visitor.Trace(_layoutObject);
        visitor.Trace(_propagatedData);
        visitor.Trace(_breakToken);
        visitor.Trace(_oofData);
    }

    /// <summary>
    /// Adds outline rects for normal children.
    /// </summary>
    public void AddOutlineRectsForNormalChildren(
        OutlineRectCollector collector,
        PhysicalOffset additionalOffset,
        OutlineType outlineType,
        LayoutBoxModelObject? containingBlock)
    {
        // TODO: Implement outline rect collection
    }

    /// <summary>
    /// Adds outline rects for cursor.
    /// </summary>
    protected void AddOutlineRectsForCursor(
        OutlineRectCollector collector,
        PhysicalOffset additionalOffset,
        OutlineType outlineType,
        LayoutBoxModelObject? containingBlock,
        InlineCursor cursor)
    {
        // TODO: Implement outline rect collection for cursor
    }

    /// <summary>
    /// Adds outline rects for descendant.
    /// </summary>
    protected void AddOutlineRectsForDescendant(
        PhysicalFragmentLink descendant,
        OutlineRectCollector collector,
        PhysicalOffset additionalOffset,
        OutlineType outlineType,
        LayoutBoxModelObject? containingBlock)
    {
        // TODO: Implement outline rect collection for descendant
    }

    #endregion
}

/// <summary>
/// Represents a link to a child fragment with its offset.
/// </summary>
public class PhysicalFragmentLink
{
    public PhysicalFragment? Fragment { get; set; }
    public PhysicalOffset Offset { get; set; }
}