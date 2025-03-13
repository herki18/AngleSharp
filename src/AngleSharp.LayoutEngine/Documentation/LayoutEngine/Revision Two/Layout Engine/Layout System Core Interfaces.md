```Sharp
// This file defines the key interfaces for the LayoutSystem architecture
// These interfaces establish the clear boundaries between components
namespace AngleSharp.LayoutEngine.Layout
{
    using System;
    using System.Collections.Generic;
    using AngleSharp.Css;
    using AngleSharp.Css.Dom;
    using AngleSharp.Dom;
    using AngleSharp.StyleSystem.Interfaces;

    /// <summary>
    /// Main entry point for layout computation.
    /// </summary>
    public interface ILayoutEngine
    {
        /// <summary>
        /// Performs layout calculation for an element and its descendants.
        /// </summary>
        /// <param name="element">The element to lay out.</param>
        /// <returns>The layout result for the subtree.</returns>
        ILayoutResult LayoutElement(IElement element);
        
        /// <summary>
        /// Recalculates layout for elements affected by changes.
        /// </summary>
        /// <param name="root">The root element of the subtree to update.</param>
        void UpdateLayout(IElement root);
        
        /// <summary>
        /// Gets the layout tree factory.
        /// </summary>
        ILayoutTreeFactory TreeFactory { get; }
        
        /// <summary>
        /// Gets the layout invalidation tracker.
        /// </summary>
        ILayoutInvalidationTracker InvalidationTracker { get; }
        
        /// <summary>
        /// Gets the style engine associated with this layout engine.
        /// </summary>
        IStyleEngine StyleEngine { get; }
    }

    /// <summary>
    /// Factory for creating layout tree nodes.
    /// </summary>
    public interface ILayoutTreeFactory
    {
        /// <summary>
        /// Creates a layout tree node for an element.
        /// </summary>
        /// <param name="element">The element to create a layout node for.</param>
        /// <param name="style">The computed style for the element.</param>
        /// <returns>A layout tree node.</returns>
        ILayoutTreeNode CreateNode(IElement element, IComputedStyle style);
        
        /// <summary>
        /// Creates a specialized layout tree node based on display type.
        /// </summary>
        /// <param name="element">The element to create a layout node for.</param>
        /// <param name="style">The computed style for the element.</param>
        /// <param name="displayType">The display type to create a node for.</param>
        /// <returns>A specialized layout tree node.</returns>
        ILayoutTreeNode CreateSpecializedNode(IElement element, IComputedStyle style, DisplayMode displayType);
    }

    /// <summary>
    /// Represents a node in the layout tree.
    /// </summary>
    public interface ILayoutTreeNode
    {
        /// <summary>
        /// Gets the element associated with this layout node.
        /// </summary>
        IElement Element { get; }
        
        /// <summary>
        /// Gets the computed style associated with this layout node.
        /// </summary>
        IComputedStyle Style { get; }
        
        /// <summary>
        /// Gets the parent layout node.
        /// </summary>
        ILayoutTreeNode? Parent { get; }
        
        /// <summary>
        /// Gets the children layout nodes.
        /// </summary>
        IReadOnlyList<ILayoutTreeNode> Children { get; }
        
        /// <summary>
        /// Gets the layout box geometry for this node.
        /// </summary>
        ILayoutBoxGeometry BoxGeometry { get; }
        
        /// <summary>
        /// Gets the layout algorithm used for this node.
        /// </summary>
        ILayoutAlgorithm LayoutAlgorithm { get; }
        
        /// <summary>
        /// Adds a child node to this layout node.
        /// </summary>
        /// <param name="child">The child node to add.</param>
        void AddChild(ILayoutTreeNode child);
        
        /// <summary>
        /// Removes a child node from this layout node.
        /// </summary>
        /// <param name="child">The child node to remove.</param>
        void RemoveChild(ILayoutTreeNode child);
        
        /// <summary>
        /// Performs layout for this node and its children.
        /// </summary>
        /// <param name="context">The layout context.</param>
        /// <returns>The layout result.</returns>
        ILayoutResult PerformLayout(ILayoutContext context);
        
        /// <summary>
        /// Invalidates the layout for this node.
        /// </summary>
        void InvalidateLayout();
    }

    /// <summary>
    /// Represents the layout geometry for a box.
    /// </summary>
    public interface ILayoutBoxGeometry
    {
        /// <summary>
        /// Gets the content rectangle.
        /// </summary>
        Rect ContentRect { get; }
        
        /// <summary>
        /// Gets the padding rectangle.
        /// </summary>
        Rect PaddingRect { get; }
        
        /// <summary>
        /// Gets the border rectangle.
        /// </summary>
        Rect BorderRect { get; }
        
        /// <summary>
        /// Gets the margin rectangle.
        /// </summary>
        Rect MarginRect { get; }
        
        /// <summary>
        /// Gets the position in the viewport coordinate space.
        /// </summary>
        Point ScreenPosition { get; }
        
        /// <summary>
        /// Gets the position in the document coordinate space.
        /// </summary>
        Point DocumentPosition { get; }
        
        /// <summary>
        /// Gets the width of the content area.
        /// </summary>
        double Width { get; }
        
        /// <summary>
        /// Gets the height of the content area.
        /// </summary>
        double Height { get; }
        
        /// <summary>
        /// Updates the position of this box.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        void UpdatePosition(double x, double y);
        
        /// <summary>
        /// Updates the size of this box.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        void UpdateSize(double width, double height);
    }

    /// <summary>
    /// Interface for layout algorithms.
    /// </summary>
    public interface ILayoutAlgorithm
    {
        /// <summary>
        /// Gets the display mode this algorithm handles.
        /// </summary>
        DisplayMode DisplayMode { get; }
        
        /// <summary>
        /// Calculates layout for a node.
        /// </summary>
        /// <param name="node">The node to lay out.</param>
        /// <param name="context">The layout context.</param>
        /// <returns>The layout result.</returns>
        ILayoutResult CalculateLayout(ILayoutTreeNode node, ILayoutContext context);
        
        /// <summary>
        /// Measures the intrinsic size of a node given constraints.
        /// </summary>
        /// <param name="node">The node to measure.</param>
        /// <param name="widthConstraint">The width constraint.</param>
        /// <param name="heightConstraint">The height constraint.</param>
        /// <returns>The measured size.</returns>
        Size MeasureIntrinsicSize(ILayoutTreeNode node, double widthConstraint, double heightConstraint);
    }

    /// <summary>
    /// Context for layout operations.
    /// </summary>
    public interface ILayoutContext
    {
        /// <summary>
        /// Gets the containing block for positioning.
        /// </summary>
        ILayoutTreeNode? ContainingBlock { get; }
        
        /// <summary>
        /// Gets the available width for layout.
        /// </summary>
        double AvailableWidth { get; }
        
        /// <summary>
        /// Gets the available height for layout.
        /// </summary>
        double AvailableHeight { get; }
        
        /// <summary>
        /// Gets the fixed position containing block.
        /// </summary>
        ILayoutTreeNode? FixedPositionContainingBlock { get; }
        
        /// <summary>
        /// Gets the writing mode for text layout.
        /// </summary>
        WritingMode WritingMode { get; }
        
        /// <summary>
        /// Creates a child context with a new containing block.
        /// </summary>
        /// <param name="containingBlock">The new containing block.</param>
        /// <returns>A new layout context.</returns>
        ILayoutContext CreateChildContext(ILayoutTreeNode containingBlock);
    }

    /// <summary>
    /// Result of a layout operation.
    /// </summary>
    public interface ILayoutResult
    {
        /// <summary>
        /// Gets the node this result is for.
        /// </summary>
        ILayoutTreeNode Node { get; }
        
        /// <summary>
        /// Gets the box geometry after layout.
        /// </summary>
        ILayoutBoxGeometry BoxGeometry { get; }
        
        /// <summary>
        /// Gets whether the layout overflowed its container.
        /// </summary>
        bool HasOverflow { get; }
        
        /// <summary>
        /// Gets overflow data if available.
        /// </summary>
        IOverflowData? OverflowData { get; }
        
        /// <summary>
        /// Gets the child results from this layout operation.
        /// </summary>
        IReadOnlyList<ILayoutResult> ChildResults { get; }
    }

    /// <summary>
    /// Tracks layout invalidation.
    /// </summary>
    public interface ILayoutInvalidationTracker
    {
        /// <summary>
        /// Marks a node as needing layout recalculation.
        /// </summary>
        /// <param name="node">The node to invalidate.</param>
        void InvalidateNode(ILayoutTreeNode node);
        
        /// <summary>
        /// Marks an element as needing layout recalculation.
        /// </summary>
        /// <param name="element">The element to invalidate.</param>
        void InvalidateElement(IElement element);
        
        /// <summary>
        /// Determines if a node needs layout recalculation.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>True if the node needs recalculation; otherwise, false.</returns>
        bool NeedsLayoutRecalculation(ILayoutTreeNode node);
        
        /// <summary>
        /// Gets all nodes needing layout recalculation.
        /// </summary>
        /// <returns>The nodes needing recalculation.</returns>
        IEnumerable<ILayoutTreeNode> GetNodesToUpdate();
        
        /// <summary>
        /// Marks a node as up-to-date.
        /// </summary>
        /// <param name="node">The node to mark as up-to-date.</param>
        void MarkAsUpToDate(ILayoutTreeNode node);
    }

    /// <summary>
    /// The box model calculator for CSS box model.
    /// </summary>
    public interface IBoxModelCalculator
    {
        /// <summary>
        /// Calculates the box model dimensions based on style and constraints.
        /// </summary>
        /// <param name="style">The computed style.</param>
        /// <param name="availableWidth">The available width.</param>
        /// <param name="availableHeight">The available height.</param>
        /// <returns>The calculated box model.</returns>
        BoxModelData CalculateBoxModel(IComputedStyle style, double availableWidth, double availableHeight);
        
        /// <summary>
        /// Applies box sizing constraints (min/max width/height).
        /// </summary>
        /// <param name="boxModel">The box model to constrain.</param>
        /// <param name="style">The computed style with constraints.</param>
        /// <returns>The constrained box model.</returns>
        BoxModelData ApplySizingConstraints(BoxModelData boxModel, IComputedStyle style);
    }

    /// <summary>
    /// Calculates margin collapsing according to CSS rules.
    /// </summary>
    public interface IMarginCollapseCalculator
    {
        /// <summary>
        /// Calculates collapsed margins between adjacent elements.
        /// </summary>
        /// <param name="topMargin">The top margin.</param>
        /// <param name="bottomMargin">The bottom margin.</param>
        /// <returns>The collapsed margin value.</returns>
        double CalculateCollapsedMargin(double topMargin, double bottomMargin);
        
        /// <summary>
        /// Determines if margins should collapse between two elements.
        /// </summary>
        /// <param name="element1">The first element.</param>
        /// <param name="element2">The second element.</param>
        /// <returns>True if margins should collapse; otherwise, false.</returns>
        bool ShouldCollapseMargins(IElement element1, IElement element2);
    }

    /// <summary>
    /// Interface for positioning elements.
    /// </summary>
    public interface IPositioningEngine
    {
        /// <summary>
        /// Positions a layout node according to its position property.
        /// </summary>
        /// <param name="node">The node to position.</param>
        /// <param name="context">The layout context.</param>
        void PositionNode(ILayoutTreeNode node, ILayoutContext context);
        
        /// <summary>
        /// Finds the containing block for a positioned element.
        /// </summary>
        /// <param name="node">The node to find a containing block for.</param>
        /// <returns>The containing block.</returns>
        ILayoutTreeNode FindContainingBlock(ILayoutTreeNode node);
        
        /// <summary>
        /// Adjusts the position of a sticky positioned element.
        /// </summary>
        /// <param name="node">The node to adjust.</param>
        /// <param name="scrollOffset">The current scroll offset.</param>
        void AdjustStickyPosition(ILayoutTreeNode node, Point scrollOffset);
    }

    /// <summary>
    /// Represents data related to overflow.
    /// </summary>
    public interface IOverflowData
    {
        /// <summary>
        /// Gets the overflow rectangle in the coordinate space of the element.
        /// </summary>
        Rect OverflowRect { get; }
        
        /// <summary>
        /// Gets whether the element overflows horizontally.
        /// </summary>
        bool OverflowsX { get; }
        
        /// <summary>
        /// Gets whether the element overflows vertically.
        /// </summary>
        bool OverflowsY { get; }
        
        /// <summary>
        /// Gets the overflow-x property.
        /// </summary>
        OverflowMode OverflowX { get; }
        
        /// <summary>
        /// Gets the overflow-y property.
        /// </summary>
        OverflowMode OverflowY { get; }
        
        /// <summary>
        /// Gets whether scrollbars should be shown.
        /// </summary>
        bool RequiresScrollbars { get; }
    }

    /// <summary>
    /// Coordinates the layout process with the document lifecycle.
    /// </summary>
    public interface ILayoutCoordinator
    {
        /// <summary>
        /// Schedules a layout update for the next frame.
        /// </summary>
        /// <param name="element">The root element of the subtree to update.</param>
        void ScheduleLayoutUpdate(IElement element);
        
        /// <summary>
        /// Performs layout updates synchronously.
        /// </summary>
        void UpdateLayoutNow();
        
        /// <summary>
        /// Attaches to a document for lifecycle coordination.
        /// </summary>
        /// <param name="document">The document to attach to.</param>
        void AttachToDocument(IDocument document);
        
        /// <summary>
        /// Detaches from the current document.
        /// </summary>
        void DetachFromDocument();
    }

    /// <summary>
    /// Specialized layout algorithm for block layout.
    /// </summary>
    public interface IBlockLayoutAlgorithm : ILayoutAlgorithm
    {
        /// <summary>
        /// Calculates margin collapsing between blocks.
        /// </summary>
        /// <param name="node">The node to calculate margins for.</param>
        /// <param name="context">The layout context.</param>
        /// <returns>The adjusted margins after collapsing.</returns>
        Edges CalculateCollapsedMargins(ILayoutTreeNode node, ILayoutContext context);
    }

    /// <summary>
    /// Specialized layout algorithm for inline layout.
    /// </summary>
    public interface IInlineLayoutAlgorithm : ILayoutAlgorithm
    {
        /// <summary>
        /// Creates line boxes for inline content.
        /// </summary>
        /// <param name="node">The node containing inline content.</param>
        /// <param name="context">The layout context.</param>
        /// <returns>The created line boxes.</returns>
        IEnumerable<LineBox> CreateLineBoxes(ILayoutTreeNode node, ILayoutContext context);
        
        /// <summary>
        /// Computes line breaks for text content.
        /// </summary>
        /// <param name="textContent">The text content to break.</param>
        /// <param name="availableWidth">The available width.</param>
        /// <param name="style">The computed style.</param>
        /// <returns>The text runs after line breaking.</returns>
        IEnumerable<TextRun> ComputeLineBreaks(string textContent, double availableWidth, IComputedStyle style);
    }

    /// <summary>
    /// Specialized layout algorithm for flexbox layout.
    /// </summary>
    public interface IFlexLayoutAlgorithm : ILayoutAlgorithm
    {
        /// <summary>
        /// Gets the flex direction.
        /// </summary>
        FlexDirection Direction { get; }
        
        /// <summary>
        /// Gets the flex wrap mode.
        /// </summary>
        FlexWrap Wrap { get; }
        
        /// <summary>
        /// Calculates flex line layout.
        /// </summary>
        /// <param name="flexItems">The flex items to lay out.</param>
        /// <param name="context">The layout context.</param>
        /// <returns>The flex lines after layout.</returns>
        IEnumerable<FlexLine> CalculateFlexLines(IEnumerable<ILayoutTreeNode> flexItems, ILayoutContext context);
        
        /// <summary>
        /// Distributes free space among flex items.
        /// </summary>
        /// <param name="flexLine">The flex line to distribute space in.</param>
        /// <param name="availableSpace">The available space.</param>
        void DistributeFreeSpace(FlexLine flexLine, double availableSpace);
    }

    /// <summary>
    /// Specialized layout algorithm for grid layout.
    /// </summary>
    public interface IGridLayoutAlgorithm : ILayoutAlgorithm
    {
        /// <summary>
        /// Creates the grid template.
        /// </summary>
        /// <param name="node">The grid container node.</param>
        /// <param name="context">The layout context.</param>
        /// <returns>The grid template.</returns>
        GridTemplate CreateGridTemplate(ILayoutTreeNode node, ILayoutContext context);
        
        /// <summary>
        /// Places items in the grid.
        /// </summary>
        /// <param name="items">The grid items to place.</param>
        /// <param name="template">The grid template.</param>
        /// <returns>The grid item placements.</returns>
        IEnumerable<GridItemPlacement> PlaceGridItems(IEnumerable<ILayoutTreeNode> items, GridTemplate template);
        
        /// <summary>
        /// Calculates grid track sizes.
        /// </summary>
        /// <param name="template">The grid template.</param>
        /// <param name="availableWidth">The available width.</param>
        /// <param name="availableHeight">The available height.</param>
        /// <returns>The calculated track sizes.</returns>
        TrackSizes CalculateTrackSizes(GridTemplate template, double availableWidth, double availableHeight);
    }

    /// <summary>
    /// Interface for cached layout data.
    /// </summary>
    public interface ILayoutCache
    {
        /// <summary>
        /// Tries to get a cached layout result.
        /// </summary>
        /// <param name="node">The node to get layout for.</param>
        /// <param name="result">The cached result if found.</param>
        /// <returns>True if the layout was found in cache; otherwise, false.</returns>
        bool TryGetLayout(ILayoutTreeNode node, out ILayoutResult result);
        
        /// <summary>
        /// Stores a layout result in the cache.
        /// </summary>
        /// <param name="node">The node the layout is for.</param>
        /// <param name="result">The layout result.</param>
        void StoreLayout(ILayoutTreeNode node, ILayoutResult result);
        
        /// <summary>
        /// Clears the layout cache.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Handles layout tasks on the main thread.
    /// </summary>
    public interface IMainThreadLayoutWork
    {
        /// <summary>
        /// Enqueues a node for layout.
        /// </summary>
        /// <param name="node">The node to lay out.</param>
        /// <param name="priority">The priority of the layout operation.</param>
        void EnqueueNode(ILayoutTreeNode node, LayoutPriority priority);
        
        /// <summary>
        /// Processes layout synchronously.
        /// </summary>
        void ProcessSync();
        
        /// <summary>
        /// Gets whether there is pending layout work.
        /// </summary>
        bool HasPendingWork { get; }
    }

    /// <summary>
    /// Builds the rendering representation from layout.
    /// </summary>
    public interface IRenderLayerBuilder
    {
        /// <summary>
        /// Builds render layers from layout results.
        /// </summary>
        /// <param name="layoutResults">The layout results to build from.</param>
        /// <returns>The built render layers.</returns>
        IEnumerable<RenderLayer> BuildRenderLayers(IEnumerable<ILayoutResult> layoutResults);
        
        /// <summary>
        /// Updates render layers for changed layout results.
        /// </summary>
        /// <param name="changedResults">The changed layout results.</param>
        void UpdateRenderLayers(IEnumerable<ILayoutResult> changedResults);
    }

    #region Basic Data Structures

    /// <summary>
    /// Represents a rectangle with position and size.
    /// </summary>
    public readonly struct Rect
    {
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        
        public Rect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        
        public double Left => X;
        public double Top => Y;
        public double Right => X + Width;
        public double Bottom => Y + Height;
    }

    /// <summary>
    /// Represents a point with x and y coordinates.
    /// </summary>
    public readonly struct Point
    {
        public double X { get; }
        public double Y { get; }
        
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Represents a size with width and height.
    /// </summary>
    public readonly struct Size
    {
        public double Width { get; }
        public double Height { get; }
        
        public Size(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Represents edges (margin, border, padding).
    /// </summary>
    public readonly struct Edges
    {
        public double Top { get; }
        public double Right { get; }
        public double Bottom { get; }
        public double Left { get; }
        
        public Edges(double top, double right, double bottom, double left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }
    }

    /// <summary>
    /// Box model data calculated during layout.
    /// </summary>
    public class BoxModelData
    {
        public double ContentWidth { get; set; }
        public double ContentHeight { get; set; }
        public Edges Padding { get; set; }
        public Edges Border { get; set; }
        public Edges Margin { get; set; }
        
        public double TotalWidth => ContentWidth + Padding.Left + Padding.Right + Border.Left + Border.Right;
        public double TotalHeight => ContentHeight + Padding.Top + Padding.Bottom + Border.Top + Border.Bottom;
        
        public double MarginBoxWidth => TotalWidth + Margin.Left + Margin.Right;
        public double MarginBoxHeight => TotalHeight + Margin.Top + Margin.Bottom;
    }

    /// <summary>
    /// Represents a line box in text layout.
    /// </summary>
    public class LineBox
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Baseline { get; set; }
        public List<TextRun> TextRuns { get; } = new List<TextRun>();
        public List<ILayoutTreeNode> InlineBoxes { get; } = new List<ILayoutTreeNode>();
    }

    /// <summary>
    /// Represents a run of text with uniform style.
    /// </summary>
    public class TextRun
    {
        public string Text { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public IComputedStyle Style { get; set; } = null!;
    }

    /// <summary>
    /// Represents a flex line in flexbox layout.
    /// </summary>
    public class FlexLine
    {
        public double MainSize { get; set; }
        public double CrossSize { get; set; }
        public List<ILayoutTreeNode> Items { get; } = new List<ILayoutTreeNode>();
    }

    /// <summary>
    /// Represents a grid template in grid layout.
    /// </summary>
    public class GridTemplate
    {
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public List<GridTrack> Rows { get; } = new List<GridTrack>();
        public List<GridTrack> Columns { get; } = new List<GridTrack>();
    }

    /// <summary>
    /// Represents a grid track in grid layout.
    /// </summary>
    public class GridTrack
    {
        public double MinSize { get; set; }
        public double MaxSize { get; set; }
        public double ActualSize { get; set; }
    }

    /// <summary>
    /// Represents a grid item placement.
    /// </summary>
    public class GridItemPlacement
    {
        public ILayoutTreeNode Item { get; set; } = null!;
        public int RowStart { get; set; }
        public int RowEnd { get; set; }
        public int ColumnStart { get; set; }
        public int ColumnEnd { get; set; }
    }

    /// <summary>
    /// Represents track sizes in grid layout.
    /// </summary>
    public class TrackSizes
    {
        public List<double> RowSizes { get; } = new List<double>();
        public List<double> ColumnSizes { get; } = new List<double>();
    }

    /// <summary>
    /// Represents a render layer.
    /// </summary>
    public class RenderLayer
    {
        public ILayoutResult LayoutResult { get; set; } = null!;
        public Rect Bounds { get; set; }
        public int ZIndex { get; set; }
        public List<RenderLayer> Children { get; } = new List<RenderLayer>();
    }

    /// <summary>
    /// Enum defining layout priority.
    /// </summary>
    public enum LayoutPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Enum defining flex direction.
    /// </summary>
    public enum FlexDirection
    {
        Row,
        RowReverse,
        Column,
        ColumnReverse
    }

    /// <summary>
    /// Enum defining flex wrap.
    /// </summary>
    public enum FlexWrap
    {
        NoWrap,
        Wrap,
        WrapReverse
    }

    #endregion
}
```