namespace LayoutEngine.NG.Layout.Inputs;

using System;
using AngleSharp.Html.Dom;
using Core;
using LayoutEngine.NG.Dom;
using LayoutEngine.NG.Layout.Algorithms;
using LayoutEngine.NG.Layout.Fragments;
using LayoutEngine.NG.Layout.Inline;
using LayoutEngine.NG.Style;
using Process;

/// <summary>
/// Represents a block-level input node in LayoutNG.
/// This is a specialized LayoutInputNode for block layout algorithms.
/// Following BlinkNG's BlockNode design patterns.
/// </summary>
public class BlockNode : LayoutInputNode
{
    #region Constructors

    public BlockNode(LayoutBox? box) : base(box, LayoutInputNodeType.Block)
    {
    }

    public BlockNode(object? nullValue) : base(null, LayoutInputNodeType.Block)
    {
        if (nullValue != null)
            throw new ArgumentException("Expected null value");
    }

    #endregion

    #region Layout Methods

    /// <summary>
    /// Performs layout for this block node.
    /// In BlinkNG: BlockNode::Layout
    /// </summary>
    public LayoutResult Layout(
        ConstraintSpace constraintSpace,
        BlockBreakToken? breakToken = null,
        EarlyBreak? earlyBreak = null,
        ColumnSpannerPath? columnSpannerPath = null)
    {
        var box = GetLayoutBox();
        if (box == null)
            throw new InvalidOperationException("Cannot layout null box");

        // Check cache first
        if (box.GetCachedLayoutResult(breakToken) is LayoutResult cachedResult)
        {
            // Simplified cache check - in real implementation would be more complex
            if (!box.NeedsLayout)
                return cachedResult;
        }

        // Calculate fragment geometry
        var fragmentGeometry = CalculateInitialFragmentGeometry(constraintSpace, breakToken);

        // Handle size container queries
        if (!IsBreakInside(breakToken) && CanMatchSizeContainerQueries())
        {
            // TODO: Implement size container query handling
        }

        // Prepare for layout
        PrepareForLayout();

        // Create layout parameters
        var layoutParams = new LayoutAlgorithmParams(
            this,
            fragmentGeometry,
            constraintSpace,
            breakToken,
            earlyBreak)
        {
            ColumnSpannerPath = columnSpannerPath
        };

        // Run layout algorithm
        var layoutResult = LayoutWithAlgorithm(layoutParams);

        // Finish layout
        FinishLayout(constraintSpace, breakToken, layoutResult);

        return layoutResult;
    }

    /// <summary>
    /// Performs simplified layout using previous constraint space.
    /// In BlinkNG: BlockNode::SimplifiedLayout
    /// </summary>
    public LayoutResult? SimplifiedLayout(PhysicalFragment previousFragment)
    {
        var box = GetLayoutBox();
        if (box == null)
            return null;

        var previousResult = box.GetSingleCachedLayoutResult();
        if (previousResult == null)
            return null;

        // Verify we're using the right fragment
        if (previousResult.GetPhysicalFragment() != previousFragment)
            return null;

        if (!box.NeedsLayout)
            return previousResult;

        // Perform layout with previous constraint space
        var space = previousResult.GetConstraintSpaceForCaching();
        var result = Layout(space);

        if (result.Status != LayoutResult.LayoutStatus.Success)
            return null;

        // Check if size or baseline changed
        var oldFragment = previousResult.GetPhysicalFragment() as PhysicalBoxFragment;
        var newFragment = result.GetPhysicalFragment() as PhysicalBoxFragment;

        if (oldFragment != null && newFragment != null)
        {
            if (oldFragment.Size != newFragment.Size)
                return null;
            if (oldFragment.Baseline != newFragment.Baseline)
                return null;
        }

        return result;
    }

    /// <summary>
    /// Layout a repeatable root during block fragmentation.
    /// In BlinkNG: BlockNode::LayoutRepeatableRoot
    /// </summary>
    public LayoutResult LayoutRepeatableRoot(
        ConstraintSpace constraintSpace,
        BlockBreakToken? breakToken = null)
    {
        if (constraintSpace.HasBlockFragmentation())
            throw new InvalidOperationException("Cannot fragment repeatable content");

        if (IsBreakInside(breakToken))
            throw new InvalidOperationException("Cannot both resume and repeat");

        bool isFirst = breakToken == null || !breakToken.IsRepeated;
        LayoutResult result;

        if (isFirst)
        {
            // First fragment - perform regular layout
            result = Layout(constraintSpace, breakToken);
        }
        else
        {
            // Repeating - clone first result
            var box = GetLayoutBox()!;
            result = LayoutResult.Clone(box.GetLayoutResult(0)!);
        }

        // Create repeat break token for next fragment
        var index = FragmentIndex(breakToken);
        var fragment = result.GetPhysicalFragment() as PhysicalBoxFragment;
        if (fragment != null)
        {
            var outgoingBreakToken = BlockBreakToken.CreateRepeated(this, index);
            var mutator = fragment.GetMutableForCloning();
            mutator.SetBreakToken(outgoingBreakToken);

            if (!isFirst)
            {
                mutator.ClearIsFirstForNode();
                mutator.ClearPropagatedOOFs();
            }
        }

        if (!constraintSpace.ShouldRepeat())
        {
            FinishRepeatableRoot();
        }

        return result;
    }

    /// <summary>
    /// Finalize the cloned layout results of a repeatable root.
    /// In BlinkNG: BlockNode::FinishRepeatableRoot
    /// </summary>
    public void FinishRepeatableRoot()
    {
        var box = GetLayoutBox();
        if (box == null)
            return;

        // Remove break token from last fragment
        var fragments = box.PhysicalFragments;
        if (fragments.Count > 0)
        {
            var lastFragment = fragments[fragments.Count - 1];
            var mutator = lastFragment.GetMutableForCloning();
            mutator.SetBreakToken(null);
        }

        box.FinalizeLayoutResults();
        box.ClearNeedsLayout();

        // TODO: Implement FragmentRepeater.DeepCloneRepeatableRoot
    }

    #endregion

    #region MinMaxSizes Computation

    /// <summary>
    /// Computes the min-content and max-content sizes for this block.
    /// In BlinkNG: BlockNode::ComputeMinMaxSizes
    /// </summary>
    public MinMaxSizesResult ComputeMinMaxSizes(
        WritingMode containerWritingMode,
        SizeType sizeType,
        ConstraintSpace constraintSpace,
        MinMaxSizesFloatInput floatInput = default)
    {
        // Update marker text if needed for list items
        if (IsListItem())
        {
            var listItem = GetLayoutBox() as LayoutListItem;
            listItem?.UpdateMarkerTextIfNeeded();
        }

        bool isOrthogonalFlowRoot = !IsParallelWritingMode(containerWritingMode, Style.GetWritingMode());

        // For orthogonal flow roots, run layout to compute sizes
        if (isOrthogonalFlowRoot)
        {
            var layoutResult = Layout(constraintSpace);
            var fragment = layoutResult.GetPhysicalFragment();
            var logicalFragment = new LogicalFragment(
                new WritingDirectionMode(containerWritingMode, TextDirection.Ltr),
                fragment);

            var sizes = new MinMaxSizes
            {
                MinSize = logicalFragment.InlineSize,
                MaxSize = logicalFragment.InlineSize
            };

            bool dependsOnBlockConstraints =
                Style.LogicalWidth().HasAuto() ||
                Style.LogicalWidth().HasPercentOrStretch() ||
                Style.LogicalMinWidth().HasPercentOrStretch() ||
                Style.LogicalMaxWidth().HasPercentOrStretch();

            return new MinMaxSizesResult(sizes, dependsOnBlockConstraints);
        }

        // Check if we depend on block constraints
        bool DependsOnBlockConstraints() =>
            Style.LogicalHeight().HasPercentOrStretch() ||
            Style.LogicalMinHeight().HasPercentOrStretch() ||
            Style.LogicalMaxHeight().HasPercentOrStretch() ||
            (Style.LogicalHeight().HasAuto() && constraintSpace.IsBlockAutoBehaviorStretch());

        // Handle replaced elements directly
        if (IsReplaced())
        {
            var fragmentGeometry = CalculateInitialFragmentGeometry(constraintSpace, null, true);
            var sizes = new MinMaxSizes
            {
                MinSize = fragmentGeometry.BorderBoxSize.InlineSize,
                MaxSize = fragmentGeometry.BorderBoxSize.InlineSize
            };
            return new MinMaxSizesResult(sizes, DependsOnBlockConstraints());
        }

        // Handle aspect ratio
        bool hasAspectRatio = !Style.AspectRatio().IsAuto();
        if (hasAspectRatio && sizeType == SizeType.Content)
        {
            var fragmentGeometry = CalculateInitialFragmentGeometry(constraintSpace, null, true);
            var borderPadding = fragmentGeometry.Border + fragmentGeometry.Padding;

            if (!float.IsNaN(fragmentGeometry.BorderBoxSize.BlockSize))
            {
                var inlineSizeFromAr = InlineSizeFromAspectRatio(
                    borderPadding,
                    Style.LogicalAspectRatio(),
                    Style.BoxSizingForAspectRatio(),
                    fragmentGeometry.BorderBoxSize.BlockSize);

                return new MinMaxSizesResult(
                    new MinMaxSizes { MinSize = inlineSizeFromAr, MaxSize = inlineSizeFromAr },
                    DependsOnBlockConstraints(),
                    appliedAspectRatio: true);
            }
        }

        // Try to use cached intrinsic sizes
        var box = GetLayoutBox()!;
        bool canUseCached = CanUseCachedIntrinsicInlineSizes(constraintSpace, floatInput);

        if (!canUseCached)
        {
            box.SetIntrinsicLogicalWidthsDirty();
        }

        MinMaxSizesResult? result = null;

        // Try cached values
        if (canUseCached && !box.IntrinsicLogicalWidthsDependsOnBlockConstraints())
        {
            result = box.CachedIndefiniteIntrinsicLogicalWidths();
        }

        if (result == null)
        {
            // Compute new min/max sizes
            var fragmentGeometry = CalculateInitialFragmentGeometry(constraintSpace, null, true);
            var layoutParams = new LayoutAlgorithmParams(this, fragmentGeometry, constraintSpace);

            result = ComputeMinMaxSizesWithAlgorithm(layoutParams, floatInput);

            // Apply content minimum if needed
            var borderPadding = fragmentGeometry.Border + fragmentGeometry.Padding;
            var minSize = ContentMinimumInlineSize(borderPadding);
            if (minSize.HasValue)
            {
                result.Value.Sizes.MinSize = minSize.Value;
            }

            // Update cache
            box.SetIntrinsicLogicalWidths(fragmentGeometry.BorderBoxSize.BlockSize, result.Value);
        }

        // Apply aspect ratio constraints if needed
        if (hasAspectRatio)
        {
            // TODO: Implement aspect ratio constraints
        }

        // Update depends on block constraints flag
        result = new MinMaxSizesResult(
            result.Value.Sizes,
            (DependsOnBlockConstraints() || UseParentPercentageResolutionBlockSizeForChildren()) &&
            (result.Value.DependsOnBlockConstraints || hasAspectRatio),
            result.Value.AppliedAspectRatio);

        return result.Value;
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Returns the next sibling block node.
    /// In BlinkNG: BlockNode::NextSibling
    /// </summary>
    public new LayoutInputNode? NextSibling()
    {
        var box = GetLayoutBox();
        if (box == null)
            return null;

        var nextSibling = box.NextSibling;

        // Skip inline siblings (they may exist due to float/OOF handling)
        while (nextSibling != null && nextSibling.IsInline())
        {
            // Clear layout on text nodes we skip
            nextSibling.ClearNeedsLayout();
            nextSibling = nextSibling.NextSibling;
        }

        if (nextSibling == null)
            return null;

        return new BlockNode(nextSibling as LayoutBox);
    }

    /// <summary>
    /// Returns the first child node.
    /// In BlinkNG: BlockNode::FirstChild
    /// </summary>
    public new LayoutInputNode? FirstChild()
    {
        // Check if layout is blocked by display lock
        if (ChildLayoutBlockedByDisplayLock())
            return null;

        var box = GetLayoutBox();
        if (box == null)
            return null;

        var block = box as LayoutBlock;
        if (block == null)
            return new BlockNode(box.FirstChildBox());

        var child = GetLayoutObjectForFirstChildNode(block);
        if (child == null)
            return null;

        if (!AreNGBlockFlowChildrenInline(block))
            return new BlockNode(child as LayoutBox);

        var blockFlow = block as LayoutBlockFlow;
        if (blockFlow != null)
        {
            var inlineNode = new InlineNode(blockFlow);
            if (!inlineNode.IsBlockLevel())
                return inlineNode;
        }

        // Handle empty or float/OOF-only containers
        while (child != null && child.IsInline())
        {
            child.ClearNeedsLayout();
            child = child.NextSibling;
        }

        if (child == null)
            return null;

        return new BlockNode(child as LayoutBox);
    }

    #endregion

    #region Fieldset Methods

    /// <summary>
    /// Gets the rendered legend for a fieldset.
    /// In BlinkNG: BlockNode::GetRenderedLegend
    /// </summary>
    public BlockNode? GetRenderedLegend()
    {
        if (!IsFieldsetContainer())
            return null;

        var block = GetLayoutBox() as LayoutBlock;
        if (block == null)
            return null;

        var legend = LayoutFieldset.FindInFlowLegend(block);
        return legend != null ? new BlockNode(legend) : null;
    }

    /// <summary>
    /// Gets the fieldset content box.
    /// In BlinkNG: BlockNode::GetFieldsetContent
    /// </summary>
    public BlockNode? GetFieldsetContent()
    {
        if (!IsFieldsetContainer())
            return null;

        var fieldset = GetLayoutBox() as LayoutFieldset;
        var contentBox = fieldset?.FindAnonymousFieldsetContentBox();
        return contentBox != null ? new BlockNode(contentBox) : null;
    }

    #endregion

    #region Properties and Helpers

    public bool IsFrameSet() => GetLayoutBox()?.IsFrameSet() == true;
    public bool IsParentNGFrameSet() => GetLayoutBox()?.Parent?.IsFrameSet() == true;
    public bool IsParentGrid() => GetLayoutBox()?.Parent?.IsLayoutGrid() == true;

    /// <summary>
    /// Returns the aspect ratio of a replaced element.
    /// In BlinkNG: BlockNode::GetReplacedAspectRatio
    /// </summary>
    public LogicalSize GetReplacedAspectRatio()
    {
        if (!IsReplaced())
            throw new InvalidOperationException("Not a replaced element");

        var arType = Style.AspectRatio().GetType();
        if (arType == AspectRatioType.Ratio)
        {
            return Style.LogicalAspectRatio();
        }

        var box = GetLayoutBox()!;
        if (!box.ShouldApplyAnySizeContainment())
        {
            var replaced = box as LayoutReplaced;
            if (replaced != null)
            {
                var sizingInfo = replaced.ComputeNaturalSizingInfo();
                if (sizingInfo.AspectRatio > 0)
                {
                    return ToLogicalSize(
                        new PhysicalSize(sizingInfo.AspectRatio, 1),
                        Style.GetWritingMode());
                }
            }
        }

        if (arType == AspectRatioType.AutoAndRatio)
        {
            return Style.LogicalAspectRatio();
        }

        return LogicalSize.Empty;
    }

    /// <summary>
    /// Returns true if this node should pass its percentage resolution block-size to its children.
    /// In BlinkNG: BlockNode::UseParentPercentageResolutionBlockSizeForChildren
    /// </summary>
    public bool UseParentPercentageResolutionBlockSizeForChildren()
    {
        var block = GetLayoutBox() as LayoutBlock;
        if (block == null)
            return false;

        var style = Style;
        bool inQuirksMode = GetDocument().InQuirksMode();

        // Anonymous blocks should not impede percentage resolution
        if (block.IsAnonymous())
        {
            if (!inQuirksMode && block.Parent?.IsFieldset() == true)
                return false;

            var display = style.Display;
            return display == DisplayMode.Block ||
                   display == DisplayMode.InlineBlock ||
                   display == DisplayMode.FlowRoot;
        }

        // For quirks mode, skip most auto-height containing blocks
        if (!inQuirksMode || !style.LogicalHeight().IsAuto())
            return false;

        // Quirky body with auto height has definite height
        if (IsQuirkyAndFillsViewport())
            return false;

        var node = GetDOMNode();
        if (node?.IsInUserAgentShadowRoot() == true)
        {
            var host = node.OwnerShadowHost();
            if (host is IHtmlInputElement input && input.Type == "range")
                return true;
        }

        return !block.IsLayoutReplaced() &&
               !block.IsTableCell() &&
               !block.IsOutOfFlowPositioned() &&
               !block.IsLayoutGrid() &&
               !block.IsFlexibleBox() &&
               !block.IsLayoutCustom();
    }

    /// <summary>
    /// Return true if this block node establishes an inline formatting context.
    /// In BlinkNG: BlockNode::IsInlineFormattingContextRoot
    /// </summary>
    public bool IsInlineFormattingContextRoot(out InlineNode? firstChild)
    {
        firstChild = null;

        var blockFlow = GetLayoutBox() as LayoutBlockFlow;
        if (blockFlow == null)
            return false;

        if (!AreNGBlockFlowChildrenInline(blockFlow))
            return false;

        var child = FirstChild();
        if (child?.IsInline == true)
        {
            firstChild = child as InlineNode;
            return true;
        }

        return false;
    }

    public bool IsInlineLevel() => GetLayoutBox()?.IsInline() == true;
    public bool IsAtomicInlineLevel() => GetLayoutBox()?.IsAtomicInlineLevel() == true && GetLayoutBox()?.IsInline() == true;
    public bool IsInTopOrViewTransitionLayer() => GetLayoutBox()?.IsInTopOrViewTransitionLayer() == true;

    public bool HasLeftOverflow() => GetLayoutBox()?.HasLeftOverflow() == true;
    public bool HasTopOverflow() => GetLayoutBox()?.HasTopOverflow() == true;
    public bool HasNonVisibleOverflow() => GetLayoutBox()?.HasNonVisibleOverflow() == true;

    /// <summary>
    /// Return true if overflow in the block direction is clipped.
    /// In BlinkNG: BlockNode::HasNonVisibleBlockOverflow
    /// </summary>
    public bool HasNonVisibleBlockOverflow()
    {
        var clipAxes = GetOverflowClipAxes();
        if (Style.IsHorizontalWritingMode())
            return (clipAxes & OverflowClipAxes.Y) != 0;
        return (clipAxes & OverflowClipAxes.X) != 0;
    }

    public OverflowClipAxes GetOverflowClipAxes() => GetLayoutBox()?.GetOverflowClipAxes() ?? OverflowClipAxes.None;

    public bool MayHaveAnchorQuery() => GetLayoutBox()?.MayHaveAnchorQuery() == true;

    /// <summary>
    /// Returns true if the custom layout node is in its loaded state.
    /// In BlinkNG: BlockNode::IsCustomLayoutLoaded
    /// </summary>
    public bool IsCustomLayoutLoaded()
    {
        var custom = GetLayoutBox() as LayoutCustom;
        return custom?.IsLoaded() == true;
    }

    public bool ShouldApplyLayoutContainment() => GetLayoutBox()?.ShouldApplyLayoutContainment() == true;
    public bool ShouldApplyPaintContainment() => GetLayoutBox()?.ShouldApplyPaintContainment() == true;

    public bool HasLineIfEmpty()
    {
        var block = GetLayoutBox() as LayoutBlock;
        return block?.HasLineIfEmpty() == true;
    }

    /// <summary>
    /// Gets the empty line block size.
    /// In BlinkNG: BlockNode::EmptyLineBlockSize
    /// </summary>
    public float EmptyLineBlockSize(BlockBreakToken? incomingBreakToken)
    {
        // Only return line height for first fragment
        if (IsBreakInside(incomingBreakToken))
            return 0;

        return GetLayoutBox()?.LogicalHeightForEmptyLine() ?? 0;
    }

    #endregion

    #region Scroll Marker Support

    /// <summary>
    /// Return the ::scroll-marker-group associated with this node, if any.
    /// In BlinkNG: BlockNode::GetScrollMarkerGroup
    /// </summary>
    public BlockNode? GetScrollMarkerGroup()
    {
        var markerGroup = GetLayoutBox()?.GetScrollMarkerGroup() as LayoutBlock;
        return markerGroup != null ? new BlockNode(markerGroup as LayoutBox) : null;
    }

    /// <summary>
    /// Search for scroll markers and attach them to this scroll marker group.
    /// In BlinkNG: BlockNode::PopulateScrollMarkerGroup
    /// </summary>
    public void PopulateScrollMarkerGroup(BlockNode scroller)
    {
        var box = GetLayoutBox();
        if (!box.IsScrollMarkerGroup())
            throw new InvalidOperationException("Not a scroll marker group");

        var scrollerBox = scroller.GetLayoutBox();
        if (scrollerBox == null)
            return;

        // Mark for layout
        box.SetNeedsLayout(LayoutInvalidationReason.ScrollMarkersChanged);
        box.SetChildNeedsLayout();

        // Detach all existing markers
        while (box.SlowFirstChild() != null)
        {
            var child = box.SlowFirstChild();
            // TODO: Search for and detach scroll markers
            child.Remove();
        }

        // Attach new scroll markers
        var context = new AttachContext { Parent = box };
        AttachScrollMarkers(scrollerBox, context);
    }

    /// <summary>
    /// Populate with scroll markers the ::scroll-marker-group associated with this node.
    /// In BlinkNG: BlockNode::HandleScrollMarkerGroup
    /// </summary>
    public void HandleScrollMarkerGroup()
    {
        var groupNode = GetScrollMarkerGroup();
        if (groupNode == null)
            return;

        groupNode.PopulateScrollMarkerGroup(this);

        var result = groupNode.GetLayoutBox()?.GetCachedLayoutResult(null);
        if (result == null)
            return;

        // Re-layout the scroll marker group
        var fragment = result.GetPhysicalFragment() as PhysicalBoxFragment;
        if (fragment == null || !fragment.IsOnlyForNode())
            return;

        var space = result.GetConstraintSpaceForCaching();
        var newResult = groupNode.Layout(space);

        // Replace children in the fragment
        fragment.GetMutableForCloning().ReplaceChildren(
            newResult.GetPhysicalFragment() as PhysicalBoxFragment);
    }

    #endregion

    #region MathML Support

    /// <summary>
    /// Get script type for MathML scripts.
    /// In BlinkNG: BlockNode::ScriptType
    /// </summary>
    public MathScriptType ScriptType()
    {
        var element = GetDOMNode() as MathMLScriptsElement;
        if (element == null)
            throw new InvalidOperationException("Not a MathML scripts element");
        return element.GetScriptType();
    }

    /// <summary>
    /// Find out if the radical has an index.
    /// In BlinkNG: BlockNode::HasIndex
    /// </summary>
    public bool HasIndex()
    {
        var element = GetDOMNode() as MathMLRadicalElement;
        if (element == null)
            throw new InvalidOperationException("Not a MathML radical element");
        return element.HasIndex();
    }

    #endregion

    #region Atomic Inline Layout

    /// <summary>
    /// Layout an atomic inline (e.g., inline block).
    /// In BlinkNG: BlockNode::LayoutAtomicInline
    /// </summary>
    public LayoutResult LayoutAtomicInline(
        ConstraintSpace parentConstraintSpace,
        ComputedStyle parentStyle,
        bool useFirstLineStyle,
        BaselineAlgorithmType baselineAlgorithmType)
    {
        var builder = new ConstraintSpaceBuilder(
            parentConstraintSpace,
            Style.GetWritingDirection(),
            isNewFc: true);

        SetOrthogonalFallbackInlineSizeIfNeeded(parentStyle, this, builder);

        builder.SetIsPaintedAtomically(true);
        builder.SetUseFirstLineStyle(useFirstLineStyle);
        builder.SetIsHiddenForPaint(parentConstraintSpace.IsHiddenForPaint());
        builder.SetBaselineAlgorithmType(baselineAlgorithmType);

        builder.SetAvailableSize(parentConstraintSpace.AvailableSize);
        builder.SetPercentageResolutionSize(
            IsReplaced()
                ? parentConstraintSpace.ReplacedChildPercentageResolutionSize()
                : parentConstraintSpace.PercentageResolutionSize);

        var constraintSpace = builder.ToConstraintSpace();
        var result = Layout(constraintSpace);

        GetLayoutBox()?.ClearNeedsLayout();

        return result;
    }

    #endregion

    #region Multicol Support

    /// <summary>
    /// Write the number of columns in a multicol container to legacy.
    /// In BlinkNG: BlockNode::StoreColumnCount
    /// </summary>
    public void StoreColumnCount(int count)
    {
        var blockFlow = GetLayoutBox() as LayoutBlockFlow;
        var flowThread = blockFlow?.MultiColumnFlowThread();
        flowThread?.SetColumnCountFromNG(count);
    }

    /// <summary>
    /// Make room for extra columns after multicol layout.
    /// In BlinkNG: BlockNode::MakeRoomForExtraColumns
    /// </summary>
    public void MakeRoomForExtraColumns(float blockSize)
    {
        var blockFlow = GetLayoutBox() as LayoutBlockFlow;
        var flowThread = blockFlow?.MultiColumnFlowThread();
        if (flowThread == null)
            return;

        var lastSet = flowThread.LastMultiColumnSet();
        lastSet?.LastFragmentainerGroup()?.ExtendLogicalBottomInFlowThread(blockSize);
    }

    #endregion

    #region Page Container Support

    /// <summary>
    /// Finish layout for page containers.
    /// In BlinkNG: BlockNode::FinishPageContainerLayout
    /// </summary>
    public void FinishPageContainerLayout(LayoutResult result)
    {
        if (result.Status != LayoutResult.LayoutStatus.Success)
            throw new InvalidOperationException("Layout must succeed");

        var fragment = result.GetPhysicalFragment();
        if (fragment.GetBoxType() != PhysicalFragment.BoxType.PageContainer &&
            fragment.GetBoxType() != PhysicalFragment.BoxType.PageBorderBox)
        {
            throw new InvalidOperationException("Must be a page container or border box");
        }

        StoreResultInLayoutBox(result, null);
    }

    #endregion

    #region Transform Support

    /// <summary>
    /// Returns the transform to apply to a child fragment.
    /// In BlinkNG: BlockNode::GetTransformForChildFragment
    /// </summary>
    public Transform? GetTransformForChildFragment(
        PhysicalBoxFragment childFragment,
        PhysicalSize size)
    {
        var childLayoutObject = childFragment.GetLayoutObject();
        if (childLayoutObject == null)
            return null;

        var box = GetLayoutBox();
        if (!childLayoutObject.ShouldUseTransformFromContainer(box))
            return null;

        Transform? fragmentTransform = null;
        if (!childFragment.IsOnlyForNode())
        {
            // Fragment is fragmented, calculate transform
            fragmentTransform = new Transform();
            var referenceBox = ComputeReferenceBox(childFragment);
            childFragment.Style.ApplyTransform(
                fragmentTransform,
                box,
                referenceBox,
                ComputedStyle.TransformOperationOptions.IncludeTransformOperations |
                ComputedStyle.TransformOperationOptions.IncludeTransformOrigin |
                ComputedStyle.TransformOperationOptions.IncludeMotionPath |
                ComputedStyle.TransformOperationOptions.IncludeIndependentTransformProperties);
        }

        var transform = new Transform();
        childLayoutObject.GetTransformFromContainer(
            box,
            PhysicalOffset.Zero,
            transform,
            size,
            fragmentTransform);

        return transform;
    }

    #endregion

    #region Child Fragment Positioning

    /// <summary>
    /// Copy child fragment position back to the layout box.
    /// In BlinkNG: BlockNode::CopyChildFragmentPosition
    /// </summary>
    public void CopyChildFragmentPosition(
        PhysicalBoxFragment childFragment,
        PhysicalOffset offset,
        PhysicalBoxFragment containerFragment,
        BlockBreakToken? previousContainerBreakToken = null,
        bool needsInvalidationCheck = false)
    {
        var layoutBox = childFragment.GetMutableLayoutObject() as LayoutBox;
        if (layoutBox == null)
            return;

        if (childFragment.GetBoxType() == PhysicalFragment.BoxType.PageContainer ||
            childFragment.GetBoxType() == PhysicalFragment.BoxType.PageBorderBox)
        {
            return;
        }

        if (layoutBox.Parent == null)
            throw new InvalidOperationException("Should be called on children only");

        var location = ComputeBoxLocation(
            childFragment,
            offset,
            containerFragment,
            previousContainerBreakToken);

        layoutBox.SetLocation(location);

        if (needsInvalidationCheck)
            layoutBox.SetShouldCheckForPaintInvalidation();
    }

    #endregion

    #region Equality and String Representation

    public override bool Equals(object? obj)
    {
        if (obj is BlockNode other)
            return GetLayoutBox() == other.GetLayoutBox();

        if (obj is LayoutInputNode input)
            return input.Type == LayoutInputNodeType.Block && GetLayoutBox() == input.GetLayoutBox();

        return false;
    }

    public override int GetHashCode() => GetLayoutBox()?.GetHashCode() ?? 0;

    public static bool operator ==(BlockNode? left, BlockNode? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(BlockNode? left, BlockNode? right) => !(left == right);

    public override string ToString()
    {
        var box = GetLayoutBox();
        if (box == null)
            return "BlockNode(null)";

        return $"BlockNode: {box}";
    }

    #endregion

    #region Private Helper Methods

    private void PrepareForLayout()
    {
        var block = GetLayoutBox() as LayoutBlock;
        if (block?.IsScrollContainer() == true)
        {
            var scrollableArea = block.GetScrollableArea();
            if (scrollableArea?.ShouldPerformScrollAnchoring() == true)
            {
                scrollableArea.GetScrollAnchor()?.NotifyBeforeLayout();
            }
        }

        // Handle scroll marker groups
        if (GetLayoutBox()?.IsScrollMarkerGroup() == true && !GetLayoutBox().EverHadLayout() && GetLayoutBox().SlowFirstChild() == null)
        {
            var scrollerBox = GetLayoutBox().ScrollerFromScrollMarkerGroup();
            if (scrollerBox != null)
            {
                PopulateScrollMarkerGroup(new BlockNode(scrollerBox));
            }
        }

        // Update list item markers
        if (IsListItem())
        {
            var listItem = GetLayoutBox() as LayoutListItem;
            listItem?.UpdateMarkerTextIfNeeded();
        }
    }

    private void FinishLayout(
        ConstraintSpace constraintSpace,
        BlockBreakToken? breakToken,
        LayoutResult layoutResult)
    {
        if (layoutResult.Status != LayoutResult.LayoutStatus.Success)
        {
            GetLayoutBox()?.SetShouldSkipLayoutCache(true);
            return;
        }

        var physicalFragment = layoutResult.GetPhysicalFragment() as PhysicalBoxFragment;
        if (physicalFragment == null)
            return;

        // Handle SVG root
        var svgRoot = GetLayoutBox() as LayoutSVGRoot;
        if (svgRoot != null)
        {
            var contentRect = physicalFragment.LocalRect();
            contentRect.Contract(physicalFragment.Borders + physicalFragment.Padding);

            if (!svgRoot.NeedsLayout)
            {
                svgRoot.SetNeedsLayout(LayoutInvalidationReason.SizeChanged);
            }
            svgRoot.LayoutRoot(contentRect);
        }

        // Store result in layout box
        bool clearTrailingResults = breakToken != null || GetLayoutBox()!.PhysicalFragmentCount() > 1;
        StoreResultInLayoutBox(layoutResult, breakToken, clearTrailingResults);

        // Handle block flow specific logic
        var blockFlow = GetLayoutBox() as LayoutBlockFlow;
        if (blockFlow != null)
        {
            var items = physicalFragment.Items();
            bool hasInlineChildren = items != null || HasInlineChildren(blockFlow);

            if (hasInlineChildren && ChildLayoutBlockedByDisplayLock())
            {
                hasInlineChildren = false;
                GetLayoutBox()!.SetChildNeedsLayout();
            }

            if (!hasInlineChildren)
            {
                blockFlow.ClearInlineNodeData();
            }
        }

        // Copy fragment data to layout box
        CopyFragmentDataToLayoutBox(constraintSpace, layoutResult, breakToken);
    }

    private void StoreResultInLayoutBox(
        LayoutResult result,
        BlockBreakToken? breakToken,
        bool clearTrailingResults = false)
    {
        var fragment = result.GetPhysicalFragment() as PhysicalBoxFragment;
        if (fragment == null)
            return;

        var box = GetLayoutBox()!;
        int fragmentIdx = 0;

        if (fragment.IsOnlyForNode())
        {
            box.SetCachedLayoutResult(result, 0);
        }
        else
        {
            fragmentIdx = FragmentIndex(breakToken);
            box.SetLayoutResult(result, fragmentIdx);
        }

        if (clearTrailingResults)
            box.ShrinkLayoutResults(fragmentIdx + 1);
    }

    private void CopyFragmentDataToLayoutBox(
        ConstraintSpace constraintSpace,
        LayoutResult layoutResult,
        BlockBreakToken? previousBreakToken)
    {
        var physicalFragment = layoutResult.GetPhysicalFragment() as PhysicalBoxFragment;
        if (physicalFragment == null)
            return;

        bool isLastFragment = physicalFragment.GetBreakToken() == null;

        // Update margin/padding info
        UpdateMarginPaddingInfoIfNeeded(constraintSpace, physicalFragment);

        var box = GetLayoutBox()!;
        if (!ChildLayoutBlockedByDisplayLock())
        {
            // TODO: Place children in layout box
        }

        if (isLastFragment)
        {
            box.UpdateAfterLayout();
        }
    }

    private void UpdateMarginPaddingInfoIfNeeded(
        ConstraintSpace space,
        PhysicalFragment fragment)
    {
        // Table cells don't have margins
        if (space.IsTableCell())
            return;

        if (Style.MayHaveMargin())
        {
            var boxFragment = fragment as PhysicalBoxFragment;
            boxFragment?.GetMutableForContainerLayout().SetMargins(
                ComputePhysicalMargins(space, Style));
        }
    }

    private LayoutResult LayoutWithAlgorithm(LayoutAlgorithmParams layoutParams)
    {
        // Use appropriate algorithm based on layout type
        var algorithm = LayoutAlgorithm.CreateAlgorithm(this, layoutParams.ConstraintSpace);
        return algorithm.Layout();
    }

    private FragmentGeometry CalculateInitialFragmentGeometry(
        ConstraintSpace constraintSpace,
        BlockBreakToken? breakToken,
        bool isIntrinsic = false)
    {
        // Simplified implementation
        return FragmentGeometry.Calculate(constraintSpace, this, breakToken, isIntrinsic);
    }

    private static bool IsBreakInside(BlockBreakToken? breakToken)
    {
        return breakToken != null && !breakToken.IsAtBlockStart();
    }

    private static int FragmentIndex(BlockBreakToken? breakToken)
    {
        return breakToken?.SequenceNumber() ?? 0;
    }

    private float? ContentMinimumInlineSize(BoxStrut borderPadding)
    {
        var box = GetLayoutBox()!;

        // Table layout never goes below min-intrinsic size
        if (box.IsTable())
            return null;

        var node = GetDOMNode();
        if (node is IHtmlMarqueeElement marquee && marquee.IsHorizontal())
            return borderPadding.InlineSum();

        var style = Style;
        var mainInlineSize = style.LogicalWidth();

        if (!mainInlineSize.HasPercent())
            return null;

        // Resolve against zero
        float inlineSize = MinimumValueForLength(mainInlineSize, 0);
        if (style.BoxSizing == BoxSizing.BorderBox)
            inlineSize = Math.Max(borderPadding.InlineSum(), inlineSize);
        else
            inlineSize += borderPadding.InlineSum();

        // Check for form controls with fixed sizing
        bool applyFormSizing = style.ApplyControlFixedSize(node);
        if (IsTextControl() && applyFormSizing)
            return inlineSize;
        if (node is IHtmlSelectElement && applyFormSizing)
            return inlineSize;
        if (node is IHtmlInputElement input)
        {
            if (input.Type == "file" && applyFormSizing)
                return inlineSize;
            if (input.Type == "range")
                return inlineSize;
        }

        return null;
    }

    private bool CanUseCachedIntrinsicInlineSizes(
        ConstraintSpace constraintSpace,
        MinMaxSizesFloatInput floatInput)
    {
        var box = GetLayoutBox()!;

        // Can't use cache if dirty
        if (box.IntrinsicLogicalWidthsDirty())
            return false;

        // Can't use cache with float sizes
        if (floatInput.FloatLeftInlineSize > 0 || floatInput.FloatRightInlineSize > 0)
            return false;

        // Check for percentage padding
        var style = Style;
        if (style.MayHavePadding() &&
            (style.PaddingTop().HasPercent() ||
             style.PaddingRight().HasPercent() ||
             style.PaddingBottom().HasPercent() ||
             style.PaddingLeft().HasPercent()))
        {
            return false;
        }

        // Special checks for table cells
        if (IsTableCell())
        {
            var tableCell = box as LayoutTableCell;
            if (tableCell?.IntrinsicLogicalWidthsBorderSizes() != constraintSpace.TableCellBorders())
                return false;
        }

        // Grid-specific checks
        if (IsGrid())
        {
            if (style.LogicalMinWidth().HasPercentOrStretch() ||
                style.LogicalMaxWidth().HasPercentOrStretch())
            {
                return false;
            }

            if (!style.AspectRatio().IsAuto() &&
                (style.LogicalMinHeight().HasPercentOrStretch() ||
                 style.LogicalMaxHeight().HasPercentOrStretch()))
            {
                return false;
            }
        }

        return true;
    }

    private MinMaxSizesResult ComputeMinMaxSizesWithAlgorithm(
        LayoutAlgorithmParams layoutParams,
        MinMaxSizesFloatInput floatInput)
    {
        var algorithm = LayoutAlgorithm.CreateAlgorithm(this, layoutParams.ConstraintSpace);
        return algorithm.ComputeMinMaxSizes(floatInput);
    }

    private static bool HasInlineChildren(LayoutBlockFlow blockFlow)
    {
        var child = GetLayoutObjectForFirstChildNode(blockFlow);
        return child != null && AreNGBlockFlowChildrenInline(blockFlow);
    }

    private static LayoutObject? GetLayoutObjectForFirstChildNode(LayoutBlock block)
    {
        // TODO: Implement proper first child retrieval
        return block.FirstChild;
    }

    private static bool AreNGBlockFlowChildrenInline(LayoutBlock block)
    {
        // TODO: Implement proper check for inline children
        var child = block.FirstChild;
        return child != null && child.IsInline();
    }

    private static void AttachScrollMarkers(LayoutObject parent, AttachContext context)
    {
        // TODO: Implement scroll marker attachment
    }

    private static bool IsParallelWritingMode(WritingMode mode1, WritingMode mode2)
    {
        return (IsHorizontalWritingMode(mode1) == IsHorizontalWritingMode(mode2));
    }

    private static bool IsHorizontalWritingMode(WritingMode mode)
    {
        return mode == WritingMode.HorizontalTb;
    }

    private static LogicalSize ToLogicalSize(PhysicalSize size, WritingMode mode)
    {
        if (IsHorizontalWritingMode(mode))
            return new LogicalSize(size.Width, size.Height);
        return new LogicalSize(size.Height, size.Width);
    }

    private static float InlineSizeFromAspectRatio(
        BoxStrut borderPadding,
        LogicalSize aspectRatio,
        BoxSizing boxSizing,
        float blockSize)
    {
        // TODO: Implement aspect ratio calculation
        return 100; // Placeholder
    }

    private static PhysicalOffset ComputeBoxLocation(
        PhysicalBoxFragment childFragment,
        PhysicalOffset offset,
        PhysicalBoxFragment containerFragment,
        BlockBreakToken? previousContainerBreakToken)
    {
        // TODO: Implement proper box location computation
        return offset;
    }

    private static BoxStrut ComputePhysicalMargins(ConstraintSpace space, ComputedStyle style)
    {
        // TODO: Implement proper margin computation
        return new BoxStrut();
    }

    private static PhysicalRect ComputeReferenceBox(PhysicalBoxFragment fragment)
    {
        // TODO: Implement reference box computation
        return fragment.LocalRect();
    }

    private static void SetOrthogonalFallbackInlineSizeIfNeeded(
        ComputedStyle parentStyle,
        BlockNode node,
        ConstraintSpaceBuilder builder)
    {
        // TODO: Implement orthogonal fallback size
    }

    private static float MinimumValueForLength(Length length, float referenceValue)
    {
        // TODO: Implement proper length resolution
        return 0;
    }

    #endregion
}