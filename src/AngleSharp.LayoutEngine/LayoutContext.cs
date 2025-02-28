#pragma warning disable CS8618, CS9264
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using Css.Dom;

/// <summary>
/// Provides context information for layout operations, including viewport constraints,
/// position, and various state needed for layout calculations.
/// </summary>
public class LayoutContext
{
    #region Viewport Properties

    /// <summary>
    /// Gets the width of the viewport in pixels.
    /// </summary>
    public float ViewportWidth { get; }

    /// <summary>
    /// Gets the height of the viewport in pixels.
    /// </summary>
    public float ViewportHeight { get; }

    /// <summary>
    /// Gets or sets the default font size for the document, typically used for 'rem' units.
    /// </summary>
    public float DefaultFontSize { get; set; } = 16f;

    /// <summary>
    /// Gets or sets the text direction for layout.
    /// </summary>
    public TextDirection Direction { get; set; } = TextDirection.LTR;

    #endregion

    #region Position Properties

    /// <summary>
    /// Gets or sets the X coordinate of the parent's content area.
    /// </summary>
    public float ParentX { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate of the parent's content area.
    /// </summary>
    public float ParentY { get; set; }

    /// <summary>
    /// Gets or sets the available width for layout within the parent's content area.
    /// </summary>
    public float AvailableWidth { get; set; }

    /// <summary>
    /// Gets or sets the available height for layout within the parent's content area.
    /// Zero indicates auto height.
    /// </summary>
    public float AvailableHeight { get; set; }

    /// <summary>
    /// Gets or sets the parent node providing the coordinate system.
    /// </summary>
    public LayoutNode ParentNode { get; set; }

    /// <summary>
    /// Gets or sets the current node being laid out.
    /// </summary>
    public LayoutNode CurrentNode { get; set; }

    /// <summary>
    /// Gets or sets the previous sibling in normal flow, if any.
    /// Used for margin collapsing calculations.
    /// </summary>
    public LayoutNode PreviousSibling { get; set; }

    #endregion

    #region Margin Collapsing Properties

    /// <summary>
    /// Gets or sets the bottom margin of the previous element in flow.
    /// Used for margin collapsing calculations.
    /// </summary>
    public float PreviousMarginBottom { get; set; }

    /// <summary>
    /// Gets or sets the top margin of the current element.
    /// Used for margin collapsing calculations.
    /// </summary>
    public float CurrentMarginTop { get; set; }

    /// <summary>
    /// Gets or sets whether there's a previous sibling in normal flow.
    /// </summary>
    public bool HasPreviousSibling => PreviousSibling != null;

    /// <summary>
    /// Gets or sets a special margin value for empty blocks.
    /// When non-zero, indicates that the next element should collapse with
    /// this value instead of a regular margin calculation.
    /// </summary>
    public float EmptyBlockCollapsedMargin { get; set; }

    /// <summary>
    /// Gets or sets whether this is a first child in normal flow.
    /// </summary>
    public bool IsFirstChild { get; set; }

    /// <summary>
    /// Gets or sets whether this is a last child in normal flow.
    /// </summary>
    public bool IsLastChild { get; set; }

    #endregion

    #region Layout State Properties

    /// <summary>
    /// Gets or sets whether this is an incremental layout operation.
    /// </summary>
    public bool IsIncrementalLayout { get; set; }

    /// <summary>
    /// Gets or sets the current layout phase.
    /// </summary>
    public LayoutPhase CurrentPhase { get; set; } = LayoutPhase.Measure;

    /// <summary>
    /// Gets or sets the margin manager for collapsing calculations.
    /// </summary>
    public IMarginManager MarginManager { get; set; }

    /// <summary>
    /// Gets or sets whether the current row has a floating element.
    /// Used for inline formatting contexts.
    /// </summary>
    public bool HasFloatsInCurrentLine { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of floats by Y position.
    /// Used for block formatting to handle floats.
    /// </summary>
    public Dictionary<float, List<LayoutNode>> FloatsByY { get; set; } = new();

    /// <summary>
    /// Gets or sets a cache for layout operations to avoid recalculation.
    /// </summary>
    public Dictionary<string, object> Cache { get; set; } = new();

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new layout context with the specified viewport dimensions.
    /// </summary>
    /// <param name="viewportWidth">Width of the viewport in pixels.</param>
    /// <param name="viewportHeight">Height of the viewport in pixels.</param>
    public LayoutContext(float viewportWidth, float viewportHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        AvailableWidth = viewportWidth;
        AvailableHeight = viewportHeight;
    }

    /// <summary>
    /// Creates a new layout context with default viewport dimensions.
    /// </summary>
    public LayoutContext() : this(800f, 600f)
    {
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a child context with modified constraints based on a parent element.
    /// </summary>
    /// <param name="parent">The parent node providing constraints.</param>
    /// <returns>A new layout context with inherited settings and adjusted constraints.</returns>
    public LayoutContext CreateChildContext(LayoutNode parent)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        // Create a new context with same viewport
        var context = new LayoutContext(ViewportWidth, ViewportHeight)
        {
            // Inherit global settings
            Direction = Direction,
            DefaultFontSize = DefaultFontSize,
            IsIncrementalLayout = IsIncrementalLayout,
            CurrentPhase = CurrentPhase,
            MarginManager = MarginManager,

            // Set the parent relationship
            ParentNode = parent,

            // Share the same cache and float information
            Cache = Cache,
            FloatsByY = FloatsByY
        };

        // Calculate available space based on parent's content area
        if (parent.Box != null)
        {
            context.ParentX = parent.Box.X + parent.Box.BorderLeft + parent.Box.PaddingLeft;
            context.ParentY = parent.Box.Y + parent.Box.BorderTop + parent.Box.PaddingTop;
            context.AvailableWidth = parent.Box.Width;
            context.AvailableHeight = parent.Box.Height;
        }
        else
        {
            // If parent has no box yet, inherit from this context
            context.ParentX = ParentX;
            context.ParentY = ParentY;
            context.AvailableWidth = AvailableWidth;
            context.AvailableHeight = AvailableHeight;
        }

        return context;
    }

    /// <summary>
    /// Creates a context for positioning a node relative to its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block providing positioning constraints.</param>
    /// <returns>A layout context appropriate for the containing block.</returns>
    public LayoutContext CreatePositioningContext(LayoutNode containingBlock)
    {
        if (containingBlock == null)
            throw new ArgumentNullException(nameof(containingBlock));

        var context = new LayoutContext(ViewportWidth, ViewportHeight)
        {
            // Inherit global settings
            Direction = Direction,
            DefaultFontSize = DefaultFontSize,
            IsIncrementalLayout = IsIncrementalLayout,
            CurrentPhase = CurrentPhase,
            MarginManager = MarginManager,

            // Set the parent relationship
            ParentNode = containingBlock,

            // Share the same cache and float information
            Cache = Cache,
            FloatsByY = FloatsByY
        };

        // For positioned elements, the containing block provides the coordinate system
        if (containingBlock.Box != null)
        {
            // For absolute positioning, the containing block includes padding
            context.ParentX = containingBlock.Box.X;
            context.ParentY = containingBlock.Box.Y;
            context.AvailableWidth = containingBlock.Box.BorderBoxWidth;
            context.AvailableHeight = containingBlock.Box.BorderBoxHeight;
        }
        else
        {
            // If containing block has no box yet, use viewport
            context.ParentX = 0;
            context.ParentY = 0;
            context.AvailableWidth = ViewportWidth;
            context.AvailableHeight = ViewportHeight;
        }

        return context;
    }

    /// <summary>
    /// Gets constraints for sizing a node based on min/max properties.
    /// </summary>
    /// <param name="node">The node to get constraints for.</param>
    /// <returns>Size constraints for width and height.</returns>
    public SizeConstraints GetConstraintsForNode(LayoutNode node)
    {
        if (node == null)
            return new SizeConstraints
            {
                AvailableWidth = AvailableWidth,
                AvailableHeight = AvailableHeight
            };

        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return new SizeConstraints
            {
                AvailableWidth = AvailableWidth,
                AvailableHeight = AvailableHeight
            };

        // Parse min/max constraints
        float minWidth = ParseLengthOrDefault(element.ComputedStyle, "min-width", 0);
        float maxWidth = ParseLengthOrDefault(element.ComputedStyle, "max-width", float.PositiveInfinity);
        float minHeight = ParseLengthOrDefault(element.ComputedStyle, "min-height", 0);
        float maxHeight = ParseLengthOrDefault(element.ComputedStyle, "max-height", float.PositiveInfinity);

        return new SizeConstraints
        {
            AvailableWidth = AvailableWidth,
            AvailableHeight = AvailableHeight,
            MinWidth = minWidth,
            MaxWidth = maxWidth,
            MinHeight = minHeight,
            MaxHeight = maxHeight
        };
    }

    /// <summary>
    /// Parses a CSS length value or returns a default.
    /// </summary>
    private float ParseLengthOrDefault(ICssStyleDeclaration style, string property, float defaultValue)
    {
        var value = style.GetPropertyValue(property);

        if (string.IsNullOrEmpty(value) || value == "auto" || value == "none")
            return defaultValue;

        if (value.EndsWith("px") && float.TryParse(value.TrimEnd('p', 'x'), out float pixels))
            return pixels;

        // For percentage, would need container size
        // Other units would need more complex conversion

        return defaultValue;
    }

    /// <summary>
    /// Gets a formatted string representation of this context.
    /// </summary>
    public override string ToString()
    {
        return $"LayoutContext(Parent=({ParentX},{ParentY}), Available=({AvailableWidth}x{AvailableHeight}), Phase={CurrentPhase})";
    }

    #endregion
}

/// <summary>
/// Contains size constraints for layout operations.
/// </summary>
public class SizeConstraints
{
    /// <summary>
    /// Gets or sets the available width.
    /// </summary>
    public float AvailableWidth { get; set; }

    /// <summary>
    /// Gets or sets the available height.
    /// </summary>
    public float AvailableHeight { get; set; }

    /// <summary>
    /// Gets or sets the minimum width.
    /// </summary>
    public float MinWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum width.
    /// </summary>
    public float MaxWidth { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// Gets or sets the minimum height.
    /// </summary>
    public float MinHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum height.
    /// </summary>
    public float MaxHeight { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// Applies constraints to a width value.
    /// </summary>
    public float ApplyWidthConstraints(float width)
    {
        return Math.Clamp(width, MinWidth, MaxWidth);
    }

    /// <summary>
    /// Applies constraints to a height value.
    /// </summary>
    public float ApplyHeightConstraints(float height)
    {
        return Math.Clamp(height, MinHeight, MaxHeight);
    }
}

/// <summary>
/// Text direction options.
/// </summary>
public enum TextDirection
{
    /// <summary>
    /// Left-to-right text direction.
    /// </summary>
    LTR,

    /// <summary>
    /// Right-to-left text direction.
    /// </summary>
    RTL
}

/// <summary>
/// Layout phase of the current operation.
/// </summary>
public enum LayoutPhase
{
    /// <summary>
    /// Measure phase determines sizes.
    /// </summary>
    Measure,

    /// <summary>
    /// Position phase determines positions.
    /// </summary>
    Position,

    /// <summary>
    /// Margin phase resolves margin collapsing.
    /// </summary>
    MarginResolve,

    /// <summary>
    /// Final adjustments phase.
    /// </summary>
    FinalAdjustment
}