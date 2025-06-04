namespace LayoutEngine.NG.Layout.Inputs;

using System;
using System.Collections.Generic;
using System.Text;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Core;
using LayoutEngine.NG.Dom;
using LayoutEngine.NG.Layout.Inline;
using LayoutEngine.NG.Style;

/// <summary>
/// Represents the input to a layout algorithm for a given node. The layout
/// engine should use the style, node type to determine which type of layout
/// algorithm to use to produce fragments for this node.
/// Follows BlinkNG's LayoutInputNode pattern.
/// </summary>
public class LayoutInputNode
{
    /// <summary>
    /// Layout input node types matching BlinkNG's enum.
    /// </summary>
    public enum LayoutInputNodeType
    {
        Block,
        Inline
        // When adding new values, ensure type_ field has enough bits
    }

    private readonly LayoutBox? _box;
    private readonly LayoutInputNodeType _type;
    private readonly List<LayoutInputNode> _children = new();

    #region Constructors

    /// <summary>
    /// Creates a LayoutInputNode with the specified box and type.
    /// </summary>
    public static LayoutInputNode Create(LayoutBox? box, LayoutInputNodeType type)
    {
        // This function should create an instance of the subclass. This works
        // because subclasses are not virtual and do not add fields.
        return new LayoutInputNode(box, type);
    }

    /// <summary>
    /// Null constructor.
    /// </summary>
    public LayoutInputNode() : this(null, LayoutInputNodeType.Block)
    {
    }

    protected LayoutInputNode(LayoutBox? box, LayoutInputNodeType type)
    {
        _box = box;
        _type = type;
    }

    /// <summary>
    /// Legacy constructor for compatibility.
    /// </summary>
    public LayoutInputNode(INode? node, ComputedStyle? style, LayoutObjectType layoutObjectType)
    {
        Node = node;
        Style = style;
        LayoutObjectType = layoutObjectType;
        _type = layoutObjectType == LayoutObjectType.Inline ? LayoutInputNodeType.Inline : LayoutInputNodeType.Block;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the type of this input node.
    /// </summary>
    public LayoutInputNodeType Type => _type;

    /// <summary>
    /// Returns true if this is an inline node.
    /// </summary>
    public bool IsInline => _type == LayoutInputNodeType.Inline;

    /// <summary>
    /// Returns true if this is a block node.
    /// </summary>
    public bool IsBlock => _type == LayoutInputNodeType.Block;

    /// <summary>
    /// Legacy property for compatibility.
    /// </summary>
    public INode? Node { get; }

    /// <summary>
    /// Legacy property for compatibility.
    /// </summary>
    public ComputedStyle? Style { get; }

    /// <summary>
    /// Legacy property for compatibility.
    /// </summary>
    public LayoutObjectType LayoutObjectType { get; }

    /// <summary>
    /// Legacy property for compatibility.
    /// </summary>
    public bool IsText => LayoutObjectType == LayoutObjectType.Text;

    /// <summary>
    /// Children nodes.
    /// </summary>
    public IReadOnlyList<LayoutInputNode> Children => _children;

    #endregion

    #region Type Checking Methods

    public bool IsBlockFlow() => IsBlock && _box?.IsLayoutBlockFlow() == true;

    public bool IsBlockInInline() => _box?.IsBlockInInline() == true;

    public bool IsCustom() => IsBlock && _box?.IsLayoutCustom() == true;

    public bool IsColumnSpanAll() => IsBlock && _box?.IsColumnSpanAll() == true;

    public bool IsFloating() => IsBlock && _box?.IsFloating() == true;

    public bool IsOutOfFlowPositioned() => IsBlock && _box?.IsOutOfFlowPositioned() == true;

    public bool IsFloatingOrOutOfFlowPositioned() => IsFloating() || IsOutOfFlowPositioned();

    public bool IsReplaced() => _box?.IsLayoutReplaced() == true;

    public bool IsAbsoluteContainer() => _box?.CanContainAbsolutePositionObjects() == true;

    public bool IsFixedContainer() => _box?.CanContainFixedPositionObjects() == true;

    public bool IsBody() => IsBlock && _box?.IsBody() == true;

    public bool IsView() => IsBlock && _box?.IsLayoutView() == true;

    public bool IsDocumentElement() => _box?.IsDocumentElement() == true;

    public bool IsFlexItem() => IsBlock && _box?.IsFlexItem() == true;

    public bool IsFlexibleBox() => IsBlock && _box?.IsFlexibleBox() == true;

    public bool IsGrid() => IsBlock && _box?.IsLayoutGrid() == true;

    public bool IsMasonry() => IsBlock && _box?.IsLayoutMasonry() == true;

    public bool ShouldBeConsideredAsReplaced() => _box?.ShouldBeConsideredAsReplaced() == true;

    public bool IsListItem() => IsBlock && _box?.IsLayoutListItem() == true;

    /// <summary>
    /// Returns the list marker if this.IsListItem() with an outside list marker.
    /// Otherwise null.
    /// </summary>
    public BlockNode? ListMarkerBlockNodeIfListItem()
    {
        if (_box is LayoutListItem listItem)
        {
            var marker = listItem.Marker();
            return marker != null ? new BlockNode(marker as LayoutBox) : null;
        }
        return null;
    }

    public bool IsListMarker() => IsBlock && _box?.IsLayoutOutsideListMarker() == true;

    public bool ListMarkerOccupiesWholeLine()
    {
        if (!IsListMarker())
            throw new InvalidOperationException("Not a list marker");

        return (_box as LayoutOutsideListMarker)?.NeedsOccupyWholeLine() == true;
    }

    public bool IsButtonOrInputButton() => IsBlock && _box?.IsButtonOrInputButton() == true;

    public bool IsFieldsetContainer() => IsBlock && _box?.IsFieldset() == true;

    public bool IsInitialLetterBox() => _box?.IsInitialLetterBox() == true;

    public bool IsMedia() => _box?.IsMedia() == true;

    public bool IsCanvas() => _box?.IsCanvas() == true;

    /// <summary>
    /// Return true if this is the legend child of a fieldset that gets special
    /// treatment (i.e. placed over the block-start border).
    /// </summary>
    public bool IsRenderedLegend() => IsBlock && _box?.IsRenderedLegend() == true;

    /// <summary>
    /// Return true if this node is for input type=range.
    /// </summary>
    public bool IsSlider()
    {
        if (_box?.GetNode() is IHtmlInputElement input)
            return input.Type == "range";
        return false;
    }

    /// <summary>
    /// Return true if this node is for a slider thumb in input type=range.
    /// </summary>
    public bool IsSliderThumb() => IsBlock && IsSliderThumbElement(GetDOMNode());

    public bool IsSvgText() => _box?.IsSVGText() == true;

    public bool IsTable() => IsBlock && _box?.IsTable() == true;

    public bool IsTextCombine() => _box?.IsLayoutTextCombine() == true;

    public bool IsTableCaption() => IsBlock && _box?.IsTableCaption() == true;

    public bool IsTableSection() => IsBlock && _box?.IsTableSection() == true;

    public bool IsTableRow() => IsBlock && _box?.IsTableRow() == true;

    public bool IsTableCell() => IsBlock && _box?.IsTableCell() == true;

    /// <summary>
    /// Section with empty rows is considered empty.
    /// </summary>
    public bool IsEmptyTableSection() => _box?.IsTableSection() == true &&
                                        (_box as LayoutTableSection)?.IsEmpty() == true;

    public bool IsTableCol() => GetStyle()?.Display == DisplayMode.TableColumn;

    public bool IsTableColgroup() => GetStyle()?.Display == DisplayMode.TableColumnGroup;

    public int TableColumnSpan()
    {
        if (!IsTableCol() && !IsTableColgroup())
            throw new InvalidOperationException("Not a table column");

        return (_box as LayoutTableColumn)?.Span() ?? 1;
    }

    public int TableCellColspan()
    {
        if (!_box?.IsTableCell() == true)
            throw new InvalidOperationException("Not a table cell");

        return (_box as LayoutTableCell)?.ColSpan() ?? 1;
    }

    public int TableCellRowspan()
    {
        if (!_box?.IsTableCell() == true)
            throw new InvalidOperationException("Not a table cell");

        return (_box as LayoutTableCell)?.ComputedRowSpan() ?? 1;
    }

    public bool IsTextArea() => _box?.IsTextArea() == true;

    public bool IsTextControl() => _box?.IsTextControl() == true;

    public bool IsTextControlPlaceholder() => IsBlock && IsTextControlPlaceholderElement(GetDOMNode());

    public bool IsTextField() => _box?.IsTextField() == true;

    public bool IsMathRoot() => _box?.IsMathMLRoot() == true;

    public bool IsMathML() => _box?.IsMathML() == true;

    public bool IsAnonymous() => _box?.IsAnonymous() == true;

    public bool IsAnonymousBlockFlow() => _box?.IsAnonymousBlockFlow() == true;

    /// <summary>
    /// If the node is a quirky container for margin collapsing.
    /// https://html.spec.whatwg.org/C/#margin-collapsing-quirks
    /// NOTE: The spec appears to only somewhat match reality.
    /// </summary>
    public bool IsQuirkyContainer() => _box?.GetDocument().InQuirksMode == true &&
                                       (_box.IsBody() || _box.IsTableCell());

    public bool IsHorizontalWritingMode() => _box?.IsHorizontalWritingMode() == true;

    public bool IsHorizontalTypographicMode() => _box?.IsHorizontalTypographicMode() == true;

    /// <summary>
    /// Return true if this node is monolithic for block fragmentation.
    /// </summary>
    public bool IsMonolithic()
    {
        // Lines are always monolithic. We cannot block-fragment inside them.
        if (IsInline)
            return true;
        return _box?.IsMonolithic() == true;
    }

    public string? PageName() => IsBlock ? GetStyle()?.Page : null;

    public bool IsScrollContainer() => IsBlock && _box?.IsScrollContainer() == true;

    /// <summary>
    /// Return true if this is the document root and it is paginated. A paginated
    /// root establishes a fragmentation context.
    /// </summary>
    public bool IsPaginatedRoot()
    {
        if (!IsBlock)
            return false;
        var view = _box as LayoutView;
        return view?.IsFragmentationContextRoot() == true;
    }

    public bool CreatesNewFormattingContext() => IsBlock && _box?.CreatesNewFormattingContext() == true;

    #endregion

    #region Intrinsic Sizing

    /// <summary>
    /// Returns intrinsic sizing information for replaced elements.
    /// ComputeReplacedSize can use it to compute actual replaced size.
    /// Corresponds to Legacy's LayoutReplaced::IntrinsicSizingInfo.
    /// Use BlockNode::GetAspectRatio to get the aspect ratio.
    /// </summary>
    public void IntrinsicSize(out float? computedInlineSize, out float? computedBlockSize)
    {
        if (!IsReplaced())
            throw new InvalidOperationException("Not a replaced element");

        GetOverrideIntrinsicSize(out computedInlineSize, out computedBlockSize);
        if (computedInlineSize.HasValue && computedBlockSize.HasValue)
            return;

        var replaced = _box as LayoutReplaced;
        if (replaced == null)
        {
            computedInlineSize = null;
            computedBlockSize = null;
            return;
        }

        var sizingInfo = replaced.ComputeNaturalSizingInfo();

        float? intrinsicInlineSize = sizingInfo.HasWidth ? sizingInfo.Size.Width : null;
        float? intrinsicBlockSize = sizingInfo.HasHeight ? sizingInfo.Size.Height : null;

        if (!IsHorizontalWritingMode())
        {
            (intrinsicInlineSize, intrinsicBlockSize) = (intrinsicBlockSize, intrinsicInlineSize);
        }

        computedInlineSize ??= intrinsicInlineSize;
        computedBlockSize ??= intrinsicBlockSize;
    }

    private void GetOverrideIntrinsicSize(out float? computedInlineSize, out float? computedBlockSize)
    {
        computedInlineSize = null;
        computedBlockSize = null;

        if (!IsReplaced())
            return;

        float overrideInlineSize = OverrideIntrinsicContentInlineSize();
        if (!float.IsNaN(overrideInlineSize))
        {
            computedInlineSize = overrideInlineSize;
        }
        else
        {
            float defaultInlineSize = DefaultIntrinsicContentInlineSize();
            if (!float.IsNaN(defaultInlineSize))
                computedInlineSize = defaultInlineSize;
        }

        float overrideBlockSize = OverrideIntrinsicContentBlockSize();
        if (!float.IsNaN(overrideBlockSize))
        {
            computedBlockSize = overrideBlockSize;
        }
        else
        {
            float defaultBlockSize = DefaultIntrinsicContentBlockSize();
            if (!float.IsNaN(defaultBlockSize))
                computedBlockSize = defaultBlockSize;
        }

        if (ShouldApplyInlineSizeContainment() && !computedInlineSize.HasValue)
            computedInlineSize = 0;
        if (ShouldApplyBlockSizeContainment() && !computedBlockSize.HasValue)
            computedBlockSize = 0;
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Returns the next sibling.
    /// </summary>
    public LayoutInputNode? NextSibling()
    {
        if (this is InlineNode)
            return null;
        return (this as BlockNode)?.NextSibling();
    }

    #endregion

    #region Document and DOM Access

    public IDocument GetDocument() => _box?.GetDocument() ?? throw new InvalidOperationException("No document");

    public INode? GetDOMNode() => _box?.GetNode();

    /// <summary>
    /// Return the DOM node of this, or, if none, that of the nearest ancestor that has one.
    /// Anonymous objects have no DOM node.
    /// </summary>
    public INode? EnclosingDOMNode() => _box?.EnclosingNode();

    public PhysicalSize InitialContainingBlockSize()
    {
        var layoutView = GetDocument().GetLayoutView();
        if (layoutView == null)
            return PhysicalSize.Zero;

        var size = layoutView.GetLayoutSize(IncludeScrollbarsOption.IncludeScrollbars);
        return new PhysicalSize(size.Width, size.Height);
    }

    #endregion

    #region Layout Object Access

    /// <summary>
    /// Returns the LayoutObject which is associated with this node.
    /// </summary>
    public LayoutBox? GetLayoutBox() => _box;

    public ComputedStyle GetStyle() => _box?.StyleRef() ?? Style ?? throw new InvalidOperationException("No style");

    #endregion

    #region Containment

    public bool ShouldApplySizeContainment() => _box?.ShouldApplySizeContainment() == true;

    /// <summary>
    /// Return true if we should apply at least inline-size containment
    /// (i.e. "contain" is "size" or "inline-size").
    /// </summary>
    public bool ShouldApplyInlineSizeContainment() => _box?.ShouldApplyInlineSizeContainment() == true;

    /// <summary>
    /// Return true if we should apply at least block-size containment
    /// (i.e. "contain" is "size" or "block-size").
    /// </summary>
    public bool ShouldApplyBlockSizeContainment() => _box?.ShouldApplyBlockSizeContainment() == true;

    public bool CanMatchSizeContainerQueries() => _box?.CanMatchSizeContainerQueries() == true;

    public LogicalAxes ContainedAxes()
    {
        var axes = LogicalAxes.None;
        if (ShouldApplyInlineSizeContainment())
            axes |= LogicalAxes.Inline;
        if (ShouldApplyBlockSizeContainment())
            axes |= LogicalAxes.Block;
        return axes;
    }

    #endregion

    #region CSS Intrinsic Sizing

    /// <summary>
    /// CSS intrinsic sizing getters.
    /// https://drafts.csswg.org/css-sizing-4/#intrinsic-size-override
    /// Note that this returns NaN if the override was not specified.
    /// </summary>
    public float OverrideIntrinsicContentInlineSize() => _box?.OverrideIntrinsicContentInlineSize() ?? float.NaN;

    public float OverrideIntrinsicContentBlockSize() => _box?.OverrideIntrinsicContentBlockSize() ?? float.NaN;

    public float DefaultIntrinsicContentInlineSize() => _box?.DefaultIntrinsicContentInlineSize() ?? float.NaN;

    public float DefaultIntrinsicContentBlockSize() => _box?.DefaultIntrinsicContentBlockSize() ?? float.NaN;

    #endregion

    #region Display Lock

    public bool ChildLayoutBlockedByDisplayLock() => _box?.ChildLayoutBlockedByDisplayLock() == true;

    #endregion

    #region Custom Layout

    public CustomLayoutChild? GetCustomLayoutChild()
    {
        // TODO(ikilpatrick): Support InlineNode.
        if (!IsBlock)
            throw new InvalidOperationException("Custom layout only supported for block nodes");

        return _box?.GetCustomLayoutChild();
    }

    #endregion

    #region Fragment Traversal

    /// <summary>
    /// Return whether we can directly traverse fragments generated from this node
    /// (for painting, hit-testing and other layout read operations). If false is
    /// returned, we need to traverse the layout object tree instead.
    /// </summary>
    public bool CanTraversePhysicalFragments() => _box?.CanTraversePhysicalFragments() == true;

    #endregion

    #region Utility Methods

    public void AddChild(LayoutInputNode child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// Gets the text content for text nodes.
    /// </summary>
    public string? GetTextContent() => Node is IText textNode ? textNode.TextContent : null;

    /// <summary>
    /// Checks if this node establishes a new formatting context.
    /// </summary>
    public bool EstablishesFormattingContext()
    {
        if (Style == null) return false;

        return Style.Display == DisplayMode.FlowRoot ||
               Style.Position == PositionMode.Absolute ||
               Style.Position == PositionMode.Fixed ||
               (Style.Overflow != Overflow.Visible);
    }

    /// <summary>
    /// Creates a LayoutInputNode from a LayoutObject.
    /// </summary>
    public static LayoutInputNode FromLayoutObject(LayoutObject layoutObject)
    {
        var box = layoutObject as LayoutBox;
        var type = layoutObject.GetLayoutObjectType() == LayoutObjectType.Inline
            ? LayoutInputNodeType.Inline
            : LayoutInputNodeType.Block;

        var inputNode = new LayoutInputNode(box, type);

        // Add children
        var child = layoutObject.FirstChild;
        while (child != null)
        {
            inputNode.AddChild(FromLayoutObject(child));
            child = child.NextSibling;
        }

        return inputNode;
    }

    #endregion

    #region Operators and Conversion

    public static implicit operator bool(LayoutInputNode? node) => node?._box != null;

    public override bool Equals(object? obj)
    {
        if (obj is not LayoutInputNode other)
            return false;

        return _box == other._box && _type == other._type;
    }

    public override int GetHashCode() => HashCode.Combine(_box, _type);

    public static bool operator ==(LayoutInputNode? left, LayoutInputNode? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(LayoutInputNode? left, LayoutInputNode? right) => !(left == right);

    #endregion

    #region String Representation and Debugging

    public override string ToString()
    {
        if (this is InlineNode inlineNode)
            return inlineNode.ToString();
        if (this is BlockNode blockNode)
            return blockNode.ToString();

        return $"LayoutInputNode({_type}, {_box?.GetType().Name ?? "null"})";
    }

#if DEBUG
    /// <summary>
    /// Dumps the node tree for debugging.
    /// </summary>
    public string DumpNodeTree(LayoutInputNode? target = null)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(".:: Layout input node tree ::.");
        AppendNodeToString(this, target, stringBuilder);
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Dump the node tree for the entire document, and mark this with an asterisk.
    /// </summary>
    public string DumpNodeTreeFromRoot()
    {
        var view = _box?.View;
        if (view == null)
            return "No view available";

        return new BlockNode(view).DumpNodeTree(this);
    }

    public void ShowNodeTree(LayoutInputNode? target = null)
    {
        Console.WriteLine(DumpNodeTree(target));
    }

    public void ShowNodeTreeFromRoot()
    {
        Console.WriteLine(DumpNodeTreeFromRoot());
    }

    private static void AppendNodeToString(LayoutInputNode node, LayoutInputNode? target,
        StringBuilder stringBuilder, int indent = 2)
    {
        if (node == null)
            return;

        IndentForDump(node, target, stringBuilder, indent);
        stringBuilder.AppendLine(node.ToString());

        if (node is BlockNode blockNode)
        {
            AppendSubtreeToString(blockNode, target, stringBuilder, indent + 2);
        }
        else if (node is InlineNode inlineNode)
        {
            // Handle inline node items
            indent += 2;
            foreach (var item in inlineNode.GetItems())
            {
                IndentForDump(null, target, stringBuilder, indent);
                stringBuilder.AppendLine(item.ToString());
            }
        }
    }

    private static void AppendSubtreeToString(BlockNode node, LayoutInputNode? target,
        StringBuilder stringBuilder, int indent)
    {
        var firstChild = node.FirstChild();
        for (var runner = firstChild; runner != null; runner = runner.NextSibling())
        {
            AppendNodeToString(runner, target, stringBuilder, indent);
        }
    }

    private static void IndentForDump(LayoutInputNode? node, LayoutInputNode? target,
        StringBuilder stringBuilder, int indent)
    {
        int startCol = 0;
        if (node != null && target != null && node.Equals(target))
        {
            stringBuilder.Append('*');
            startCol = 1;
        }

        for (int i = startCol; i < indent; i++)
        {
            stringBuilder.Append(' ');
        }
    }
#endif

    #endregion

    #region Helper Methods

    private static bool IsSliderThumbElement(INode? node)
    {
        // TODO: Implement proper slider thumb detection
        return node?.NodeName == "slider-thumb";
    }

    private static bool IsTextControlPlaceholderElement(INode? node)
    {
        // TODO: Implement proper text control placeholder detection
        return node?.NodeName == "placeholder";
    }

    #endregion
}