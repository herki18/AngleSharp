#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618, CS9264
#pragma warning disable CS8600, CS8602, CS8603, CS8625
namespace AngleSharp.LayoutEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Css.Dom;
using AngleSharp.Html.Dom;

/// <summary>
/// Main entry point for the AngleSharp Layout Engine. Provides a simple API to perform layout
/// on AngleSharp DOM trees. This class is the primary interface for users of the layout engine.
/// </summary>
public class AngleSharpLayoutProvider
{
    private BrowserLayoutEngine _layoutEngine;
    private RenderTreeBuilder _renderTreeBuilder;
    private float _viewportWidth = 800;
    private float _viewportHeight = 600;
    private bool _isInitialized = false;

    /// <summary>
    /// Event raised when layout is completed.
    /// </summary>
    public event EventHandler<LayoutCompletedEventArgs> LayoutCompleted;

    /// <summary>
    /// Creates a new instance of the layout provider.
    /// </summary>
    public AngleSharpLayoutProvider()
    {
        _renderTreeBuilder = new RenderTreeBuilder();
    }

    /// <summary>
    /// Gets or sets the viewport width for layout calculations.
    /// </summary>
    public float ViewportWidth
    {
        get => _viewportWidth;
        set
        {
            _viewportWidth = value;
            if (_isInitialized && _layoutEngine != null)
            {
                _layoutEngine.ViewportWidth = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the viewport height for layout calculations.
    /// </summary>
    public float ViewportHeight
    {
        get => _viewportHeight;
        set
        {
            _viewportHeight = value;
            if (_isInitialized && _layoutEngine != null)
            {
                _layoutEngine.ViewportHeight = value;
            }
        }
    }

    /// <summary>
    /// Performs layout on the specified document and returns the computed layout.
    /// </summary>
    /// <param name="document">The document to perform layout on.</param>
    /// <returns>A layout result containing the layout tree and other information.</returns>
    public LayoutResult PerformLayout(IDocument document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        // Build the render tree
        var renderTree = _renderTreeBuilder.BuildRenderTree(document);

        // Create or update the layout engine
        if (!_isInitialized || _layoutEngine == null)
        {
            _layoutEngine = new BrowserLayoutEngine();
            _layoutEngine.Initialize(renderTree.Root, _viewportWidth, _viewportHeight);
            _isInitialized = true;
        }
        else
        {
            // Handle updates to an existing document
            var changedNodes = renderTree.GetChangedNodes();
            if (changedNodes.Any())
            {
                _layoutEngine.ProcessDomMutation(
                    renderTree.GetAddedNodes(),
                    renderTree.GetRemovedNodes(),
                    changedNodes);
            }
            else
            {
                // Full layout if we can't determine what changed
                _layoutEngine.PerformFullLayout();
            }
        }

        // Create layout result
        var result = new LayoutResult(_layoutEngine.LayoutTree, _viewportWidth, _viewportHeight);

        // Raise event
        LayoutCompleted?.Invoke(this, new LayoutCompletedEventArgs(result));

        return result;
    }

    /// <summary>
    /// Updates the layout for the specified element and its descendants.
    /// </summary>
    /// <param name="element">The element to update.</param>
    /// <returns>A layout result containing the updated layout tree.</returns>
    public LayoutResult UpdateLayoutForElement(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (!_isInitialized || _layoutEngine == null)
        {
            // If not initialized, do full layout on the element's document
            return PerformLayout(element.OwnerDocument);
        }

        // Find the corresponding render node
        var renderNode = _renderTreeBuilder.FindRenderNode(element);
        if (renderNode == null)
        {
            // If node not found, refresh render tree and do full layout
            var renderTree = _renderTreeBuilder.BuildRenderTree(element.OwnerDocument);
            _layoutEngine.ProcessDomMutation(new[] { renderTree.Root }, Array.Empty<IRenderNode>(), Array.Empty<IRenderNode>());
        }
        else
        {
            // Just update this element
            _layoutEngine.PerformIncrementalLayout(new[] { renderNode });
        }

        // Create layout result
        var result = new LayoutResult(_layoutEngine.LayoutTree, _viewportWidth, _viewportHeight);

        // Raise event
        LayoutCompleted?.Invoke(this, new LayoutCompletedEventArgs(result));

        return result;
    }

    /// <summary>
    /// Resizes the viewport and updates the layout accordingly.
    /// </summary>
    /// <param name="width">The new viewport width.</param>
    /// <param name="height">The new viewport height.</param>
    /// <returns>A layout result after the resize.</returns>
    public LayoutResult ResizeViewport(float width, float height)
    {
        _viewportWidth = width;
        _viewportHeight = height;

        if (_isInitialized && _layoutEngine != null)
        {
            _layoutEngine.ViewportWidth = width;
            _layoutEngine.ViewportHeight = height;
            _layoutEngine.InvalidateLayout();
            _layoutEngine.PerformFullLayout();
        }

        // Create layout result
        var result = new LayoutResult(_layoutEngine.LayoutTree, _viewportWidth, _viewportHeight);

        // Raise event
        LayoutCompleted?.Invoke(this, new LayoutCompletedEventArgs(result));

        return result;
    }

    /// <summary>
    /// Gets layout information for a specific element.
    /// </summary>
    /// <param name="element">The element to get layout information for.</param>
    /// <returns>A layout info object, or null if the element has no layout.</returns>
    public LayoutInfo GetLayoutForElement(IElement element)
    {
        if (element == null || !_isInitialized || _layoutEngine == null)
            return null;

        // Find the corresponding render node
        var renderNode = _renderTreeBuilder.FindRenderNode(element);
        if (renderNode == null)
            return null;

        // Find the layout node
        var layoutNode = _layoutEngine.LayoutTree.FindNodeForDomNode(renderNode);
        if (layoutNode == null || layoutNode.Box == null)
            return null;

        // Create layout info
        return new LayoutInfo
        {
            X = layoutNode.Box.X,
            Y = layoutNode.Box.Y,
            Width = layoutNode.Box.Width,
            Height = layoutNode.Box.Height,
            MarginTop = layoutNode.Box.MarginTop,
            MarginRight = layoutNode.Box.MarginRight,
            MarginBottom = layoutNode.Box.MarginBottom,
            MarginLeft = layoutNode.Box.MarginLeft,
            PaddingTop = layoutNode.Box.PaddingTop,
            PaddingRight = layoutNode.Box.PaddingRight,
            PaddingBottom = layoutNode.Box.PaddingBottom,
            PaddingLeft = layoutNode.Box.PaddingLeft,
            BorderTop = layoutNode.Box.BorderTop,
            BorderRight = layoutNode.Box.BorderRight,
            BorderBottom = layoutNode.Box.BorderBottom,
            BorderLeft = layoutNode.Box.BorderLeft
        };
    }

    /// <summary>
    /// Gets the style sheets from the document.
    /// </summary>
    private ICssStyleSheet[] GetStyleSheets(IDocument document)
    {
        var styleSheets = new List<ICssStyleSheet>();

        // Get style sheets from document
        foreach (var styleSheet in document.StyleSheets.OfType<ICssStyleSheet>())
        {
            styleSheets.Add(styleSheet);
        }

        // Get inline styles from style elements
        foreach (var styleElement in document.QuerySelectorAll("style").OfType<IHtmlStyleElement>())
        {
            if (styleElement.Sheet is ICssStyleSheet sheet)
            {
                styleSheets.Add(sheet);
            }
        }

        return styleSheets.ToArray();
    }
}

/// <summary>
/// Contains the result of a layout operation.
/// </summary>
public class LayoutResult
{
    /// <summary>
    /// Creates a new layout result.
    /// </summary>
    /// <param name="layoutTree">The layout tree produced by layout.</param>
    /// <param name="viewportWidth">The viewport width used for layout.</param>
    /// <param name="viewportHeight">The viewport height used for layout.</param>
    public LayoutResult(LayoutTree layoutTree, float viewportWidth, float viewportHeight)
    {
        LayoutTree = layoutTree;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    /// <summary>
    /// Gets the layout tree produced by layout.
    /// </summary>
    public LayoutTree LayoutTree { get; }

    /// <summary>
    /// Gets the viewport width used for layout.
    /// </summary>
    public float ViewportWidth { get; }

    /// <summary>
    /// Gets the viewport height used for layout.
    /// </summary>
    public float ViewportHeight { get; }

    /// <summary>
    /// Gets a layout node by its corresponding DOM node.
    /// </summary>
    /// <param name="node">The DOM node to find the layout node for.</param>
    /// <returns>The layout node, or null if not found.</returns>
    public LayoutNode GetLayoutNode(IRenderNode node)
    {
        return LayoutTree.FindNodeForDomNode(node);
    }
}

/// <summary>
/// Contains layout information for an element.
/// </summary>
public class LayoutInfo
{
    /// <summary>
    /// Gets the X coordinate of the content box.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets the Y coordinate of the content box.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets the width of the content box.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Gets the height of the content box.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Gets the top margin.
    /// </summary>
    public float MarginTop { get; set; }

    /// <summary>
    /// Gets the right margin.
    /// </summary>
    public float MarginRight { get; set; }

    /// <summary>
    /// Gets the bottom margin.
    /// </summary>
    public float MarginBottom { get; set; }

    /// <summary>
    /// Gets the left margin.
    /// </summary>
    public float MarginLeft { get; set; }

    /// <summary>
    /// Gets the top padding.
    /// </summary>
    public float PaddingTop { get; set; }

    /// <summary>
    /// Gets the right padding.
    /// </summary>
    public float PaddingRight { get; set; }

    /// <summary>
    /// Gets the bottom padding.
    /// </summary>
    public float PaddingBottom { get; set; }

    /// <summary>
    /// Gets the left padding.
    /// </summary>
    public float PaddingLeft { get; set; }

    /// <summary>
    /// Gets the top border.
    /// </summary>
    public float BorderTop { get; set; }

    /// <summary>
    /// Gets the right border.
    /// </summary>
    public float BorderRight { get; set; }

    /// <summary>
    /// Gets the bottom border.
    /// </summary>
    public float BorderBottom { get; set; }

    /// <summary>
    /// Gets the left border.
    /// </summary>
    public float BorderLeft { get; set; }

    /// <summary>
    /// Gets the left edge of the border box.
    /// </summary>
    public float BorderBoxLeft => X - PaddingLeft - BorderLeft;

    /// <summary>
    /// Gets the top edge of the border box.
    /// </summary>
    public float BorderBoxTop => Y - PaddingTop - BorderTop;

    /// <summary>
    /// Gets the width of the border box.
    /// </summary>
    public float BorderBoxWidth => Width + PaddingLeft + PaddingRight + BorderLeft + BorderRight;

    /// <summary>
    /// Gets the height of the border box.
    /// </summary>
    public float BorderBoxHeight => Height + PaddingTop + PaddingBottom + BorderTop + BorderBottom;

    /// <summary>
    /// Gets the left edge of the margin box.
    /// </summary>
    public float MarginBoxLeft => BorderBoxLeft - MarginLeft;

    /// <summary>
    /// Gets the top edge of the margin box.
    /// </summary>
    public float MarginBoxTop => BorderBoxTop - MarginTop;

    /// <summary>
    /// Gets the width of the margin box.
    /// </summary>
    public float MarginBoxWidth => BorderBoxWidth + MarginLeft + MarginRight;

    /// <summary>
    /// Gets the height of the margin box.
    /// </summary>
    public float MarginBoxHeight => BorderBoxHeight + MarginTop + MarginBottom;
}

/// <summary>
/// Event arguments for when layout is completed.
/// </summary>
public class LayoutCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Creates new layout completed event arguments.
    /// </summary>
    /// <param name="result">The layout result.</param>
    public LayoutCompletedEventArgs(LayoutResult result)
    {
        Result = result;
    }

    /// <summary>
    /// Gets the layout result.
    /// </summary>
    public LayoutResult Result { get; }
}