#pragma warning disable CS8603 // Possible null reference return.
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Css.Dom;

/// <summary>
/// The core layout engine class that orchestrates the layout process,
/// leveraging AngleSharp's DOM and style computation capabilities.
/// </summary>
public class BrowserLayoutEngine
{
    private readonly StyleEngine _styleEngine;
    private readonly LayoutTree _layoutTree;
    private readonly FormattingContextFactory _formattingContextFactory;
    private readonly IMarginManager _marginManager;
    private readonly LayoutObserver _layoutObserver;
    private readonly FloatManager _floatManager;
    private readonly PositionResolver _positionResolver;

    private bool _initialized = false;
    private float _viewportWidth = 800;
    private float _viewportHeight = 600;

    /// <summary>
    /// Gets or sets the viewport width.
    /// </summary>
    public float ViewportWidth
    {
        get => _viewportWidth;
        set
        {
            if (_viewportWidth != value)
            {
                _viewportWidth = value;
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets the viewport height.
    /// </summary>
    public float ViewportHeight
    {
        get => _viewportHeight;
        set
        {
            if (_viewportHeight != value)
            {
                _viewportHeight = value;
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Gets the layout tree.
    /// </summary>
    public LayoutTree LayoutTree => _layoutTree;

    /// <summary>
    /// Creates a new layout engine instance.
    /// </summary>
    public BrowserLayoutEngine()
    {
        _marginManager = new MarginManager();
        _layoutTree = new LayoutTree();
        _styleEngine = new StyleEngine(); // No need to pass stylesheets - AngleSharp handles them
        _formattingContextFactory = new FormattingContextFactory(_marginManager);
        _layoutObserver = new LayoutObserver();
        _floatManager = new FloatManager();
        _positionResolver = new PositionResolver();
    }

    /// <summary>
    /// Initializes the layout engine with the specified DOM root node and viewport dimensions.
    /// </summary>
    /// <param name="domRoot">The root node of the DOM tree.</param>
    /// <param name="viewportWidth">The width of the viewport.</param>
    /// <param name="viewportHeight">The height of the viewport.</param>
    public void Initialize(IRenderNode domRoot, float viewportWidth, float viewportHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        // Build the layout tree from the DOM
        _layoutTree.BuildFromDOM(domRoot);

        // Mark initialized
        _initialized = true;

        // Perform a full layout
        PerformFullLayout();
    }

    /// <summary>
    /// Performs a complete layout from scratch. This is used for initial layout or when significant changes occur.
    /// </summary>
    public void PerformFullLayout()
    {
        if (!_initialized)
            throw new InvalidOperationException("Layout engine must be initialized before performing layout.");

        // Clear any float tracking
        _floatManager.ClearFloats();

        // Compute styles for all nodes - use AngleSharp's computed styles
        _styleEngine.ComputeStyles(_layoutTree);

        // Create formatting contexts
        _formattingContextFactory.CreateFormattingContexts(_layoutTree);

        // Create layout context
        var layoutContext = new LayoutContext(_viewportWidth, _viewportHeight)
        {
            MarginManager = _marginManager
        };

        // Perform layout on the root formatting context
        if (_layoutTree.Root.FormattingContext != null)
        {
            _layoutTree.Root.FormattingContext.Layout(layoutContext);
        }

        // Process positioned elements
        _positionResolver.ResolvePositions(_layoutTree, layoutContext);

        // Clear dirty state
        _layoutObserver.ClearDirty();
    }

    /// <summary>
    /// Performs an incremental layout update for the specified changed DOM nodes.
    /// This is more efficient than a full layout when only a few nodes change.
    /// </summary>
    /// <param name="changedNodes">The DOM nodes that have changed.</param>
    public void PerformIncrementalLayout(IEnumerable<IRenderNode> changedNodes)
    {
        if (!_initialized)
            throw new InvalidOperationException("Layout engine must be initialized before performing layout.");

        // Find layout nodes corresponding to changed DOM nodes
        var dirtyLayoutNodes = _layoutTree.FindNodesForDomNodes(changedNodes);

        // Update styles for changed nodes - let AngleSharp recompute styles
        // Force recomputation of the DOM node's computed style if needed
        foreach (var node in changedNodes)
        {
            if (node is ElementNode element && element.Ref is IElement domElement && domElement.OwnerDocument?.DefaultView != null)
            {
                // Access computed style to encourage recomputation
                var style = domElement.OwnerDocument.DefaultView.GetComputedStyle(domElement, null);
            }
        }

        // Update our cached style values
        _styleEngine.UpdateStyles(dirtyLayoutNodes);

        // Mark nodes and their ancestors as dirty for layout
        _layoutObserver.MarkDirtyNodes(dirtyLayoutNodes);

        // Determine which formatting contexts need updating
        var dirtyContexts = _formattingContextFactory.GetAffectedContexts(_layoutObserver.GetDirtyNodes());

        // Create layout context
        var layoutContext = new LayoutContext(_viewportWidth, _viewportHeight)
        {
            MarginManager = _marginManager,
            IsIncrementalLayout = true
        };

        // Update float positions for dirty nodes that are floats
        foreach (var node in dirtyLayoutNodes.Where(n => n.Float != FloatType.None))
        {
            _floatManager.UpdateFloat(node);
        }

        // Reflow only affected contexts
        foreach (var context in dirtyContexts)
        {
            context.Reflow(layoutContext);
        }

        // Update positioned elements that are dirty
        var dirtyPositionedNodes = _layoutObserver.GetDirtyNodes()
            .Where(n => n.Position != PositionType.Static);

        _positionResolver.ResolvePositionsForNodes(dirtyPositionedNodes, layoutContext);

        // Clear dirty state
        _layoutObserver.ClearDirty();
    }

    /// <summary>
    /// Processes a DOM mutation (changes to the DOM structure).
    /// </summary>
    /// <param name="addedNodes">Nodes that were added to the DOM.</param>
    /// <param name="removedNodes">Nodes that were removed from the DOM.</param>
    /// <param name="changedNodes">Nodes whose attributes or content changed.</param>
    public void ProcessDomMutation(
        IEnumerable<IRenderNode> addedNodes,
        IEnumerable<IRenderNode> removedNodes,
        IEnumerable<IRenderNode> changedNodes)
    {
        bool requiresFullLayout = false;

        // Handle removed nodes
        if (removedNodes.Any())
        {
            // For each removed node, check if its IElement has been disconnected from the DOM
            // AngleSharp will handle removing it from style calculations automatically

            // Find parent nodes that had children removed
            var parentNodes = removedNodes
                .Where(n => n.Parent != null)
                .Select(n => n.Parent)
                .Distinct();

            // Mark parent nodes as dirty
            foreach (var parentNode in parentNodes)
            {
                var layoutNode = _layoutTree.FindNodeForDomNode(parentNode);
                if (layoutNode != null)
                {
                    _layoutObserver.MarkSubtreeDirty(layoutNode);
                }
            }

            // For removals, we typically need a full layout since they affect flow
            requiresFullLayout = true;
        }

        // Handle added nodes
        if (addedNodes.Any())
        {
            // For added nodes, AngleSharp will automatically include them in style calculations
            // But we need to update our layout tree
            requiresFullLayout = true;
        }

        // Handle changed nodes
        if (changedNodes.Any() && !requiresFullLayout)
        {
            // For each changed node, see if AngleSharp needs to recompute styles
            foreach (var node in changedNodes)
            {
                if (node is ElementNode element && element.Ref is IElement domElement && domElement.OwnerDocument?.DefaultView != null)
                {
                    // Access computed style to ensure it's recalculated
                    var style = domElement.OwnerDocument.DefaultView.GetComputedStyle(domElement, null);
                }
            }

            // Perform incremental layout for changed nodes
            PerformIncrementalLayout(changedNodes);
        }

        // If we need a full layout, perform it
        if (requiresFullLayout)
        {
            // Rebuild the layout tree if needed
            if (addedNodes.Any() || removedNodes.Any())
            {
                var domRoot = FindDomRoot();
                if (domRoot != null)
                {
                    _layoutTree.BuildFromDOM(domRoot);
                }
            }

            PerformFullLayout();
        }
    }

    /// <summary>
    /// Finds the DOM root node from the current layout tree.
    /// </summary>
    private IRenderNode FindDomRoot()
    {
        return _layoutTree.Root?.DomNode;
    }

    /// <summary>
    /// Invalidates the entire layout, requiring a full reflow on the next layout pass.
    /// </summary>
    public void InvalidateLayout()
    {
        if (_initialized && _layoutTree.Root != null)
        {
            _layoutObserver.MarkSubtreeDirty(_layoutTree.Root);
        }
    }

    /// <summary>
    /// Invalidates layout for a specific node and its descendants.
    /// </summary>
    /// <param name="node">The node to invalidate.</param>
    public void InvalidateNode(LayoutNode node)
    {
        if (node != null)
        {
            _layoutObserver.MarkSubtreeDirty(node);
        }
    }
}