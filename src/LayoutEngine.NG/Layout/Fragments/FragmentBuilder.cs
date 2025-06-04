// Copyright 2017 The Chromium Authors
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

namespace LayoutEngine.NG.Layout.Fragments;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using LayoutEngine.NG.Layout;
using LayoutEngine.NG.Layout.Core;
using LayoutEngine.NG.Layout.Inputs;
using LayoutEngine.NG.Layout.Process;
using LayoutEngine.NG.Style;

/// <summary>
/// Builds physical fragments during layout.
/// Corresponds to Blink's FragmentBuilder.
/// </summary>
public class FragmentBuilder
{
    protected LayoutInputNode Node { get; }
    protected ConstraintSpace Space { get; }
    protected ComputedStyle Style { get; }
    protected WritingDirectionMode WritingDirection { get; }
    protected StyleVariant StyleVariant { get; set; }
    protected PhysicalFragment.BoxType BoxType { get; set; }
    protected LogicalSize CurrentSize { get; set; }
    protected LayoutObject LayoutObject { get; }

    /// <summary>
    /// The break token from the previous fragment, that serves as input now.
    /// </summary>
    protected BreakToken PreviousBreakToken { get; }

    /// <summary>
    /// The break token to store in the resulting fragment.
    /// </summary>
    protected BreakToken OutgoingBreakToken { get; set; }

    protected List<LayoutBoxModelObject> StickyDescendants { get; set; }
    protected List<IElement> SnapAreas { get; set; }

    /// <summary>
    /// The scroll start target.
    /// See: https://drafts.csswg.org/css-scroll-snap-2/#scroll-initial-target
    /// </summary>
    protected LayoutObject ScrollStartTarget { get; set; }
    protected PhysicalAnchorQuery CurrentAnchorQuery { get; set; }
    protected float BfcLineOffset { get; set; }
    protected float? BfcBlockOffset { get; set; }
    protected MarginStrut EndMarginStrut { get; set; }
    protected ExclusionSpace CurrentExclusionSpace { get; set; }
    protected int? LinesUntilClamp { get; set; }

    protected List<LogicalFragmentLink> Children { get; }
    protected List<LogicalFragmentLink> ChildrenWithSizeDependentPropagation { get; }

    protected FragmentItemsBuilder ItemsBuilder { get; set; }

    // Only used by the BoxFragmentBuilder subclass, but defined here to avoid
    // a virtual function call.
    protected List<BreakToken> ChildBreakTokens { get; }
    protected InlineBreakToken LastInlineBreakToken { get; set; }

    protected List<LogicalOofPositionedNode> OofPositionedCandidates { get; }
    protected List<LogicalOofNodeForFragmentation> OofPositionedFragmentainerDescendants { get; }
    protected List<LogicalOofPositionedNode> OofPositionedDescendants { get; }
    protected Dictionary<LayoutBox, MulticolWithPendingOofs<LogicalOffset>> MulticolsWithPendingOofs { get; }

    protected UnpositionedListMarker CurrentUnpositionedListMarker { get; set; }

    protected ColumnSpannerPath CurrentColumnSpannerPath { get; set; }

    protected EarlyBreak CurrentEarlyBreak { get; set; }

    /// <summary>
    /// The appeal of breaking inside this container.
    /// </summary>
    protected BreakAppeal CurrentBreakAppeal { get; set; } = BreakAppeal.Perfect;

    /// <summary>
    /// See LayoutResult.AnnotationOverflow().
    /// </summary>
    protected float AnnotationOverflow { get; set; }

    /// <summary>
    /// See LayoutResult.BlockEndAnnotationSpace().
    /// </summary>
    protected float BlockEndAnnotationSpace { get; set; }

    protected float MinimalSpaceShortage { get; set; } = LayoutUnit.Indefinite;
    protected float TallestUnbreakableBlockSize { get; set; } = LayoutUnit.Min;

    /// <summary>
    /// The number of line boxes or flex lines added to the builder. Only updated
    /// if we're performing block fragmentation.
    /// </summary>
    protected int LineCount { get; set; } = 0;

    protected AdjoiningObjectTypes CurrentAdjoiningObjectTypes { get; set; } = AdjoiningObjectTypes.None;
    protected bool HasAdjoiningObjectDescendants { get; set; } = false;
    protected bool IsSelfCollapsing { get; set; } = false;
    protected bool IsPushedByFloats { get; set; } = false;
    protected bool SubtreeModifiedMarginStrut { get; set; } = false;
    protected bool IsNewFc { get; set; } = false;
    protected bool IsBlockInInline { get; set; } = false;
    protected bool IsLineForParallelFlow { get; set; } = false;
    protected bool HasFloatingDescendantsForPaint { get; set; } = false;
    protected bool HasDescendantThatDependsOnPercentageBlockSize { get; set; } = false;
    protected bool HasOrthogonalFallbackSizeDescendant { get; set; } = false;
    protected bool MayHaveDescendantAboveBlockStart { get; set; } = false;
    protected bool HasBlockFragmentation { get; set; } = false;
    protected bool IsFragmentationContextRoot { get; set; } = false;
    protected bool IsHiddenForPaint { get; set; }
    protected bool IsOpaque { get; set; } = false;
    protected bool HasCollapsedBorders { get; set; } = false;
    protected bool HasColumnSpanner { get; set; } = false;
    protected bool IsEmptySpannerParent { get; set; } = false;
    protected bool ShouldForceSameFragmentationFlow { get; set; } = false;
    protected bool RequiresContentBeforeBreaking { get; set; } = false;
    protected bool HasOutOfFlowFragmentChild { get; set; } = false;
    protected bool HasOutOfFlowInFragmentainerSubtree { get; set; } = false;
    protected bool IsBlockEndTrimmableLine { get; set; } = false;
    protected bool WouldBeLastLineIfNotForEllipsis { get; set; } = false;
    protected bool HasFinalSize { get; private set; } = false;

    protected bool OofCandidatesMayHaveAnchorQueries { get; set; } = false;
    protected bool OofFragmentainerDescendantsMayHaveAnchorQueries { get; set; } = false;

#if DEBUG
    protected bool IsMayHaveDescendantAboveBlockStartExplicitlySet { get; set; } = false;
    private bool _isFinalized = false;
#endif

    protected FragmentBuilder(
        LayoutInputNode node,
        ComputedStyle style,
        ConstraintSpace space,
        WritingDirectionMode writingDirection,
        BreakToken previousBreakToken)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Style = style ?? throw new ArgumentNullException(nameof(style));
        Space = space ?? throw new ArgumentNullException(nameof(space));
        WritingDirection = writingDirection; // struct copy
        PreviousBreakToken = previousBreakToken;

        LayoutObject = node.GetLayoutBox();
        StyleVariant = StyleVariant.Standard;
        BoxType = PhysicalFragment.BoxType.NormalBox;
        IsHiddenForPaint = space.IsHiddenForPaint();

        Children = new List<LogicalFragmentLink>();
        ChildrenWithSizeDependentPropagation = new List<LogicalFragmentLink>();
        ChildBreakTokens = new List<BreakToken>();
        OofPositionedCandidates = new List<LogicalOofPositionedNode>();
        OofPositionedFragmentainerDescendants = new List<LogicalOofNodeForFragmentation>();
        OofPositionedDescendants = new List<LogicalOofPositionedNode>();
        MulticolsWithPendingOofs = new Dictionary<LayoutBox, MulticolWithPendingOofs<LogicalOffset>>();
    }

    public WritingMode GetWritingMode() => WritingDirection.GetWritingMode();
    public TextDirection Direction() => WritingDirection.Direction;

    /// <summary>
    /// Return true if this is a builder for the root fragment.
    /// </summary>
    public bool IsRoot() => Node != null && Node.IsView() && !Space.IsAnonymous();

    /// <summary>
    /// Return true if this is a builder for the root fragment, and the root is
    /// paginated.
    /// </summary>
    public bool IsPaginatedRoot() => IsRoot() && Node.IsPaginatedRoot();

    /// <summary>
    /// Either this function or SetBoxType must be called before ToBoxFragment().
    /// </summary>
    public void SetIsNewFormattingContext(bool isNewFc) => IsNewFc = isNewFc;

    public PhysicalFragment.BoxType GetBoxType()
    {
        if (BoxType != PhysicalFragment.BoxType.NormalBox)
        {
            return BoxType;
        }

        // When implicit, compute from LayoutObject.
        Debug.Assert(LayoutObject != null);
        if (LayoutObject.IsFloating())
        {
            return PhysicalFragment.BoxType.Floating;
        }
        if (LayoutObject.IsOutOfFlowPositioned())
        {
            return PhysicalFragment.BoxType.OutOfFlowPositioned;
        }
        if (LayoutObject.IsRenderedLegend())
        {
            return PhysicalFragment.BoxType.RenderedLegend;
        }
        if (LayoutObject.StyleRef().IsPageMarginBox()) // TODO: Implement IsPageMarginBox
        {
            return PhysicalFragment.BoxType.PageMargin;
        }
        if (LayoutObject.IsInline())
        {
            // Check IsAtomicInlineLevel() after IsInline() because LayoutReplaced
            // sets IsAtomicInlineLevel() even when it's block-level. crbug.com/567964
            if (LayoutObject.IsAtomicInlineLevel())
            {
                return PhysicalFragment.BoxType.AtomicInline;
            }
            return PhysicalFragment.BoxType.InlineBox;
        }
        Debug.Assert(Node != null, "Must call SetBoxType if there is no node");
        Debug.Assert(IsNewFc == Node.CreatesNewFormattingContext(),
            "Forgot to call builder.SetIsNewFormattingContext");
        if (IsNewFc)
        {
            return PhysicalFragment.BoxType.BlockFlowRoot;
        }
        return PhysicalFragment.BoxType.NormalBox;
    }

    public bool IsFragmentainerBoxType()
    {
        PhysicalFragment.BoxType boxType = GetBoxType();
        return boxType == PhysicalFragment.BoxType.ColumnBox ||
               boxType == PhysicalFragment.BoxType.PageArea;
    }

    public float InlineSize() => CurrentSize.InlineSize;
    public float BlockSize()
    {
        Debug.Assert(CurrentSize.BlockSize != LayoutUnit.Indefinite);
        return CurrentSize.BlockSize;
    }
    public LogicalSize Size()
    {
        Debug.Assert(CurrentSize.BlockSize != LayoutUnit.Indefinite);
        return CurrentSize;
    }
    public void SetBlockSize(float blockSize)
    {
        var size = CurrentSize;
        size.BlockSize = blockSize;
        CurrentSize = size;
    }

    public bool HasBlockSize() => CurrentSize.BlockSize != LayoutUnit.Indefinite;

    public void SetIsHiddenForPaint(bool value) => IsHiddenForPaint = value;
    public void SetIsOpaque() => IsOpaque = true;

    public void SetHasCollapsedBorders(bool value) => HasCollapsedBorders = value;

    public void ResetBfcBlockOffset() => BfcBlockOffset = null;

    public void SetMayHaveDescendantAboveBlockStart(bool b)
    {
#if DEBUG
        IsMayHaveDescendantAboveBlockStartExplicitlySet = true;
#endif
        MayHaveDescendantAboveBlockStart = b;
    }

    public void ClearUnpositionedListMarker()
    {
        CurrentUnpositionedListMarker = default; // Or new UnpositionedListMarker();
    }

    public void ReplaceChild(int index, PhysicalFragment newChild, LogicalOffset offset)
    {
        Debug.Assert(index < Children.Count);
        Children[index] = new LogicalFragmentLink(newChild, offset);
    }

    protected List<LayoutBoxModelObject> EnsureStickyDescendants()
    {
        if (StickyDescendants == null)
        {
            StickyDescendants = new List<LayoutBoxModelObject>();
        }
        return StickyDescendants;
    }

    public void PropagateStickyDescendants(PhysicalFragment child)
    {
        if (child.HasStickyConstrainedPosition()) // TODO: Implement HasStickyConstrainedPosition
        {
            EnsureStickyDescendants().Insert(0, (LayoutBoxModelObject)child.GetMutableLayoutObject());
        }

        var childStickyDescendants = child.PropagatedStickyDescendants();
        if (childStickyDescendants != null)
        {
            EnsureStickyDescendants().AddRange(childStickyDescendants);
        }
    }

    protected List<IElement> EnsureSnapAreas()
    {
        if (SnapAreas == null)
        {
            SnapAreas = new List<IElement>();
        }
        return SnapAreas;
    }

    private static bool IsInlineContainerForNode(BlockNode node, LayoutObject inlineContainer)
    {
        return inlineContainer != null && inlineContainer.IsLayoutInline() &&
               inlineContainer.CanContainOutOfFlowPositionedElement(node.Style.GetPosition());
    }

    private static PhysicalAnchorQuery.SetOptions AnchorQuerySetOptions(
        PhysicalFragment fragment,
        LayoutInputNode container,
        bool maybeOutOfOrderIfOof)
    {
        if (!fragment.IsOutOfFlowPositioned())
        {
            return PhysicalAnchorQuery.SetOptions.InFlow;
        }

        Debug.Assert(fragment.GetLayoutObject() != null);
        if (!maybeOutOfOrderIfOof)
        {
            return PhysicalAnchorQuery.SetOptions.OutOfFlow;
        }

        if (container.GetLayoutBox() == null)
        {
            return PhysicalAnchorQuery.SetOptions.OutOfFlow;
        }

        LayoutObject layoutObject = fragment.GetLayoutObject();
        LayoutObject containingBlock = layoutObject.Container(); // TODO: Implement Container()
        Debug.Assert(containingBlock != null);
        if (containingBlock == container.GetLayoutBox())
        {
            return PhysicalAnchorQuery.SetOptions.OutOfFlow;
        }
        return PhysicalAnchorQuery.SetOptions.InFlow;
    }


    public void PropagateSnapAreas(PhysicalFragment child)
    {
        // Simplified C# translation
        // TODO: Full translation of PropagateSnapAreas logic from .cc file
        // This requires LayoutBox.IsBeforeInPreOrder and Document.CountUse
        // and correct handling of ColumnPseudoElement.

        if (child.IsSnapArea()) // TODO: Implement IsSnapArea
        {
            var childBoxFragment = child as PhysicalBoxFragment;
            if (childBoxFragment == null || childBoxFragment.GetBreakToken() == null)
            {
                var snapAreaNode = child.GetLayoutObject()?.GetNode();
                if (snapAreaNode is IElement snapAreaElement)
                {
                    // Simplified insertion
                    EnsureSnapAreas().Add(snapAreaElement);
                }
            }
        }

        var childSnapAreas = child.PropagatedSnapAreas();
        if (childSnapAreas != null)
        {
            // Simplified insertion
            EnsureSnapAreas().AddRange(childSnapAreas);
        }

        if (child.IsSnapArea() && child.PropagatedSnapAreas() != null)
        {
            // child.GetDocument().CountUse(WebFeature.kScrollSnapNestedSnapAreas); // TODO
        }
    }


    public void AddSnapAreaForColumn(ColumnPseudoElement columnPseudo)
    {
        EnsureSnapAreas().Add(columnPseudo); // Assuming ColumnPseudoElement : IElement
    }

    protected PhysicalAnchorQuery EnsureAnchorQuery()
    {
        if (CurrentAnchorQuery == null)
            CurrentAnchorQuery = new PhysicalAnchorQuery();
        return CurrentAnchorQuery;
    }

    public void PropagateChildAnchors(PhysicalFragment child, LogicalOffset childOffset)
    {
        // TODO: Full translation of PropagateChildAnchors logic from .cc file
        // This requires WritingModeConverter, ScopedCSSName, AnchorScopedName, etc.
        // For now, a simplified stub.

        PhysicalAnchorQuery.SetOptions? options = null;
        IElement context = null;
        var node = child.GetNode();
        if (node is IElement element)
        {
            // TODO: DisplayLockContext logic
            // if (element.GetDisplayLockContext()?.IsLocked() == true) return;
            context = element;
        }

        if (child.IsAnchor()) // TODO: Implement IsAnchor
        {
            Debug.Assert(child.GetLayoutObject() != null);
            var logicalRect = new LogicalRect(childOffset,
                WritingModeConverter.ToLogicalSize(child.Size, GetWritingMode(), CurrentSize)); // Placeholder for converter
            // PhysicalRect rect = converter.ToPhysical(logicalRect); // Placeholder

            options = AnchorQuerySetOptions(child, Node, IsBlockFragmentationContextRoot() || HasItems());

            if (child.IsExplicitAnchor()) // TODO: Implement IsExplicitAnchor
            {
                // foreach (ScopedCSSName name in child.Style.AnchorName.GetNames()) { // TODO
                //    AnchorScopedName anchorScopedName = ToAnchorScopedName(name, child.GetLayoutObject()); // TODO
                //    EnsureAnchorQuery().Set(anchorScopedName, child.GetLayoutObject(), rect, options.Value, context); // TODO
                // }
            }
            if (child.IsImplicitAnchor()) // TODO: Implement IsImplicitAnchor
            {
                // EnsureAnchorQuery().Set((IElement)child.GetNode(), child.GetLayoutObject(), rect, options.Value, context); // TODO
            }
        }

        PhysicalAnchorQuery childAnchorQuery = child.AnchorQuery();
        if (childAnchorQuery != null)
        {
            if (options == null)
            {
                options = AnchorQuerySetOptions(child, Node, IsBlockFragmentationContextRoot() || HasItems());
            }
            // WritingModeConverter converter = new WritingModeConverter(GetWritingDirection(), Size()); // Placeholder
            // PhysicalOffset additionalOffset = converter.ToPhysical(childOffset, child.Size()); // Placeholder
            // EnsureAnchorQuery().SetFromChild(childAnchorQuery, additionalOffset, options.Value, context); // TODO
        }
    }


    public void AddOutOfFlowChildCandidate(
        BlockNode child,
        LogicalOffset childOffset,
        LogicalStaticPosition.InlineEdge inlineEdge = LogicalStaticPosition.InlineEdge.InlineStart,
        LogicalStaticPosition.BlockEdge blockEdge = LogicalStaticPosition.BlockEdge.BlockStart,
        LogicalStaticPosition.LogicalAlignmentDirection alignSelfDirection =
            LogicalStaticPosition.LogicalAlignmentDirection.Block,
        bool allowTopLayerNodes = false)
    {
        Debug.Assert(child != null);
        if (child.IsInTopOrViewTransitionLayer() && !allowTopLayerNodes)
        {
            return;
        }

        OofCandidatesMayHaveAnchorQueries |= child.MayHaveAnchorQuery();
        OofPositionedCandidates.Add(new LogicalOofPositionedNode(
            child,
            new LogicalStaticPosition(childOffset, inlineEdge, blockEdge, alignSelfDirection),
            RequiresContentBeforeBreaking()
        ));
    }

    public void AddOutOfFlowInlineChildCandidate(
        BlockNode child,
        LogicalOffset childOffset,
        WritingDirectionMode inlineContainerWritingDirection,
        float lineBoxBlockSize)
    {
        Debug.Assert(Node.IsInline() || (LayoutObject != null && LayoutObject.IsLayoutInline()));

        LogicalOffset staticOffset = childOffset;

        var inlineAxisEdge = BlockLayoutAlgorithmUtils.InlineStaticPositionEdge( // TODO: Move/implement BlockLayoutAlgorithmUtils
            child, null, // justify_items_style
            inlineContainerWritingDirection,
            !TextUtil.IsLtr(inlineContainerWritingDirection.Direction)); // TODO: Implement TextUtil.IsLtr
        var blockAxisEdge = BlockLayoutAlgorithmUtils.BlockStaticPositionEdge( // TODO: Move/implement BlockLayoutAlgorithmUtils
            child, null, // align_items_style
            inlineContainerWritingDirection);

        switch (blockAxisEdge)
        {
            case LogicalStaticPosition.BlockEdge.BlockCenter:
                staticOffset.BlockOffset += lineBoxBlockSize / 2;
                break;
            case LogicalStaticPosition.BlockEdge.BlockEnd:
                staticOffset.BlockOffset += lineBoxBlockSize;
                break;
            case LogicalStaticPosition.BlockEdge.BlockStart:
                // Static position is already correct.
                break;
        }

        AddOutOfFlowChildCandidate(child, staticOffset, inlineAxisEdge, blockAxisEdge);
    }

    public void AddOutOfFlowFragmentainerDescendant(LogicalOofNodeForFragmentation descendant)
    {
        OofFragmentainerDescendantsMayHaveAnchorQueries |= descendant.Box.MayHaveAnchorQuery();
        OofPositionedFragmentainerDescendants.Add(descendant);
    }

    public void AddOutOfFlowFragmentainerDescendant(LogicalOofPositionedNode descendant)
    {
        Debug.Assert(!descendant.IsForFragmentation); // This property needs to be added to LogicalOofPositionedNode
        var fragmentainerDescendant = new LogicalOofNodeForFragmentation(descendant);
        AddOutOfFlowFragmentainerDescendant(fragmentainerDescendant);
    }


    public void AddOutOfFlowDescendant(LogicalOofPositionedNode descendant)
    {
        OofPositionedDescendants.Add(descendant);
    }

    public void SwapOutOfFlowPositionedCandidates(List<LogicalOofPositionedNode> candidates)
    {
        Debug.Assert(candidates.Count == 0);
        if (OofCandidatesMayHaveAnchorQueries)
        {
            OofPositionedCandidates.Sort((a, b) =>
                    a.Box.IsBeforeInPreOrder(b.Box) ? -1 : 1 // TODO: Implement IsBeforeInPreOrder
            );
            OofCandidatesMayHaveAnchorQueries = false;
        }
        // Swap logic
        candidates.AddRange(OofPositionedCandidates);
        OofPositionedCandidates.Clear();
    }

    public void ClearOutOfFlowPositionedCandidates()
    {
        OofCandidatesMayHaveAnchorQueries = false;
        OofPositionedCandidates.Clear();
    }

    public void AddMulticolWithPendingOOFs(
        BlockNode multicol,
        MulticolWithPendingOofs<LogicalOffset> multicolInfo = null)
    {
        if (multicolInfo == null)
            multicolInfo = new MulticolWithPendingOofs<LogicalOffset>();

        Debug.Assert(multicol.GetLayoutBox().IsMulticolContainer()); // TODO: Implement IsMulticolContainer
        if (MulticolsWithPendingOofs.ContainsKey(multicol.GetLayoutBox()))
            return;
        MulticolsWithPendingOofs.Add(multicol.GetLayoutBox(), multicolInfo);
    }

    public void SwapMulticolsWithPendingOOFs(
        Dictionary<LayoutBox, MulticolWithPendingOofs<LogicalOffset>> multicolsWithPendingOofs)
    {
        Debug.Assert(multicolsWithPendingOofs.Count == 0);
        // Swap logic
        foreach (var entry in MulticolsWithPendingOofs)
        {
            multicolsWithPendingOofs.Add(entry.Key, entry.Value);
        }
        MulticolsWithPendingOofs.Clear();
    }

    public void SwapOutOfFlowFragmentainerDescendants(
        List<LogicalOofNodeForFragmentation> descendants)
    {
        Debug.Assert(descendants.Count == 0);
        if (OofFragmentainerDescendantsMayHaveAnchorQueries)
        {
            OofPositionedFragmentainerDescendants.Sort((a, b) =>
                    a.Box.IsBeforeInPreOrder(b.Box) ? -1 : 1 // TODO: Implement IsBeforeInPreOrder
            );
            OofFragmentainerDescendantsMayHaveAnchorQueries = false;
        }
        // Swap logic
        descendants.AddRange(OofPositionedFragmentainerDescendants);
        OofPositionedFragmentainerDescendants.Clear();
    }

    public void TransferOutOfFlowCandidates(
        FragmentBuilder destinationBuilder,
        LogicalOffset additionalOffset,
        MulticolWithPendingOofs<LogicalOffset> multicol = null)
    {
        foreach (var candidate in OofPositionedCandidates)
        {
            BlockNode node = candidate.Node;
            var updatedCandidate = candidate; // struct copy
            updatedCandidate.StaticPosition.Offset += additionalOffset;

            if (multicol != null && multicol.FixedPosContainingBlock.Fragment != null &&
                node.Style.GetPosition() == PositionMode.Fixed)
            {
                // TODO: Implement fixedpos_containing_block logic from C++
                // This involves creating a LogicalOofNodeForFragmentation
                // destinationBuilder.AddOutOfFlowFragmentainerDescendant(...);
                continue;
            }
            destinationBuilder.OofPositionedCandidates.Add(updatedCandidate);
        }
        destinationBuilder.OofCandidatesMayHaveAnchorQueries |= OofCandidatesMayHaveAnchorQueries;
        ClearOutOfFlowPositionedCandidates();
    }


    public bool HasOutOfFlowPositionedCandidates() => OofPositionedCandidates.Any();
    public bool HasOutOfFlowPositionedDescendants() => OofPositionedDescendants.Any();
    public bool HasOutOfFlowFragmentainerDescendants() => OofPositionedFragmentainerDescendants.Any();
    public bool HasMulticolsWithPendingOOFs() => MulticolsWithPendingOofs.Any();

    public void MoveOutOfFlowDescendantCandidatesToDescendants()
    {
        Debug.Assert(!OofPositionedDescendants.Any());
        // Swap
        OofPositionedDescendants.AddRange(OofPositionedCandidates);
        OofPositionedCandidates.Clear();

        if (LayoutObject == null || !LayoutObject.IsInline())
            return;

        for (int i = 0; i < OofPositionedDescendants.Count; i++)
        {
            var candidate = OofPositionedDescendants[i];
            if (candidate.InlineContainer.Container == null &&
                IsInlineContainerForNode(candidate.Node, LayoutObject))
            {
                candidate.InlineContainer = new OofInlineContainer<LogicalOffset>(
                    (LayoutInline)LayoutObject,
                    LogicalOffset.Zero
                );
                OofPositionedDescendants[i] = candidate; // Update if struct
            }
        }
    }

    public float BlockOffsetAdjustmentForFragmentainer(float fragmentainerConsumedBlockSize = 0f)
    {
        if (IsFragmentainerBoxType() && PreviousBreakToken != null)
        {
            return ((BlockBreakToken)PreviousBreakToken).ConsumedBlockSize; // TODO: Add ConsumedBlockSize
        }
        return fragmentainerConsumedBlockSize;
    }

    public void PropagateOOFPositionedInfo(
        PhysicalFragment fragment,
        LogicalOffset offset,
        LogicalOffset relativeOffset,
        LogicalOffset? offsetAdjustment = null,
        OofInlineContainer<LogicalOffset>? inlineContainer = null,
        float containingBlockAdjustment = 0f,
        OofContainingBlock<LogicalOffset>? containingBlock = null,
        OofContainingBlock<LogicalOffset>? fixedPosContainingBlock = null,
        OofInlineContainer<LogicalOffset>? fixedPosInlineContainer = null,
        LogicalOffset? additionalFixedPosOffset = null)
    {
        // TODO: Full translation of PropagateOOFPositionedInfo logic from .cc file
        // This is a complex method involving WritingModeConverter and detailed logic for OOF propagation.
        // For now, a simplified stub.
        Debug.Assert(fragment.NeedsOOFPositionedInfoPropagation()); // TODO: Implement NeedsOOFPositionedInfoPropagation

        LogicalOffset currentOffsetAdjustment = offsetAdjustment ?? LogicalOffset.Zero;
        LogicalOffset adjustedOffset = offset + currentOffsetAdjustment + relativeOffset;

        // ... (rest of the complex logic)
        throw new NotImplementedException();
    }

    public void PropagateOOFFragmentainerDescendants(
        PhysicalFragment fragment,
        LogicalOffset offset,
        LogicalOffset relativeOffset,
        float containingBlockAdjustment,
        OofContainingBlock<LogicalOffset>? containingBlock,
        OofContainingBlock<LogicalOffset>? fixedPosContainingBlock,
        List<LogicalOofNodeForFragmentation> outList = null)
    {
        // TODO: Full translation of PropagateOOFFragmentainerDescendants logic from .cc file
        throw new NotImplementedException();
    }

    public void AdjustFixedposContainerInfo(
        PhysicalFragment boxFragment,
        LogicalOffset relativeOffset,
        ref OofInlineContainer<LogicalOffset> fixedPosInlineContainer,
        ref PhysicalFragment fixedPosContainingBlockFragment,
        OofInlineContainer<LogicalOffset>? currentInlineContainer = null)
    {
        // TODO: Full translation of AdjustFixedposContainerInfo logic from .cc file
        throw new NotImplementedException();
    }


    public void SetIsSelfCollapsing() => IsSelfCollapsing = true;
    public void SetIsPushedByFloats() => IsPushedByFloats = true;

    public void SetSubtreeModifiedMarginStrut()
    {
        Debug.Assert(BfcBlockOffset == null);
        SubtreeModifiedMarginStrut = true;
    }

    public void ResetAdjoiningObjectTypes()
    {
        CurrentAdjoiningObjectTypes = AdjoiningObjectTypes.None;
        HasAdjoiningObjectDescendants = false;
    }
    public void AddAdjoiningObjectTypes(AdjoiningObjectTypes adjoiningObjectTypes)
    {
        CurrentAdjoiningObjectTypes |= adjoiningObjectTypes;
        HasAdjoiningObjectDescendants |= (adjoiningObjectTypes != AdjoiningObjectTypes.None);
    }
    public void SetAdjoiningObjectTypes(AdjoiningObjectTypes adjoiningObjectTypes)
    {
        CurrentAdjoiningObjectTypes = adjoiningObjectTypes;
    }
    public void SetHasAdjoiningObjectDescendants(bool hasAdjoiningObjectDescendants)
    {
        HasAdjoiningObjectDescendants = hasAdjoiningObjectDescendants;
    }

    public void SetIsBlockInInline() => IsBlockInInline = true;
    public void SetIsLineForParallelFlow() => IsLineForParallelFlow = true;
    public void SetHasBlockFragmentation() => HasBlockFragmentation = true;
    public void SetIsBlockFragmentationContextRoot() => IsFragmentationContextRoot = true;

    public void SetHasColumnSpanner(bool hasColumnSpanner) => HasColumnSpanner = hasColumnSpanner;
    public void SetColumnSpannerPath(ColumnSpannerPath spannerPath)
    {
        CurrentColumnSpannerPath = spannerPath;
        SetHasColumnSpanner(spannerPath != null);
    }
    public bool FoundColumnSpanner()
    {
        Debug.Assert(HasColumnSpanner || CurrentColumnSpannerPath == null);
        return HasColumnSpanner;
    }
    public void SetIsEmptySpannerParent(bool isEmptySpannerParent)
    {
        Debug.Assert(FoundColumnSpanner());
        IsEmptySpannerParent = isEmptySpannerParent;
    }

    public void SetShouldForceSameFragmentationFlow() => ShouldForceSameFragmentationFlow = true;

    public void SetRequiresContentBeforeBreaking(bool b) => RequiresContentBeforeBreaking = b;

    public void ClampBreakAppeal(BreakAppeal appeal)
    {
        CurrentBreakAppeal = (BreakAppeal)Math.Min((int)CurrentBreakAppeal, (int)appeal);
    }

    public void SetHasDescendantThatDependsOnPercentageBlockSize(bool b = true)
    {
        HasDescendantThatDependsOnPercentageBlockSize = b;
    }

    public void SetAnnotationOverflow(float overflow) => AnnotationOverflow = overflow;
    public void SetBlockEndAnnotationSpace(float space) => BlockEndAnnotationSpace = space;

    public void PropagateSpaceShortage(float? spaceShortage)
    {
        Debug.Assert(!IsInitialColumnBalancingPass());
        LayoutResult.UpdateMinimalSpaceShortage(spaceShortage, ref MinimalSpaceShortage); // TODO: Make UpdateMinimalSpaceShortage accessible
    }

    public float? GetMinimalSpaceShortage()
    {
        if (MinimalSpaceShortage == LayoutUnit.Indefinite)
        {
            return null;
        }
        return MinimalSpaceShortage;
    }

    public void PropagateTallestUnbreakableBlockSize(float unbreakableBlockSize)
    {
        Debug.Assert(IsInitialColumnBalancingPass());
        TallestUnbreakableBlockSize = Math.Max(TallestUnbreakableBlockSize, unbreakableBlockSize);
    }

    public void SetIsInitialColumnBalancingPass()
    {
        Debug.Assert(TallestUnbreakableBlockSize == LayoutUnit.Min);
        TallestUnbreakableBlockSize = 0f;
    }
    public bool IsInitialColumnBalancingPass() => TallestUnbreakableBlockSize >= 0f;

    public void FinalizeLayout()
    {
#if DEBUG
        Debug.Assert(!_isFinalized);
        _isFinalized = true;
#endif
        HasFinalSize = true;
        PropagateSizeDependentData();
    }

    public LayoutResult Abort(LayoutResult.LayoutStatus status)
    {
        return new LayoutResult(status, this); // TODO: Adjust LayoutResult constructor or pass key
    }

    protected void PropagateFromLayoutResultAndFragment(
        LayoutResult childResult,
        LogicalOffset childOffset,
        LogicalOffset relativeOffset,
        OofInlineContainer<LogicalOffset>? inlineContainer = null)
    {
        PropagateFromLayoutResult(childResult);
        PropagateFromFragment(childResult.GetPhysicalFragment(), childOffset, relativeOffset, inlineContainer);
    }

    protected void PropagateFromLayoutResult(LayoutResult childResult)
    {
        HasOrthogonalFallbackSizeDescendant |=
            childResult.HasOrthogonalFallbackInlineSize() || // TODO: Implement HasOrthogonalFallbackInlineSize
            childResult.HasOrthogonalFallbackSizeDescendant(); // TODO: Implement HasOrthogonalFallbackSizeDescendant
    }

    protected void UpdateScrollInitialTarget(LayoutObject newTarget)
    {
        if (newTarget != ScrollStartTarget &&
            (ScrollStartTarget == null || newTarget.IsBeforeInPreOrder(ScrollStartTarget))) // TODO: Implement IsBeforeInPreOrder
        {
            ScrollStartTarget = newTarget;
        }
    }

    protected void PropagateScrollInitialTarget(PhysicalFragment child)
    {
        // TODO: Implement Style.ScrollInitialTarget, EScrollInitialTarget
        // if (child.Style.ScrollInitialTarget() != EScrollInitialTarget.None)
        // {
        //     LayoutObject childObject = child.GetMutableLayoutObject();
        //     if (childObject != null)
        //     {
        //         UpdateScrollInitialTarget(childObject);
        //     }
        // }

        LayoutObject target = child.PropagatedScrollInitialTarget();
        if (target != null)
        {
            UpdateScrollInitialTarget(target);
        }
    }

    protected void PropagateFromFragment(
        PhysicalFragment child,
        LogicalOffset childOffset,
        LogicalOffset relativeOffset,
        OofInlineContainer<LogicalOffset>? inlineContainer = null)
    {
        // TODO: Full translation of PropagateFromFragment logic from .cc file
        // This is a complex method. For now, a simplified stub.

        if (GetBoxType() == PhysicalFragment.BoxType.PageBorderBox)
        {
            Debug.Assert(child.GetBoxType() == PhysicalFragment.BoxType.PageArea);
            return;
        }

        if (child.HasAnchorQueryToPropagate()) // TODO: Implement HasAnchorQueryToPropagate
        {
            LogicalOffset totalOffset = childOffset + relativeOffset;
            if (HasFinalSize)
            {
                PropagateChildAnchors(child, totalOffset);
            }
            else
            {
                ChildrenWithSizeDependentPropagation.Add(new LogicalFragmentLink(child, totalOffset));
            }
        }

        PropagateStickyDescendants(child);
        PropagateSnapAreas(child);
        PropagateScrollInitialTarget(child);

        // ... (rest of the complex logic from C++ .cc file)
    }

    protected void AddChildInternal(PhysicalFragment child, LogicalOffset childOffset)
    {
        if (child.IsListMarker())
        {
            Children.Insert(0, new LogicalFragmentLink(child, childOffset));
            return;
        }

        if (child.IsTextControlPlaceholder()) // TODO: Implement IsTextControlPlaceholder
        {
            int size = Children.Count;
            if (size > 0)
            {
                Children.Insert(size - 1, new LogicalFragmentLink(child, childOffset));
                return;
            }
        }
        Children.Add(new LogicalFragmentLink(child, childOffset));
    }

    protected void PropagateSizeDependentData()
    {
        Debug.Assert(HasFinalSize);
        foreach (var link in ChildrenWithSizeDependentPropagation)
        {
            PropagateChildAnchors(link.Fragment, link.Offset);
        }
        ChildrenWithSizeDependentPropagation.Clear();
    }


#if DEBUG
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendFormat("FragmentBuilder {0:F2}x{1:F2}, Children {2}\n",
            InlineSize(), BlockSize(), Children.Count);
        foreach (var child in Children)
        {
            builder.Append(child.Fragment.DumpFragmentTree(
                PhysicalFragment.DumpFlags.All & ~PhysicalFragment.DumpFlags.HeaderText));
        }
        return builder.ToString();
    }
#endif
}
