namespace AngleSharp.LayoutEngine.API;

using Core;
using DOM;

#pragma warning disable CS8604, CS8618, CS9264, CS8600, CS8602, CS8603, CS8625
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