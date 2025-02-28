#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace AngleSharp.LayoutEngine.FormattingContexts.FlexFormattingContext;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.LayoutEngine.Box;
using AngleSharp.LayoutEngine.Core;
using AngleSharp.LayoutEngine.DOM;
using AngleSharp.LayoutEngine.FormattingContexts;

/// <summary>
/// Implements a flex formatting context according to the CSS specification.
/// A flex formatting context is established by a flex container that manages flex items.
/// </summary>
public class FlexFormattingContext : FormattingContext
{
    private List<FlexItem> _flexItems = new();

    // State for tracking incremental updates
    private class LayoutState
    {
        public float MainSize { get; set; }
        public float CrossSize { get; set; }
        public Dictionary<LayoutNode, FlexItemMetrics> ItemMetrics { get; } = new();
    }

    private LayoutState _lastState = new LayoutState();

    /// <summary>
    /// Creates a new flex formatting context for the specified establishing node.
    /// </summary>
    public FlexFormattingContext(LayoutNode establishingNode)
        : base(establishingNode)
    {
        // Collect participants
        CollectFlexItems(establishingNode);
    }

    /// <summary>
    /// Performs a full layout of the flex container and its items.
    /// </summary>
    public override void Layout(LayoutContext context)
    {
        // Reset state
        _flexItems.Clear();
        _lastState = new LayoutState();

        // Determine flex container properties
        var containerProps = GetFlexContainerProperties(EstablishingNode);

        // Prepare flex items
        PrepareFlexItems();

        // 1. Generate anonymous flex items (for text nodes)
        CreateAnonymousFlexItems();

        // 2. Determine the available main and cross sizes
        DetermineContainerSizes(containerProps, context);

        // 3. Calculate flex basis and hypothetical main sizes
        CalculateFlexBasis(containerProps, context);

        // 4. Determine the main size of the flex container
        DetermineMainSize(containerProps);

        // 5. Resolve flexible lengths (the main algorithm for distributing space)
        ResolveFlexibleLengths(containerProps);

        // 6. Determine the cross size of each item
        DetermineCrossSizes(containerProps, context);

        // 7. Determine the cross size of the flex container
        DetermineContainerCrossSize(containerProps);

        // 8. Align all flex items along the main axis (justify-content)
        AlignItemsInMainAxis(containerProps);

        // 9. Align all flex items along the cross axis (align-items, align-self)
        AlignItemsInCrossAxis(containerProps);

        // 10. Apply final positions
        ApplyItemPositions(containerProps);

        // Save state for incremental updates
        SaveLayoutState();
    }

    /// <summary>
    /// Performs an incremental update of the layout.
    /// </summary>
    public override void Reflow(LayoutContext context)
    {
        // Find changed nodes
        var dirtyNodes = _participants.Where(p => p.IsDirty).ToList();

        if (!dirtyNodes.Any())
            return; // Nothing to do

        // For flex layouts, even small changes can affect the entire layout
        // Best practice is usually to perform a full layout when anything changes
        Layout(context);
    }

    /// <summary>
    /// Collects the flex items that participate in this formatting context.
    /// </summary>
    private void CollectFlexItems(LayoutNode container)
    {
        // Add the container as a participant
        _participants.Add(container);

        // Process direct children as flex items
        foreach (var child in container.Children)
        {
            // Skip non-renderable children
            if (child.DomNode is NonRenderableNode)
                continue;

            // Add as participant
            _participants.Add(child);

            // Create a flex item for this child
            var item = new FlexItem
            {
                Node = child
            };
            _flexItems.Add(item);
        }
    }

    /// <summary>
    /// Creates anonymous flex items for text nodes if needed.
    /// </summary>
    private void CreateAnonymousFlexItems()
    {
        // In a real browser, text nodes would be wrapped in anonymous flex items
        // This is a simplified implementation - we already count text nodes as flex items
    }

    /// <summary>
    /// Gets flex container properties from the establishing node.
    /// </summary>
    private FlexContainerProperties GetFlexContainerProperties(LayoutNode container)
    {
        var element = container.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
        {
            return new FlexContainerProperties(); // Default values
        }

        var props = new FlexContainerProperties();

        // Get flex properties
        string flexDirection = element.ComputedStyle.GetPropertyValue("flex-direction") ?? "row";
        string flexWrap = element.ComputedStyle.GetPropertyValue("flex-wrap") ?? "nowrap";
        string justifyContent = element.ComputedStyle.GetPropertyValue("justify-content") ?? "flex-start";
        string alignItems = element.ComputedStyle.GetPropertyValue("align-items") ?? "stretch";
        string alignContent = element.ComputedStyle.GetPropertyValue("align-content") ?? "stretch";

        // Parse direction
        props.Direction = flexDirection switch
        {
            "row" => FlexDirection.Row,
            "row-reverse" => FlexDirection.RowReverse,
            "column" => FlexDirection.Column,
            "column-reverse" => FlexDirection.ColumnReverse,
            _ => FlexDirection.Row
        };

        // Parse wrap
        props.Wrap = flexWrap switch
        {
            "nowrap" => FlexWrap.NoWrap,
            "wrap" => FlexWrap.Wrap,
            "wrap-reverse" => FlexWrap.WrapReverse,
            _ => FlexWrap.NoWrap
        };

        // Parse justify-content
        props.JustifyContent = justifyContent switch
        {
            "flex-start" => FlexJustifyContent.FlexStart,
            "flex-end" => FlexJustifyContent.FlexEnd,
            "center" => FlexJustifyContent.Center,
            "space-between" => FlexJustifyContent.SpaceBetween,
            "space-around" => FlexJustifyContent.SpaceAround,
            "space-evenly" => FlexJustifyContent.SpaceEvenly,
            _ => FlexJustifyContent.FlexStart
        };

        // Parse align-items
        props.AlignItems = alignItems switch
        {
            "flex-start" => FlexAlignItems.FlexStart,
            "flex-end" => FlexAlignItems.FlexEnd,
            "center" => FlexAlignItems.Center,
            "baseline" => FlexAlignItems.Baseline,
            "stretch" => FlexAlignItems.Stretch,
            _ => FlexAlignItems.Stretch
        };

        // Parse align-content
        props.AlignContent = alignContent switch
        {
            "flex-start" => FlexAlignContent.FlexStart,
            "flex-end" => FlexAlignContent.FlexEnd,
            "center" => FlexAlignContent.Center,
            "space-between" => FlexAlignContent.SpaceBetween,
            "space-around" => FlexAlignContent.SpaceAround,
            "space-evenly" => FlexAlignContent.SpaceEvenly,
            "stretch" => FlexAlignContent.Stretch,
            _ => FlexAlignContent.Stretch
        };

        return props;
    }

    /// <summary>
    /// Prepares flex items by extracting their initial properties.
    /// </summary>
    private void PrepareFlexItems()
    {
        foreach (var item in _flexItems)
        {
            var element = item.Node.DomNode as ElementNode;
            if (element?.ComputedStyle == null)
                continue;

            // Get flex item properties
            string flexGrow = element.ComputedStyle.GetPropertyValue("flex-grow") ?? "0";
            string flexShrink = element.ComputedStyle.GetPropertyValue("flex-shrink") ?? "1";
            string flexBasis = element.ComputedStyle.GetPropertyValue("flex-basis") ?? "auto";
            string alignSelf = element.ComputedStyle.GetPropertyValue("align-self") ?? "auto";

            // Parse flex-grow
            if (float.TryParse(flexGrow, out float grow))
            {
                item.FlexGrow = Math.Max(0, grow);
            }

            // Parse flex-shrink
            if (float.TryParse(flexShrink, out float shrink))
            {
                item.FlexShrink = Math.Max(0, shrink);
            }

            // Parse flex-basis
            if (flexBasis == "auto")
            {
                item.FlexBasis = FlexBasis.Auto;
                item.FlexBasisValue = 0; // Will be calculated from item's dimensions
            }
            else if (flexBasis.EndsWith("%") && float.TryParse(flexBasis.TrimEnd('%'), out float percentValue))
            {
                item.FlexBasis = FlexBasis.Percentage;
                item.FlexBasisValue = percentValue / 100f;
            }
            else if (flexBasis.EndsWith("px") && float.TryParse(flexBasis.TrimEnd('p', 'x'), out float pxValue))
            {
                item.FlexBasis = FlexBasis.Length;
                item.FlexBasisValue = pxValue;
            }
            else
            {
                item.FlexBasis = FlexBasis.Auto;
                item.FlexBasisValue = 0;
            }

            // Parse align-self
            item.AlignSelf = alignSelf switch
            {
                "auto" => FlexAlignSelf.Auto,
                "flex-start" => FlexAlignSelf.FlexStart,
                "flex-end" => FlexAlignSelf.FlexEnd,
                "center" => FlexAlignSelf.Center,
                "baseline" => FlexAlignSelf.Baseline,
                "stretch" => FlexAlignSelf.Stretch,
                _ => FlexAlignSelf.Auto
            };
        }
    }

    /// <summary>
    /// Determines the available main and cross sizes for the flex container.
    /// </summary>
    private void DetermineContainerSizes(FlexContainerProperties props, LayoutContext context)
    {
        var containerNode = EstablishingNode;

        // Get container dimensions from the box model
        float containerWidth = containerNode.Box.Width;
        float containerHeight = containerNode.Box.Height;

        // Check for auto width/height
        bool isWidthAuto = containerWidth <= 0;
        bool isHeightAuto = float.IsNaN(containerHeight) || containerHeight <= 0;

        // Determine available main and cross sizes based on direction
        if (props.IsHorizontal)
        {
            props.AvailableMainSize = isWidthAuto ? context.ViewportWidth : containerWidth;
            props.AvailableCrossSize = isHeightAuto ? 0 : containerHeight; // 0 means auto
        }
        else
        {
            props.AvailableMainSize = isHeightAuto ? 0 : containerHeight; // 0 means auto
            props.AvailableCrossSize = isWidthAuto ? context.ViewportWidth : containerWidth;
        }
    }

    /// <summary>
    /// Calculates the flex basis for each flex item.
    /// </summary>
    private void CalculateFlexBasis(FlexContainerProperties props, LayoutContext context)
    {
        foreach (var item in _flexItems)
        {
            // Measure the item first
            MeasureFlexItem(item, props, context);

            // Determine the flex basis
            if (item.FlexBasis == FlexBasis.Auto)
            {
                // Use the item's main size
                item.HypotheticalMainSize = props.IsHorizontal ? item.Node.Box.Width : item.Node.Box.Height;
            }
            else if (item.FlexBasis == FlexBasis.Length)
            {
                // Use the specified length
                item.HypotheticalMainSize = item.FlexBasisValue;
            }
            else if (item.FlexBasis == FlexBasis.Percentage)
            {
                // Use percentage of the container's main size
                item.HypotheticalMainSize = props.AvailableMainSize * item.FlexBasisValue;
            }

            // Ensure the main size respects min/max constraints
            ApplyMainSizeConstraints(item, props);
        }
    }

    /// <summary>
    /// Measures a flex item to determine its intrinsic dimensions.
    /// </summary>
    private void MeasureFlexItem(FlexItem item, FlexContainerProperties props, LayoutContext context)
    {
        var node = item.Node;
        var element = node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        // Create a child context with appropriate constraints
        var childContext = props.IsHorizontal ?
            context :
            context.CreateChildContext(EstablishingNode);

        // Measure the item
        float availableWidth = props.IsHorizontal ? props.AvailableMainSize : props.AvailableCrossSize;
        var calculator = new BoxModelCalculator(element.ComputedStyle, availableWidth);
        var boxValues = calculator.GetBoxValues();

        // Update the layout box
        node.Box.UpdateFromBoxValues(boxValues);

        // If this creates its own formatting context, lay it out
        if (node.FormattingContext != null)
        {
            var formattingContext = childContext.CreateChildContext(node);
            node.FormattingContext.Layout(formattingContext);
        }
    }

    /// <summary>
    /// Applies min and max constraints to an item's main size.
    /// </summary>
    private void ApplyMainSizeConstraints(FlexItem item, FlexContainerProperties props)
    {
        var element = item.Node.DomNode as ElementNode;
        if (element?.ComputedStyle == null)
            return;

        // Get min/max constraints
        string minProp = props.IsHorizontal ? "min-width" : "min-height";
        string maxProp = props.IsHorizontal ? "max-width" : "max-height";

        float minSize = 0;
        float maxSize = float.PositiveInfinity;

        string minValue = element.ComputedStyle.GetPropertyValue(minProp) ?? "0";
        if (minValue.EndsWith("px") && float.TryParse(minValue.TrimEnd('p', 'x'), out float minPx))
        {
            minSize = minPx;
        }

        string maxValue = element.ComputedStyle.GetPropertyValue(maxProp) ?? "none";
        if (maxValue.EndsWith("px") && float.TryParse(maxValue.TrimEnd('p', 'x'), out float maxPx))
        {
            maxSize = maxPx;
        }

        // Apply constraints
        item.HypotheticalMainSize = Math.Clamp(item.HypotheticalMainSize, minSize, maxSize);
    }

    /// <summary>
    /// Determines the main size of the flex container.
    /// </summary>
    private void DetermineMainSize(FlexContainerProperties props)
    {
        if (props.AvailableMainSize > 0)
        {
            // Use the specified main size
            props.MainSize = props.AvailableMainSize;
        }
        else
        {
            // Auto main size - sum up item sizes
            props.MainSize = _flexItems.Sum(item => item.HypotheticalMainSize);
        }
    }

    /// <summary>
    /// Resolves flexible lengths by distributing space between flex items.
    /// This is the core algorithm of flexbox layout.
    /// </summary>
    private void ResolveFlexibleLengths(FlexContainerProperties props)
    {
        // Calculate the total size of all items
        float totalItemsSize = _flexItems.Sum(item => item.HypotheticalMainSize);

        // Calculate the free space
        float freeSpace = props.MainSize - totalItemsSize;

        // Determine if we're growing or shrinking
        if (freeSpace > 0 && _flexItems.Any(item => item.FlexGrow > 0))
        {
            // Growing items
            GrowFlexItems(props, freeSpace);
        }
        else if (freeSpace < 0 && _flexItems.Any(item => item.FlexShrink > 0))
        {
            // Shrinking items
            ShrinkFlexItems(props, -freeSpace);
        }

        // Set final main sizes
        foreach (var item in _flexItems)
        {
            item.MainSize = item.HypotheticalMainSize;
        }
    }

    /// <summary>
    /// Grows flex items to fill available space.
    /// </summary>
    private void GrowFlexItems(FlexContainerProperties props, float freeSpace)
    {
        // Calculate total flex grow factor
        float totalGrowFactor = _flexItems.Sum(item => item.FlexGrow);
        if (totalGrowFactor <= 0)
            return;

        // Distribute free space proportionally to flex-grow
        foreach (var item in _flexItems)
        {
            if (item.FlexGrow > 0)
            {
                float growAmount = freeSpace * (item.FlexGrow / totalGrowFactor);
                item.HypotheticalMainSize += growAmount;
            }
        }
    }

    /// <summary>
    /// Shrinks flex items to fit within available space.
    /// </summary>
    private void ShrinkFlexItems(FlexContainerProperties props, float overflowSpace)
    {
        // Calculate weighted total flex shrink factor
        float totalShrinkFactor = 0;
        foreach (var item in _flexItems)
        {
            totalShrinkFactor += item.FlexShrink * item.HypotheticalMainSize;
        }

        if (totalShrinkFactor <= 0)
            return;

        // Distribute overflow space proportionally to flex-shrink and item size
        foreach (var item in _flexItems)
        {
            if (item.FlexShrink > 0)
            {
                float shrinkFactor = item.FlexShrink * item.HypotheticalMainSize / totalShrinkFactor;
                float shrinkAmount = overflowSpace * shrinkFactor;

                item.HypotheticalMainSize = Math.Max(0, item.HypotheticalMainSize - shrinkAmount);
            }
        }
    }

    /// <summary>
    /// Determines the cross size of each flex item.
    /// </summary>
    private void DetermineCrossSizes(FlexContainerProperties props, LayoutContext context)
    {
        foreach (var item in _flexItems)
        {
            // Determine the cross size based on align-items and align-self
            FlexAlignItems alignItems = props.AlignItems;

            // align-self overrides align-items
            if (item.AlignSelf != FlexAlignSelf.Auto)
            {
                alignItems = (FlexAlignItems)item.AlignSelf;
            }

            // Get the node's dimensions
            float itemWidth = item.Node.Box.Width;
            float itemHeight = item.Node.Box.Height;

            // Set the cross size
            if (props.IsHorizontal)
            {
                item.CrossSize = itemHeight;

                // For stretch, item fills the container's cross size
                if (alignItems == FlexAlignItems.Stretch && props.AvailableCrossSize > 0)
                {
                    item.CrossSize = props.AvailableCrossSize;
                }
            }
            else
            {
                item.CrossSize = itemWidth;

                // For stretch, item fills the container's cross size
                if (alignItems == FlexAlignItems.Stretch && props.AvailableCrossSize > 0)
                {
                    item.CrossSize = props.AvailableCrossSize;
                }
            }
        }
    }

    /// <summary>
    /// Determines the cross size of the flex container.
    /// </summary>
    private void DetermineContainerCrossSize(FlexContainerProperties props)
    {
        if (props.AvailableCrossSize > 0)
        {
            // Use the specified cross size
            props.CrossSize = props.AvailableCrossSize;
        }
        else
        {
            // Auto cross size - use the largest item cross size
            props.CrossSize = _flexItems.Any() ? _flexItems.Max(item => item.CrossSize) : 0;
        }

        // Update the establishing node's dimensions
        if (props.IsHorizontal)
        {
            EstablishingNode.Box.Width = props.MainSize;
            EstablishingNode.Box.Height = props.CrossSize;
        }
        else
        {
            EstablishingNode.Box.Width = props.CrossSize;
            EstablishingNode.Box.Height = props.MainSize;
        }
    }

    /// <summary>
    /// Aligns flex items along the main axis (justify-content).
    /// </summary>
    private void AlignItemsInMainAxis(FlexContainerProperties props)
    {
        // Calculate total items size
        float totalItemsMainSize = _flexItems.Sum(item => item.MainSize);

        // Calculate free space
        float freeSpace = props.MainSize - totalItemsMainSize;

        // Calculate position for each item
        float mainPos = 0;

        switch (props.JustifyContent)
        {
            case FlexJustifyContent.FlexStart:
                // Items are packed toward the start
                // mainPos starts at 0
                break;

            case FlexJustifyContent.FlexEnd:
                // Items are packed toward the end
                mainPos = freeSpace;
                break;

            case FlexJustifyContent.Center:
                // Items are centered
                mainPos = freeSpace / 2;
                break;

            case FlexJustifyContent.SpaceBetween:
                // Items are evenly distributed with equal space between them
                if (_flexItems.Count > 1)
                {
                    float spaceBetween = freeSpace / (_flexItems.Count - 1);
                    for (int i = 0; i < _flexItems.Count; i++)
                    {
                        _flexItems[i].MainPosition = mainPos;
                        mainPos += _flexItems[i].MainSize + spaceBetween;
                    }
                    return;
                }
                // Single item centered
                if (_flexItems.Count == 1)
                {
                    _flexItems[0].MainPosition = freeSpace / 2;
                }
                return;

            case FlexJustifyContent.SpaceAround:
                // Items are evenly distributed with equal space around them
                if (_flexItems.Count > 0)
                {
                    float spaceAround = freeSpace / _flexItems.Count;
                    float halfSpace = spaceAround / 2;
                    mainPos = halfSpace;
                    for (int i = 0; i < _flexItems.Count; i++)
                    {
                        _flexItems[i].MainPosition = mainPos;
                        mainPos += _flexItems[i].MainSize + spaceAround;
                    }
                    return;
                }
                break;

            case FlexJustifyContent.SpaceEvenly:
                // Items are evenly distributed with equal space between and around them
                if (_flexItems.Count > 0)
                {
                    float spaceEvenly = freeSpace / (_flexItems.Count + 1);
                    mainPos = spaceEvenly;
                    for (int i = 0; i < _flexItems.Count; i++)
                    {
                        _flexItems[i].MainPosition = mainPos;
                        mainPos += _flexItems[i].MainSize + spaceEvenly;
                    }
                    return;
                }
                break;
        }

        // Default case (FlexStart or no items for other cases)
        for (int i = 0; i < _flexItems.Count; i++)
        {
            _flexItems[i].MainPosition = mainPos;
            mainPos += _flexItems[i].MainSize;
        }
    }

    /// <summary>
    /// Aligns flex items along the cross axis (align-items, align-self).
    /// </summary>
    private void AlignItemsInCrossAxis(FlexContainerProperties props)
    {
        foreach (var item in _flexItems)
        {
            // Determine alignment
            FlexAlignItems alignItems = props.AlignItems;

            // align-self overrides align-items
            if (item.AlignSelf != FlexAlignSelf.Auto)
            {
                alignItems = (FlexAlignItems)item.AlignSelf;
            }

            // Calculate cross position
            switch (alignItems)
            {
                case FlexAlignItems.FlexStart:
                    // Item is placed at the start of the cross axis
                    item.CrossPosition = 0;
                    break;

                case FlexAlignItems.FlexEnd:
                    // Item is placed at the end of the cross axis
                    item.CrossPosition = props.CrossSize - item.CrossSize;
                    break;

                case FlexAlignItems.Center:
                    // Item is centered along the cross axis
                    item.CrossPosition = (props.CrossSize - item.CrossSize) / 2;
                    break;

                case FlexAlignItems.Baseline:
                    // Items are aligned by their baselines
                    // We'd need to calculate baselines for text
                    // For simplicity, we'll use flex-start instead
                    item.CrossPosition = 0;
                    break;

                case FlexAlignItems.Stretch:
                    // Item is stretched to fill the container's cross size
                    item.CrossPosition = 0;
                    item.CrossSize = props.CrossSize;
                    break;
            }
        }
    }

    /// <summary>
    /// Applies calculated positions to flex items.
    /// </summary>
    private void ApplyItemPositions(FlexContainerProperties props)
    {
        foreach (var item in _flexItems)
        {
            var node = item.Node;

            // Calculate actual dimensions based on main size and cross size
            if (props.IsHorizontal)
            {
                node.Box.Width = item.MainSize;
                node.Box.Height = item.CrossSize;

                // Set position
                node.Box.X = EstablishingNode.Box.X + EstablishingNode.Box.BorderLeft + EstablishingNode.Box.PaddingLeft + item.MainPosition;
                node.Box.Y = EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop + item.CrossPosition;

                // Adjust for direction
                if (props.Direction == FlexDirection.RowReverse)
                {
                    node.Box.X = EstablishingNode.Box.X + EstablishingNode.Box.BorderLeft + EstablishingNode.Box.PaddingLeft +
                                (props.MainSize - item.MainPosition - item.MainSize);
                }
            }
            else
            {
                node.Box.Width = item.CrossSize;
                node.Box.Height = item.MainSize;

                // Set position
                node.Box.X = EstablishingNode.Box.X + EstablishingNode.Box.BorderLeft + EstablishingNode.Box.PaddingLeft + item.CrossPosition;
                node.Box.Y = EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop + item.MainPosition;

                // Adjust for direction
                if (props.Direction == FlexDirection.ColumnReverse)
                {
                    node.Box.Y = EstablishingNode.Box.Y + EstablishingNode.Box.BorderTop + EstablishingNode.Box.PaddingTop +
                                (props.MainSize - item.MainPosition - item.MainSize);
                }
            }
        }
    }

    /// <summary>
    /// Saves the layout state for incremental updates.
    /// </summary>
    private void SaveLayoutState()
    {
        _lastState = new LayoutState
        {
            MainSize = EstablishingNode.Box.Width,
            CrossSize = EstablishingNode.Box.Height
        };

        // Save item metrics
        foreach (var item in _flexItems)
        {
            _lastState.ItemMetrics[item.Node] = new FlexItemMetrics
            {
                MainSize = item.MainSize,
                CrossSize = item.CrossSize,
                MainPosition = item.MainPosition,
                CrossPosition = item.CrossPosition
            };
        }
    }
}