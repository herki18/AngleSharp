namespace LayoutEngine.Core.LayoutNG.Public;

using System;
using AngleSharp.Dom;
using Infrastructure.EventAggregator.API.Aggregation;
using Internal;
using LayoutEngine.Core.Events;
using LayoutEngine.Core.Style.Public;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of the LayoutNG-based layout system.
/// </summary>
public class LayoutSystemNG : ILayoutSystemNG
{
    private readonly IStyleSystem _styleSystem;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<LayoutSystemNG> _logger;
    private readonly LayoutTreeBuilder _treeBuilder;
    private readonly LayoutObjectTree _layoutTree;
    private IDocument? _document;

    public LayoutSystemNG(
        IStyleSystem styleSystem,
        IEventAggregator eventAggregator,
        ILogger<LayoutSystemNG> logger,
        ILogger<LayoutTreeBuilder> treeBuilderLogger,
        ILogger<LayoutObjectTree> treeLogger)
    {
        _styleSystem = styleSystem ?? throw new ArgumentNullException(nameof(styleSystem));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _treeBuilder = new LayoutTreeBuilder(_styleSystem, treeBuilderLogger);
        _layoutTree = new LayoutObjectTree(_treeBuilder, treeLogger);
    }

    public LayoutObjectTree LayoutTree => _layoutTree;

    public void BuildLayoutTree(IDocument document)
    {
        _logger.LogInformation("Building layout tree for document");
        _document = document;

        // Ensure styles are computed first
        _styleSystem.ComputeDocumentStyles(document);

        // Build the layout tree
        _layoutTree.BuildFromDocument(document);

        _logger.LogInformation("Layout tree built successfully");
    }

    public void PerformLayout()
    {
        _logger.LogDebug("Performing layout");

        if (_layoutTree.RootLayoutObject == null)
        {
            _logger.LogWarning("No root layout object to perform layout on");
            return;
        }

        // Phase 1: Update styles for dirty objects
        UpdateStyles();

        // Phase 2: Calculate intrinsic sizes (bottom-up)
        CalculateIntrinsicSizes();

        // Phase 3: Calculate final sizes and positions (top-down)
        CalculateFinalLayout();

        // Phase 4: Clear layout flags
        ClearLayoutFlags();

        // Publish completion event
        _eventAggregator.Publish(new FragmentTreeUpdatedEvent(_layoutTree));

        _logger.LogDebug("Layout completed");
    }

    public ILayoutObject? GetLayoutObject(INode node)
    {
        return _layoutTree.GetLayoutObject(node);
    }

    public ILayoutObject? GetLayoutObject(IElement element)
    {
        return _layoutTree.GetLayoutObject(element);
    }

    public bool NeedsStyleRecalc()
    {
        if (_layoutTree.RootLayoutObject == null)
            return false;

        return CheckNeedsStyleRecalc(_layoutTree.RootLayoutObject);
    }

    public bool NeedsLayout()
    {
        if (_layoutTree.RootLayoutObject == null)
            return false;

        return CheckNeedsLayout(_layoutTree.RootLayoutObject);
    }

    public void HandleDOMMutation(IMutationRecord mutation)
    {
        _logger.LogDebug("Handling DOM mutation: {Type}", mutation.Type);

        switch (mutation.Type)
        {
            case "childList":
                HandleChildListMutation(mutation);
                break;

            case "attributes":
                HandleAttributeMutation(mutation);
                break;

            case "characterData":
                HandleCharacterDataMutation(mutation);
                break;
        }
    }

    public void RebuildLayoutTree()
    {
        _logger.LogInformation("Rebuilding entire layout tree");

        if (_document != null)
        {
            BuildLayoutTree(_document);
        }
    }

    public LayoutMetrics? GetLayoutMetrics(ILayoutObject layoutObject)
    {
        if (layoutObject is ILayoutBox box)
        {
            return new LayoutMetrics
            {
                ContentBox = box.ContentRect,
                PaddingBox = box.PaddingRect,
                BorderBox = box.BorderRect,
                MarginBox = box.MarginRect,
                IsVisible = true, // TODO: Calculate actual visibility
                ContainingBlock = FindContainingBlock(box)
            };
        }

        return null;
    }

    // Private helper methods

    private void UpdateStyles()
    {
        _layoutTree.TraversePreOrder(layoutObject =>
        {
            if (layoutObject.NeedsStyleRecalc() && layoutObject.Element != null)
            {
                var oldStyle = layoutObject.Style;
                var newStyle = _styleSystem.GetComputedStyle(layoutObject.Element);

                if (newStyle != null)
                {
                    layoutObject.UpdateStyle(oldStyle, newStyle);
                }

                layoutObject.ClearNeedsStyleRecalc();
            }
        });
    }

    private void CalculateIntrinsicSizes()
    {
        // Bottom-up traversal for intrinsic sizing
        _layoutTree.TraversePostOrder(layoutObject =>
        {
            if (layoutObject.NeedsLayout() && layoutObject is ILayoutBox box)
            {
                // Calculate intrinsic sizes based on content
                // This is where min-content and max-content widths would be calculated
                // For now, just a placeholder
                box.ComputeLogicalWidth();
            }
        });
    }

    private void CalculateFinalLayout()
    {
        // Top-down traversal for final layout
        var viewport = new LayoutViewport(800, 600); // Default viewport size

        _layoutTree.TraversePreOrder(layoutObject =>
        {
            if (layoutObject.NeedsLayout() && layoutObject is ILayoutBox box)
            {
                // Compute final dimensions
                box.ComputeLogicalWidth();
                box.ComputeLogicalHeight();

                // Update position
                box.UpdateLocation();

                // Layout algorithm would go here
                // For now, simple block layout
                if (box is LayoutBlock block && box is ILayoutContainer container)
                {
                    LayoutBlockChildren(block, container);
                }
            }
        });
    }

    private void LayoutBlockChildren(LayoutBlock block, ILayoutContainer container)
    {
        float currentY = block.ContentRect.Y;

        foreach (var child in container.Children)
        {
            if (child is ILayoutBox childBox)
            {
                // Simple vertical stacking
                childBox.LocationOffset = new Layout.Internal.Point(
                    block.ContentRect.X,
                    currentY
                );

                childBox.UpdateLocation();
                currentY += childBox.MarginRect.Height;
            }
        }
    }

    private void ClearLayoutFlags()
    {
        _layoutTree.TraversePreOrder(layoutObject =>
        {
            layoutObject.ClearNeedsLayout();
            layoutObject.ClearChildNeedsLayout();
            layoutObject.ClearNeedsPaintInvalidation();
        });
    }

    private bool CheckNeedsStyleRecalc(ILayoutObject layoutObject)
    {
        if (layoutObject.NeedsStyleRecalc() || layoutObject.ChildNeedsStyleRecalc())
            return true;

        if (layoutObject is ILayoutContainer container)
        {
            foreach (var child in container.Children)
            {
                if (CheckNeedsStyleRecalc(child))
                    return true;
            }
        }

        return false;
    }

    private bool CheckNeedsLayout(ILayoutObject layoutObject)
    {
        if (layoutObject.NeedsLayout() || layoutObject.ChildNeedsLayout())
            return true;

        if (layoutObject is ILayoutContainer container)
        {
            foreach (var child in container.Children)
            {
                if (CheckNeedsLayout(child))
                    return true;
            }
        }

        return false;
    }

    private void HandleChildListMutation(IMutationRecord mutation)
    {
        if (mutation.Target is INode target)
        {
            // Handle additions
            if (mutation.Added != null)
            {
                foreach (var added in mutation.Added)
                {
                    _layoutTree.HandleNodeInserted(added, target);
                }
            }

            // Handle removals
            if (mutation.Removed != null)
            {
                foreach (var removed in mutation.Removed)
                {
                    _layoutTree.HandleNodeRemoved(removed);
                }
            }
        }
    }

    private void HandleAttributeMutation(IMutationRecord mutation)
    {
        if (mutation.Target is IElement element && mutation.AttributeName != null)
        {
            _layoutTree.HandleAttributeChanged(element, mutation.AttributeName);
        }
    }

    private void HandleCharacterDataMutation(IMutationRecord mutation)
    {
        if (mutation.Target is IText textNode)
        {
            _layoutTree.HandleTextChanged(textNode);
        }
    }

    private ILayoutBox? FindContainingBlock(ILayoutBox box)
    {
        // Find the containing block for positioned elements
        var parent = box.Parent;

        while (parent != null)
        {
            if (parent is ILayoutBox parentBox)
            {
                if (box.IsPositioned)
                {
                    // For positioned elements, find the nearest positioned ancestor
                    if (parentBox.IsPositioned || parent == _layoutTree.RootLayoutObject)
                    {
                        return parentBox;
                    }
                }
                else
                {
                    // For non-positioned elements, the containing block is the parent
                    return parentBox;
                }
            }

            parent = parent.Parent;
        }

        return null;
    }

    private class LayoutViewport
    {
        public float Width { get; }
        public float Height { get; }

        public LayoutViewport(float width, float height)
        {
            Width = width;
            Height = height;
        }
    }
}